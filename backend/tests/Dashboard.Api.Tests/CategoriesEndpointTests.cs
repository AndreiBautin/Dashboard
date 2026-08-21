using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dashboard.Application.Metrics;
using Dashboard.Domain.Metrics;

namespace Dashboard.Api.Tests;

/// <summary>
/// Unlike DashboardEndpointTests, this class has several scenarios that need
/// different seed data, so rather than sharing one SqliteWebApplicationFactory
/// via IClassFixture (whose IAsyncLifetime-driven seeding would re-run, and
/// re-seed the same shared connection, once per test method -- exactly the
/// pitfall that class's own doc comment warns about), each test here creates
/// and disposes its own factory instance, giving genuine per-test isolation.
/// </summary>
public class CategoriesEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task GetAll_ReturnsTheSeededCategories()
    {
        await using var factory = new SqliteWebApplicationFactory();
        await factory.InitializeDatabaseAsync(dbContext =>
        {
            dbContext.Categories.AddRange(
                new Category("Fitness", sortOrder: 0),
                new Category("Finance", sortOrder: 1));
        });

        var categories = await factory.CreateClient().GetFromJsonAsync<Category[]>("/api/categories", JsonOptions);

        Assert.NotNull(categories);
        Assert.Contains(categories!, c => c.Name == "Fitness");
        Assert.Contains(categories!, c => c.Name == "Finance");
    }

    [Fact]
    public async Task GetDetail_ReturnsTheCategorysMetricsWithNoCurrentMonthValueYet()
    {
        await using var factory = new SqliteWebApplicationFactory();

        await factory.InitializeDatabaseAsync(dbContext =>
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
        });

        var detail = await factory.CreateClient().GetFromJsonAsync<CategoryDetail>("/api/categories/1", JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal("Fitness", detail!.CategoryName);
        var metric = Assert.Single(detail.Metrics);
        Assert.Equal("Powerlifting Total", metric.MetricName);
        Assert.Equal(1000, metric.LatestValue);
        Assert.Null(metric.CurrentMonthValue);
    }

    [Fact]
    public async Task GetDetail_WithUnknownCategory_ReturnsNotFound()
    {
        await using var factory = new SqliteWebApplicationFactory();
        await factory.InitializeDatabaseAsync(_ => { });

        var response = await factory.CreateClient().GetAsync("/api/categories/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecordEntries_PersistsAndIsReflectedInAFollowUpGet()
    {
        await using var factory = new SqliteWebApplicationFactory();

        await factory.InitializeDatabaseAsync(dbContext =>
        {
            var fitness = new Category("Fitness", sortOrder: 0);
            dbContext.Categories.Add(fitness);
            dbContext.SaveChanges();

            var powerliftingTotal = new MetricDefinition(
                fitness.Id, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0);
            dbContext.MetricDefinitions.Add(powerliftingTotal);
            dbContext.SaveChanges();
        });

        var client = factory.CreateClient();
        var thisMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var response = await client.PostAsJsonAsync(
            "/api/categories/1/entries",
            new { month = thisMonth, values = new Dictionary<int, decimal> { [1] = 1050 } },
            JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await client.GetFromJsonAsync<CategoryDetail>("/api/categories/1", JsonOptions);
        var metric = Assert.Single(detail!.Metrics);
        Assert.Equal(1050, metric.LatestValue);
        Assert.Equal(1050, metric.CurrentMonthValue);
    }

    [Fact]
    public async Task RecordEntries_WithAMetricFromAnotherCategory_ReturnsNotFound()
    {
        await using var factory = new SqliteWebApplicationFactory();

        await factory.InitializeDatabaseAsync(dbContext =>
        {
            var fitness = new Category("Fitness", sortOrder: 0);
            var finance = new Category("Finance", sortOrder: 1);
            dbContext.Categories.AddRange(fitness, finance);
            dbContext.SaveChanges();

            var powerliftingTotal = new MetricDefinition(
                fitness.Id, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0);
            var netWorth = new MetricDefinition(
                finance.Id, "Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0);
            dbContext.MetricDefinitions.AddRange(powerliftingTotal, netWorth);
            dbContext.SaveChanges();
        });

        var client = factory.CreateClient();
        var thisMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // Metric 2 (Net Worth) belongs to Finance (category 2), not Fitness (category 1).
        var response = await client.PostAsJsonAsync(
            "/api/categories/1/entries",
            new { month = thisMonth, values = new Dictionary<int, decimal> { [2] = 60_000 } },
            JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
