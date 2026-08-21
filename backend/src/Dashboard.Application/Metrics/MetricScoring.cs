using Dashboard.Application.Settings;
using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Metrics;

/// <summary>
/// Turns one metric's status (or, absent a trend, its rated value) into a
/// 0-100 score. Shared by <see cref="Dashboard.DashboardService"/> (which
/// blends every category's metrics into one category score) and
/// <see cref="CategoryDetailService"/> (which needs that same per-category
/// score on the category's own detail page) so the two places a category
/// score is shown can never quietly drift apart.
/// </summary>
public static class MetricScoring
{
    // Deliberately simple starting points, not tuned against real data yet —
    // see docs/phase-0-design-proposal.md's risk notes.
    private const int ImprovedScore = 100;
    private const int StagnantScore = 70;
    private const int RegressedScore = 30;

    public static int ScoreFor(MetricStatus status) => status switch
    {
        MetricStatus.Improved => ImprovedScore,
        MetricStatus.Stagnant => StagnantScore,
        MetricStatus.Regressed => RegressedScore,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unexpected scoreable status."),
    };

    /// <summary>
    /// A continuous, rating-based score for a metric that has a configured
    /// rating (KnownMetricRatings) and at least one recorded value, used
    /// only as a fallback when there isn't yet enough history for a trend
    /// score (i.e. status is <see cref="MetricStatus.InsufficientData"/>).
    /// Uses <see cref="MetricRatingCalculator.RateContinuous"/> rather than
    /// the coarse 4-tier answer, so e.g. a Net Worth that's barely above a
    /// tier boundary doesn't score the same as one that's most of the way to
    /// the next tier -- several hundred thousand dollars of real progress
    /// shouldn't be invisible to the score just because it didn't cross a
    /// line. Returns null when the metric has no rating definition or no
    /// value at all -- callers should treat that the same as any other "no
    /// data", i.e. leave it out of the average rather than counting it as
    /// zero.
    /// </summary>
    public static async Task<int?> GetRatedValueScoreAsync(
        int metricDefinitionId,
        string metricName,
        IMetricSnapshotRepository metricSnapshotRepository,
        IAppSettingRepository appSettingRepository,
        CancellationToken cancellationToken = default)
    {
        var ratingDefinition = KnownMetricRatings.ForMetricName(metricName);
        if (ratingDefinition is null)
        {
            return null;
        }

        var snapshots = await metricSnapshotRepository.GetForMetricAsync(metricDefinitionId, cancellationToken);
        if (snapshots.Count == 0)
        {
            return null;
        }

        var thresholds = await ratingDefinition.GetThresholdsAsync(appSettingRepository, cancellationToken);
        var continuousScore = MetricRatingCalculator.RateContinuous(snapshots[^1].Value, thresholds);

        return (int)Math.Round(continuousScore);
    }

    /// <summary>
    /// The full "one metric's contribution to its category's score" decision:
    /// trend-based when there's history, rated-value fallback otherwise, or
    /// nothing at all to contribute when neither is available.
    /// </summary>
    public static async Task<int?> GetScoreAsync(
        MetricStatus status,
        int metricDefinitionId,
        string metricName,
        IMetricSnapshotRepository metricSnapshotRepository,
        IAppSettingRepository appSettingRepository,
        CancellationToken cancellationToken = default) =>
        status == MetricStatus.InsufficientData
            ? await GetRatedValueScoreAsync(metricDefinitionId, metricName, metricSnapshotRepository, appSettingRepository, cancellationToken)
            : ScoreFor(status);
}
