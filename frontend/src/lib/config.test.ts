import { describe, expect, it } from "vitest";
import {
  buildConfig,
  normalizeBasePath,
  parseApiBaseUrl,
  parseDataSource,
  toRouterBasename,
} from "@/lib/config";

/**
 * Configuration is the one part of this app where a silent mistake is worse
 * than a crash. Two failures matter, and both are covered here:
 *
 *  - selecting the *wrong* data source, which would point a build at the
 *    wrong backend without anyone noticing, and
 *  - a base path that disagrees with itself, which ships a site whose assets
 *    load and whose every route 404s.
 */

describe("parseDataSource", () => {
  it("accepts both valid values", () => {
    expect(parseDataSource("api", [])).toBe("api");
    expect(parseDataSource("demo", [])).toBe("demo");
  });

  it("is forgiving about case and surrounding whitespace", () => {
    expect(parseDataSource("DEMO", [])).toBe("demo");
    expect(parseDataSource("  Demo  ", [])).toBe("demo");
  });

  it("defaults to api when unset, without warning about it", () => {
    const warnings: string[] = [];
    expect(parseDataSource(undefined, warnings)).toBe("api");
    expect(parseDataSource("", warnings)).toBe("api");
    expect(warnings).toEqual([]);
  });

  it.each(["demoo", "prod", "true", "1", "ap i"])(
    "falls back to api and warns for %o rather than guessing",
    (raw) => {
      const warnings: string[] = [];

      // The specific risk: "demoo" must never be read as "demo". Shipping a
      // demo build against someone's real data, or the reverse, is exactly
      // the mistake a helpful fuzzy match would cause.
      expect(parseDataSource(raw, warnings)).toBe("api");
      expect(warnings).toHaveLength(1);
      expect(warnings[0]).toContain(raw);
    },
  );

  it.each([null, 42, {}, [], true])("never throws on the non-string %o", (raw) => {
    const warnings: string[] = [];
    expect(() => parseDataSource(raw, warnings)).not.toThrow();
    expect(parseDataSource(raw, warnings)).toBe("api");
  });
});

describe("parseApiBaseUrl", () => {
  it("keeps a valid URL and strips trailing slashes", () => {
    expect(parseApiBaseUrl("https://api.example.com", [])).toBe("https://api.example.com");
    expect(parseApiBaseUrl("https://api.example.com///", [])).toBe("https://api.example.com");
  });

  it.each(["not a url", "://broken", "example.com"])(
    "falls back to the documented default for %o",
    (raw) => {
      const warnings: string[] = [];
      expect(parseApiBaseUrl(raw, warnings)).toBe("http://localhost:5199");
      expect(warnings).toHaveLength(1);
    },
  );

  it("never throws, whatever it is handed", () => {
    for (const raw of [undefined, null, 0, {}, [], Symbol.iterator.toString()]) {
      expect(() => parseApiBaseUrl(raw, [])).not.toThrow();
    }
  });
});

describe("normalizeBasePath", () => {
  it.each([
    ["/Dashboard/", "/Dashboard/"],
    ["/Dashboard", "/Dashboard/"],
    ["Dashboard", "/Dashboard/"],
    ["Dashboard/", "/Dashboard/"],
    ["  /Dashboard  ", "/Dashboard/"],
  ])("normalizes %o to %o", (raw, expected) => {
    expect(normalizeBasePath(raw)).toBe(expected);
  });

  it.each([undefined, null, "", "/", 42, {}])("treats %o as the site root", (raw) => {
    expect(normalizeBasePath(raw)).toBe("/");
  });
});

describe("toRouterBasename", () => {
  it("drops the trailing slash React Router does not want", () => {
    expect(toRouterBasename("/Dashboard/")).toBe("/Dashboard");
  });

  it("maps the site root to an empty basename", () => {
    expect(toRouterBasename("/")).toBe("");
  });

  it("agrees with normalizeBasePath for every accepted spelling", () => {
    // The property that matters: however the base path is written, the
    // bundler's base and the router's basename describe the same location.
    for (const raw of ["/Dashboard/", "/Dashboard", "Dashboard", "Dashboard/"]) {
      expect(`${toRouterBasename(raw)}/`).toBe(normalizeBasePath(raw));
    }
  });
});

describe("buildConfig", () => {
  it("produces a complete config from an empty environment", () => {
    const config = buildConfig({});

    expect(config).toMatchObject({
      dataSource: "api",
      apiBaseUrl: "http://localhost:5199",
      basePath: "/",
      commit: "dev",
      builtAt: "dev",
      warnings: [],
    });
  });

  it("collects every warning rather than failing on the first", () => {
    const config = buildConfig({ VITE_DATA_SOURCE: "nope", VITE_API_BASE_URL: "also nope" });

    expect(config.warnings).toHaveLength(2);
    expect(config.dataSource).toBe("api");
    expect(config.apiBaseUrl).toBe("http://localhost:5199");
  });

  it("reads the demo data source and base path a Pages build would set", () => {
    const config = buildConfig({ VITE_DATA_SOURCE: "demo", BASE_URL: "/Dashboard/" });

    expect(config.dataSource).toBe("demo");
    expect(config.basePath).toBe("/Dashboard/");
    expect(config.warnings).toEqual([]);
  });

  it("cannot be made to throw", () => {
    // Total by construction: startup must never die on a bad env var.
    const hostile: Record<string, unknown> = {
      VITE_DATA_SOURCE: { toString: () => { throw new Error("boom"); } },
      VITE_API_BASE_URL: [Symbol("x")],
      BASE_URL: 12345,
      VITE_COMMIT: null,
      VITE_BUILT_AT: undefined,
    };

    expect(() => buildConfig(hostile)).not.toThrow();
    expect(buildConfig(hostile).dataSource).toBe("api");
  });
});
