# Architecture

Written to be read in ten minutes and explained out loud in three.

---

## Shape

Four .NET projects plus two that exist only for the demo, and a React SPA.

```
┌──────────────────────────────────────────────────────────────────────────┐
│  frontend/            React 19 · Vite · Tailwind · React Router          │
│                                                                          │
│      lib/api.ts  ──  one contract, two adapters                          │
│           ├── adapters/httpApi.ts   ── fetch ──►  Dashboard.Api            │
│           └── adapters/demoApi.ts   ── interop ►  Dashboard.Wasm           │
└──────────────────────────────────────────────────────────────────────────┘
                  │                                    │
                  ▼                                    ▼
┌──────────────────────────────┐      ┌───────────────────────────────────┐
│  Dashboard.Api                 │      │  Dashboard.Wasm                     │
│  controllers, composition    │      │  [JSExport] façade                │
│  root, health checks, CORS   │      │  (browser only)                   │
└──────────────────────────────┘      └───────────────────────────────────┘
                  │                                    │
                  ▼                                    ▼
┌──────────────────────────────┐      ┌───────────────────────────────────┐
│  Dashboard.Infrastructure      │      │  Dashboard.Demo                     │
│  EF Core · Npgsql            │      │  in-memory repositories           │
│  repository implementations  │      │  + the generated fixture          │
└──────────────────────────────┘      └───────────────────────────────────┘
                  │                                    │
                  └────────────────┬───────────────────┘
                                   ▼
                  ┌──────────────────────────────────┐
                  │  Dashboard.Application             │
                  │  services · DTOs                 │
                  │  repository INTERFACES           │
                  └──────────────────────────────────┘
                                   │
                                   ▼
                  ┌──────────────────────────────────┐
                  │  Dashboard.Domain                  │
                  │  entities · evaluators · rating  │
                  │  ZERO dependencies               │
                  └──────────────────────────────────┘
```

Dependencies point inward, always. `Dashboard.Domain` references no project and
no NuGet package at all. `Dashboard.Application` references Domain plus
`Microsoft.Extensions.DependencyInjection.Abstractions` and nothing else — in
particular, no EF Core, no ASP.NET Core.

---

## What each layer is responsible for

**`Dashboard.Domain`** — the rules that would still be true if the app had no
database and no HTTP. Entities (`Category`, `MetricDefinition`,
`MonthlySnapshot`, `Friend`, `KeyRelationship`), the five evaluation
strategies, and the rating maths. Entities protect their own invariants:
`Id` and every property have private setters, constructors validate, and
`MetricSnapshot` can only be created through its aggregate root
(`MonthlySnapshot.AddMetricSnapshot`). `Friend.LogHangout` only ever moves the
date *forward*, so a late entry can't overwrite a more recent one.

**`Dashboard.Application`** — use cases. One service per question the app
answers: `DashboardService` ("how am I doing overall?"), `CategoryDetailService`,
`SocialService`, `MetricEntryService`, `SettingsService`. It also *declares*
what it needs from persistence — seven repository interfaces plus `IUnitOfWork`
— without knowing what implements them.

**`Dashboard.Infrastructure`** — the EF Core implementations of those interfaces,
the `DbContext`, entity configurations, migrations, and the database health
check. The only project that knows PostgreSQL exists.

**`Dashboard.Api`** — HTTP. Controllers are deliberately thin: parse, call one
service, return. `Program.cs` is the composition root.

**`Dashboard.Demo`** — in-memory implementations of the same seven interfaces,
plus `DemoDataset` (the generated fixture). Referenced by Infrastructure too,
so local dev seeding and the public demo share one fixture.

**`Dashboard.Wasm`** — a `[JSExport]` façade mirroring the API's endpoints, so
the browser can call the same services directly. Contains no logic; anything
here would be logic the real API doesn't run.

---

## A request, end to end

**Loading the dashboard.** Real files, in order.

1. **`frontend/src/features/dashboard/DashboardPage.tsx`**
   A `useEffect` calls `fetchDashboardSummary()`. The component knows nothing
   about HTTP, URLs, or WebAssembly — only that it gets a `DashboardSummary`.

2. **`frontend/src/lib/api.ts`**
   Picks the backend once, from configuration:
   `const backend = config.dataSource === "demo" ? demoApi : httpApi`.
   This is the only line in the codebase that decides which backend a build
   talks to.

3. **`frontend/src/lib/adapters/httpApi.ts`** *(the real app)*
   `GET {apiBaseUrl}/api/dashboard`. A non-OK response becomes a thrown
   `Error`, which the page renders as an error state.

   *In the deployed demo, `adapters/demoApi.ts` runs instead: it lazily boots
   the .NET runtime and calls `DemoApi.GetDashboard()` across JS interop. From
   step 5 onward, everything below is identical.*

4. **`backend/src/Dashboard.Api/Controllers/DashboardController.cs`**
   Nine lines of body. Calls `_dashboardService.GetSummaryAsync(ct)` and wraps
   it in `Ok(...)`. No logic lives here — that's the point of it being this
   short.

5. **`backend/src/Dashboard.Application/Dashboard/DashboardService.cs`**
   The real work:
   - `ICategoryRepository.GetAllAsync()` → `EfCategoryRepository` → PostgreSQL.
   - `CategoryStatusCalculator.GetThresholdsAsync()` reads the three score
     cutoffs via `AppSettingReader`, which falls back to each setting's
     declared default when it isn't stored or won't parse.
   - For each metric: `MetricEvaluationService.EvaluateAsync(metric.Id)` →
     loads the snapshot history → `MetricEvaluatorFactory.GetEvaluator(strategy)`
     → e.g. `IncreaseMetricEvaluator.Evaluate(snapshots, config)` → a
     `MetricStatus`.
   - `MetricScoring.GetScoreAsync(...)` turns that into 0–100 — from the trend
     when there's history, and from the metric's *rated level* when there
     isn't, so month one isn't a wall of "no data".
   - Alerts are collected per metric, naming what is drifting.
   - `SocialService.GetSummaryAsync()` supplies Social as a synthetic category
     (id `-1`), scored by Social itself rather than recomputed here — one
     source of truth for a number shown in two places.
   - Category scores are averaged into the overall score.

6. **Back out.** `DashboardSummary` → `Ok()` → serialized camelCase with
   string enums (configured in `Program.cs`) → typed as `DashboardSummary` in
   `frontend/src/lib/apiTypes.ts` → rendered by `HealthScoreRing`,
   `CategoryStatusCard`, and `AlertsList`.

The important property: steps 5 and 6 are byte-identical whether the request
arrived over HTTP or through WebAssembly interop, because
`Dashboard.Wasm/DemoJsonContext.cs` mirrors the API's serialization options
exactly.

---

## Where business logic lives

In `Dashboard.Domain` and `Dashboard.Application`, and nowhere else.

- **Controllers** contain no logic. If one grows a branch, that branch belongs
  in a service.
- **React components** contain no scoring. `frontend/src/lib/` holds only
  presentation mapping — status → CSS class, number → formatted string. There
  is no second implementation of any rule.
- **The database** holds no logic. No triggers, no computed columns, no stored
  procedures. `Strength Total` is derived in `MetricEntryService`, not by the
  database.

That discipline is what made the WebAssembly demo possible at all, and it's
the strongest argument the layering has.

---

## How dependencies flow

Constructor injection throughout, with a composition root per layer:

- `ApplicationServiceCollectionExtensions.AddApplication()` registers services
  and the Domain evaluators. Registering evaluators here rather than in Domain
  is what keeps Domain free of any DI reference.
- `InfrastructureServiceCollectionExtensions.AddInfrastructure()` registers the
  `DbContext` and binds the seven interfaces to their EF implementations.
- `Program.cs` calls both. It is the only file that knows the whole graph.
- `DemoWorkspace.Create()` calls `AddApplication()` — the *same* method — and
  binds the interfaces to the in-memory implementations instead.

The connection string is read lazily *inside* the `AddDbContext` options
callback rather than eagerly in `AddInfrastructure`. That is deliberate and
non-obvious: `AddInfrastructure` runs during `Program.cs`'s top-level
statements, before `WebApplicationFactory`-based tests can layer in their own
configuration. An eager read would throw before those overrides ever applied.

---

## Auth

There isn't any. Dashboard is a single-user application that has only ever bound
to `localhost`, and the deployed demo is a static bundle with no server and no
per-user data.

This is a real design decision, not an omission, and it is what removes whole
categories of risk rather than mitigating them — see
[SECURITY.md](SECURITY.md). What it would cost to add is described there too.

---

## Error handling

- **Domain** throws on invariant violations (`ArgumentException` from a
  constructor). These are programmer errors and should be loud.
- **Application** throws `InvalidOperationException` for "you asked for
  something that isn't there" and `ArgumentException` for invalid input, with
  messages written for a user to read (`"abc" is not a valid whole number for
  "Overdue window (months)"`).
- **Controllers** translate those into status codes.
- **The WASM façade** catches at the boundary and returns
  `{ ok: false, error }` rather than throwing across interop, where a managed
  exception would arrive as an opaque runtime error and lose the message.
- **The frontend** turns either failure into a thrown `Error`; pages hold an
  `error` state and render a recoverable message, never a blank screen.

Settings parsing is a deliberate exception to "throw on bad input":
`AppSettingReader` falls back to the declared default when a *stored* value
won't parse, because one bad settings row should not take down the dashboard.

---

## Config and secrets

Every environment difference is configuration, documented in `.env.example`.

- The frontend reads `VITE_*` variables through `frontend/src/lib/config.ts`,
  which is pure and total: bad input degrades to a documented default with a
  warning, and can never crash startup or silently select the wrong mode.
- **`VITE_` values are inlined into the bundle and are not secret.** No
  credential ever gets that prefix; `.env.example` says so at the top.
- The one genuine secret is the PostgreSQL connection string, read from
  `dotnet user-secrets` or `ConnectionStrings__Dashboard`. It has no default and
  the app fails with an explicit message if it's missing.
- The deployed demo holds no secrets at all, because it talks to nothing.

---

## Why this shape suits this app

The layering is shallow — four projects, not fourteen — and there is no
mediator, no CQRS, no event sourcing, and no repository-per-entity ceremony
beyond the seven interfaces that earn their place.

What justified the boundaries that *do* exist:

- The **evaluation engine** genuinely needed to be strategy-based, because
  "adding a metric must not require a code change" was a real requirement.
- The **persistence seam** justified itself twice: once for unit-testing the
  services against fakes, and once — unplanned — when it turned out to be the
  reason the whole application could be lifted into a browser.

Both are boundaries that paid for themselves. The absence of the rest is the
other half of the design.
