import { useCallback, useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { fetchSettings, type AppSettingSummary } from "@/lib/api";
import { SettingRow } from "@/features/settings/SettingRow";

/** Groups settings by section, preserving each section's first-seen order from the API. */
function groupBySection(settings: AppSettingSummary[]): { section: string; settings: AppSettingSummary[] }[] {
  const groups: { section: string; settings: AppSettingSummary[] }[] = [];
  const indexBySection = new Map<string, number>();

  for (const setting of settings) {
    const existingIndex = indexBySection.get(setting.section);
    if (existingIndex === undefined) {
      indexBySection.set(setting.section, groups.length);
      groups.push({ section: setting.section, settings: [setting] });
    } else {
      groups[existingIndex].settings.push(setting);
    }
  }

  return groups;
}

export function SettingsPage() {
  const [settings, setSettings] = useState<AppSettingSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setSettings(await fetchSettings());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load settings.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const header = (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight">Settings</h1>
      <p className="text-sm text-muted">Config values used across the app, editable without a code change.</p>
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

  const sections = groupBySection(settings!);

  return (
    <div className="flex flex-col gap-6">
      {header}
      {sections.map(({ section, settings: sectionSettings }) => (
        <Card key={section}>
          <CardHeader>
            <CardTitle>{section}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-col">
              {sectionSettings.map((setting) => (
                <SettingRow key={setting.key} setting={setting} onSaved={load} />
              ))}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
