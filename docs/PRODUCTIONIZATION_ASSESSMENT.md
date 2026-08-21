# Productionization Assessment

**Date:** 2026-08-20
**Repository:** Vantage — a personal monthly life-metrics dashboard
**Assessed at commit:** `2dd0709`, plus uncommitted work on `KnownAppSettings.cs`

This is an honest review of what the application is today, what stands between it
and a public portfolio deployment, and the order in which those things should be
fixed. It is deliberately blunt in both directions.

---

## 1. Current architecture

Four .NET projects plus a React SPA:

```
Vantage.Domain          entities, evaluation strategies, rating maths.  No dependencies at all.
Vantage.Application     services, DTOs, repository interfaces.          Depends on Domain + DI abstractions.
Vantage.Infrastructure  EF Core, Npgsql, repository implementations.    Depends on Application + Domain.
Vantage.Api             ASP.NET Core controllers, composition root.     Depends on everything.

frontend/               React 19 + Vite 8 + Tailwind 4 + React Router 7. Talks HTTP/JSON to the API.
```

Storage is PostgreSQL via EF Core with four migrations. The connection string comes
from `dotnet user-secrets` or `ConnectionStrings__Vantage`. There is no auth — it is
a single-user app that has only ever run on `localhost`.

### Baseline, measured rather than assumed

| Check | Result |
| --- | --- |
| `dotnet build Vantage.sln -c Release` | succeeds, **0 warnings** |
| Backend tests, working tree as found | **211 total — 203 pass, 8 fail** |
| Backend tests, uncommitted change stashed | **211 total — 210 pass, 1 fail** |
| `npm run build` | succeeds; one 578 kB chunk-size warning |
| `npm test` | **1 test**, passes |
| `npm run lint` | 1 warning (`react-refresh` on `button.tsx`) |

Two distinct problems hide in that test column, and separating them matters:

- **7 failures come from uncommitted work in progress.** `KnownAppSettings.cs`
  retunes the category-status cutoffs from 49/74/89 to 25/50/75 and raises the
  strength standards. The tests still assert the old numbers. This is unfinished
  work, not a defect — the change is deliberate and well reasoned.
- **1 failure predates it.** `DashboardEndpointTests.GetDashboard_ReturnsASummaryReflectingTheSeededData`
  fails at `HEAD` with a clean working tree (expected `100`, actual `50`). A genuine
  pre-existing red test.

---

## 2. Honest strengths

This is not a codebase that needs rescuing, and it would be dishonest to invent
problems in order to justify restructuring it.

- **The layering is real, not decorative.** `Vantage.Domain` genuinely has zero
  package references. `Vantage.Application` depends only on
  `Microsoft.Extensions.DependencyInjection.Abstractions` and Domain. EF Core appears
  in exactly one project. That discipline is rarer than it sounds, and it is what
  makes the deployment strategy in section 6 possible at all.
- **The persistence seam already exists.** Seven small repository interfaces plus
  `IUnitOfWork`, declared in Application and implemented in Infrastructure. No
  service touches a `DbContext`. Nothing had to be introduced to make this testable.
- **The evaluation engine is properly data-driven.** `EvaluationStrategy` plus one
  `IMetricEvaluator` per strategy plus a factory. Adding a metric is a row, not a
  code change — exactly what the Phase 0 design document promised, actually
  delivered.
- **The domain model protects its own invariants.** Private setters, private
  EF-only constructors, `internal` mutators reachable only through the aggregate
  root, and ratchet-forward semantics on `LogHangout` / `LogContact` so a stale entry
  cannot rewrite a more recent one.
- **The comments explain why, not what.** The CORS block explains why the port is
  pinned off Vite's default. `AddInfrastructure` explains why the connection string
  is read lazily inside the options callback rather than eagerly. This is the single
  strongest signal in the repository that the author understood the code.
- **Configuration is already a first-class feature.** `KnownAppSettings` is one
  registry of every tunable, surfaced automatically on a Settings page.

Architectural decisions were documented up front in `docs/phase-0-design-proposal.md`,
including ones that were *rejected* (MediatR, a rules DSL). That document is an asset
and should be kept.

---

## 3. Weaknesses that matter

Ordered by impact. Cosmetic nits are excluded on purpose.

| # | Finding | Impact |
| --- | --- | --- |
| 1 | **Real personal financial data is committed to the repository and its git history.** See section 5. | **Critical.** Hard blocker on publishing. |
| 2 | The "development seeder" is not a seeder. It is an eight-step, marker-gated migration script that deletes real rows (`MetricSnapshots.RemoveRange(...)`) and writes real balances. | High. Runs automatically on every Development startup, and no test covers it. |
| 3 | One pre-existing failing test, unrelated to the uncommitted work. | High. A red suite trains you to ignore the suite. |
| 4 | Eight failing tests in the working tree overall; the frontend has **1** test, which asserts almost nothing. | High. The scoring engine has decent backend coverage; the UI has none. |
| 5 | No CI. Nothing has ever built or tested this outside one machine. | High. |
| 6 | No deployment of any kind. Nothing to show an employer but a clone-and-run README. | High — this is the entire point of the exercise. |
| 7 | Config parsing is *almost* total. `AppSettingReader` correctly falls back to the definition's default when a **stored** value is unparsable, so a bad setting cannot take down the dashboard. It is not total against a malformed **default** — `int.Parse(definition.DefaultValue)` throws — and nothing pins the 100+ defaults in `KnownAppSettings` as parsable. | Low-medium. A typo in a default constant would surface as a runtime crash, not a build error. |
| 8 | CORS is hardcoded to `http://localhost:5180` in `Program.cs`. | Medium. Correct for today, blocks any deployment. |
| 9 | No `.gitattributes` on a Windows checkout. `KnownAppSettings.cs` is already CRLF in the tree while git wants LF. | Medium. Guarantees a formatting gate that passes locally and fails in CI. |
| 10 | Migrations and seeding run automatically at startup under Development. | Low locally, unacceptable in any hosted environment. |
| 11 | Frontend ships as a single 578 kB chunk. | Low. Recharts dominates it. Acceptable for a portfolio app. |
| 12 | `.install1.log` (empty) and an orphaned `frontend/src/App.css` are gitignored but still on disk. | Cosmetic. |

---

## 4. Security findings

The threat model is what makes this section short, so it comes first.

**Threat model.** Single user, single tenant, no authentication, no multi-user data,
no file uploads, no user-generated HTML, no outbound integrations, no payment data,
and no personal information beyond what the owner types about themselves. Until now
it has bound only to `localhost`. After this work the deployed artifact is a **static
bundle with no server and no database**, which structurally eliminates whole
categories rather than merely mitigating them:

- **No CSRF** — there is no server session and no cookie to ride.
- **No SQL injection in the deployed demo** — no SQL and no database exist in it.
- **No IDOR or broken access control** — one user, no accounts, no ownership
  relationships, nothing to enumerate.
- **No secrets in the deployed artifact** — it holds no credentials because it talks
  to nothing.

What remains real:

| # | Finding | Severity | Status |
| --- | --- | --- | --- |
| S1 | Personal financial data in source and git history (section 5). | **Critical** | Must be fixed before publishing. |
| S2 | `AllowedHosts: "*"` and a dev-only CORS origin in the shipped `appsettings.json`. | Low | Applies only if the API is ever hosted. Documented. |
| S3 | No rate limiting on the API. | Low | Irrelevant on `localhost`; would matter if hosted. |
| S4 | EF Core parameterizes everything; no raw SQL anywhere. | — | Verified, no action. |
| S5 | No hardcoded credentials anywhere in the full history. | — | Verified across all 11 commits. Every match was a placeholder (`<your-pg-user>`, `placeholder`, `unused`) or an interactive `Read-Host` prompt. |

Deliberately **not** added, because on a static host it would be security theater:
CSP / HSTS / X-Frame-Options configured in application code that GitHub Pages would
never send, and input sanitizers on values that React renders as text (escaping them)
and never as HTML.

---

## 5. Data and privacy — the critical finding

`DevelopmentDataSeeder.cs` does not contain sample data. It contains the author's
actual figures, and says so in its own comments — "replaces July's Finance numbers
with the real ones", "seeded with their real current balances":

| Value | Location |
| --- | --- |
| Net Worth **$236,200** | `DevelopmentDataSeeder.cs:173` plus three test files |
| Credit Score **809** | seeder, tests, and a code comment |
| Emergency Fund **$31,193** | `DevelopmentDataSeeder.cs:350`, `CategoryDetailServiceTests.cs:160` |
| Retirement Fund **$74,095** | `DevelopmentDataSeeder.cs:351` |
| Four friends' first names | seeder and `SocialEndpointTests.cs` |
| Last date with wife, last visit to mother | seeder, exact dates |

The values appear in **three commits** and have spread from the seeder into test
files and comments. Scrubbing the working tree is therefore *not* sufficient: they
survive in history and would be published, indexed, and forked the moment the
repository goes public.

**Decision taken:** history is replaced with a single clean initial commit and
force-pushed. Eleven solo commits with no collaborators depending on them is a small
price for a provably clean history.

`KeyRelationshipKind.DateWithWife` / `VisitedMother` stay. They are genuine product
features in the domain model and in a migration — not leaked values — and they make
the app read as something real rather than a generic CRUD demo.

---

## 6. Recommended deployment

**The constraint:** GitHub Pages serves static files only. It cannot run ASP.NET
Core, and on a free account Pages is available only from a **public** repository. So
"deploy this to GitHub Pages" cannot mean "deploy the API."

Three options were considered:

| Option | Verdict |
| --- | --- |
| Frontend on Pages, API on a free host (Fly, Render, Railway, Azure) | **Rejected.** Every viable .NET container host now wants a credit card, and the brief specified Pages. Adds an account, a secret, a CORS policy, and a cold start. |
| Reimplement the scoring engine in TypeScript for a browser-only demo | **Rejected.** 1,270 lines of real logic would become a second implementation that silently drifts from the first. A demo that disagrees with the app is worse than no demo. |
| **Compile the real Application and Domain assemblies to WebAssembly** | **Chosen.** |

Why the chosen option is available at all: because `Vantage.Application` depends on
nothing but DI abstractions, and because persistence already sits behind seven small
interfaces, the *actual* scoring engine can run in the browser with in-memory
repositories substituted for the EF Core ones. **There is no second implementation.**
The demo executes the same `DashboardService`, the same `MetricRatingCalculator`, and
the same evaluators as the real API.

```
Local / real                          Deployed demo (GitHub Pages)
────────────                          ────────────────────────────
React SPA                             React SPA   (same bundle, different adapter)
   │ HTTP/JSON                           │ direct call
Vantage.Api (ASP.NET)                 Vantage.Wasm   [JSExport] façade
   │                                     │
Vantage.Application  ◄── same assemblies, same IL ──►  Vantage.Application
Vantage.Domain                        Vantage.Domain
   │                                     │
EF Core repositories                  In-memory repositories + generated fixture
   │                                     │
PostgreSQL                            browser memory
```

Verified before committing to it: a `browser-wasm` build referencing
`Vantage.Application` compiles clean and executes in a real browser, returning
`.NET 9.0.19` from a `[JSExport]` method with both Vantage assemblies loaded.

Cost: **$0**, no new account, no new secret — Pages authenticates with the workflow's
built-in token. Trade-off: a larger first-load payload than a plain SPA, and the demo
resets on refresh because it holds no persistent store. Both are acceptable for a
demo and both are stated plainly in the README.

---

## 7. Major risks

1. **The force-push is irreversible.** The remote's history is replaced. Mitigated by
   taking a full local backup bundle first.
2. **The pre-existing failing test may encode a real bug** rather than a stale
   expectation. It must be understood before it is touched — changing a test to match
   broken behaviour is worse than leaving it red.
3. **The uncommitted retune must not be silently reverted.** It is the author's work.
   The tests get updated to match it; the change itself stays.
4. **WASM payload size** could make first load unpleasant if left untrimmed.
5. **Demo fixture drift** — a fixture written against absolute dates will rot and show
   a dead dashboard a year from now.

---

## 8. Implementation order

1. Replace the real personal data with a generated fixture; add a test that scans for it.
2. Understand and fix the pre-existing failing test.
3. Update the tests the uncommitted retune invalidates, preserving the retune.
4. Extract the demo dataset and in-memory repositories into `Vantage.Demo`.
5. Build the `Vantage.Wasm` façade over the real Application services.
6. Add the frontend data-source seam, keeping the HTTP adapter as the default.
7. Config hardening: `.env.example`, `.gitattributes`, total config parsing, base path.
8. CI: build, test, lint, dependency audit, secret scan over full history.
9. Tests for the properties the deployment now depends on.
10. Documentation and the interview guide.
11. Scrub history, publish, deploy, and verify against the live URL.
