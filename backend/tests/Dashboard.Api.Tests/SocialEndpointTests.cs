using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dashboard.Application.Metrics;
using Dashboard.Application.Social;
using Dashboard.Domain.Metrics;
using Dashboard.Domain.Social;

namespace Dashboard.Api.Tests;

/// <summary>
/// Same per-test-own-factory pattern as CategoriesEndpointTests -- each
/// scenario needs its own seed data, so a shared IClassFixture would
/// re-seed on every test method against the same connection.
/// </summary>
public class SocialEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task GetSummary_ReturnsFriendsRankedByDaysSinceLastHangout()
    {
        await using var factory = new SqliteWebApplicationFactory();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await factory.InitializeDatabaseAsync(dbContext =>
        {
            dbContext.Friends.AddRange(
                new Friend("Robin", today.AddMonths(-1), DateTimeOffset.UtcNow),
                new Friend("Casey", today.AddMonths(-6), DateTimeOffset.UtcNow));
        });

        var summary = await factory.CreateClient().GetFromJsonAsync<SocialSummary>("/api/social", JsonOptions);

        Assert.NotNull(summary);
        Assert.Equal(2, summary!.Friends.Count);
        Assert.Equal("Casey", summary.Friends[0].Name);
        Assert.Equal("Robin", summary.Friends[1].Name);
    }

    [Fact]
    public async Task GetTrend_ReturnsActiveFriendCountPerMonth()
    {
        await using var factory = new SqliteWebApplicationFactory();

        await factory.InitializeDatabaseAsync(dbContext =>
        {
            var lastMonth = new MonthlySnapshot(
                DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(-1));
            lastMonth.SetSocialSnapshot(3);
            dbContext.MonthlySnapshots.Add(lastMonth);
        });

        var trend = await factory.CreateClient().GetFromJsonAsync<MetricTrendPoint[]>("/api/social/trend", JsonOptions);

        Assert.NotNull(trend);
        var point = Assert.Single(trend!);
        Assert.Equal(3, point.Value);
    }

    [Fact]
    public async Task AddFriend_PersistsAndIsReflectedInAFollowUpGet()
    {
        await using var factory = new SqliteWebApplicationFactory();
        await factory.InitializeDatabaseAsync(_ => { });

        var client = factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var response = await client.PostAsJsonAsync(
            "/api/social/friends",
            new { name = "Jordan", lastHangoutDate = today, notes = (string?)null },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var summary = await client.GetFromJsonAsync<SocialSummary>("/api/social", JsonOptions);
        var friend = Assert.Single(summary!.Friends);
        Assert.Equal("Jordan", friend.Name);
    }

    [Fact]
    public async Task LogHangout_UpdatesLastHangoutDate()
    {
        await using var factory = new SqliteWebApplicationFactory();
        var oldDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-6);

        await factory.InitializeDatabaseAsync(dbContext =>
        {
            dbContext.Friends.Add(new Friend("Robin", oldDate, DateTimeOffset.UtcNow));
        });

        var client = factory.CreateClient();
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var response = await client.PostAsJsonAsync(
            "/api/social/friends/1/hangouts", new { date = newDate }, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var summary = await client.GetFromJsonAsync<SocialSummary>("/api/social", JsonOptions);
        var friend = Assert.Single(summary!.Friends);
        Assert.Equal(0, friend.DaysSinceLastHangout);
    }

    [Fact]
    public async Task LogHangout_WithUnknownFriend_ReturnsNotFound()
    {
        await using var factory = new SqliteWebApplicationFactory();
        await factory.InitializeDatabaseAsync(_ => { });

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/social/friends/999/hangouts",
            new { date = DateOnly.FromDateTime(DateTime.UtcNow) },
            JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
