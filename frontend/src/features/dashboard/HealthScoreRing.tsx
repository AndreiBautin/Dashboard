import { cn } from "@/lib/utils";

const RADIUS = 54;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

const SIZE_CLASSES = {
  lg: { wrapper: "h-32 w-32", value: "text-3xl", caption: "text-xs" },
  sm: { wrapper: "h-16 w-16", value: "text-lg", caption: "text-[10px]" },
} as const;

/**
 * The same ring used for the big Dashboard overall score also renders at
 * "sm" size inside each category card, so a category's score is a filled
 * ring rather than a bare number -- visually consistent, just scaled down
 * to fit alongside a card's other content.
 */
export function HealthScoreRing({ score, size = "lg" }: { score: number | null; size?: "lg" | "sm" }) {
  const offset = CIRCUMFERENCE - ((score ?? 0) / 100) * CIRCUMFERENCE;
  const classes = SIZE_CLASSES[size];

  return (
    <div className={cn("relative flex shrink-0 items-center justify-center", classes.wrapper)}>
      <svg className="h-full w-full -rotate-90" viewBox="0 0 120 120">
        <circle
          cx="60"
          cy="60"
          r={RADIUS}
          fill="none"
          stroke="currentColor"
          strokeWidth="8"
          className="text-border"
        />
        {score !== null && (
          <circle
            cx="60"
            cy="60"
            r={RADIUS}
            fill="none"
            stroke="currentColor"
            strokeWidth="8"
            strokeLinecap="round"
            strokeDasharray={CIRCUMFERENCE}
            strokeDashoffset={offset}
            className="text-accent transition-[stroke-dashoffset] duration-700 ease-out"
          />
        )}
      </svg>
      <div className="absolute flex flex-col items-center">
        <span className={cn("font-semibold tabular-nums", classes.value)}>{score ?? "—"}</span>
        {size === "lg" && <span className={cn("text-muted", classes.caption)}>/ 100</span>}
      </div>
    </div>
  );
}
