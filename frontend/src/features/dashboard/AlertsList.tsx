import type { DashboardAlert } from "@/lib/api";
import { cn } from "@/lib/utils";
import { ScoreBar } from "@/components/ScoreBar";
import { categoryStatusMeta } from "@/features/dashboard/categoryStatus";

export function AlertsList({ alerts }: { alerts: DashboardAlert[] }) {
  if (alerts.length === 0) {
    return <p className="text-sm text-muted">Nothing needs attention this month.</p>;
  }

  return (
    <ul className="flex flex-col gap-2">
      {alerts.map((alert) => (
        <li
          key={`${alert.categoryName}-${alert.metricDefinitionId}`}
          className={cn(
            "flex items-center justify-between gap-4 rounded-md border border-border border-l-4 bg-background/40 px-3 py-2",
            categoryStatusMeta[alert.status].borderClassName,
          )}
        >
          <div className="flex flex-col gap-0.5">
            <span className="text-[10px] font-medium uppercase tracking-wide text-muted">{alert.categoryName}</span>
            <span className="text-sm font-medium">{alert.message}</span>
          </div>
          {alert.score !== null && <ScoreBar score={alert.score} />}
        </li>
      ))}
    </ul>
  );
}
