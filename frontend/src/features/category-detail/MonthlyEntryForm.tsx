import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { submitCategoryEntries, type MetricDetail } from "@/lib/api";

function currentMonthIso(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-01`;
}

export function MonthlyEntryForm({
  categoryId,
  metrics,
  onSaved,
}: {
  categoryId: number;
  metrics: MetricDetail[];
  onSaved: () => void;
}) {
  const editableMetrics = metrics.filter((metric) => !metric.isCalculated);
  const calculatedMetrics = metrics.filter((metric) => metric.isCalculated);

  const [values, setValues] = useState<Record<number, string>>(() =>
    Object.fromEntries(
      editableMetrics.map((metric) => [metric.metricDefinitionId, metric.currentMonthValue?.toString() ?? ""]),
    ),
  );
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    const parsed: Record<number, number> = {};
    for (const metric of editableMetrics) {
      const raw = values[metric.metricDefinitionId];
      if (raw === undefined || raw.trim() === "") {
        continue;
      }

      const value = Number(raw);
      if (Number.isNaN(value)) {
        setError(`"${raw}" isn't a valid number for ${metric.metricName}.`);
        return;
      }

      parsed[metric.metricDefinitionId] = value;
    }

    if (Object.keys(parsed).length === 0) {
      setError("Enter at least one value before saving.");
      return;
    }

    setIsSaving(true);
    try {
      await submitCategoryEntries(categoryId, currentMonthIso(), parsed);
      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save this month's numbers.");
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {editableMetrics.map((metric) => (
        <label key={metric.metricDefinitionId} className="flex flex-col gap-1 text-sm">
          <span>
            {metric.metricName} <span className="text-xs text-muted">({metric.unit})</span>
          </span>
          <input
            type="number"
            step="any"
            inputMode="decimal"
            autoFocus={metric === editableMetrics[0]}
            value={values[metric.metricDefinitionId] ?? ""}
            onChange={(event) =>
              setValues((prev) => ({ ...prev, [metric.metricDefinitionId]: event.target.value }))
            }
            placeholder="Not yet entered"
            className="h-9 rounded-md border border-border bg-transparent px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          />
        </label>
      ))}
      {calculatedMetrics.length > 0 && (
        <p className="text-xs text-muted">
          {calculatedMetrics.map((metric) => metric.metricName).join(", ")} calculated automatically from the values above.
        </p>
      )}
      {error && <p className="text-xs text-danger">{error}</p>}
      <Button type="submit" disabled={isSaving} className="self-end">
        {isSaving ? "Saving…" : "Save"}
      </Button>
    </form>
  );
}
