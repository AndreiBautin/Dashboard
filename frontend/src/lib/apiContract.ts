import type {
  AppSettingSummary,
  Category,
  CategoryDetail,
  DashboardSummary,
  MetricTrendPoint,
  SocialSummary,
} from "@/lib/apiTypes";

/**
 * The set of operations the app needs from a backend, stated once so both
 * adapters are checked against it.
 *
 * This exists to make the two implementations provably interchangeable. Add a
 * feature to the HTTP adapter and forget the demo one, and the build fails
 * here rather than at runtime on the deployed page — which is the only place
 * that particular mistake would otherwise show up.
 */
export interface DashboardApi {
  fetchDashboardSummary(): Promise<DashboardSummary>;

  fetchMetricTrend(metricDefinitionId: number): Promise<MetricTrendPoint[]>;

  fetchCategories(): Promise<Category[]>;

  fetchCategoryDetail(categoryId: number): Promise<CategoryDetail>;

  /** Records (or updates) one month's values for a subset of a category's metrics. */
  submitCategoryEntries(categoryId: number, month: string, values: Record<number, number>): Promise<void>;

  fetchSocialSummary(): Promise<SocialSummary>;

  fetchSocialTrend(): Promise<MetricTrendPoint[]>;

  addFriend(name: string, lastHangoutDate: string, notes: string | null): Promise<void>;

  logHangout(friendId: number, date: string): Promise<void>;

  logKeyRelationshipContact(keyRelationshipId: number, date: string): Promise<void>;

  fetchSettings(): Promise<AppSettingSummary[]>;

  updateSetting(key: string, value: string): Promise<void>;

  /**
   * Restores the demo fixture, discarding anything the visitor changed.
   *
   * `null` on any adapter backed by real persistence. Typed as a nullable
   * capability rather than an always-present method that throws, so a caller
   * has to check for it — which is what lets the UI show the reset control
   * only where it genuinely exists.
   */
  resetDemoData: (() => Promise<void>) | null;
}
