# Dashboard

> **Archived — absorbed into [LifeOS](https://github.com/AndreiBautin/LifeOS).**
>
> Dashboard was never an area; it was the machinery by which areas get
> scored, and absorbing it is what let five other areas score themselves.
> Its five evaluator classes are now one exhaustive switch in the hub's
> `domain/review/`, and its vocabulary — improved, regressed, stagnant,
> insufficient-data — is the vocabulary the whole hub uses.
>
> Live at https://andreibautin.github.io/LifeOS/. This repository is kept for its history and is no
> longer developed.

A monthly review dashboard for the parts of life that don't send you a report.

Once a month you sit down for ten minutes and answer one question honestly:
*am I actually progressing in the areas I care about?* Dashboard makes that
answerable at a glance — you enter a handful of numbers, and it tells you what
improved, what stalled, and what needs attention. It is deliberately **not** a
daily-use app: no streaks, no notifications, no guilt mechanics.

**[▶ Open the live demo](https://andreibautin.github.io/Dashboard/)**

No login, no sign-up — the demo opens straight onto a populated dashboard.
It's seeded with a generated fixture for a fictional person; none of it is
anyone's real data. Everything is editable: record this month's numbers, log a
hangout, change a threshold on the Settings page and watch the scores move.
Changes live in browser memory only, so a refresh restores the fixture.

---

## The demo runs the real backend

This is the part worth two minutes of an interviewer's time.

Dashboard is an ASP.NET Core API over PostgreSQL. GitHub Pages serves static
files and cannot run any of that. The usual answer is to reimplement the
scoring rules in TypeScript for a browser-only demo — which means two
implementations of the same 1,270 lines of logic, quietly drifting apart until
the demo is lying about the app.

Instead, the demo **compiles the real `Dashboard.Application` and
`Dashboard.Domain` assemblies to WebAssembly** and runs them in the browser.
Same `DashboardService`, same `MetricRatingCalculator`, same evaluators, same
IL. The only thing swapped out is persistence: the seven repository interfaces
are bound to in-memory implementations instead of the EF Core ones.

```
Local / self-hosted                   Deployed demo (GitHub Pages)
───────────────────                   ────────────────────────────
React SPA                             React SPA   (same bundle, different adapter)
   │ HTTP/JSON                           │ direct call
Dashboard.Api (ASP.NET Core)            Dashboard.Wasm   [JSExport] façade
   │                                     │
Dashboard.Application  ◄── same assemblies, same IL ──►  Dashboard.Application
Dashboard.Domain                        Dashboard.Domain
   │                                     │
EF Core repositories                  In-memory repositories + generated fixture
   │                                     │
PostgreSQL                            browser memory
```

That substitution was possible only because the app already kept persistence
behind interfaces on the Application layer. It's the clearest payoff the
architecture has produced, and it wasn't designed for.

The cost, stated plainly: a ~4 MB first load while the .NET runtime downloads,
and no persistence between refreshes. Both are fine for a demo and both are
wrong for anything else.

See **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** for a request traced end
to end through real files.

---

## Features

- **Dashboard** — one blended 0–100 score, a status per category, and alerts
  that name the specific metric that's drifting rather than just colouring a
  card red.
- **Graduated statuses** — Struggling / Needs Attention / On Track / Excelling,
  from configurable score cutoffs, so two mediocre scores aren't shown as the
  same thing.
- **Metric evaluation engine** — five strategies (Increase, Decrease,
  StayAbove, StayBelow, StayWithinRange) chosen per metric as *data*. Adding a
  metric is a row, not a code change.
- **Rating tiers** — a metric's absolute level rated on a four-tier scale
  (Beginner → Elite, Building → Thriving), scored continuously so progress
  within a band still counts.
- **Calculated metrics** — Strength Total derives from the four lifts and
  recomputes whenever any of them changes.
- **Social** — active circle size, upkeep (who's overdue), and key
  relationships tracked and alerted on separately, because a large neglected
  circle and a small well-kept one should not read the same.
- **Settings** — every threshold, cutoff, and description in the app is
  editable at runtime from one page. No redeploy to retune a scale.

---

## Tech stack, and why

| Choice | Why this one |
| --- | --- |
| **.NET 9 / ASP.NET Core** | Static typing and a domain model that can enforce its own invariants — private setters, aggregate-only mutation. The scoring rules are the app; they needed to be somewhere they could be tested in isolation. |
| **Four-project layering** | Domain has *zero* package references. That discipline is what let the demo run the real logic in a browser — see above. |
| **PostgreSQL + EF Core** | Relational data with genuine relationships. EF Core is confined to one project, reached only through repository interfaces. |
| **Plain services, no MediatR** | One developer, three domains, no cross-cutting pipeline to hang behaviours off. The indirection would have been pure cost. Reconsidered and documented, not skipped by default. |
| **React 19 + Vite + Tailwind 4** | The UI is read-mostly and mainly presentational. Fast builds, no server rendering to justify. |
| **Recharts** | The trend charts needed to work, not to be bespoke. It's the largest dependency and it earns its place. |
| **WebAssembly for the demo** | The only option that deploys to a static host without a second implementation of the business logic. |
| **GitHub Pages** | Genuinely free, no credit card, no new account, and no secret to manage — deployment authenticates with the workflow's own token. |

---

## Architecture

```
Dashboard.Domain          entities, evaluation strategies, rating maths.  No dependencies.
Dashboard.Application     services, DTOs, repository interfaces.          Domain + DI abstractions.
Dashboard.Infrastructure  EF Core, Npgsql, repository implementations.    Application + Domain.
Dashboard.Api             controllers, composition root.                  All of the above.
Dashboard.Demo            in-memory repositories + the generated fixture. Application + Domain.
Dashboard.Wasm            [JSExport] façade for the browser.              Demo.

frontend/               React SPA. Two data adapters behind one contract.
```

Full detail, including a request traced end to end:
**[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**

---

## Security

Single-user app with no authentication, and the deployed artifact is a static
bundle with no server and no database — which structurally removes CSRF, SQL
injection, and access-control bugs rather than mitigating them. The threat
model, what that leaves, and what was deliberately *not* added (headers a
static host would never send) are in
**[docs/SECURITY.md](docs/SECURITY.md)**.

The public demo contains no personal data by construction, not by care:
generated fixture, separate namespace, seeded only into empty storage, with
tests that scan for anything personal —
**[docs/DEMO_DATA.md](docs/DEMO_DATA.md)**.

---

## Testing

**278 tests** — 241 backend (xUnit), 37 frontend (Vitest). What's prioritised,
and what is deliberately *not* tested and why:
**[docs/TESTING.md](docs/TESTING.md)**.

```bash
dotnet test backend/Dashboard.sln
```

---

## Deployment

Pushes to `main` build the WebAssembly bundle and the SPA and publish to
GitHub Pages, then smoke-test the live URL. Provider choice, rejected
alternatives, and a troubleshooting table:
**[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)**.

---

## Running it locally

The full app, against a real PostgreSQL database.

**Prerequisites:** .NET 9 SDK, Node 24+, PostgreSQL.

```bash
git clone https://github.com/AndreiBautin/Dashboard.git
cd Dashboard
```

Set the connection string (never committed — see `.env.example`):

```bash
cd backend/src/Dashboard.Api
dotnet user-secrets set "ConnectionStrings:Dashboard" "Host=localhost;Database=vantage_dev;Username=<user>;Password=<password>"
```

Then, on Windows, double-click **`start-app.bat`** — it checks prerequisites,
installs dependencies, applies migrations, starts both processes on pinned
ports, and opens a browser. Otherwise:

```bash
dotnet run --project backend/src/Dashboard.Api
```

```bash
cd frontend && npm install && npm run dev
```

The API is on `http://localhost:5199`, the app on `http://localhost:5180`.
Running in Development applies migrations and seeds the same fixture the demo
uses — into an empty database only; it will never overwrite existing data.

To run the demo build locally instead, with no database at all:

```bash
cd frontend && npm run build:demo && npx serve dist
```

---

## Documentation

| Document | What's in it |
| --- | --- |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Layers, dependency flow, a request traced through real files |
| [SECURITY.md](docs/SECURITY.md) | Threat model, findings, and what was deliberately not done |
| [DEMO_DATA.md](docs/DEMO_DATA.md) | The fixture, and the three barriers keeping personal data out |
| [DEPLOYMENT.md](docs/DEPLOYMENT.md) | Provider choice, CI/CD, troubleshooting, free-tier headroom |
| [TESTING.md](docs/TESTING.md) | Strategy per layer, and what is not tested |
| [INTERVIEW_GUIDE.md](docs/INTERVIEW_GUIDE.md) | How to explain all of this out loud |
| [PRODUCTIONIZATION_ASSESSMENT.md](docs/PRODUCTIONIZATION_ASSESSMENT.md) | The honest review this work started from |
| [phase-0-design-proposal.md](docs/phase-0-design-proposal.md) | The original design document, including rejected options |
