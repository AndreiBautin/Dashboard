import { cn } from "@/lib/utils";
import type { MetricRatingTier } from "@/lib/api";

const TIER_ORDER: MetricRatingTier[] = ["Tier1", "Tier2", "Tier3", "Tier4"];

const tierColorClassName: Record<MetricRatingTier, string> = {
  Tier1: "bg-danger",
  Tier2: "bg-warning",
  Tier3: "bg-success",
  Tier4: "bg-accent",
};

/**
 * A small 4-segment scale (worst to best, left to right) shown alongside a
 * rating label like "Growing" or "Elite" -- the label alone doesn't convey
 * where it falls without already knowing that metric's specific vocabulary,
 * but the filled-segment count plus a consistent worst-to-best color ramp
 * reads at a glance regardless of which metric it's for.
 */
export function RatingTierIndicator({ tier, label }: { tier: MetricRatingTier; label: string }) {
  const filledCount = TIER_ORDER.indexOf(tier) + 1;

  return (
    <span
      className="inline-flex items-center gap-1.5 text-xs font-medium"
      title={`${label} (${filledCount} of ${TIER_ORDER.length})`}
    >
      <span className="inline-flex items-center gap-0.5">
        {TIER_ORDER.map((segment, index) => (
          <span
            key={segment}
            className={cn(
              "h-1.5 w-1.5 rounded-full",
              index < filledCount ? tierColorClassName[tier] : "bg-border",
            )}
          />
        ))}
      </span>
      {label}
    </span>
  );
}
