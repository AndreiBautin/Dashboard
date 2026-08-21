import { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import {
  fetchSocialSummary,
  fetchSocialTrend,
  type CategorySummary,
  type MetricTrendPoint,
  type SocialSummary,
} from "@/lib/api";
import { categoryStatusMeta } from "@/features/dashboard/categoryStatus";
import { HealthScoreRing } from "@/features/dashboard/HealthScoreRing";
import { TrendChart } from "@/features/dashboard/TrendChart";
import { ScoreBar } from "@/components/ScoreBar";

/**
 * Social doesn't have MetricDefinition-backed metrics (it's Friend data, not
 * a Category row), so its CategorySummary from the Dashboard endpoint always
 * arrives with an empty Metrics list -- rendering it through the generic
 * CategoryStatusCard left it looking bare next to Fitness/Finance's metric
 * rows. This fetches Social's own richer data (rating + trend + maintenance
 * score, same as the Social page) so the card shows two comparable rows:
 * circle size (Active Circle) and how well it's kept up (Circle Upkeep).
 */
export function SocialStatusCard({ summary }: { summary: CategorySummary }) {
  const [social, setSocial] = useState<SocialSummary | null>(null);
  const [trend, setTrend] = useState<MetricTrendPoint[] | null>(null);

  useEffect(() => {
    let cancelled = false;

    Promise.all([fetchSocialSummary(), fetchSocialTrend()])
      .then(([socialData, trendData]) => {
        if (!cancelled) {
          setSocial(socialData);
          setTrend(trendData);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setSocial(null);
          setTrend([]);
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

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
          <div className="flex flex-col gap-1">
            <div className="flex items-center justify-between text-xs text-muted">
              <span>Active Circle</span>
              {social && <ScoreBar score={social.ratingScore} />}
            </div>
            {trend === null ? <p className="text-xs text-muted">Loading…</p> : <TrendChart points={trend} />}
          </div>
          <div className="flex flex-col gap-1">
            <div className="flex items-center justify-between text-xs text-muted">
              <span>Circle Upkeep</span>
              {social?.maintenanceScore != null && <ScoreBar score={social.maintenanceScore} />}
            </div>
            {social && social.maintenanceScore === null && (
              <p className="text-xs text-muted">No active circle yet to maintain.</p>
            )}
          </div>
          {social?.keyRelationships.map((keyRelationship) => (
            <div key={keyRelationship.keyRelationshipId} className="flex items-center justify-between text-xs text-muted">
              <span>{keyRelationship.label}</span>
              <ScoreBar score={keyRelationship.score} />
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
