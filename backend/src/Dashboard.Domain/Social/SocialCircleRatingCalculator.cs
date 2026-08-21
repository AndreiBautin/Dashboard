namespace Dashboard.Domain.Social;

/// <summary>
/// Configurable size bands for <see cref="SocialCircleRatingCalculator"/>.
/// Backed by AppSettings (SocialCircleThinMax/HealthyMax/RobustMax in
/// Dashboard.Application.Settings.KnownAppSettings) rather than hardcoded, so
/// the bands can be tuned from the Settings page without a code change --
/// same rationale as Friend.IsActive's configurable active-circle window.
/// Defaults (4/8/14) come from a rough brainstormed scale (~3 = thin,
/// 5-8 = healthy, 8-12 = robust, 15+ = requires a very social lifestyle)
/// that had overlapping boundaries by nature of being a rough sketch;
/// resolved here into clean, non-overlapping cutoffs.
/// </summary>
public sealed record SocialCircleRatingThresholds(int ThinMax, int HealthyMax, int RobustMax);

public static class SocialCircleRatingCalculator
{
    public static SocialCircleRating Rate(int activeFriendCount, SocialCircleRatingThresholds thresholds)
    {
        if (activeFriendCount <= thresholds.ThinMax)
        {
            return SocialCircleRating.Thin;
        }

        if (activeFriendCount <= thresholds.HealthyMax)
        {
            return SocialCircleRating.Healthy;
        }

        if (activeFriendCount <= thresholds.RobustMax)
        {
            return SocialCircleRating.Robust;
        }

        return SocialCircleRating.Expansive;
    }

    /// <summary>
    /// A continuous 0-100 read on where <paramref name="activeFriendCount"/>
    /// falls, mirroring Dashboard.Domain.Metrics.MetricRatingCalculator's
    /// banding scheme (each of the first three tiers maps to a 25-point
    /// slice of 0-100, linearly interpolated across it) so circle size can
    /// contribute a nuanced score to the Dashboard the same way Net Worth or
    /// Credit Score do, rather than a flat "which tier" number that can't
    /// move within a tier. Unlike the circle maintenance score computed in
    /// the Application layer (which needs an active circle to divide by),
    /// this is always defined -- even 0 active friends is a valid, if low,
    /// point on the scale. Expansive (past RobustMax) is a flat 100, not a
    /// fourth interpolated band -- once every defined cutoff is cleared
    /// there's nothing further to measure progress against, same reasoning
    /// as MetricRatingCalculator.RateContinuous's own top tier.
    /// </summary>
    public static decimal RateContinuous(int activeFriendCount, SocialCircleRatingThresholds thresholds)
    {
        decimal value = activeFriendCount;

        if (value <= thresholds.ThinMax)
        {
            return Interpolate(value, 0m, thresholds.ThinMax, 0m, 25m);
        }

        if (value <= thresholds.HealthyMax)
        {
            return Interpolate(value, thresholds.ThinMax, thresholds.HealthyMax, 25m, 50m);
        }

        if (value <= thresholds.RobustMax)
        {
            return Interpolate(value, thresholds.HealthyMax, thresholds.RobustMax, 50m, 75m);
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
