# Vantage — Phase 0 Design Proposal

> **Historical document, kept as written.** The product was later renamed from
> **Vantage** to **Dashboard**. The .NET namespaces and project names still use
> `Vantage.*`, so this document remains accurate about the code; only the
> product name changed. Nothing below has been edited after the fact — the
> point of keeping it is that it records what was decided, and what was
> rejected, before any of it was built.

Prepared as a design review document. No code has been written. This document exists to align on product direction, architecture, and scope before Phase 1 begins.

---

## 1. Product Vision

Once a month, you sit down for ten minutes and answer one question honestly: *am I progressing in every area of life I care about?* This application is the tool that makes that question answerable in one glance — an executive board meeting for your own life, held monthly, chaired by you.

It is deliberately not a daily-use app. It has no streaks, no daily check-ins, no notifications, no guilt mechanics. Its entire value is concentrated into a single monthly ritual: enter a small number of updated metrics, and let the system tell you, in plain language, what improved, what stalled, and what needs attention. Every screen should read like a briefing document, not a data table.

The product succeeds if, twelve months from now, you can look at a single dashboard and see — without re-deriving it yourself — whether your fitness, finances, and social life are trending in the direction you want.

---

## 2. Application Name — Decided

**Vantage.** A vantage point is an elevated, deliberate viewpoint — the framing of a monthly step-back review. Confirmed as the product name; it's used throughout this document, in namespaces, and in package naming from Phase 1 onward.

Other options considered before deciding: Meridian, Overview, Summit, The Standing Report, Keel, Panorama.

---

## 3. Architecture Proposal

### 3.1 Overall shape

Clean Architecture, kept intentionally shallow for a V1 with three domains and a single user:

```
Vantage.Domain          — entities, value objects, evaluation strategies. No dependencies.
Vantage.Application     — use cases (services), DTOs, interfaces for persistence. Depends on Domain only.
Vantage.Infrastructure  — EF Core, PostgreSQL, repository implementations. Depends on Application + Domain.
Vantage.Api             — ASP.NET Core Web API, controllers, DI composition root. Depends on all of the above.
```

The frontend is a separate deployable unit (Vite/React SPA) that talks to the API over HTTP/JSON. No BFF layer — unnecessary for a single-user local app.

### 3.2 Key architectural decision: no mediator library

Clean Architecture often pairs with MediatR (request/handler-per-use-case). For this project I'm recommending **plain application services with constructor-injected interfaces** instead — e.g., `IMetricEvaluationService`, `ISnapshotService` — called directly from controllers.

**Tradeoff:** MediatR adds a layer of indirection (locate the handler, trace the pipeline) that pays off in large teams or apps with many cross-cutting concerns (logging, validation, caching pipelines) attached to every request. For a single-developer, single-user, three-domain V1, that indirection is pure cost. Plain services are easier to read top-to-bottom, easier to unit test, and add zero framework lock-in. If the app grows enough cross-cutting complexity later (e.g., you add domain events, audit logging, or a plugin system for new life categories), MediatR or a lightweight event bus can be introduced without restructuring the layers — the interfaces at the Application boundary don't change.

### 3.3 The metric evaluation engine (the architectural centerpiece)

This is the one piece of the domain that most directly determines whether the app is extensible or a rewrite waiting to happen. The requirement is: *metrics must be data-configured, not code-configured* — adding a fourth domain (e.g., "Career") or a new metric ("VO2 Max") should require inserting rows, not writing C#.

Proposed shape: each `MetricDefinition` stores an `EvaluationStrategy` enum (`Increase`, `Decrease`, `StayAbove`, `StayBelow`, `StayWithinRange`) plus a small JSON `EvaluationConfig` blob holding strategy-specific parameters (e.g., a tolerance band for "stagnation," or `min`/`max` for `StayWithinRange`). In the Domain layer, an `IMetricEvaluator` interface has one implementation per strategy, registered in DI and resolved via a small factory keyed on the enum (a textbook Strategy pattern). The evaluation service pulls the last two (or N) snapshots for a metric, hands them plus the config to the matching evaluator, and gets back a `MetricStatus` (`Improved`, `Regressed`, `Stagnant`, `InsufficientData`). Adding a new strategy later means adding one new class and one enum value — nothing else in the system changes.

`ManualReview` was considered and dropped for V1 — no current metric needs it, and the enum can gain a case later without disrupting the factory/evaluator shape.

This keeps the engine genuinely pluggable without over-engineering it into a rules DSL or scripting engine, which would be premature for three domains and six metrics.

### 3.4 Local-only constraints

No Docker per your instructions, so PostgreSQL runs as a native local install (Postgres.app on macOS, the standard installer on Windows/Linux), with connection details in user-secrets/appsettings.Development.json. EF Core Migrations manage schema. This is noted as a minor setup step in Phase 1, not a blocker.

---

## 4. Technology Decisions

Confirming the stack as specified, with the reasoning for each choice:

- **.NET 9 / ASP.NET Core Web API** — minimal API or controller-based (recommend controllers for this size; slightly more structure without meaningful overhead).
- **EF Core + PostgreSQL** — code-first migrations, Npgsql provider. PostgreSQL is well beyond what a single-user local app strictly needs, but it's explicitly requested and costs nothing extra at this scale, and it means the schema is production-portable if this ever needs to be hosted.
- **React + TypeScript + Vite** — Vite for fast local dev, no need for Next.js's routing/SSR machinery in a local single-user SPA.
- **TailwindCSS + shadcn/ui + Lucide** — shadcn gives unstyled, composable primitives (not a themed component library like MUI), which matches the "Linear/Raycast/Stripe" aesthetic goal far better than a pre-themed kit.
- **Recharts** — declarative, composable charts that are easy to restyle to match a custom dark theme, versus heavier libraries (Nivo, Chart.js) that fight you on custom aesthetics.
- **xUnit + React Testing Library + Playwright (later)** — standard, well-supported, nothing exotic.

---

## 5. UI/UX Design Direction

**Tone:** dark-mode-first, generous whitespace, restrained color use where color is meaningful (status, not decoration). Typography carries most of the visual weight — a strong type scale (e.g., a display face for headline numbers like "88/100," a clean sans for body) does more for the "premium" feeling than any amount of chrome or shadows.

**Interaction philosophy:** since this is used ~12 times a year, first-open clarity matters more than power-user efficiency. No dense toolbars, no nested settings menus. The home dashboard should be readable in under 10 seconds: status per category, one headline score, a short list of things that need attention — in that order, top to bottom, no scrolling required for the essentials.

**Motion:** subtle only — number count-ups when a snapshot loads, a soft fade/slide when switching between monthly snapshots, chart lines drawing in on mount. Motion should communicate state changes, never be decorative for its own sake.

**Color as signal:** green/amber/red status dots are the one place color is semantically loaded (as in your example layout). Everywhere else, the palette stays neutral (near-black backgrounds, off-white text, one accent color for interactive elements) so that status colors actually stand out when they appear.

---

## 6. High-Level Domain Model

```
Category
  Id, Name, SortOrder
  (Fitness, Finance, Social — seeded data, not hardcoded enums,
   so a future category requires no code change)

MetricDefinition
  Id, CategoryId, Name, Unit, EvaluationStrategy, EvaluationConfig (JSON), SortOrder, IsActive
  (e.g., "Powerlifting Total" / kg / Increase / { stagnationToleranceMonths: 2 })

MetricSnapshot
  Id, MetricDefinitionId, MonthlySnapshotId, Value, RecordedAt

MonthlySnapshot   (the aggregate root representing "this month's review")
  Id, Month (first-of-month date), CreatedAt
  → has many MetricSnapshots
  → has one SocialSnapshot

SocialSnapshot
  Id, MonthlySnapshotId, ActiveFriendCount
  (captured at review time so the "circle size over time" trend has history,
   even though Friend itself is mutable, not append-only)

Friend
  Id, Name, LastHangoutDate, Notes, CreatedAt
  IsActive → derived at query time (LastHangoutDate within the configurable
             active-circle threshold, default 12 months), not stored, so it's
             never stale. Never hard-deleted by the system — dropping out of
             "active" only removes a friend from the active list/count, the
             record and history remain intact.
  FlaggedOverdue → derived (LastHangoutDate > 3 months ago)

AppSetting   (small key/value config table)
  Key, Value
  e.g. ActiveCircleThresholdMonths = 12 — editable without a code change,
  read by the Social evaluation logic instead of a hardcoded constant.

MetricEvaluationResult   (computed, not persisted in V1 — recomputed on read)
  MetricDefinitionId, MonthlySnapshotId, Status (Improved/Regressed/Stagnant/InsufficientData)

OverallHealthScore   (computed, not persisted in V1)
  Equal-weighted blend across Fitness/Finance/Social category rollups.
  No single Needs Attention item unilaterally sinks a category's status —
  status is always a blend of that category's metric/friend signals.
```

Keeping `MetricEvaluationResult` and `OverallHealthScore` computed-on-read rather than stored avoids a whole class of cache-invalidation bugs (e.g., editing a past snapshot silently leaving stale downstream scores). Revisit persistence only if computation becomes a measurable performance problem — unlikely at this data scale.

---

## 7. UI Wireframes (ASCII)

### 7.1 Home Dashboard

```
┌──────────────────────────────────────────────────────────────┐
│  Vantage                                    July 2026 ▾       │
│  Monthly Executive Review                                     │
│                                                                │
│   Overall Score                                               │
│   ┌────────────┐                                              │
│   │    88      │   ● Fitness    On track                      │
│   │   / 100    │   ● Finance    On track                      │
│   └────────────┘   ● Social     Needs attention                │
│                                                                │
│   Needs Attention                                             │
│   ─────────────────────────────────────────────                │
│   • Powerlifting Total has stalled for 2 months                │
│   • Haven't seen Alex in 127 days                              │
│   • Net Worth declined this month                              │
│                                                                │
│   [ Fitness card ]   [ Finance card ]   [ Social card ]        │
│    small trend         small trend         active: 9 (+1)      │
│    sparkline           sparkline           sparkline            │
└──────────────────────────────────────────────────────────────┘
```

### 7.2 Fitness Detail

```
┌──────────────────────────────────────────────────────────────┐
│  ← Fitness                                                     │
│                                                                │
│   Powerlifting Total                     Arm Measurement        │
│   1050 lb   ▲ +15 since last month       15.25"  ▬ stalled       │
│   [ trend chart, last 12 months ]        [ trend chart ]        │
│                                                                │
│   Status: On Track                       Status: Needs Attention│
└──────────────────────────────────────────────────────────────┘
```

### 7.3 Social Detail

```
┌──────────────────────────────────────────────────────────────┐
│  ← Social                                                       │
│                                                                │
│   Active Circle: 9 friends (+1 this month)   [ trend chart ]    │
│                                                                │
│   Ranked by time since last hangout                             │
│   ─────────────────────────────────────────────                 │
│   ⚠ Alex           127 days ago                                 │
│   ⚠ Priya           98 days ago                                 │
│     Sam              41 days ago                                 │
│     Jordan            12 days ago                                 │
│                                                                │
│   [ + Log a hangout ]      [ + Add friend ]                     │
└──────────────────────────────────────────────────────────────┘
```

### 7.4 Monthly Entry Flow

```
┌──────────────────────────────────────────────────────────────┐
│  New Monthly Review — July 2026                    Step 1 of 3 │
│                                                                │
│   Fitness                                                       │
│   Powerlifting Total (lb)     [ 1050        ]                   │
│   Arm Measurement (in)        [ 15.25       ]                   │
│                                                                │
│                                        [ Back ]   [ Continue ]  │
└──────────────────────────────────────────────────────────────┘
```

---

## 8. Folder Structure

```
Dashboard/
├── backend/
│   ├── src/
│   │   ├── Vantage.Domain/
│   │   ├── Vantage.Application/
│   │   ├── Vantage.Infrastructure/
│   │   └── Vantage.Api/
│   ├── tests/
│   │   ├── Vantage.Domain.Tests/
│   │   ├── Vantage.Application.Tests/
│   │   └── Vantage.Api.Tests/
│   └── Vantage.sln
├── frontend/
│   ├── src/
│   │   ├── components/        (shared UI primitives)
│   │   ├── features/
│   │   │   ├── dashboard/
│   │   │   ├── fitness/
│   │   │   ├── finance/
│   │   │   └── social/
│   │   ├── lib/
│   │   ├── hooks/
│   │   └── types/
│   ├── tests/
│   └── vite.config.ts
├── docs/
│   └── phase-0-design-proposal.md   (this document)
└── README.md
```

(Namespaces and package names use "Vantage" — confirmed product name. The `Dashboard/` root folder is your existing local project folder and doesn't need to be renamed to match.)

---

## 9. Development Roadmap

Restating the phase plan as agreed, for reference — each phase stops for review before the next begins:

Phase 0 (this document) — product and architecture design, no code.
Phase 1 — project scaffolding: backend/frontend/DB setup, shared config, test harness, design system primitives. No business features.
Phase 2 — core metrics engine: categories, configurable metrics, monthly snapshots, trend calculation, regression detection. Tests before implementation.
Phase 3 — Executive Dashboard: summary, status cards, trend visualizations, alerts, health score.
Phase 4 — Fitness module.
Phase 5 — Finance module.
Phase 6 — Social module.

No dates are attached deliberately — each phase's scope should be reviewed and approved before estimating the next.

---

## 10. Risks

The evaluation engine is the piece most likely to need rework if the strategy set proves too rigid once real data flows through it (e.g., "increase, but a plateau after a PR is expected and shouldn't count as regression" may need a more nuanced stagnation tolerance than a flat threshold). Recommend validating the strategies against your actual last 6–12 months of real Fitness/Finance numbers before Phase 2 locks the schema.

Running PostgreSQL without Docker adds a one-time local setup step that a containerized dev environment would have avoided; acceptable given the explicit no-Docker constraint, but worth flagging.

Because usage is monthly rather than daily, the cost of any UI friction is higher than in a daily-use app — a bad ten minutes, twelve times a year, is a much larger share of the app's total impression than a bad ten minutes in an app used daily. Phase 3's UI polish should be treated as core scope, not a nice-to-have.

Fixed units (inches, etc.) mean a future switch to metric would need a migration/conversion step rather than being free — acceptable tradeoff for V1 simplicity, worth remembering if this ever gets used by someone else.

---

## 11. Decisions Log

All Phase 0 open questions have been resolved:

Name: **Vantage** — confirmed.

Health score: equal weight across Fitness/Finance/Social. Category status is always a blend of its underlying signals — no single flagged item unilaterally sinks a category to red.

Friend inactivity: soft-removal only. A friend who crosses the inactivity threshold drops out of the active circle (list and count) but is never deleted — their record and history persist, and they reappear if hung out with again. The threshold defaults to 12 months and is stored as a configurable setting, not hardcoded.

Units: fixed per metric (e.g., inches for Arm Measurement) — no unit conversion in V1.

Evaluation strategies: `Increase`, `Decrease`, `StayAbove`, `StayBelow`, `StayWithinRange`. `ManualReview` is dropped from V1 — no current metric needs it.

Auth: none for V1, matching the original single-user/local-only scope.

Month boundaries: calendar month, local system timezone.

---

**Status: Phase 0 approved. Ready to begin Phase 1 (project scaffolding) on your go-ahead.**
