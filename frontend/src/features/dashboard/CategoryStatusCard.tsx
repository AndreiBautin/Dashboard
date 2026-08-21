import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import type { CategorySummary } from "@/lib/api";
import { categoryStatusMeta } from "@/features/dashboard/categoryStatus";
import { HealthScoreRing } from "@/features/dashboard/HealthScoreRing";
import { MetricTrendRow } from "@/features/dashboard/MetricTrendRow";

export function CategoryStatusCard({ summary }: { summary: CategorySummary }) {
  const meta = categoryStatusMeta[summary.status];

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>{summary.categoryName}</CardTitle>
          <span className={cn("h-2 w-2 rounded-full", meta.dotClassName)} />
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <div className="flex items-center gap-3">
          <HealthScoreRing score={summary.score} size="sm" />
          <p className="text-xs text-muted">{meta.label}</p>
        </div>
        <div className="flex flex-col gap-3">
          {summary.metrics.map((metric) => (
            <MetricTrendRow key={metric.metricDefinitionId} metric={metric} />
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
