using Vantage.Application.Dashboard;
using Vantage.Application.Metrics;
using Vantage.Application.Social;
using Vantage.Application.Tests.Metrics.Fakes;
using Vantage.Application.Tests.Social.Fakes;
using Vantage.Domain.Metrics;
using Vantage.Domain.Metrics.Evaluators;
using Vantage.Domain.Settings;
using Vantage.Domain.Social;

namespace Vantage.Application.Tests.Dashboard;

public class DashboardServiceTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeMetricDefinitionRepository _metricDefinitions = new();
    private readonly FakeMetricSnapshotRepository _metricSnapshots = new();
    private readonly FakeAppSettingRepository _appSettings = new();
    private readonly FakeFriendRepository _friends = new();
    private readonly FakeKeyRelationshipRepository _keyRelationships = new();
    private readonly FakeMonthlySnapshotRepository _monthlySnapshots = new();

    private DashboardService CreateService()
    {
        var evaluatorFactory = new MetricEvaluatorFactory(
        [
            new IncreaseMetricEvaluator(),
            new DecreaseMetricEvaluator(),
            new StayAboveMetricEvaluator(),
            new StayBelowMetricEvaluator(),
            new StayWithinRangeMetricEvaluator(),
        ]);

        var evaluationService = new MetricEvaluationService(_metricDefinitions, _metricSnapshots, evaluatorFactory);
        var socialService = new SocialService(_friends, _keyRelationships, _monthlySnapshots, _appSettings);

        return new DashboardService(
            _categories, _metricDefinitions, _metricSnapshots, _appSettings, evaluationService, socialService);
    }

    private static MetricDefinition IncreasingMetric(int categoryId, string name) =>
        new(categoryId, name, "unit", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0);

    private void SeedTwoPointHistory(int metricId, decimal previous, decimal latest)
    {
        _metricSnapshots.SeedSnapshots(metricId,
            new MetricSnapshot(metricId, previous, DateTimeOffset.UtcNow.AddMonths(-1)),
            new MetricSnapshot(metricId, latest, DateTimeOffset.UtcNow));
    }

    /// <summary>An active, non-overdue friend by default -- tests that want an
    /// overdue or inactive one seed a specific hangout date instead.</summary>
    private void SeedMaintainedFriend(int id, string name) =>
        _friends.Seed(id, new Friend(name, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5), DateTimeOffset.UtcNow));

    [Fact]
    public async Task GetSummaryAsync_WithAllMetricsImproved_ScoresCategoryAsExcelling()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, IncreasingMetric(1, "Powerlifting Total"));
        _metricDefinitions.Seed(2, IncreasingMetric(1, "Arm Measurement"));
        SeedTwoPointHistory(1, previous: 1000, latest: 1050);
        SeedTwoPointHistory(2, previous: 15.0m, latest: 15.25m);

        var summary = await CreateService().GetSummaryAsync();

        // Social is always appended as its own pseudo-category. With no
        // friends seeded, its RatingScore (circle size) is 0 -- the bottom
        // of the scale, not "no data" -- so it still counts: overall is
        // (Fitness 100 + Social 0) / 2 = 50, not just Fitness's 100.
        var fitness = summary.Categories.Single(c => c.CategoryName == "Fitness");
        Assert.Equal(100, fitness.Score);
        Assert.Equal(CategoryStatus.Excelling, fitness.Status);
        Assert.Equal(50, summary.OverallScore);
        Assert.Equal(CategoryStatus.NeedsAttention, summary.OverallStatus);
        Assert.All(fitness.Metrics, metric => Assert.Equal(100, metric.Score));

        // Fitness itself raises no alerts (everything improved), but Social's
        // Active Circle is still sitting at 0 (no friends seeded), which is
        // exactly why the overall score dropped to 50 -- so it's called out.
        var alert = Assert.Single(summary.Alerts);
        Assert.Equal("Active Circle needs attention", alert.Message);
        Assert.Equal(0, alert.Score);
        Assert.Equal(CategoryStatus.Struggling, alert.Status);
    }

    [Fact]
    public async Task GetSummaryAsync_WithAMixOfImprovedAndRegressed_BlendsTheScoresAndRaisesAnAlert()
    {
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, IncreasingMetric(1, "Net Worth"));
        _metricDefinitions.Seed(2, IncreasingMetric(1, "Credit Score"));
        SeedTwoPointHistory(1, previous: 54500, latest: 53800); // regressed
        SeedTwoPointHistory(2, previous: 690, latest: 715);     // improved

        var summary = await CreateService().GetSummaryAsync();

        var finance = summary.Categories.Single(c => c.CategoryName == "Finance");
        Assert.Equal(65, finance.Score); // (30 + 100) / 2, rounded
        // 65 falls in the On Track band (51-75) against the configured
        // cutoffs, even though one of the two metrics went backwards -- which
        // is why the alert below matters: the category badge alone would not
        // tell you anything was wrong.
        Assert.Equal(CategoryStatus.OnTrack, finance.Status);

        // Net Worth's trend-based alert wins over a score-based one for the
        // same metric (it's the more informative wording). Social's Active
        // Circle (no friends seeded here) raises its own separate alert.
        Assert.Equal(2, summary.Alerts.Count);
        Assert.Contains(summary.Alerts, a => a.Message == "Net Worth has declined");
        Assert.Contains(summary.Alerts, a => a.Message == "Active Circle needs attention");
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoScoreableMetrics_ReturnsNoDataAndExcludesFromTheOverallScore()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _categories.Seed(2, new Category("Finance", sortOrder: 1));

        _metricDefinitions.Seed(1, IncreasingMetric(1, "Powerlifting Total"));
        // Only one snapshot recorded -- InsufficientData.
        _metricSnapshots.SeedSnapshots(1, new MetricSnapshot(1, 1000, DateTimeOffset.UtcNow));

        _metricDefinitions.Seed(2, IncreasingMetric(2, "Net Worth"));
        SeedTwoPointHistory(2, previous: 50_000, latest: 52_000);

        var summary = await CreateService().GetSummaryAsync();

        var fitness = summary.Categories.Single(c => c.CategoryName == "Fitness");
        Assert.Null(fitness.Score);
        Assert.Equal(CategoryStatus.NoData, fitness.Status);

        // Fitness (genuinely no data) is excluded rather than counted as 0,
        // but Social (0 friends seeded -> RatingScore 0) isn't "no data" --
        // it's a real, if low, score -- so overall is (Finance 100 + Social 0) / 2 = 50.
        Assert.Equal(50, summary.OverallScore);
    }

    [Fact]
    public async Task GetSummaryAsync_WithOnlyOneMonthRecorded_RatedMetricsStillContributeAScore()
    {
        // Only July exists so far -- no metric has the 2 months of history
        // a trend needs, but Net Worth has a configured rating (KnownMetricRatings)
        // that can read a level off just this one value, so it shouldn't be
        // stuck at "no data" for the whole first month.
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, IncreasingMetric(1, "Net Worth"));
        _metricSnapshots.SeedSnapshots(1, new MetricSnapshot(1, 170_000, DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        // 170,000 sits exactly 60% of the way through the default Tier2 band
        // (50,000-250,000), which maps onto the 25-50 score slice ->
        // 25 + 0.6 * 25 = 40. Not a flat "Tier2 = 50" regardless of position
        // within the band.
        var finance = summary.Categories.Single(c => c.CategoryName == "Finance");
        Assert.Equal(40, finance.Score);
        Assert.Equal(40, Assert.Single(finance.Metrics).Score);

        // No trend exists yet for Net Worth (only one month recorded), but 40
        // still falls in the Needs Attention band (26-50), so it is called out
        // on level alone -- same reasoning as Social's Active Circle, which is
        // at 0 with no friends seeded and so reads as Struggling.
        Assert.Equal(2, summary.Alerts.Count);
        Assert.Contains(summary.Alerts, a => a.Message == "Net Worth is trailing" && a.Score == 40);
        Assert.Contains(summary.Alerts, a => a.Message == "Active Circle needs attention" && a.Score == 0);

        // Social (0 friends seeded) contributes its RatingScore of 0, so
        // overall is (Finance 40 + Social 0) / 2 = 20, not Finance's 40 alone.
        Assert.Equal(20, summary.OverallScore);
    }

    [Fact]
    public async Task GetSummaryAsync_WithOnlyOneMonthRecorded_UnratedMetricsStayExcluded()
    {
        // Powerlifting Total has no KnownMetricRatings entry, so with only
        // one month recorded it has genuinely nothing to score yet.
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, IncreasingMetric(1, "Powerlifting Total"));
        _metricSnapshots.SeedSnapshots(1, new MetricSnapshot(1, 1000, DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        var fitness = summary.Categories.Single(c => c.CategoryName == "Fitness");
        Assert.Null(fitness.Score);
        Assert.Equal(CategoryStatus.NoData, fitness.Status);
        Assert.All(fitness.Metrics, metric => Assert.Null(metric.Score));

        // Fitness is genuinely excluded (no rating, no trend), but Social's
        // RatingScore (0, at 0 friends) still counts, so overall is 0, not
        // null -- there IS one real number to show, just a low one.
        Assert.Equal(0, summary.OverallScore);
        Assert.Equal(CategoryStatus.Struggling, summary.OverallStatus);
    }

    [Theory]
    // Against the configured cutoffs (Struggling <=25, NeedsAttention <=50,
    // OnTrack <=75): a regressed metric scores 30, a stagnant one 70, an
    // improved one 100.
    [InlineData(1050, 1000, 30, CategoryStatus.NeedsAttention)] // regressed only
    [InlineData(1000, 1000, 70, CategoryStatus.OnTrack)]        // stagnant only
    [InlineData(1000, 1050, 100, CategoryStatus.Excelling)]     // improved only
    public async Task GetSummaryAsync_MapsScoreToTheExpectedStatusThreshold(
        decimal previous, decimal latest, int expectedScore, CategoryStatus expectedStatus)
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, IncreasingMetric(1, "Powerlifting Total"));
        SeedTwoPointHistory(1, previous, latest);

        var summary = await CreateService().GetSummaryAsync();

        var fitness = summary.Categories.Single(c => c.CategoryName == "Fitness");
        Assert.Equal(expectedScore, fitness.Score);
        Assert.Equal(expectedStatus, fitness.Status);
    }

    [Fact]
    public async Task GetSummaryAsync_SocialScoreBlendsRatingScoreAndMaintenanceScore()
    {
        // 3 active friends (default ThinMax=4 band), one of them overdue.
        // RatingScore: 3 is 3/4 of the way through the Thin band (0-4 -> 0-25)
        // -> 18.75, rounds to 19. MaintenanceScore: 2 of 3 maintained -> 67.
        // Social's Score blends them like Finance blends Net Worth and
        // Credit Score: (19 + 67) / 2 = 43.
        SeedMaintainedFriend(1, "Robin");
        SeedMaintainedFriend(2, "Casey");
        _friends.Seed(3, new Friend("Overdue Pal", DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-6), DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        var social = summary.Categories.Single(c => c.CategoryName == "Social");
        Assert.Equal(43, social.Score);
    }

    [Fact]
    public async Task GetSummaryAsync_SocialWithBothFacetsMaxedOut_ScoresOneHundred()
    {
        // Narrow thresholds so a small, fully-maintained circle can also max
        // out RatingScore: Thin<=1, Healthy<=2, Robust<=3, and the heuristic
        // Expansive ceiling is 3 + (3 - 2) = 4 -- exactly 4 friends reaches it.
        _appSettings.Seed(new AppSetting("SocialCircleThinMax", "1"));
        _appSettings.Seed(new AppSetting("SocialCircleHealthyMax", "2"));
        _appSettings.Seed(new AppSetting("SocialCircleRobustMax", "3"));

        SeedMaintainedFriend(1, "Robin");
        SeedMaintainedFriend(2, "Casey");
        SeedMaintainedFriend(3, "Jordan");
        SeedMaintainedFriend(4, "Riley");

        var summary = await CreateService().GetSummaryAsync();

        var social = summary.Categories.Single(c => c.CategoryName == "Social");
        Assert.Equal(100, social.Score);
        Assert.Equal(CategoryStatus.Excelling, social.Status);
    }

    [Fact]
    public async Task GetSummaryAsync_SocialWithNoActiveCircleYet_StillContributesViaRatingScoreAlone()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, IncreasingMetric(1, "Powerlifting Total"));
        SeedTwoPointHistory(1, previous: 1000, latest: 1050);
        // No friends seeded at all -- MaintenanceScore is null (nothing to
        // maintain), but RatingScore is still a defined 0 (bottom of the
        // Thin band), so Social isn't "no data" here the way Fitness would
        // be with zero metrics.

        var summary = await CreateService().GetSummaryAsync();

        var social = summary.Categories.Single(c => c.CategoryName == "Social");
        Assert.Equal(0, social.Score);
        Assert.Equal(CategoryStatus.Struggling, social.Status);

        // (Fitness 100 + Social 0) / 2 = 50.
        Assert.Equal(50, summary.OverallScore);
    }

    [Fact]
    public async Task GetSummaryAsync_SocialAlertsNameTheSpecificFacetThatNeedsAttention()
    {
        // Narrow thresholds so that four active friends sits past the top
        // cutoff -- a flat RatingScore of 100, so Active Circle raises no
        // alert at all -- while half of them being overdue drags Circle
        // Upkeep to 50 (Needs Attention). The point is that the two facets
        // are alerted on independently: a circle can be the right size and
        // still be neglected, and the blended Social score would hide it.
        _appSettings.Seed(new AppSetting("SocialCircleThinMax", "1"));
        _appSettings.Seed(new AppSetting("SocialCircleHealthyMax", "2"));
        _appSettings.Seed(new AppSetting("SocialCircleRobustMax", "3"));

        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-6);
        SeedMaintainedFriend(1, "Robin");
        SeedMaintainedFriend(2, "Casey");
        // Overdue (more than 3 months) but still inside the 12-month active
        // window, so they keep counting toward circle size while dragging
        // upkeep down -- which is the whole distinction under test.
        _friends.Seed(3, new Friend("Overdue Pal", sixMonthsAgo, DateTimeOffset.UtcNow));
        _friends.Seed(4, new Friend("Another Overdue Pal", sixMonthsAgo, DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        Assert.DoesNotContain(summary.Alerts, a => a.MetricName == "Active Circle");
        var socialAlert = Assert.Single(summary.Alerts, a => a.CategoryName == "Social");
        Assert.Equal("Circle Upkeep is trailing", socialAlert.Message);
        Assert.Equal(50, socialAlert.Score);
        Assert.Equal(CategoryStatus.NeedsAttention, socialAlert.Status);
    }

    [Fact]
    public async Task GetSummaryAsync_BlendsSocialIntoTheOverallScoreAlongsideOtherCategories()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, IncreasingMetric(1, "Powerlifting Total"));
        SeedTwoPointHistory(1, previous: 1000, latest: 1050); // Fitness scores 100

        SeedMaintainedFriend(1, "Robin");
        _friends.Seed(2, new Friend("Overdue Pal", DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-6), DateTimeOffset.UtcNow));
        // 2 active friends: RatingScore is 2/4 through the Thin band -> 12.5,
        // rounds to even -> 12. MaintenanceScore: 1 of 2 maintained -> 50.
        // Social's Score is (12 + 50) / 2 = 31.

        var summary = await CreateService().GetSummaryAsync();

        var social = summary.Categories.Single(c => c.CategoryName == "Social");
        Assert.Equal(31, social.Score);

        // (Fitness 100 + Social 31) / 2 = 65.5, rounds to even -> 66.
        Assert.Equal(66, summary.OverallScore);
    }

    [Fact]
    public async Task GetSummaryAsync_RaisesAnAlertForAnOverdueKeyRelationshipByName()
    {
        // Default DateWithWifeThresholdMonths is 1 -- 2 months ago is overdue.
        _keyRelationships.Seed(1, new KeyRelationship(KeyRelationshipKind.DateWithWife, DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2), DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        var alert = Assert.Single(summary.Alerts, a => a.MetricName == "Date with Wife");
        Assert.Equal("Date with Wife is trailing", alert.Message);
        Assert.Equal(30, alert.Score);
        Assert.Equal(CategoryStatus.NeedsAttention, alert.Status);

        // Active Circle (0 friends) also alerts, so Date with Wife isn't the only one.
        Assert.Equal(2, summary.Alerts.Count);
    }
}
