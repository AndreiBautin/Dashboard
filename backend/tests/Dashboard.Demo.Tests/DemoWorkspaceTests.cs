using Dashboard.Application.Dashboard;
using Dashboard.Domain.Metrics;

namespace Dashboard.Demo.Tests;

/// <summary>
/// Proves that the real application services run correctly against the
/// in-memory repositories — which is the single assumption the entire public
/// deployment rests on. If these pass, the deployed demo is executing the
/// same logic as the API, not an approximation of it.
///
/// These also double as the fixture's quality gate: a demo that renders an
/// empty dashboard demonstrates nothing, so the expectations below assert
/// that it is genuinely populated and genuinely varied.
/// </summary>
public class DemoWorkspaceTests
{
    // Must match the clock the application services read. They call
    // DateTime.UtcNow directly rather than taking an injected clock, so a
    // hardcoded date here disagrees with them either side of UTC midnight.
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static DemoWorkspace Workspace() => DemoWorkspace.Create(Today);

    [Fact]
    public async Task Dashboard_RendersAScoreForEveryCategoryIncludingSocial()
    {
        var summary = await Workspace().Dashboard.GetSummaryAsync();

        Assert.NotNull(summary.OverallScore);
        Assert.Contains(summary.Categories, c => c.CategoryName == "Fitness");
        Assert.Contains(summary.Categories, c => c.CategoryName == "Finance");
        Assert.Contains(summary.Categories, c => c.CategoryName == "Social");

        // Every category has enough data to score. A NoData card in the demo
        // would read as a broken app rather than as a designed empty state.
        Assert.All(summary.Categories, category => Assert.NotNull(category.Score));
    }

    [Fact]
    public async Task Dashboard_ShowsAMixOfMetricTrends_NotAWallOfGreen()
    {
        var summary = await Workspace().Dashboard.GetSummaryAsync();

        var statuses = summary.Categories
            .SelectMany(category => category.Metrics)
            .Select(metric => metric.Status)
            .ToHashSet();

        // The fixture is built to demonstrate the app's judgement, which means
        // it has to contain things going well and things going badly.
        Assert.Contains(MetricStatus.Improved, statuses);
        Assert.Contains(MetricStatus.Regressed, statuses);
        Assert.Contains(MetricStatus.Stagnant, statuses);
    }

    [Fact]
    public async Task Dashboard_RaisesAlertsNamingTheSpecificThingsThatAreDrifting()
    {
        var summary = await Workspace().Dashboard.GetSummaryAsync();

        Assert.NotEmpty(summary.Alerts);

        var alertedNames = summary.Alerts.Select(alert => alert.MetricName).ToList();
        Assert.Contains("Overhead Press", alertedNames);  // declined
        Assert.Contains("Emergency Fund", alertedNames);  // stalled
        Assert.Contains("Visited Mother", alertedNames);  // overdue key relationship
    }

    [Fact]
    public async Task CategoryDetail_ProducesRatingLabelsAcrossMoreThanOneTier()
    {
        var workspace = Workspace();
        var categories = await workspace.CategoryRepository.GetAllAsync();

        var tiers = new HashSet<MetricRatingTier>();
        foreach (var category in categories)
        {
            var detail = await workspace.CategoryDetail.GetDetailAsync(category.Id);
            foreach (var metric in detail.Metrics.Where(m => m.RatingTier is not null))
            {
                tiers.Add(metric.RatingTier!.Value);
            }
        }

        // A demo where every rated metric lands in the same tier never shows
        // the reviewer what the tier indicator is for.
        Assert.True(tiers.Count >= 2, $"Expected the fixture to span multiple rating tiers, saw: {string.Join(", ", tiers)}");
    }

    [Fact]
    public async Task Social_ShowsActiveOverdueAndInactiveFriendsTogether()
    {
        var social = await Workspace().Social.GetSummaryAsync();

        Assert.Contains(social.Friends, f => f.IsActive && !f.IsFlaggedOverdue);
        Assert.Contains(social.Friends, f => f.IsActive && f.IsFlaggedOverdue);
        Assert.Contains(social.Friends, f => !f.IsActive);

        // Both key relationships are present, one healthy and one overdue.
        Assert.Equal(2, social.KeyRelationships.Count);
        Assert.Contains(social.KeyRelationships, k => k.IsFlaggedOverdue);
        Assert.Contains(social.KeyRelationships, k => !k.IsFlaggedOverdue);
    }

    [Fact]
    public async Task Trends_HaveEnoughPointsToDrawALine()
    {
        var workspace = Workspace();
        var categories = await workspace.CategoryRepository.GetAllAsync();
        var detail = await workspace.CategoryDetail.GetDetailAsync(categories[0].Id);

        var trend = await workspace.MetricTrend.GetTrendAsync(detail.Metrics[0].MetricDefinitionId);

        Assert.True(trend.Count >= 3, $"Expected a drawable trend, got {trend.Count} point(s).");

        // Oldest first — the chart depends on it, and so does evaluation.
        Assert.Equal(trend.OrderBy(point => point.Month), trend);
    }

    [Fact]
    public async Task RecordingThisMonthsEntries_MovesTheScore()
    {
        var workspace = Workspace();
        var categories = await workspace.CategoryRepository.GetAllAsync();
        var fitness = categories.Single(c => c.Name == "Fitness");

        var before = await workspace.CategoryDetail.GetDetailAsync(fitness.Id);
        var overheadPress = before.Metrics.Single(m => m.MetricName == "Overhead Press");

        // Overhead Press is the one that declined in the fixture. Recording a
        // clear improvement for the current month should flip its status.
        await workspace.MetricEntry.RecordEntriesAsync(
            fitness.Id,
            new DateOnly(Today.Year, Today.Month, 1),
            new Dictionary<int, decimal> { [overheadPress.MetricDefinitionId] = 135m });

        var after = await workspace.CategoryDetail.GetDetailAsync(fitness.Id);
        var updated = after.Metrics.Single(m => m.MetricName == "Overhead Press");

        Assert.Equal(MetricStatus.Regressed, overheadPress.Status);
        Assert.Equal(MetricStatus.Improved, updated.Status);
        Assert.Equal(135m, updated.CurrentMonthValue);
    }

    [Fact]
    public async Task AddingAFriend_ChangesTheSocialSummary()
    {
        var workspace = Workspace();
        var before = await workspace.Social.GetSummaryAsync();

        await workspace.Friends.AddFriendAsync("Rowan", Today.AddDays(-2), notes: null);

        var after = await workspace.Social.GetSummaryAsync();

        Assert.Equal(before.ActiveFriendCount + 1, after.ActiveFriendCount);
        Assert.Contains(after.Friends, friend => friend.Name == "Rowan");
    }

    [Fact]
    public async Task LoggingAHangout_ClearsAnOverdueFlag()
    {
        var workspace = Workspace();
        var before = await workspace.Social.GetSummaryAsync();
        var overdue = before.Friends.First(friend => friend.IsFlaggedOverdue && friend.IsActive);

        await workspace.Friends.LogHangoutAsync(overdue.FriendId, Today);

        var after = await workspace.Social.GetSummaryAsync();
        var updated = after.Friends.Single(friend => friend.FriendId == overdue.FriendId);

        Assert.False(updated.IsFlaggedOverdue);
        Assert.Equal(0, updated.DaysSinceLastHangout);
    }

    [Fact]
    public async Task ChangingASetting_ChangesWhatTheDashboardReports()
    {
        var workspace = Workspace();
        var before = await workspace.Social.GetSummaryAsync();

        // Narrowing the active-circle window should shrink the active circle,
        // proving settings genuinely flow through the demo the way they do
        // through the API.
        await workspace.Settings.SetAsync("ActiveCircleThresholdMonths", "3");

        var after = await workspace.Social.GetSummaryAsync();

        Assert.True(after.ActiveFriendCount < before.ActiveFriendCount);
    }

    [Fact]
    public async Task Reset_RestoresTheFixtureAfterEdits()
    {
        var workspace = Workspace();
        await workspace.Friends.AddFriendAsync("Temporary", Today, notes: null);

        var edited = await workspace.Social.GetSummaryAsync();
        Assert.Contains(edited.Friends, friend => friend.Name == "Temporary");

        workspace.Reset(Today);

        var reset = await workspace.Social.GetSummaryAsync();
        Assert.DoesNotContain(reset.Friends, friend => friend.Name == "Temporary");
    }
}
