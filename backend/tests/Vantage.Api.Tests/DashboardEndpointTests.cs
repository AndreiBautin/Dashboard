using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vantage.Application.Dashboard;
using Vantage.Domain.Metrics;

namespace Vantage.Api.Tests;

/// <summary>
/// Only one test method by design: InitializeAsync runs before every test
/// method in this class but the underlying SQLite connection (and its data)
/// is shared for the factory's whole lifetime via IClassFixture. Adding a
/// second [Fact] here would re-seed the same fixed rows on top of the first
/// test's data. If this class grows, give each test its own
/// SqliteWebApplicationFactory instance instead of sharing one.
/// </summary>
public class DashboardEndpointTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    // Mirrors the server's JSON configuration (camelCase names, string enums)
    // since HttpClientJsonExtensions doesn't inherit it automatically.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly SqliteWebApplicationFactory _factory;

    public DashboardEndpointTests(SqliteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.InitializeDatabaseAsync(dbContext =>
    {
        var fitness = new Category("Fitness", sortOrder: 0);
        dbContext.Categories.Add(fitness);
        dbContext.SaveChanges();

        var powerliftingTotal = new MetricDefinition(
            fitness.Id, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0);
        dbContext.MetricDefinitions.Add(powerliftingTotal);
        dbContext.SaveChanges();

        var lastMonth = new MonthlySnapshot(DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(-1));
        lastMonth.AddMetricSnapshot(powerliftingTotal.Id, 1000, DateTimeOffset.UtcNow.AddMonths(-1));
        dbContext.MonthlySnapshots.Add(lastMonth);

        var thisMonth = new MonthlySnapshot(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow);
        thisMonth.AddMetricSnapshot(powerliftingTotal.Id, 1050, DateTimeOffset.UtcNow);
        dbContext.MonthlySnapshots.Add(thisMonth);
    });

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDashboard_ReturnsASummaryReflectingTheSeededData()
    {
        var client = _factory.CreateClient();

        var summary = await client.GetFromJsonAsync<DashboardSummary>("/api/dashboard", JsonOptions);

        Assert.NotNull(summary);

        var fitness = summary!.Categories.Single(c => c.CategoryName == "Fitness");
        Assert.Equal(100, fitness.Score);
        Assert.Equal(CategoryStatus.Excelling, fitness.Status);

        // Social is always appended as its own pseudo-category, and with no
        // friends seeded its circle-size score is 0 -- the bottom of a real
        // scale, not "no data" (see SocialService: "even 0 friends is a real,
        // if low, point on the scale"). So it counts toward the average:
        // (Fitness 100 + Social 0) / 2 = 50.
        var social = summary.Categories.Single(c => c.CategoryName == "Social");
        Assert.Equal(0, social.Score);
        Assert.Equal(50, summary.OverallScore);

        // Fitness raises nothing -- its one metric improved -- but Social's
        // empty circle is exactly what pulled the overall score down, so it
        // is named rather than left silent.
        var alert = Assert.Single(summary.Alerts);
        Assert.Equal("Active Circle needs attention", alert.Message);
    }
}
