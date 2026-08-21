import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { logHangout, type FriendSummary } from "@/lib/api";

function todayIso(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
}

export function FriendRow({ friend, rank, onLogged }: { friend: FriendSummary; rank: number; onLogged: () => void }) {
  const [isLogging, setIsLogging] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleLogHangout() {
    setError(null);
    setIsLogging(true);
    try {
      await logHangout(friend.friendId, todayIso());
      onLogged();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to log this hangout.");
    } finally {
      setIsLogging(false);
    }
  }

  return (
    <div className="-mx-2 flex items-center justify-between gap-4 rounded-md border-b border-border px-2 py-3 transition-colors last:border-b-0 hover:bg-background/40">
      <div className="flex items-start gap-3">
        <span
          className={cn(
            "mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[10px] font-semibold tabular-nums",
            rank === 1 ? "bg-danger/15 text-danger" : "bg-background text-muted",
          )}
          title={`#${rank} most overdue for a hangout`}
        >
          {rank}
        </span>
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-2">
            <span className={cn("h-2 w-2 rounded-full", friend.isFlaggedOverdue ? "bg-danger" : friend.isActive ? "bg-success" : "bg-muted")} />
            <span className="text-sm font-medium">{friend.name}</span>
            {friend.isFlaggedOverdue && <span className="text-xs text-danger">Overdue</span>}
          </div>
          <p className="text-xs text-muted">
            {friend.daysSinceLastHangout === 0 ? "Hung out today" : `${friend.daysSinceLastHangout} days since last hangout`}
            {friend.notes ? ` · ${friend.notes}` : ""}
          </p>
          {error && <p className="text-xs text-danger">{error}</p>}
        </div>
      </div>
      <Button variant="outline" size="sm" disabled={isLogging} onClick={handleLogHangout}>
        {isLogging ? "Logging…" : "Log hangout"}
      </Button>
    </div>
  );
}
