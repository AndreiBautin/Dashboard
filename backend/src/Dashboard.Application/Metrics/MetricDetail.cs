using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Metrics;

/// <summary>
/// One metric as shown on its category's detail screen: enough to render the
/// headline value/status and to pre-fill an entry form with whatever's
/// already been recorded this month. <paramref name="RatingLabel"/> is a
/// qualitative read on <paramref name="LatestValue"/> (e.g. "Good", "Elite")
/// for metrics with a configured rating (see KnownMetricRatings) -- null for
/// metrics that don't have one, or that have no value yet to rate.
/// <paramref name="RatingTier"/> is the same rating's raw tier (Tier1-4,
/// worst to best) alongside the label, so the frontend can render a
/// consistent at-a-glance position indicator (e.g. a 4-dot scale) without
/// having to know what "Growing" or "Elite" means relative to the others.
/// <paramref name="RatingDescription"/> is a longer, configurable blurb for
/// that tier (e.g. "Steady progress with real momentum building").
/// <paramref name="Score"/> is this metric's own 0-100 contribution to its
/// category's score (see MetricScoring) -- the category's Score on
/// <see cref="CategoryDetail"/> is the average of every metric's Score here,
/// so showing both together lets you see exactly why the category landed
/// where it did. Null when this metric has nothing to score yet (no rating
/// definition and no trend history).
/// <paramref name="IsCalculated"/> metrics (e.g. Strength Total) are derived
/// from other metrics and shouldn't get an editable input on the entry form.
/// <paramref name="RatingBands"/> is the metric's whole rating scale (every
/// tier's cutoff and label, not just the one that currently applies) so the
/// detail page can show the full picture -- what it actually takes to reach
/// the next tier -- rather than just the current tier in isolation. Null for
/// metrics with no configured rating.
/// </summary>
public sealed record MetricDetail(
    int MetricDefinitionId,
    string MetricName,
    string Unit,
    decimal? LatestValue,
    decimal? CurrentMonthValue,
    MetricStatus Status,
    string? RatingLabel,
    MetricRatingTier? RatingTier,
    string? RatingDescription,
    int? Score,
    bool IsCalculated,
    IReadOnlyList<RatingBand>? RatingBands = null);
