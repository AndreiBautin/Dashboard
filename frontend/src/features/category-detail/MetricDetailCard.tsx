import { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { RatingTierIndicator } from "@/components/RatingTierIndicator";
import { RatingScale } from "@/components/RatingScale";
import { ScoreBar } from "@/components/ScoreBar";
import { cn } from "@/lib/utils";
import { fetchMetricTrend, type MetricDetail, type MetricTrendPoint } from "@/lib/api";
import { formatMetricValue } from "@/lib/formatValue";
import { metricStatusMeta } from "@/lib/metricStatus";
import { TrendChart } from "@/features/dashboard/TrendChart";

export function MetricDetailCard({ metric }: { metric: MetricDetail }) {
  const [points, setPoints] = useState<MetricTrendPoint[] | null>(null);
  const meta = metricStatusMeta[metric.status];

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

  const delta =
    points && points.length >= 2 ? points[points.length - 1].value - points[points.length - 2].value : null;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>
            {metric.metricName}
            {metric.isCalculated && <span className="ml-2 text-xs text-muted">(calculated)</span>}
          </CardTitle>
          <span className={cn("h-2 w-2 rounded-full", meta.dotClassName)} />
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <div>
          <div className="flex items-baseline gap-2">
            <p className="text-2xl font-semibold tabular-nums">
              {metric.latestValue !== null ? formatMetricValue(metric.latestValue, metric.unit) : "—"}{" "}
              {metric.unit !== "USD" && <span className="text-sm font-normal text-muted">{metric.unit}</span>}
            </p>
            {metric.ratingLabel && metric.ratingTier && (
              <RatingTierIndicator tier={metric.ratingTier} label={metric.ratingLabel} />
            )}
          </div>
          <p className="text-xs text-muted">
            {delta !== null
              ? `${delta >= 0 ? "▲" : "▼"} ${Math.abs(delta).toFixed(2)} since last month`
              : meta.label}
          </p>
          {metric.ratingDescription && <p className="mt-1 text-xs text-muted">{metric.ratingDescription}</p>}
          {metric.ratingBands && (
            <RatingScale bands={metric.ratingBands} unit={metric.unit} activeLabel={metric.ratingLabel} />
          )}
          {metric.score !== null && (
            <div className="mt-2">
              <ScoreBar score={metric.score} />
            </div>
          )}
        </div>
        {points === null ? <p className="text-xs text-muted">Loading…</p> : <TrendChart points={points} />}
      </CardContent>
    </Card>
  );
}
