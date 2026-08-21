import { cn } from "@/lib/utils";
import { formatMetricValue } from "@/lib/formatValue";
import type { RatingBand } from "@/lib/api";

/**
 * The full cutoff-to-label breakdown for a rated metric (e.g. "≤$5,000
 * Starting · ≤$15,000 Building · ≤$25,000 Almost There · >$25,000 Fully
 * Funded"), with whichever band currently applies highlighted. Answers "what
 * does it actually take to reach the next tier" and "is the math doing what
 * I'd expect" without a trip to the Settings page -- the same threshold
 * values shown there are surfaced right next to the metric they describe.
 */
export function RatingScale({
  bands,
  unit,
  activeLabel,
}: {
  bands: RatingBand[];
  unit: string;
  activeLabel: string | null;
}) {
  return (
    <div className="mt-1.5 flex flex-wrap items-center gap-1 text-[11px]">
      {bands.map((band, index) => {
        const isActive = band.label === activeLabel;
        const rangeText =
          band.upToValue !== null
            ? `≤${formatMetricValue(band.upToValue, unit)}`
            : `>${formatMetricValue(bands[index - 1]?.upToValue ?? 0, unit)}`;

        return (
          <span
            key={band.label}
            className={cn(
              "rounded px-1.5 py-0.5 tabular-nums",
              isActive ? "bg-accent/15 font-medium text-accent" : "text-muted",
            )}
            title={`${rangeText}: ${band.label}`}
          >
            {rangeText} {band.label}
          </span>
        );
      })}
    </div>
  );
}
