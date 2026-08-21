using Dashboard.Application.Metrics;
using Dashboard.Application.Social;
using Dashboard.Application.Tests.Metrics.Fakes;
using Dashboard.Application.Tests.Social.Fakes;
using Dashboard.Domain.Metrics;
using Dashboard.Domain.Settings;
using Dashboard.Domain.Social;

namespace Dashboard.Application.Tests.Social;

public class SocialServiceTests
{
    private readonly FakeFriendRepository _friends = new();
    private readonly FakeKeyRelationshipRepository _keyRelationships = new();
    private readonly FakeMonthlySnapshotRepository _monthlySnapshots = new();
    private readonly FakeAppSettingRepository _appSettings = new();

    private SocialService CreateService() => new(_friends, _keyRelationships, _monthlySnapshots, _appSettings);

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static DateOnly ThisMonth()
    {
        var today = Today();
        return new DateOnly(today.Year, today.Month, 1);
    }

    [Fact]
    public async Task GetSummaryAsync_RanksFriendsByDaysSinceLastHangoutDescending()
    {
        _friends.Seed(1, new Friend("Recent", Today().AddDays(-10), DateTimeOffset.UtcNow));
        _friends.Seed(2, new Friend("Overdue", Today().AddDays(-200), DateTimeOffset.UtcNow));
        _friends.Seed(3, new Friend("Middling", Today().AddDays(-60), DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(["Overdue", "Middling", "Recent"], summary.Friends.Select(f => f.Name));
    }

    [Fact]
    public async Task GetSummaryAsync_UsesTheConfiguredActiveCircleThreshold_NotAHardcodedDefault()
    {
        // 90 days ago is within the default 12-month threshold, but outside a
        // configured 2-month one -- proves the setting is actually read.
        _friends.Seed(1, new Friend("Robin", Today().AddDays(-90), DateTimeOffset.UtcNow));
        _appSettings.Seed(new AppSetting("ActiveCircleThresholdMonths", "2"));

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(0, summary.ActiveFriendCount);
        Assert.False(Assert.Single(summary.Friends).IsActive);
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoPriorMonthSnapshot_DeltaIsNull()
    {
        _friends.Seed(1, new Friend("Robin", Today().AddDays(-10), DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        Assert.Null(summary.ActiveFriendCountDelta);
    }

    [Fact]
    public async Task GetSummaryAsync_WithAPriorMonthSnapshot_ComputesTheDelta()
    {
        _friends.Seed(1, new Friend("Robin", Today().AddDays(-10), DateTimeOffset.UtcNow));
        _friends.Seed(2, new Friend("Casey", Today().AddDays(-10), DateTimeOffset.UtcNow));

        var lastMonthSnapshot = new MonthlySnapshot(ThisMonth().AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(-1));
        lastMonthSnapshot.SetSocialSnapshot(1);
        _monthlySnapshots.Seed(lastMonthSnapshot);

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(2, summary.ActiveFriendCount);
        Assert.Equal(1, summary.ActiveFriendCountDelta);
    }

    [Fact]
    public async Task GetSummaryAsync_UsesTheConfiguredOverdueThreshold_NotAHardcodedDefault()
    {
        // 40 days ago is within the default 3-month overdue window, but
        // outside a configured 1-month one -- proves the setting is read.
        _friends.Seed(1, new Friend("Robin", Today().AddDays(-40), DateTimeOffset.UtcNow));
        _appSettings.Seed(new AppSetting("OverdueThresholdMonths", "1"));

        var summary = await CreateService().GetSummaryAsync();

        Assert.True(Assert.Single(summary.Friends).IsFlaggedOverdue);
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoOverride_RatesUsingTheDefaultThresholds()
    {
        // 5 active friends is just past the default ThinMax of 4 -- Healthy.
        for (var i = 1; i <= 5; i++)
        {
            _friends.Seed(i, new Friend($"Friend{i}", Today().AddDays(-10), DateTimeOffset.UtcNow));
        }

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(5, summary.ActiveFriendCount);
        Assert.Equal(SocialCircleRating.Healthy, summary.Rating);
        Assert.Equal("A solid, sustainable circle size.", summary.RatingDescription);
    }

    [Fact]
    public async Task GetSummaryAsync_UsesConfiguredRatingDescription_NotHardcodedDefault()
    {
        // Same 5-friends-is-Healthy setup as above, but with a configured
        // override for the Healthy tier's description -- proves the
        // description is actually read from settings, not hardcoded.
        for (var i = 1; i <= 5; i++)
        {
            _friends.Seed(i, new Friend($"Friend{i}", Today().AddDays(-10), DateTimeOffset.UtcNow));
        }
        _appSettings.Seed(new AppSetting("SocialTier2Description", "Custom healthy blurb."));

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(SocialCircleRating.Healthy, summary.Rating);
        Assert.Equal("Custom healthy blurb.", summary.RatingDescription);
    }

    [Fact]
    public async Task GetSummaryAsync_UsesConfiguredRatingThresholds_NotHardcodedDefaults()
    {
        // 2 active friends would default to Thin (ThinMax 4), but a
        // configured ThinMax of 1 pushes it into Healthy -- proves the
        // setting is actually read rather than a hardcoded constant.
        _friends.Seed(1, new Friend("Robin", Today().AddDays(-10), DateTimeOffset.UtcNow));
        _friends.Seed(2, new Friend("Casey", Today().AddDays(-10), DateTimeOffset.UtcNow));
        _appSettings.Seed(new AppSetting("SocialCircleThinMax", "1"));

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(2, summary.ActiveFriendCount);
        Assert.Equal(SocialCircleRating.Healthy, summary.Rating);
    }

    [Fact]
    public async Task GetSummaryAsync_RatingScoreIsAContinuousReadOnCircleSize_NotJustTheTierLabel()
    {
        // Same 5-friends-is-Healthy setup as above (default thresholds
        // ThinMax=4, HealthyMax=8): 5 is 1/4 of the way through the Healthy
        // band (4-8 -> 25-50) -> 25 + 0.25*25 = 31.25, rounds to 31.
        for (var i = 1; i <= 5; i++)
        {
            _friends.Seed(i, new Friend($"Friend{i}", Today().AddDays(-10), DateTimeOffset.UtcNow));
        }

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(SocialCircleRating.Healthy, summary.Rating);
        Assert.Equal(31, summary.RatingScore);
    }

    [Fact]
    public async Task GetSummaryAsync_ExposesTheFullActiveCircleScale_AscendingByFriendCount()
    {
        // Default thresholds: ThinMax=4, HealthyMax=8, RobustMax=14.
        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(
            [
                new RatingBand(4, "Thin"),
                new RatingBand(8, "Healthy"),
                new RatingBand(14, "Robust"),
                new RatingBand(null, "Expansive"),
            ],
            summary.RatingBands);
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoActiveCircle_RatingScoreIsZero_NotNull()
    {
        // Unlike MaintenanceScore (which has nothing to divide by with no
        // active circle), RatingScore is always defined -- 0 friends is
        // just the bottom of the scale.
        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(0, summary.RatingScore);
    }

    [Fact]
    public async Task GetSummaryAsync_MaintenanceScoreIsThePercentOfTheActiveCircleThatIsNotOverdue()
    {
        // 3 active friends, 1 overdue -- 2/3 maintained -> 67.
        _friends.Seed(1, new Friend("Robin", Today().AddDays(-5), DateTimeOffset.UtcNow));
        _friends.Seed(2, new Friend("Casey", Today().AddDays(-5), DateTimeOffset.UtcNow));
        _friends.Seed(3, new Friend("Overdue Pal", Today().AddMonths(-6), DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(67, summary.MaintenanceScore);
    }

    [Fact]
    public async Task GetSummaryAsync_WithAPerfectlyMaintainedCircle_MaintenanceScoreIsOneHundred()
    {
        _friends.Seed(1, new Friend("Robin", Today().AddDays(-5), DateTimeOffset.UtcNow));
        _friends.Seed(2, new Friend("Casey", Today().AddDays(-5), DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(100, summary.MaintenanceScore);
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoActiveCircle_MaintenanceScoreIsNull()
    {
        var summary = await CreateService().GetSummaryAsync();

        Assert.Null(summary.MaintenanceScore);
    }

    [Fact]
    public async Task GetSummaryAsync_KeyRelationshipOnTrack_ScoresOneHundred()
    {
        _keyRelationships.Seed(1, new KeyRelationship(KeyRelationshipKind.DateWithWife, Today().AddDays(-5), DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        var dateWithWife = Assert.Single(summary.KeyRelationships, k => k.Kind == KeyRelationshipKind.DateWithWife);
        Assert.Equal("Date with Wife", dateWithWife.Label);
        Assert.False(dateWithWife.IsFlaggedOverdue);
        Assert.Equal(100, dateWithWife.Score);
    }

    [Fact]
    public async Task GetSummaryAsync_KeyRelationshipOverdue_ScoresThirty()
    {
        // Default DateWithWifeThresholdMonths is 1 -- 2 months ago is overdue.
        _keyRelationships.Seed(1, new KeyRelationship(KeyRelationshipKind.DateWithWife, Today().AddMonths(-2), DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        var dateWithWife = Assert.Single(summary.KeyRelationships, k => k.Kind == KeyRelationshipKind.DateWithWife);
        Assert.True(dateWithWife.IsFlaggedOverdue);
        Assert.Equal(30, dateWithWife.Score);
    }

    [Fact]
    public async Task GetSummaryAsync_KeyRelationshipThresholdsAreConfiguredIndependently()
    {
        // Both start 2 months ago -- overdue under the default 1-month
        // window, but a 3-month override for Visited Mother only should
        // clear just that one, proving the two thresholds aren't shared.
        _keyRelationships.Seed(1, new KeyRelationship(KeyRelationshipKind.DateWithWife, Today().AddMonths(-2), DateTimeOffset.UtcNow));
        _keyRelationships.Seed(2, new KeyRelationship(KeyRelationshipKind.VisitedMother, Today().AddMonths(-2), DateTimeOffset.UtcNow));
        _appSettings.Seed(new AppSetting("VisitedMotherThresholdMonths", "3"));

        var summary = await CreateService().GetSummaryAsync();

        Assert.True(Assert.Single(summary.KeyRelationships, k => k.Kind == KeyRelationshipKind.DateWithWife).IsFlaggedOverdue);
        Assert.False(Assert.Single(summary.KeyRelationships, k => k.Kind == KeyRelationshipKind.VisitedMother).IsFlaggedOverdue);
    }

    [Fact]
    public async Task GetSummaryAsync_KeyRelationshipNotYetSeeded_IsExcludedRatherThanCrashing()
    {
        // Neither key relationship seeded at all (e.g. a fresh install
        // before dev seeding has run) -- shouldn't throw, and the list
        // should simply be empty rather than containing placeholders.
        var summary = await CreateService().GetSummaryAsync();

        Assert.Empty(summary.KeyRelationships);
    }

    [Fact]
    public async Task GetSummaryAsync_ScoreBlendsRatingScoreMaintenanceScoreAndKeyRelationships()
    {
        // 2 active friends, both maintained: RatingScore = 12 (2/4 through
        // the Thin band -> 12.5, banker's-rounds to 12), MaintenanceScore =
        // 100. One key relationship on track (100), one overdue (30).
        // Blended: round(avg(12, 100, 100, 30)) = round(60.5) = 60 (rounds
        // to even, but 60 IS even so no ambiguity here).
        _friends.Seed(1, new Friend("Robin", Today().AddDays(-5), DateTimeOffset.UtcNow));
        _friends.Seed(2, new Friend("Casey", Today().AddDays(-5), DateTimeOffset.UtcNow));
        _keyRelationships.Seed(1, new KeyRelationship(KeyRelationshipKind.DateWithWife, Today().AddDays(-5), DateTimeOffset.UtcNow));
        _keyRelationships.Seed(2, new KeyRelationship(KeyRelationshipKind.VisitedMother, Today().AddMonths(-2), DateTimeOffset.UtcNow));

        var summary = await CreateService().GetSummaryAsync();

        Assert.Equal(12, summary.RatingScore);
        Assert.Equal(100, summary.MaintenanceScore);
        Assert.Equal(60, summary.Score);
    }

    [Fact]
    public async Task GetTrendAsync_ReturnsOnlyMonthsWithACapturedSocialSnapshot()
    {
        var monthWithSnapshot = new MonthlySnapshot(ThisMonth().AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(-1));
        monthWithSnapshot.SetSocialSnapshot(7);
        _monthlySnapshots.Seed(monthWithSnapshot);

        var monthWithoutSnapshot = new MonthlySnapshot(ThisMonth().AddMonths(-2), DateTimeOffset.UtcNow.AddMonths(-2));
        _monthlySnapshots.Seed(monthWithoutSnapshot);

        var trend = await CreateService().GetTrendAsync();

        var point = Assert.Single(trend);
        Assert.Equal(7, point.Value);
    }
}
