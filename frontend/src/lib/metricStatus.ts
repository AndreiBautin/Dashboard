import type { MetricStatus } from "@/lib/api";

export const metricStatusMeta: Record<MetricStatus, { dotClassName: string; label: string }> = {
  Improved: { dotClassName: "bg-success", label: "Improved" },
  Stagnant: { dotClassName: "bg-warning", label: "Stalled" },
  Regressed: { dotClassName: "bg-danger", label: "Declined" },
  InsufficientData: { dotClassName: "bg-muted", label: "Not enough history" },
};
