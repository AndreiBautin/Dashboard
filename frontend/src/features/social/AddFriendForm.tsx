import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { addFriend } from "@/lib/api";

function todayIso(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
}

export function AddFriendForm({ onAdded }: { onAdded: () => void }) {
  const [name, setName] = useState("");
  const [lastHangoutDate, setLastHangoutDate] = useState(todayIso());
  const [notes, setNotes] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    if (name.trim() === "") {
      setError("Enter a name before saving.");
      return;
    }

    setIsSaving(true);
    try {
      await addFriend(name.trim(), lastHangoutDate, notes.trim() === "" ? null : notes.trim());
      setName("");
      setNotes("");
      setLastHangoutDate(todayIso());
      onAdded();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add this friend.");
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <label className="flex flex-col gap-1 text-sm">
        <span>Name</span>
        <input
          type="text"
          autoFocus
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="h-9 rounded-md border border-border bg-transparent px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>Last hangout</span>
        <input
          type="date"
          value={lastHangoutDate}
          onChange={(event) => setLastHangoutDate(event.target.value)}
          className="h-9 rounded-md border border-border bg-transparent px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>Notes (optional)</span>
        <input
          type="text"
          value={notes}
          onChange={(event) => setNotes(event.target.value)}
          className="h-9 rounded-md border border-border bg-transparent px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        />
      </label>
      {error && <p className="text-xs text-danger">{error}</p>}
      <Button type="submit" disabled={isSaving} className="self-end">
        {isSaving ? "Saving…" : "Add friend"}
      </Button>
    </form>
  );
}
