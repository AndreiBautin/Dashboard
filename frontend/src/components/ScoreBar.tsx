import { cn } from "@/lib/utils";

// Mirrors CategoryStatusCalculator's graduated four-tier thresholds on the
// backend (Struggling/NeedsAttention/OnTrack/Excelling), so a metric's bar
// color always agrees with what its category's overall status would be at
// that same score.
const STRUGGLING_MAX = 49;
const NEEDS_ATTENTION_MAX = 74;
const ON_TRACK_MAX = 89;

function colorClassNameFor(score: number): string {
  if (score <= STRUGGLING_MAX) return "bg-danger";
  if (score <= NEEDS_ATTENTION_MAX) return "bg-warning";
  if (score <= ON_TRACK_MAX) return "bg-accent";
  return "bg-success";
}

/**
 * A small filled progress bar for a metric's 0-100 score -- more legible at
 * a glance than a bare percent number, since the filled fraction and color
 * both carry meaning without requiring you to do the math on the number
 * yourself.
 */
export function ScoreBar({ score }: { score: number }) {
  const clamped = Math.max(0, Math.min(100, score));

  return (
    <span
      className="inline-flex items-center gap-1.5"
      title={`Contributes ${score}/100 to this category's score`}
    >
      <span className="h-1.5 w-14 overflow-hidden rounded-full bg-border">
        <span
          className={cn("block h-full rounded-full transition-[width] duration-500 ease-out", colorClassNameFor(clamped))}
          style={{ width: `${clamped}%` }}
        />
      </span>
      <span className="text-xs font-medium tabular-nums text-muted">{score}</span>
    </span>
  );
}
