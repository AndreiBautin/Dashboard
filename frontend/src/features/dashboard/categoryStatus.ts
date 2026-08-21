import type { CategoryStatus } from "@/lib/api";

interface CategoryStatusMeta {
  /** Small solid dot, used wherever status is shown inline with text. */
  dotClassName: string;
  label: string;
  /** Tinted pill background + matching text color, for standalone status badges. */
  badgeClassName: string;
  /** Left-edge accent border, for card/row-style containers. */
  borderClassName: string;
  /** Solid fill, meant to be used at low opacity for background glows/accents. */
  glowClassName: string;
}

// A graduated, four-tier gradient (worst to best) rather than a flat
// good/bad split -- mirrors the same treatment Social's circle-size rating
// already gets (Thin/Healthy/Robust/Expansive). Colors read Struggling (red)
// -> NeedsAttention (amber) -> OnTrack (indigo) -> Excelling (green), so two
// scores in different tiers are always visually distinguishable, not just
// differently worded.
export const categoryStatusMeta: Record<CategoryStatus, CategoryStatusMeta> = {
  Struggling: {
    dotClassName: "bg-danger",
    label: "Struggling",
    badgeClassName: "bg-danger/15 text-danger",
    borderClassName: "border-l-danger",
    glowClassName: "bg-danger",
  },
  NeedsAttention: {
    dotClassName: "bg-warning",
    label: "Needs attention",
    badgeClassName: "bg-warning/15 text-warning",
    borderClassName: "border-l-warning",
    glowClassName: "bg-warning",
  },
  OnTrack: {
    dotClassName: "bg-accent",
    label: "On track",
    badgeClassName: "bg-accent/15 text-accent",
    borderClassName: "border-l-accent",
    glowClassName: "bg-accent",
  },
  Excelling: {
    dotClassName: "bg-success",
    label: "Excelling",
    badgeClassName: "bg-success/15 text-success",
    borderClassName: "border-l-success",
    glowClassName: "bg-success",
  },
  NoData: {
    dotClassName: "bg-muted",
    label: "No data yet",
    badgeClassName: "bg-muted/15 text-muted",
    borderClassName: "border-l-border",
    glowClassName: "bg-muted",
  },
};
