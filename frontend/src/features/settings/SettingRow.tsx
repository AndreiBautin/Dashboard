import { useState } from "react";
import { Button } from "@/components/ui/button";
import { updateSetting, type AppSettingSummary } from "@/lib/api";

export function SettingRow({ setting, onSaved }: { setting: AppSettingSummary; onSaved: () => void }) {
  const [value, setValue] = useState(setting.value);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isDirty = value !== setting.value;

  async function handleSave() {
    setError(null);
    setIsSaving(true);
    try {
      await updateSetting(setting.key, value);
      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save this setting.");
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div className="flex flex-col gap-2 border-b border-border py-4 last:border-b-0">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-sm font-medium">{setting.label}</p>
          <p className="text-xs text-muted">{setting.description}</p>
          {value !== setting.defaultValue && (
            <p className="text-xs text-muted">Default: {setting.defaultValue}</p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <input
            type="text"
            value={value}
            onChange={(event) => setValue(event.target.value)}
            className="h-9 w-28 rounded-md border border-border bg-transparent px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          />
          <Button variant="outline" size="sm" disabled={!isDirty || isSaving} onClick={handleSave}>
            {isSaving ? "Saving…" : "Save"}
          </Button>
        </div>
      </div>
      {error && <p className="text-xs text-danger">{error}</p>}
    </div>
  );
}
