namespace Vantage.Domain.Metrics;

/// <summary>
/// A qualitative read on a metric's absolute value -- e.g. a net worth
/// figure or a credit score doesn't mean much at a glance without a
/// sense of whether that's low, solid, or excellent. Orthogonal to
/// <see cref="MetricStatus"/>: Status is about the trend (did it improve
/// this month?), Tier is about the level (is this a good number at all?).
/// Deliberately generic/decimal-valued (unlike Social's SocialCircleRating,
/// which is Social-specific) so the same four-tier shape can back Net
/// Worth, Credit Score, Strength Total, Arm Measurement, or any future
/// metric that wants one -- only the thresholds and display labels differ
/// per metric (see Vantage.Application.Metrics.KnownMetricRatings).
/// </summary>
public enum MetricRatingTier
{
    Tier1,
    Tier2,
    Tier3,
    Tier4,
}

/// <summary>
/// Configurable upper bounds for tiers 1-3; anything above Tier3Max is Tier4.
/// <paramref name="HigherIsBetter"/> covers the common case (Net Worth,
/// Credit Score, Strength Total, Arm Measurement, VO2 Max -- bigger number,
/// better tier) but some metrics run the other way (Waist Measurement --
/// smaller number, better tier). Rather than asking every metric to define
/// its own inverted threshold shape, the three cutoffs always mean the same
/// thing (ascending Tier1Max &lt;= Tier2Max &lt;= Tier3Max) and this flag just
/// mirrors which end -- Tier1 or Tier4 -- counts as "best".
/// </summary>
public sealed record MetricRatingThresholds(decimal Tier1Max, decimal Tier2Max, decimal Tier3Max, bool HigherIsBetter = true);

public static class MetricRatingCalculator
{
    public static MetricRatingTier Rate(decimal value, MetricRatingThresholds thresholds)
    {
        var ascendingTier = RateAscending(value, thresholds);
        return thresholds.HigherIsBetter ? ascendingTier : Invert(ascendingTier);
    }

    private static MetricRatingTier RateAscending(decimal value, MetricRatingThresholds thresholds)
    {
        if (value <= thresholds.Tier1Max)
        {
            return MetricRatingTier.Tier1;
        }

        if (value <= thresholds.Tier2Max)
        {
            return MetricRatingTier.Tier2;
        }

        if (value <= thresholds.Tier3Max)
        {
            return MetricRatingTier.Tier3;
        }

        return MetricRatingTier.Tier4;
    }

    private static MetricRatingTier Invert(MetricRatingTier tier) => tier switch
    {
        MetricRatingTier.Tier1 => MetricRatingTier.Tier4,
        MetricRatingTier.Tier2 => MetricRatingTier.Tier3,
        MetricRatingTier.Tier3 => MetricRatingTier.Tier2,
        MetricRatingTier.Tier4 => MetricRatingTier.Tier1,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };

    /// <summary>
    /// A continuous 0-100 read on where <paramref name="value"/> falls,
    /// rather than the coarse "which of the 4 tiers" answer <see cref="Rate"/>
    /// gives. Two values that both land in Tier2 but sit at opposite ends of
    /// that band (e.g. just past Tier1Max vs. right at Tier2Max) get
    /// meaningfully different scores instead of being flattened to the same
    /// number -- e.g. a Net Worth that's $1 away from the next tier shouldn't
    /// score identically to one that's $200,000 away from it. Each of the
    /// first three tiers maps onto a 25-point slice of the 0-100 range
    /// (Tier1: 0-25, Tier2: 25-50, Tier3: 50-75), linearly interpolated
    /// across the band -- Tier1 has no configured floor, so 0 is used as a
    /// practical one (or Tier1Max itself, if that's already at or below 0).
    /// Tier4 is different in kind, not just position: it's whatever's left
    /// once you're past every defined cutoff, with nothing further to
    /// measure progress against, so it's a flat 100 rather than an
    /// interpolated range -- matching <see cref="Rate"/>'s own tier badge,
    /// which likewise treats Tier4 as a single "you've arrived" bucket, not
    /// four more sub-levels to climb through.
    /// </summary>
    public static decimal RateContinuous(decimal value, MetricRatingThresholds thresholds)
    {
        var ascendingScore = RateContinuousAscending(value, thresholds);
        return thresholds.HigherIsBetter ? ascendingScore : 100m - ascendingScore;
    }

    private static decimal RateContinuousAscending(decimal value, MetricRatingThresholds thresholds)
    {
        if (value <= thresholds.Tier1Max)
        {
            var floor = Math.Min(0m, thresholds.Tier1Max);
            return Interpolate(value, floor, thresholds.Tier1Max, 0m, 25m);
        }

        if (value <= thresholds.Tier2Max)
        {
            return Interpolate(value, thresholds.Tier1Max, thresholds.Tier2Max, 25m, 50m);
        }

        if (value <= thresholds.Tier3Max)
        {
            return Interpolate(value, thresholds.Tier2Max, thresholds.Tier3Max, 50m, 75m);
        }

        return 100m;
    }

    private static decimal Interpolate(decimal value, decimal bandStart, decimal bandEnd, decimal scoreStart, decimal scoreEnd)
    {
        if (bandEnd <= bandStart)
        {
            return scoreEnd;
        }

        var progress = Math.Clamp((value - bandStart) / (bandEnd - bandStart), 0m, 1m);
        return scoreStart + progress * (scoreEnd - scoreStart);
    }
}
