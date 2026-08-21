import { config } from "@/lib/config";
import { httpApi } from "@/lib/adapters/httpApi";
import { demoApi } from "@/lib/adapters/demoApi";
import type { DashboardApi } from "@/lib/apiContract";

/**
 * The app's single data-access module. Every feature imports from here and
 * none of them know, or can find out, which backend is answering.
 *
 * Two adapters implement the same contract:
 *
 * - `httpApi` — the real application. Talks to `Dashboard.Api` over HTTP/JSON,
 *   backed by PostgreSQL. The default, and what runs in local development.
 * - `demoApi` — the public demo. Runs the same .NET Application and Domain
 *   assemblies in the browser via WebAssembly, backed by in-memory
 *   repositories and a generated fixture.
 *
 * Selection happens once, here, from configuration — not per call site and
 * not from a runtime check. That keeps the choice auditable: there is exactly
 * one line in the codebase that decides which backend a build talks to.
 */

const backend: DashboardApi = config.dataSource === "demo" ? demoApi : httpApi;

// Re-exported so `import type { DashboardSummary } from "@/lib/api"` keeps
// working exactly as it did before the adapters were split out.
export type * from "@/lib/apiTypes";

export const fetchDashboardSummary = () => backend.fetchDashboardSummary();

export const fetchMetricTrend = (metricDefinitionId: number) => backend.fetchMetricTrend(metricDefinitionId);

export const fetchCategories = () => backend.fetchCategories();

export const fetchCategoryDetail = (categoryId: number) => backend.fetchCategoryDetail(categoryId);

/** Records (or updates) one month's values for a subset of a category's metrics. */
export const submitCategoryEntries = (categoryId: number, month: string, values: Record<number, number>) =>
  backend.submitCategoryEntries(categoryId, month, values);

export const fetchSocialSummary = () => backend.fetchSocialSummary();

export const fetchSocialTrend = () => backend.fetchSocialTrend();

export const addFriend = (name: string, lastHangoutDate: string, notes: string | null) =>
  backend.addFriend(name, lastHangoutDate, notes);

export const logHangout = (friendId: number, date: string) => backend.logHangout(friendId, date);

export const logKeyRelationshipContact = (keyRelationshipId: number, date: string) =>
  backend.logKeyRelationshipContact(keyRelationshipId, date);

export const fetchSettings = () => backend.fetchSettings();

export const updateSetting = (key: string, value: string) => backend.updateSetting(key, value);

/**
 * `null` unless this build is running against the demo backend. The UI reads
 * it to decide whether to offer a reset control at all, rather than showing
 * one that fails.
 */
export const resetDemoData = backend.resetDemoData;

/** True when the app is running the in-browser demo rather than a real API. */
export const isDemo = config.dataSource === "demo";
