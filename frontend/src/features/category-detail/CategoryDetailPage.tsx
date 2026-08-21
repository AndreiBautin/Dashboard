import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Modal } from "@/components/ui/modal";
import { cn } from "@/lib/utils";
import { fetchCategories, fetchCategoryDetail, type CategoryDetail } from "@/lib/api";
import { categoryStatusMeta } from "@/features/dashboard/categoryStatus";
import { HealthScoreRing } from "@/features/dashboard/HealthScoreRing";
import { MetricDetailCard } from "@/features/category-detail/MetricDetailCard";
import { MonthlyEntryForm } from "@/features/category-detail/MonthlyEntryForm";

/**
 * Generic "how's this category doing, and here's this month's entry form"
 * page, shared by Fitness and Finance (and, once it has metric-shaped data
 * rather than the Friend model, potentially Social). Resolves the category
 * by name via GET /api/categories rather than a hardcoded id, since
 * categories are data, not code.
 */
export function CategoryDetailPage({
  categoryName,
  tagline,
}: {
  categoryName: string;
  tagline: string;
}) {
  const [detail, setDetail] = useState<CategoryDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isEntryModalOpen, setIsEntryModalOpen] = useState(false);
  // Bumped after every successful reload and passed down so each metric card
  // refetches its own trend. Without it, saving this month's entry updates
  // the values and scores while the charts and deltas keep showing last
  // month's data.
  const [reloadToken, setReloadToken] = useState(0);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const categories = await fetchCategories();
      const category = categories.find((c) => c.name === categoryName);
      if (!category) {
        setDetail(null);
        return;
      }

      setDetail(await fetchCategoryDetail(category.id));
      setReloadToken((token) => token + 1);
    } catch (err) {
      setError(err instanceof Error ? err.message : `Failed to load ${categoryName}.`);
    } finally {
      setIsLoading(false);
    }
  }, [categoryName]);

  useEffect(() => {
    load();
  }, [load]);

  const header = (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight">{categoryName}</h1>
      <p className="text-sm text-muted">{tagline}</p>
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

  if (!detail || detail.metrics.length === 0) {
    return (
      <div className="flex flex-col gap-6">
        {header}
        <Card>
          <CardHeader>
            <CardTitle>No {categoryName} metrics yet</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted">
              Add a {categoryName} category and at least one metric definition to start tracking.
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  const statusMeta = categoryStatusMeta[detail.status];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-6">
        <div className="flex flex-wrap items-center gap-6">
          <HealthScoreRing score={detail.score} />
          <div className="flex flex-col gap-1">
            {header}
            <span className="inline-flex w-fit items-center gap-1.5 text-xs font-medium">
              <span className={cn("h-2 w-2 rounded-full", statusMeta.dotClassName)} />
              {statusMeta.label}
            </span>
          </div>
        </div>
        <Button onClick={() => setIsEntryModalOpen(true)}>+ Add this month's numbers</Button>
      </div>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        {detail.metrics.map((metric) => (
          <MetricDetailCard key={metric.metricDefinitionId} metric={metric} reloadToken={reloadToken} />
        ))}
      </div>
      <Modal isOpen={isEntryModalOpen} onClose={() => setIsEntryModalOpen(false)} title="This month's numbers">
        <MonthlyEntryForm
          categoryId={detail.categoryId}
          metrics={detail.metrics}
          onSaved={() => {
            setIsEntryModalOpen(false);
            load();
          }}
        />
      </Modal>
    </div>
  );
}
