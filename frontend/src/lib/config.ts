/**
 * Every value that differs between local development, the deployed demo, and
 * a self-hosted build, resolved in one place.
 *
 * Two properties matter here and are covered by `config.test.ts`:
 *
 * - **Parsing is total.** No input, however malformed, can throw. A typo in
 *   an environment variable degrades to the documented default and reports a
 *   warning; it never takes the app down at startup, and — more importantly —
 *   it never silently selects a *different* mode. `VITE_DATA_SOURCE=Demo`
 *   works; `VITE_DATA_SOURCE=demoo` falls back to `api` and says so, rather
 *   than quietly shipping a demo build to someone's real data or vice versa.
 *
 * - **The base path has exactly one source.** A project page is served from
 *   `/<repo>/`, and if the bundler's base and the router's basename are
 *   configured separately they drift — the classic result being assets that
 *   load and a 404 on every route. Vite's `base` is set from
 *   `VITE_BASE_PATH` at build time and exposed at runtime as
 *   `import.meta.env.BASE_URL`, which is what the router reads. One value,
 *   derived twice.
 */

export type DataSource = "api" | "demo";

export interface AppConfig {
  /** Where the app's data comes from: the HTTP API, or the in-browser demo runtime. */
  dataSource: DataSource;
  /** Base URL of the ASP.NET Core API. Unused when `dataSource` is `demo`. */
  apiBaseUrl: string;
  /** Path the app is served from, always with a trailing slash. Router basename. */
  basePath: string;
  /** Short commit SHA of the build, or `"dev"` when built outside CI. */
  commit: string;
  /** ISO timestamp of the build, or `"dev"` when built outside CI. */
  builtAt: string;
  /** Anything that could not be parsed, so the app can surface it rather than hide it. */
  warnings: string[];
}

const DEFAULT_API_BASE_URL = "http://localhost:5199";

export function parseDataSource(raw: unknown, warnings: string[]): DataSource {
  if (raw === undefined || raw === null || raw === "") {
    return "api";
  }

  if (typeof raw !== "string") {
    warnings.push(`VITE_DATA_SOURCE was not a string; falling back to "api".`);
    return "api";
  }

  const normalized = raw.trim().toLowerCase();
  if (normalized === "api" || normalized === "demo") {
    return normalized;
  }

  // Deliberately not a "did you mean" guess. Selecting the wrong data source
  // is exactly the mistake this must never make on the user's behalf.
  warnings.push(`VITE_DATA_SOURCE="${raw}" is not "api" or "demo"; falling back to "api".`);
  return "api";
}

export function parseApiBaseUrl(raw: unknown, warnings: string[]): string {
  if (raw === undefined || raw === null || raw === "") {
    return DEFAULT_API_BASE_URL;
  }

  if (typeof raw !== "string") {
    warnings.push(`VITE_API_BASE_URL was not a string; falling back to ${DEFAULT_API_BASE_URL}.`);
    return DEFAULT_API_BASE_URL;
  }

  const trimmed = raw.trim().replace(/\/+$/, "");

  try {
    // Constructing a URL is the only reliable validity check, and it throws —
    // so it is wrapped here rather than at the call site.
    new URL(trimmed);
    return trimmed;
  } catch {
    warnings.push(`VITE_API_BASE_URL="${raw}" is not a valid URL; falling back to ${DEFAULT_API_BASE_URL}.`);
    return DEFAULT_API_BASE_URL;
  }
}

/**
 * Normalizes a base path to the leading-and-trailing-slash form both Vite and
 * React Router expect. Exported so `vite.config.ts` can use the identical
 * function at build time rather than reimplementing the rule.
 */
export function normalizeBasePath(raw: unknown): string {
  if (typeof raw !== "string") {
    return "/";
  }

  const trimmed = raw.trim();
  if (trimmed === "" || trimmed === "/") {
    return "/";
  }

  const withLeading = trimmed.startsWith("/") ? trimmed : `/${trimmed}`;
  return withLeading.endsWith("/") ? withLeading : `${withLeading}/`;
}

/**
 * React Router wants a basename *without* the trailing slash (it treats "/"
 * and "" as the same root), while Vite wants one *with* it. Same value,
 * two required shapes — converted here rather than at each call site.
 */
export function toRouterBasename(basePath: string): string {
  const normalized = normalizeBasePath(basePath);
  return normalized === "/" ? "" : normalized.slice(0, -1);
}

export function buildConfig(env: Record<string, unknown>): AppConfig {
  const warnings: string[] = [];

  return {
    dataSource: parseDataSource(env.VITE_DATA_SOURCE, warnings),
    apiBaseUrl: parseApiBaseUrl(env.VITE_API_BASE_URL, warnings),
    // BASE_URL is set by Vite itself from `base`, so it is already normalized
    // and needs no fallback warning of its own.
    basePath: normalizeBasePath(env.BASE_URL ?? "/"),
    commit: typeof env.VITE_COMMIT === "string" && env.VITE_COMMIT !== "" ? env.VITE_COMMIT : "dev",
    builtAt: typeof env.VITE_BUILT_AT === "string" && env.VITE_BUILT_AT !== "" ? env.VITE_BUILT_AT : "dev",
    warnings,
  };
}

export const config: AppConfig = buildConfig(import.meta.env as unknown as Record<string, unknown>);

// Surfaced once at startup rather than swallowed. A misconfigured deployment
// should be visible to whoever opens the console, not silently wrong.
for (const warning of config.warnings) {
  console.warn(`[dashboard config] ${warning}`);
}
