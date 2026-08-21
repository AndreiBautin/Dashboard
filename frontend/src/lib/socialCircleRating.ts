import type { MetricRatingTier, SocialCircleRating } from "@/lib/api";

/**
 * Maps the social-circle-specific rating onto the same Tier1-4 scale (worst
 * to best) that Finance/Fitness ratings use, so <RatingTierIndicator> can
 * render social with the identical 4-segment "out of four" visual instead of
 * a one-off single dot. Descriptive copy for each tier now comes from the
 * backend (SocialSummary.ratingDescription, configurable via Settings), so
 * there's nothing else to keep in sync here.
 */
export const socialCircleRatingTier: Record<SocialCircleRating, MetricRatingTier> = {
  Thin: "Tier1",
  Healthy: "Tier2",
  Robust: "Tier3",
  Expansive: "Tier4",
};
