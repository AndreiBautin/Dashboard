import { useState } from "react";
import { Button } from "@/components/ui/button";
import { ScoreBar } from "@/components/ScoreBar";
import { cn } from "@/lib/utils";
import { logKeyRelationshipContact, type KeyRelationshipSummary } from "@/lib/api";

function todayIso(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
}

/**
 * Mirrors FriendRow, but for the fixed key relationships (Date with Wife,
 * Visited Mother) -- no rank badge (there's no "most overdue" ordering
 * across just two fixed rows) and no notes, since these aren't part of the
 * open-ended Friends list.
 */
export function KeyRelationshipRow({
  relationship,
  onLogged,
}: {
  relationship: KeyRelationshipSummary;
  onLogged: () => void;
}) {
  const [isLogging, setIsLogging] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleLogContact() {
    setError(null);
    setIsLogging(true);
    try {
      await logKeyRelationshipContact(relationship.keyRelationshipId, todayIso());
      onLogged();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to log this.");
    } finally {
      setIsLogging(false);
    }
  }

  return (
    <div className="-mx-2 flex items-center justify-between gap-4 rounded-md border-b border-border px-2 py-3 transition-colors last:border-b-0 hover:bg-background/40">
      <div className="flex flex-col gap-1">
        <div className="flex items-center gap-2">
          <span className={cn("h-2 w-2 rounded-full", relationship.isFlaggedOverdue ? "bg-danger" : "bg-success")} />
          <span className="text-sm font-medium">{relationship.label}</span>
          {relationship.isFlaggedOverdue && <span className="text-xs text-danger">Overdue</span>}
        </div>
        <p className="text-xs text-muted">
          {relationship.daysSinceLastContact === 0 ? "Done today" : `${relationship.daysSinceLastContact} days ago`}
        </p>
        {error && <p className="text-xs text-danger">{error}</p>}
      </div>
      <div className="flex items-center gap-3">
        <ScoreBar score={relationship.score} />
        <Button variant="outline" size="sm" disabled={isLogging} onClick={handleLogContact}>
          {isLogging ? "Logging…" : "Log"}
        </Button>
      </div>
    </div>
  );
}
