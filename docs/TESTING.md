# Testing

**278 tests.** 241 backend (xUnit), 37 frontend (Vitest).

```bash
dotnet test backend/Vantage.sln     # 241
cd frontend && npm test             # 37
```

---

## Where the work started

Worth recording, because the starting point is part of the story:

| | Before | After |
| --- | --- | --- |
| Backend total | 211 | 241 |
| Backend passing | **203** | **241** |
| Backend failing | **8** | **0** |
| Frontend total | **1** | **37** |

The eight failures were two separate problems, and separating them mattered
more than fixing them:

- **Seven** were caused by uncommitted work in progress. `KnownAppSettings.cs`
  retuned the category-status cutoffs from 49/74/89 to 25/50/75 and raised the
  strength standards; the tests still asserted the old numbers. That change was
  deliberate and well reasoned, so **the tests were updated to match it** —
  the retune was preserved, not reverted.
- **One** predated it and failed on a clean checkout:
  `DashboardEndpointTests.GetDashboard_ReturnsASummaryReflectingTheSeededData`
  expected an overall score of 100 and got 50.

That last one is worth dwelling on, because the tempting fix was wrong. Its
comment claimed Social is excluded from the average when it has no data. But
`SocialService` says the opposite, explicitly: *"even 0 friends is a real, if
low, point on the scale"*, and `SocialSummary.Score` is non-nullable, so Social
always counts. The **test's expectation was stale**, not the behaviour. Changing
`DashboardService` to match the comment would have been changing product
behaviour to satisfy an out-of-date assertion. The test was corrected and the
reasoning written into it.

---

## Strategy per layer

### Domain (119 tests)

Pure functions and invariants, tested with no infrastructure at all — which is
possible because `Vantage.Domain` has zero dependencies.

Each of the five evaluators gets its own file covering improvement, regression,
stagnation, and insufficient history. `MetricRatingCalculatorTests` covers the
continuous 0–100 scale including the inverted `HigherIsBetter: false` case and
behaviour past the top cutoff. Entity tests pin the invariants: constructors
reject empty names, `MetricDefinition` rejects a strategy/config mismatch, and
`LogHangout` / `LogContact` only ratchet forward.

### Application (76 tests)

Services against hand-written fakes in `Metrics/Fakes/` and `Social/Fakes/`.
Fakes rather than a mocking framework: the interfaces are four methods each, so
a fake is shorter than the setup code for a mock and reads as data.

Covers score blending, threshold mapping, alert selection (trend-based wording
winning over score-based for the same metric), Social's two facets being
alerted independently, and settings genuinely flowing through to results.

### API (16 tests)

`WebApplicationFactory` over SQLite in-memory, exercising real routing, real
model binding, and real JSON serialization. Enough to catch a broken route or a
serialization contract change; not a second copy of the service tests.

`DashboardEndpointTests` deliberately holds **one** test method: the factory's
SQLite connection is shared for the fixture's lifetime, so a second `[Fact]`
would re-seed on top of the first test's data. That constraint is documented in
the class itself.

### Demo (30 tests)

The layer the public deployment depends on, so it is tested from two angles.

**That the fixture is safe** — privacy scans, seeding safety, determinism,
staleness. Detailed in [DEMO_DATA.md](DEMO_DATA.md#the-tests-that-enforce-this).

**That the real services work against in-memory repositories.**
`DemoWorkspaceTests` drives `DashboardService`, `CategoryDetailService`,
`SocialService`, `MetricEntryService`, `FriendService` and `SettingsService`
through the same composition root the API uses, asserting on real outputs:
recording this month's entry flips Overhead Press from Regressed to Improved;
logging a hangout clears an overdue flag; narrowing the active-circle setting
shrinks the circle. If these pass, the deployed demo is running the same logic
as the API rather than an approximation.

### Frontend (37 tests)

Concentrated on configuration, because that is where a silent mistake is worst.

`config.test.ts` covers the two failures that actually matter: selecting the
**wrong data source** (a build pointed at the wrong backend, which nobody
notices until it is too late), and a **base path that disagrees with itself**
(assets that load, routes that all 404). It asserts that parsing is total —
hostile input, non-strings, an object whose `toString` throws — and that
`VITE_DATA_SOURCE=demoo` falls back to `api` with a warning rather than being
helpfully read as `demo`.

---

## The properties this deployment depends on

Introduced by the productionization work, and pinned so they cannot regress:

| Property | Test |
| --- | --- |
| The demo fixture contains nothing personal | `DemoDatasetPrivacyTests` (5 patterns + the specific figures and names once committed here) |
| Seeding cannot overwrite existing data | `FillIfEmpty_WillNotTouchAStoreHoldingDataItDidNotSeed` |
| Reset is complete and identity-stable | `ResetAndFill_ReplacesEverythingAndReproducesTheFixtureExactly` |
| Config parsing cannot crash | `buildConfig` "cannot be made to throw" |
| A typo cannot enable the wrong mode | `parseDataSource` falls back and warns for every near-miss |
| Base path and router basename agree | `toRouterBasename` "agrees with normalizeBasePath for every accepted spelling" |
| The fixture cannot go stale | `SeededDatesMoveWithToday_SoTheFixtureCannotGoStale` |
| The real services work in the browser's runtime | `DemoWorkspaceTests` (12 tests) |
| The shipped bundle contains the runtime | CI asserts `dotnet.js`, `dotnet.native.wasm`, `404.html`, `.nojekyll` exist |
| The live site actually answers | Deploy smoke test — status, body, runtime assets, deep link |

---

## Deliberately not tested

This section is the honest half.

- **No React component tests.** The components are presentational: they map a
  status to a CSS class and render a number. Testing them would mostly assert
  that Tailwind class names have not changed, which is a rename detector, not a
  bug detector. The logic worth testing was deliberately kept out of them.
- **No end-to-end browser suite.** Playwright over this app would be slow,
  flaky, and would largely re-cover what `DemoWorkspaceTests` covers
  deterministically in milliseconds. The workflows *were* verified in a real
  browser during this work — dashboard render, deep-link routing, and a full
  write round-trip — and the deploy smoke test checks the live site on every
  push. What is missing is a *regression* net for UI behaviour, and that is a
  real gap rather than a solved problem.
- **No tests for `Vantage.Wasm`.** It is a thin interop façade with no logic;
  everything it calls is covered by `DemoWorkspaceTests`. The part that could
  genuinely break — trimming removing a serializer — cannot be caught by a unit
  test on the host platform. The deploy smoke test covers it instead, by
  fetching the runtime from the live site.
- **No EF Core repository tests against real PostgreSQL.** They would test EF
  Core, which is already tested. The SQLite-backed API tests cover the mapping
  and query shapes that are genuinely this project's.
- **No load or performance testing.** Single user, monthly use, a handful of
  rows. There is no performance requirement to verify.
- **No tests for `start-app.bat`.** A Windows launcher script, verified by
  running it.
- **No coverage threshold.** Deliberate. A number turns into tests written to
  raise the number, which are exactly the tests that do not catch bugs. What
  matters here is that the scoring engine, the trust boundaries, the
  destructive operations, and the deployment's load-bearing assumptions are
  covered — and they are.

---

## Test helpers

| Helper | Purpose |
| --- | --- |
| `Metrics/Fakes/*`, `Social/Fakes/*` | Hand-written in-memory repositories with a `Seed(...)` method. Shorter than mock setup, and readable as data. |
| `SqliteWebApplicationFactory` | `WebApplicationFactory` over a shared in-memory SQLite connection, with `InitializeDatabaseAsync(...)` for per-class seeding. |
| `CustomWebApplicationFactory` | For tests that must not touch a database at all — supplies a placeholder connection string so DI resolves without connecting. |
| `DemoStore` internals | Exposed to `Vantage.Demo.Tests` via `InternalsVisibleTo`, so seeding-safety tests can insert a record directly and prove seeding refuses to touch it. |
| `DemoSeederTests.Snapshot(store)` | Renders the whole store as a stable comparable string, so "nothing changed" assertions cover everything rather than the one collection the author remembered. |

---

## A note on the clock

Several services call `DateTime.UtcNow` directly rather than taking an injected
clock. This surfaced during the work: a demo test pinned to a hardcoded
`2026-08-20` failed because UTC had already rolled over to the 21st while local
time had not.

The tests were changed to read the same clock the services do, which makes them
honest but leaves the underlying issue: **date-sensitive behaviour is not
deterministically testable, and the app behaves differently either side of UTC
midnight**. The user-visible symptom is documented in
[DEPLOYMENT.md](DEPLOYMENT.md#known-issues).

Introducing a `TimeProvider` through the Application layer is the correct fix
and a genuinely worthwhile next change. It was left alone here because it
touches every date-aware service and belongs in a change of its own, not
smuggled into a deployment task.
