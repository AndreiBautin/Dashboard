using Vantage.Application.Dashboard;
using Vantage.Application.Metrics;
using Vantage.Application.Settings;
using Vantage.Domain.Social;

namespace Vantage.Application.Social;

/// <summary>
/// Answers "how's my circle doing?" -- active count (with the delta versus
/// last month's captured <c>SocialSnapshot</c>), a qualitative rating of
/// that count, and the full friend list ranked by how long it's been since
/// you last saw them (most overdue first), matching the Phase 0 design's
/// "surface what needs attention" principle.
/// </summary>
public sealed class SocialService
{
    private readonly IFriendRepository _friendRepository;
    private readonly IKeyRelationshipRepository _keyRelationshipRepository;
    private readonly IMonthlySnapshotRepository _monthlySnapshotRepository;
    private readonly IAppSettingRepository _appSettingRepository;

    public SocialService(
        IFriendRepository friendRepository,
        IKeyRelationshipRepository keyRelationshipRepository,
        IMonthlySnapshotRepository monthlySnapshotRepository,
        IAppSettingRepository appSettingRepository)
    {
        _friendRepository = friendRepository;
        _keyRelationshipRepository = keyRelationshipRepository;
        _monthlySnapshotRepository = monthlySnapshotRepository;
        _appSettingRepository = appSettingRepository;
    }

    public async Task<SocialSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var thresholdMonths = await GetActiveCircleThresholdMonthsAsync(cancellationToken);
        var overdueThresholdMonths = await AppSettingReader.GetIntAsync(
            _appSettingRepository, KnownAppSettings.OverdueThresholdMonths, cancellationToken);
        var ratingThresholds = await GetRatingThresholdsAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var friends = await _friendRepository.GetAllAsync(cancellationToken);

        var friendSummaries = friends
            .Select(friend => new FriendSummary(
                friend.Id,
                friend.Name,
                friend.Notes,
                friend.LastHangoutDate,
                friend.DaysSinceLastHangout(today),
                friend.IsActive(thresholdMonths, today),
                friend.IsFlaggedOverdue(overdueThresholdMonths, today)))
            .OrderByDescending(summary => summary.DaysSinceLastHangout)
            .ToList();

        var activeFriendCount = friendSummaries.Count(summary => summary.IsActive);
        var delta = await GetActiveFriendCountDeltaAsync(activeFriendCount, today, cancellationToken);
        var rating = SocialCircleRatingCalculator.Rate(activeFriendCount, ratingThresholds);
        var ratingDescription = await AppSettingReader.GetTextAsync(
            _appSettingRepository, DescriptionSettingFor(rating), cancellationToken);
        var ratingScore = (int)Math.Round(SocialCircleRatingCalculator.RateContinuous(activeFriendCount, ratingThresholds));
        var ratingBands = DescribeRatingBands(ratingThresholds);
        var maintenanceScore = ComputeMaintenanceScore(friendSummaries);
        var keyRelationshipSummaries = await GetKeyRelationshipSummariesAsync(today, cancellationToken);

        // Score/Status are Social's single blended read, computed once here
        // rather than by every consumer -- the Dashboard, this page's own
        // header ring, and anywhere else Social's score shows up all read
        // these same two fields instead of quietly re-deriving them (and
        // risking drifting apart). Same "blend what's available" approach as
        // Finance averaging Net Worth and Credit Score: RatingScore always
        // counts (even 0 friends is a real, if low, point on the scale),
        // MaintenanceScore only counts when there's an active circle to
        // maintain, and every key relationship counts too.
        var scores = new List<int> { ratingScore };
        if (maintenanceScore is { } maintenanceScoreValue)
        {
            scores.Add(maintenanceScoreValue);
        }
        scores.AddRange(keyRelationshipSummaries.Select(summary => summary.Score));

        var score = (int)Math.Round(scores.Average());
        var statusThresholds = await CategoryStatusCalculator.GetThresholdsAsync(_appSettingRepository, cancellationToken);
        var status = CategoryStatusCalculator.From(score, statusThresholds);

        return new SocialSummary(
            activeFriendCount, delta, rating, ratingDescription, ratingScore, maintenanceScore,
            keyRelationshipSummaries, score, status, friendSummaries, ratingBands);
    }

    /// <summary>
    /// Builds a summary for whichever key relationships actually have a
    /// seeded row -- a kind with no row yet (e.g. a fresh install before dev
    /// seeding has run) is skipped rather than crashing, same "no data is
    /// excluded, not defaulted to 0" rule as MaintenanceScore.
    /// </summary>
    private async Task<IReadOnlyList<KeyRelationshipSummary>> GetKeyRelationshipSummariesAsync(
        DateOnly today, CancellationToken cancellationToken)
    {
        var relationships = await _keyRelationshipRepository.GetAllAsync(cancellationToken);
        var summaries = new List<KeyRelationshipSummary>();

        foreach (var definition in KeyRelationshipDefinitions.All)
        {
            var relationship = relationships.FirstOrDefault(r => r.Kind == definition.Kind);
            if (relationship is null)
            {
                continue;
            }

            var relationshipThresholdMonths = await AppSettingReader.GetIntAsync(
                _appSettingRepository, definition.ThresholdSetting, cancellationToken);
            var isFlaggedOverdue = relationship.IsFlaggedOverdue(relationshipThresholdMonths, today);

            summaries.Add(new KeyRelationshipSummary(
                relationship.Id,
                definition.Kind,
                definition.Label,
                relationship.LastContactDate,
                relationship.DaysSinceLastContact(today),
                isFlaggedOverdue,
                isFlaggedOverdue ? 30 : 100));
        }

        return summaries;
    }

    /// <summary>
    /// What percent of the active circle isn't flagged overdue -- orthogonal
    /// to the circle SIZE rating above: a small circle that's perfectly kept
    /// up with scores 100 here, and a large one that's badly neglected
    /// scores low, regardless of size. Null (not 0) when there's no active
    /// circle at all yet to maintain, so it's excluded rather than dragging
    /// an average down.
    /// </summary>
    private static int? ComputeMaintenanceScore(IReadOnlyList<FriendSummary> friendSummaries)
    {
        var activeFriends = friendSummaries.Where(summary => summary.IsActive).ToList();
        if (activeFriends.Count == 0)
        {
            return null;
        }

        var maintainedCount = activeFriends.Count(summary => !summary.IsFlaggedOverdue);
        return (int)Math.Round(100m * maintainedCount / activeFriends.Count);
    }

    /// <summary>
    /// The full circle-size scale, ascending by friend count, each cutoff
    /// paired with its label -- same "show the whole scale, not just the
    /// current tier" treatment MetricRatingDefinition.DescribeBands gives
    /// rated metrics. Reuses SocialCircleRatingCalculator.Rate itself
    /// (evaluated once per cutoff, plus once just past the last one) rather
    /// than re-deriving the tier boundaries here.
    /// </summary>
    private static IReadOnlyList<RatingBand> DescribeRatingBands(SocialCircleRatingThresholds thresholds) =>
    [
        new(thresholds.ThinMax, SocialCircleRatingCalculator.Rate(thresholds.ThinMax, thresholds).ToString()),
        new(thresholds.HealthyMax, SocialCircleRatingCalculator.Rate(thresholds.HealthyMax, thresholds).ToString()),
        new(thresholds.RobustMax, SocialCircleRatingCalculator.Rate(thresholds.RobustMax, thresholds).ToString()),
        new(null, SocialCircleRatingCalculator.Rate(thresholds.RobustMax + 1, thresholds).ToString()),
    ];

    private static AppSettingDefinition DescriptionSettingFor(SocialCircleRating rating) => rating switch
    {
        SocialCircleRating.Thin => KnownAppSettings.SocialTier1Description,
        SocialCircleRating.Healthy => KnownAppSettings.SocialTier2Description,
        SocialCircleRating.Robust => KnownAppSettings.SocialTier3Description,
        SocialCircleRating.Expansive => KnownAppSettings.SocialTier4Description,
        _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, null),
    };

    /// <summary>
    /// The active-circle size over time, reusing <see cref="MetricTrendPoint"/>
    /// (Month/Value) rather than a Social-specific type -- the frontend's
    /// existing trend chart just works unmodified.
    /// </summary>
    public async Task<IReadOnlyList<MetricTrendPoint>> GetTrendAsync(CancellationToken cancellationToken = default)
    {
        var monthlySnapshots = await _monthlySnapshotRepository.GetAllAsync(cancellationToken);

        return monthlySnapshots
            .Where(snapshot => snapshot.SocialSnapshot is not null)
            .Select(snapshot => new MetricTrendPoint(snapshot.Month, snapshot.SocialSnapshot!.ActiveFriendCount))
            .ToList();
    }

    /// <summary>
    /// Shared with <see cref="FriendService"/> so the threshold used when
    /// recomputing this month's captured count always matches what
    /// <see cref="GetSummaryAsync"/> itself would use.
    /// </summary>
    internal Task<int> GetActiveCircleThresholdMonthsAsync(CancellationToken cancellationToken = default) =>
        AppSettingReader.GetIntAsync(_appSettingRepository, KnownAppSettings.ActiveCircleThresholdMonths, cancellationToken);

    private async Task<SocialCircleRatingThresholds> GetRatingThresholdsAsync(CancellationToken cancellationToken) =>
        new(
            ThinMax: await AppSettingReader.GetIntAsync(_appSettingRepository, KnownAppSettings.SocialCircleThinMax, cancellationToken),
            HealthyMax: await AppSettingReader.GetIntAsync(_appSettingRepository, KnownAppSettings.SocialCircleHealthyMax, cancellationToken),
            RobustMax: await AppSettingReader.GetIntAsync(_appSettingRepository, KnownAppSettings.SocialCircleRobustMax, cancellationToken));

    private async Task<int?> GetActiveFriendCountDeltaAsync(
        int activeFriendCount, DateOnly today, CancellationToken cancellationToken)
    {
        var lastMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        var lastMonthSnapshot = await _monthlySnapshotRepository.GetByMonthAsync(lastMonth, cancellationToken);

        return lastMonthSnapshot?.SocialSnapshot is { } social
            ? activeFriendCount - social.ActiveFriendCount
            : null;
    }
}
