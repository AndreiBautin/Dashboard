import { useEffect, useState } from "react";
import { fetchMetricTrend, type MetricSummary, type MetricTrendPoint } from "@/lib/api";
import { ScoreBar } from "@/components/ScoreBar";
import { metricStatusMeta } from "@/lib/metricStatus";
import { TrendChart } from "@/features/dashboard/TrendChart";

export function MetricTrendRow({ metric }: { metric: MetricSummary }) {
  const [points, setPoints] = useState<MetricTrendPoint[] | null>(null);

  useEffect(() => {
    let cancelled = false;

    fetchMetricTrend(metric.metricDefinitionId)
      .then((data) => {
        if (!cancelled) setPoints(data);
      })
      .catch(() => {
        if (!cancelled) setPoints([]);
      });

    return () => {
      cancelled = true;
    };
  }, [metric.metricDefinitionId]);

  return (
    <div className="-mx-2 flex flex-col gap-1 rounded-md px-2 py-1 transition-colors hover:bg-background/40">
      <div className="flex items-center justify-between text-xs text-muted">
        <span>{metric.metricName}</span>
        {metric.score !== null ? <ScoreBar score={metric.score} /> : <span>{metricStatusMeta[metric.status].label}</span>}
      </div>
      {points === null ? (
        <p className="text-xs text-muted">Loading…</p>
      ) : (
        <TrendChart points={points} />
      )}
    </div>
  );
}
