import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Modal } from "@/components/ui/modal";
import { ScoreBar } from "@/components/ScoreBar";
import { RatingScale } from "@/components/RatingScale";
import { cn } from "@/lib/utils";
import { fetchSocialSummary, fetchSocialTrend, type MetricTrendPoint, type SocialCircleRating, type SocialSummary } from "@/lib/api";
import { categoryStatusMeta } from "@/features/dashboard/categoryStatus";
import { HealthScoreRing } from "@/features/dashboard/HealthScoreRing";
import { TrendChart } from "@/features/dashboard/TrendChart";
import { AddFriendForm } from "@/features/social/AddFriendForm";
import { FriendRow } from "@/features/social/FriendRow";
import { KeyRelationshipRow } from "@/features/social/KeyRelationshipRow";
import type { FriendSummary } from "@/lib/api";

const ratingBadgeClassName: Record<SocialCircleRating, string> = {
  Thin: "bg-danger/15 text-danger",
  Healthy: "bg-warning/15 text-warning",
  Robust: "bg-success/15 text-success",
  Expansive: "bg-accent/15 text-accent",
};

/**
 * Dense ranking off daysSinceLastHangout (already sorted descending by the
 * backend): friends tied on the same number of days share the same rank
 * rather than being arbitrarily split into consecutive numbers, since two
 * people equally overdue are equally worth prioritizing.
 */
function friendRanks(friends: FriendSummary[]): { friend: FriendSummary; rank: number }[] {
  let rank = 0;
  let previousDays: number | null = null;

  return friends.map((friend) => {
    if (friend.daysSinceLastHangout !== previousDays) {
      rank += 1;
      previousDays = friend.daysSinceLastHangout;
    }
    return { friend, rank };
  });
}

export function SocialPage() {
  const [summary, setSummary] = useState<SocialSummary | null>(null);
  const [trend, setTrend] = useState<MetricTrendPoint[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isAddFriendOpen, setIsAddFriendOpen] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [summaryData, trendData] = await Promise.all([fetchSocialSummary(), fetchSocialTrend()]);
      setSummary(summaryData);
      setTrend(trendData);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load your social circle.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const header = (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight">Social</h1>
      <p className="text-sm text-muted">Your active circle and hangout rankings.</p>
    </div>
  );

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6">
        {header}
        <p className="text-sm text-muted">Loading…</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col gap-6">
        {header}
        <Card>
          <CardHeader>
            <CardTitle>Couldn't reach the API</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted">
              {error} Make sure the backend is running at the address configured in{" "}
              <code>VITE_API_BASE_URL</code> (defaults to http://localhost:5199).
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  const statusMeta = categoryStatusMeta[summary!.status];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center gap-6">
        <HealthScoreRing score={summary!.score} />
        <div className="flex flex-col gap-1">
          {header}
          <span className="inline-flex w-fit items-center gap-1.5 text-xs font-medium">
            <span className={cn("h-2 w-2 rounded-full", statusMeta.dotClassName)} />
            {statusMeta.label}
          </span>
        </div>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Active circle</CardTitle>
            <span className={cn("inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium", ratingBadgeClassName[summary!.rating])}>
              {summary!.rating}
            </span>
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex items-end justify-between gap-4">
            <div>
              <p className="text-4xl font-semibold tabular-nums">{summary!.activeFriendCount}</p>
              <p className="text-xs text-muted">
                {summary!.activeFriendCountDelta !== null
                  ? `${summary!.activeFriendCountDelta >= 0 ? "▲" : "▼"} ${Math.abs(summary!.activeFriendCountDelta)} since last month`
                  : "Not enough history yet"}
              </p>
            </div>
            <ScoreBar score={summary!.ratingScore} />
          </div>
          <p className="text-xs text-muted">{summary!.ratingDescription}</p>
          <RatingScale bands={summary!.ratingBands} unit="friends" activeLabel={summary!.rating} />
          {trend && trend.length > 0 ? <TrendChart points={trend} /> : null}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Circle upkeep</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex items-end justify-between gap-4">
            <div>
              <p className="text-4xl font-semibold tabular-nums">
                {summary!.maintenanceScore !== null ? `${summary!.maintenanceScore}%` : "—"}
              </p>
              <p className="text-xs text-muted">
                {summary!.maintenanceScore !== null
                  ? "of your active circle isn't overdue for a hangout"
                  : "No active circle yet to maintain"}
              </p>
            </div>
            {summary!.maintenanceScore !== null && <ScoreBar score={summary!.maintenanceScore} />}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Key relationships</CardTitle>
          <p className="text-xs text-muted">The people who always come first, tracked separately from the circle above.</p>
        </CardHeader>
        <CardContent>
          {summary!.keyRelationships.length === 0 ? (
            <p className="text-sm text-muted">Nothing to show yet.</p>
          ) : (
            <div className="flex flex-col">
              {summary!.keyRelationships.map((relationship) => (
                <KeyRelationshipRow key={relationship.keyRelationshipId} relationship={relationship} onLogged={load} />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Friends</CardTitle>
              <p className="mt-1 text-xs text-muted">
                Ranked by longest since you've last hung out -- prioritize making plans with friends closer to the top.
              </p>
            </div>
            <Button size="sm" onClick={() => setIsAddFriendOpen(true)}>
              + Add friend
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {summary!.friends.length === 0 ? (
            <p className="text-sm text-muted">Add a friend to start tracking hangouts.</p>
          ) : (
            <div className="flex flex-col">
              {friendRanks(summary!.friends).map(({ friend, rank }) => (
                <FriendRow key={friend.friendId} friend={friend} rank={rank} onLogged={load} />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Modal isOpen={isAddFriendOpen} onClose={() => setIsAddFriendOpen(false)} title="Add a friend">
        <AddFriendForm
          onAdded={() => {
            setIsAddFriendOpen(false);
            load();
          }}
        />
      </Modal>
    </div>
  );
}
