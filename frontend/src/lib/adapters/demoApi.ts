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
 * Runs the application's real .NET Application and Domain assemblies in the
 * browser, compiled to WebAssembly, with in-memory repositories standing in
 * for PostgreSQL.
 *
 * This is not a mock and it is not a reimplementation. `DashboardService`,
 * `MetricRatingCalculator`, `SocialService` and every evaluator execute here
 * exactly as they do behind the HTTP API — same IL, same code paths. The only
 * substitution is at the persistence seam. That is why the deployed demo can
 * be trusted to behave like the real thing, and why there is no second
 * scoring implementation to drift out of sync.
 *
 * The cost is an up-front download of the .NET runtime, so the module is
 * loaded lazily and only once, on the first call.
 */

interface DemoExports {
  Initialize(): string;
  Reset(): string;
  GetDashboard(): string;
  GetCategories(): string;
  GetCategoryDetail(categoryId: number): string;
  GetMetricTrend(metricDefinitionId: number): string;
  GetSocial(): string;
  GetSocialTrend(): string;
  GetSettings(): string;
  RecordEntries(categoryId: number, month: string, valuesJson: string): string;
  AddFriend(name: string, lastHangoutDate: string, notes: string | null): string;
  LogHangout(friendId: number, date: string): string;
  LogKeyRelationshipContact(keyRelationshipId: number, date: string): string;
  UpdateSetting(key: string, value: string): string;
}

interface DemoEnvelope<T> {
  ok: boolean;
  data?: T;
  error?: string;
}

let runtime: Promise<DemoExports> | null = null;

/**
 * Resolved at runtime from `BASE_URL` rather than baked in at build time, so
 * the same bundle works whether it is served from `/` locally or from
 * `/<repo>/` on a project page. The dynamic import is `@vite-ignore`d because
 * the URL is only known at runtime and must not be rewritten into the bundle
 * graph — the .NET runtime resolves its own assets relative to this file's
 * location.
 */
async function loadRuntime(): Promise<DemoExports> {
  const url = `${config.basePath}dashboard-wasm/_framework/dotnet.js`;
  const { dotnet } = (await import(/* @vite-ignore */ url)) as {
    dotnet: {
      create(): Promise<{
        // Exports are nested by namespace then type: Dashboard → Wasm → DemoApi.
        getAssemblyExports(name: string): Promise<{ Dashboard: { Wasm: { DemoApi: DemoExports } } }>;
        getConfig(): { mainAssemblyName: string };
      }>;
    };
  };

  const { getAssemblyExports, getConfig } = await dotnet.create();
  const assembly = await getAssemblyExports(getConfig().mainAssemblyName);
  const api = assembly.Dashboard.Wasm.DemoApi;

  const info = unwrap<{ categories: number; metrics: number; months: number; friends: number }>(api.Initialize());
  console.info(
    `[dashboard] demo runtime ready — ${info.categories} categories, ${info.metrics} metrics, ` +
      `${info.months} months, ${info.friends} friends`,
  );

  return api;
}

function exports(): Promise<DemoExports> {
  runtime ??= loadRuntime();
  return runtime;
}

/**
 * Turns the façade's envelope back into ordinary promise semantics: a
 * resolved value, or a thrown `Error` carrying the message the .NET side
 * produced. Callers cannot tell this apart from a failed `fetch`, which is
 * the point — nothing above the adapter should branch on which backend is in
 * use.
 */
function unwrap<T>(raw: string): T {
  let envelope: DemoEnvelope<T>;

  try {
    envelope = JSON.parse(raw) as DemoEnvelope<T>;
  } catch {
    throw new Error("The demo runtime returned a malformed response.");
  }

  if (!envelope.ok) {
    throw new Error(envelope.error ?? "The demo runtime reported an unknown failure.");
  }

  return envelope.data as T;
}

async function read<T>(call: (api: DemoExports) => string): Promise<T> {
  return unwrap<T>(call(await exports()));
}

async function write(call: (api: DemoExports) => string): Promise<void> {
  unwrap<null>(call(await exports()));
}

export const demoApi: DashboardApi = {
  fetchDashboardSummary: () => read<DashboardSummary>((api) => api.GetDashboard()),

  fetchMetricTrend: (metricDefinitionId) =>
    read<MetricTrendPoint[]>((api) => api.GetMetricTrend(metricDefinitionId)),

  fetchCategories: () => read<Category[]>((api) => api.GetCategories()),

  fetchCategoryDetail: (categoryId) => read<CategoryDetail>((api) => api.GetCategoryDetail(categoryId)),

  submitCategoryEntries: (categoryId, month, values) =>
    // Sent as JSON rather than a marshalled object: JS interop marshals only
    // a small set of primitives, and serializing here keeps the payload
    // byte-identical to what the HTTP adapter would POST.
    write((api) => api.RecordEntries(categoryId, month, JSON.stringify(values))),

  fetchSocialSummary: () => read<SocialSummary>((api) => api.GetSocial()),

  fetchSocialTrend: () => read<MetricTrendPoint[]>((api) => api.GetSocialTrend()),

  addFriend: (name, lastHangoutDate, notes) =>
    write((api) => api.AddFriend(name, lastHangoutDate, notes)),

  logHangout: (friendId, date) => write((api) => api.LogHangout(friendId, date)),

  logKeyRelationshipContact: (keyRelationshipId, date) =>
    write((api) => api.LogKeyRelationshipContact(keyRelationshipId, date)),

  fetchSettings: () => read<AppSettingSummary[]>((api) => api.GetSettings()),

  updateSetting: (key, value) => write((api) => api.UpdateSetting(key, value)),

  resetDemoData: () => write((api) => api.Reset()),
};
