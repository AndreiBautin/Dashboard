import { config } from "@/lib/config";
import type {
  AppSettingSummary,
  Category,
  CategoryDetail,
  DashboardSummary,
  MetricTrendPoint,
  SocialSummary,
} from "@/lib/apiTypes";
import type { DashboardApi } from "@/lib/apiContract";

/**
 * Talks to the ASP.NET Core API over HTTP/JSON. This is the real app: the
 * one that reads and writes PostgreSQL, and the default in local development
 * and any self-hosted deployment.
 *
 * Behaviour is unchanged from before the demo adapter existed — the same
 * requests, the same error messages. It moved into a file of its own only so
 * that a second implementation could sit beside it.
 */

const baseUrl = config.apiBaseUrl;

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`);

  if (!response.ok) {
    throw new Error(`Request to ${path} failed with status ${response.status}`);
  }

  return (await response.json()) as T;
}

async function send(path: string, method: "POST" | "PUT", body: unknown, failureMessage: string): Promise<void> {
  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    // The API returns a plain-text reason for validation failures (see
    // SettingsController), so prefer it over the generic message when present.
    const message = await response.text();
    throw new Error(message || failureMessage);
  }
}

export const httpApi: DashboardApi = {
  fetchDashboardSummary: () => getJson<DashboardSummary>("/api/dashboard"),

  fetchMetricTrend: (metricDefinitionId) =>
    getJson<MetricTrendPoint[]>(`/api/metrics/${metricDefinitionId}/trend`),

  fetchCategories: () => getJson<Category[]>("/api/categories"),

  fetchCategoryDetail: (categoryId) => getJson<CategoryDetail>(`/api/categories/${categoryId}`),

  submitCategoryEntries: (categoryId, month, values) =>
    send(
      `/api/categories/${categoryId}/entries`,
      "POST",
      { month, values },
      `Submitting entries for category ${categoryId} failed`,
    ),

  fetchSocialSummary: () => getJson<SocialSummary>("/api/social"),

  fetchSocialTrend: () => getJson<MetricTrendPoint[]>("/api/social/trend"),

  addFriend: (name, lastHangoutDate, notes) =>
    send("/api/social/friends", "POST", { name, lastHangoutDate, notes }, "Adding friend failed"),

  logHangout: (friendId, date) =>
    send(
      `/api/social/friends/${friendId}/hangouts`,
      "POST",
      { date },
      `Logging hangout for friend ${friendId} failed`,
    ),

  logKeyRelationshipContact: (keyRelationshipId, date) =>
    send(
      `/api/social/key-relationships/${keyRelationshipId}/log`,
      "POST",
      { date },
      `Logging contact for key relationship ${keyRelationshipId} failed`,
    ),

  fetchSettings: () => getJson<AppSettingSummary[]>("/api/settings"),

  updateSetting: (key, value) =>
    send(`/api/settings/${encodeURIComponent(key)}`, "PUT", { value }, `Updating setting "${key}" failed`),

  // The HTTP adapter deliberately has no reset: there is no such thing as
  // resetting someone's real data, and an adapter that silently no-ops would
  // be worse than one that says so.
  resetDemoData: null,
};
