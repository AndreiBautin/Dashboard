import { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { fetchDashboardSummary, type DashboardSummary } from "@/lib/api";
import { HealthScoreRing } from "@/features/dashboard/HealthScoreRing";
import { CategoryStatusCard } from "@/features/dashboard/CategoryStatusCard";
import { SocialStatusCard } from "@/features/dashboard/SocialStatusCard";
import { AlertsList } from "@/features/dashboard/AlertsList";
import { categoryStatusMeta } from "@/features/dashboard/categoryStatus";

export function DashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    fetchDashboardSummary()
      .then((data) => {
        if (!cancelled) setSummary(data);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load the dashboard.");
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const header = (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight">Monthly Executive Review</h1>
      <p className="text-sm text-muted">How you're doing, at a glance.</p>
    </div>
  );

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6">
        {header}
        <p className="text-sm text-muted">Loading your monthly review…</p>
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

  if (!summary || summary.categories.length === 0) {
    return (
      <div className="flex flex-col gap-6">
        {header}
        <Card>
          <CardHeader>
            <CardTitle>No categories yet</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted">
              Fitness, Finance, and Social get their own modules in later phases.
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-8">
      {header}

      <div className="relative overflow-hidden rounded-2xl border border-border bg-card p-6 sm:p-8">
        <div
          className={cn(
            "pointer-events-none absolute -right-16 -top-20 h-64 w-64 rounded-full opacity-20 blur-3xl",
            categoryStatusMeta[summary.overallStatus].glowClassName,
          )}
        />
        <div className="relative flex flex-wrap items-center gap-8">
          <div className="flex flex-col items-center gap-3">
            <HealthScoreRing score={summary.overallScore} />
            <span
              className={cn(
                "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium",
                categoryStatusMeta[summary.overallStatus].badgeClassName,
              )}
            >
              <span className={cn("h-1.5 w-1.5 rounded-full", categoryStatusMeta[summary.overallStatus].dotClassName)} />
              {categoryStatusMeta[summary.overallStatus].label}
            </span>
          </div>

          <div className="hidden h-24 w-px bg-border sm:block" />

          <div className="grid flex-1 grid-cols-1 gap-3 sm:grid-cols-3">
            {summary.categories.map((category) => (
              <div
                key={category.categoryId}
                className="flex items-center justify-between gap-3 rounded-xl border border-border bg-background/40 px-4 py-3"
              >
                <div className="flex flex-col gap-1.5">
                  <span className="text-xs font-medium uppercase tracking-wide text-muted">{category.categoryName}</span>
                  <span
                    className={cn(
                      "inline-flex w-fit items-center gap-1.5 rounded-full px-2 py-0.5 text-[11px] font-medium",
                      categoryStatusMeta[category.status].badgeClassName,
                    )}
                  >
                    <span className={cn("h-1.5 w-1.5 rounded-full", categoryStatusMeta[category.status].dotClassName)} />
                    {categoryStatusMeta[category.status].label}
                  </span>
                </div>
                <span className="text-2xl font-semibold tabular-nums">{category.score ?? "—"}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Needs Attention</CardTitle>
        </CardHeader>
        <CardContent>
          <AlertsList alerts={summary.alerts} />
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {summary.categories.map((category) =>
          category.categoryName === "Social" ? (
            <SocialStatusCard key={category.categoryId} summary={category} />
          ) : (
            <CategoryStatusCard key={category.categoryId} summary={category} />
          ),
        )}
      </div>
    </div>
  );
}
