# Interview guide

Written as things you can say out loud. Every claim here is true of the code in
this repository — if you are ever unsure, the answer is "let me show you the
file", not an elaboration.

---

## The 30-second version

> Dashboard is a monthly review dashboard for the parts of life that don't send
> you a report — fitness, finances, and how much you're actually seeing your
> friends. You enter a handful of numbers once a month and it tells you what
> improved, what stalled, and what needs attention.
>
> The interesting engineering problem wasn't the app, it was the demo. It's a
> .NET API over Postgres, and I wanted it on GitHub Pages, which only serves
> static files. The obvious move is to rewrite the scoring rules in TypeScript
> for a browser demo — but that's a second implementation of your business
> logic that drifts until the demo is lying about the app. So instead I
> compiled the real Application and Domain assemblies to WebAssembly and swapped
> the repository implementations for in-memory ones. The demo runs the actual
> code. There's no second implementation.

Stop there. That's the hook, and it invites the follow-up you want.

---

## Explaining the architecture

**Lead with these three, in this order:**

1. **"Dependencies point inward, and Domain has zero of them."** Not "I used
   Clean Architecture" — say the concrete fact. `Dashboard.Domain` references no
   project and no NuGet package.
2. **"Persistence is seven interfaces on the Application layer."** No service
   touches a `DbContext`.
3. **"That's what made the WebAssembly demo possible — and I didn't design it
   for that."** This is the strongest thing you can say about the architecture,
   because it's evidence the boundaries were real rather than decorative. A
   boundary that pays off in a way you didn't plan is a boundary that was
   actually a boundary.

Then the shape:

```
Dashboard.Domain          entities, evaluators, rating maths.    No dependencies.
Dashboard.Application     services, DTOs, repository interfaces.  Domain only.
Dashboard.Infrastructure  EF Core, Npgsql, implementations.
Dashboard.Api             controllers, composition root.
Dashboard.Demo            in-memory repositories + fixture.
Dashboard.Wasm            [JSExport] façade for the browser.
```

If they push on "isn't four projects a lot for this?" — see the
[uncomfortable questions](#the-uncomfortable-questions).

---

## Request lifecycle

Have this ready; it's the most common follow-up.

> A dashboard load starts in `DashboardPage.tsx`, which calls
> `fetchDashboardSummary()`. It doesn't know about HTTP — `lib/api.ts` picks
> one of two adapters from config, and that's the only line in the codebase
> that decides which backend a build talks to.
>
> The HTTP adapter GETs `/api/dashboard`. `DashboardController` is about nine
> lines — it calls one service and wraps the result. All the work is in
> `DashboardService.GetSummaryAsync`: it loads categories through
> `ICategoryRepository`, reads the score cutoffs from settings, and for each
> metric calls `MetricEvaluationService`, which pulls the snapshot history and
> hands it to whichever evaluator matches that metric's configured strategy —
> that's a factory keyed on an enum. `MetricScoring` turns the resulting status
> into 0–100, falling back to the metric's rated *level* when there isn't
> enough history for a trend, so month one isn't a wall of "no data". Then it
> collects alerts, folds in Social as a synthetic category, and averages.
>
> In the deployed demo, everything from `DashboardService` down is identical —
> only the top and bottom differ: a `[JSExport]` call instead of a controller,
> and in-memory lists instead of Postgres.

---

## Engineering decisions

Each one: decision → alternatives → why → **trade-off**. The trade-off is what
makes it credible; never skip it.

### WebAssembly for the demo

- **Alternatives:** reimplement scoring in TypeScript; host the API on a free
  container platform; ship a read-only demo with canned JSON.
- **Why:** TypeScript meant two implementations of 1,270 lines of logic that
  would drift. Free .NET hosts all want a credit card. Canned JSON means the
  reviewer can't *do* anything, and half the app is the write path.
- **Trade-off:** a ~4 MB first load and no persistence across refreshes. For a
  demo, both are fine. For anything real, both would be disqualifying — which
  is exactly why the API still exists and is still the default.

### Strategy pattern for metric evaluation

- **Alternatives:** a `switch` on a metric-type enum; a rules DSL.
- **Why:** the requirement was "adding a metric must not require a code
  change". Each strategy is one class implementing `IMetricEvaluator`, resolved
  by a factory. Adding one is a new class and an enum value; nothing else moves.
- **Trade-off:** five small classes and a factory where a `switch` would have
  been fewer lines. It earns that at five strategies; at two it would have been
  overhead. A DSL was considered and rejected as premature.

### Plain services, no MediatR

- **Alternatives:** MediatR request/handler per use case.
- **Why:** one developer, three domains, no cross-cutting pipeline to hang
  behaviours off. The indirection would be pure cost — you'd trade "read the
  method" for "find the handler".
- **Trade-off:** if the app grew domain events, audit logging, or a validation
  pipeline, this would need revisiting. The Application-layer interfaces don't
  change if it does. This was decided up front and written down in
  `docs/phase-0-design-proposal.md`, including the rejection.

### Settings as data, not constants

- **Alternatives:** hardcode the thresholds; `appsettings.json`.
- **Why:** every threshold in the app is a judgement call that wants retuning
  once real data exists. `KnownAppSettings` is one registry; the Settings page
  renders itself from it, so adding a tunable requires no UI change.
- **Trade-off:** an extra read on most requests, and validation logic to keep a
  bad value out. `AppSettingReader` falls back to the declared default when a
  stored value won't parse, so one bad row can't take down the dashboard.

### Two named seeding operations instead of a boolean

- **Alternatives:** `Seed(store, overwrite: true)`.
- **Why:** the destructive path must not be reachable by passing the wrong
  argument. `FillIfEmpty` and `ResetAndFill` are separate methods; the startup
  path only ever calls the first.
- **Trade-off:** slightly more API surface, for a failure mode that destroys
  data. Worth it every time.

### Source-generated JSON in the WASM façade

- **Alternatives:** reflection-based `System.Text.Json`.
- **Why:** the published bundle is trimmed, and reflection is exactly what the
  trimmer can't see. Properties would work locally and silently vanish in
  production — the worst failure shape there is.
- **Trade-off:** every serialized type must be declared on the context. That's
  a real maintenance cost, and it's the right one: it fails at compile time
  instead of on the deployed page.

### `UnsafeAccessor` for entity ids in the demo store

- **Alternatives:** reflection; loosening the domain's private setters.
- **Why:** EF assigns keys normally; an in-memory store has to. Reflection is
  trim-hostile. Relaxing the setters would weaken the domain model everywhere
  to serve one adapter. `UnsafeAccessor` resolves at compile time and is
  trim-safe.
- **Trade-off:** an unusual API most reviewers haven't seen, confined to one
  named file that explains itself. The encapsulation stays intact.

---

## Security talking points

**Lead with the threat model — it's what makes the rest short.**

> It's a single-user app with no auth, and the deployed artifact is a static
> bundle with no server and no database. That doesn't *mitigate* CSRF, SQL
> injection, and broken access control — it removes them structurally. There's
> no session to forge a request against, no SQL, and no second user to have
> access to anything.

Then the finding you actually want to talk about:

> The genuinely interesting one was privacy, not security. The "development
> seeder" wasn't sample data — it had my real net worth, credit score, savings
> balances, and friends' names, and it said so in its own comments. It had
> spread into three test files and a code comment across three commits. So
> scrubbing the working tree wasn't enough; going public would have published
> all of it from history.
>
> I replaced it with a generated fixture, made the guarantee structural rather
> than careful — generate never capture, separate store, seed only into empty
> storage — and added tests that scan the seeded output for anything that looks
> personal.

**The part worth telling, because it's the mistake I nearly made:**

> I squashed the history to one clean commit and force-pushed, and then
> checked whether that had actually worked. It hadn't. Force-pushing doesn't
> delete anything on GitHub — the old commit was still fetchable by SHA
> through the API, with all the figures in it. If I'd flipped the repo to
> public at that point I'd have published exactly what I was trying to remove.
>
> So I pushed the clean commit to a brand-new repository instead, one that had
> never held the data in any commit. The takeaway I'd generalise: a force-push
> is not a delete. If it's a secret, rotate it. If it's data you can't rotate,
> publish from somewhere it has never been.

That answer is worth more than the original finding, because it shows you
verified a remediation instead of assuming it worked.

If asked what you *didn't* do:

> I didn't add CSP or HSTS headers in application code. GitHub Pages sends its
> own and ignores anything the app defines, so that middleware would pass code
> review and do nothing. That's worse than no header, because it looks like
> protection. I documented the gap instead.

---

## Database

- **Schema:** `Category` 1—* `MetricDefinition`; `MonthlySnapshot` 1—*
  `MetricSnapshot`; `MonthlySnapshot` 1—0..1 `SocialSnapshot`; `Friend` and
  `KeyRelationship` standalone; `AppSetting` a key/value table.
- **The aggregate:** `MonthlySnapshot` is the root — one monthly review, many
  metric values recorded together. `MetricSnapshot` can only be created through
  it, which is what keeps the "one review, many metrics" invariant in one place.
- **Constraints:** one snapshot per metric per month, enforced with a unique
  index. `SetMetricValue` upserts, so resubmitting a month fixes a typo instead
  of creating a duplicate.
- **Migrations:** EF Core, four of them, applied at startup **only** in
  Development. In a hosted environment that should be a deliberate deploy step.
- **Access:** repository interfaces on Application, EF implementations on
  Infrastructure. LINQ throughout, no raw SQL.
- **Design choice worth mentioning:** `SocialSnapshot` captures the active
  circle size at each review rather than deriving it on read. Otherwise a friend
  going quiet today would rewrite last year's history. The trend should show
  what was true then, not what would be true now.

**What breaks at scale:** `DashboardService` loads every metric's full snapshot
history to evaluate it — fine for one user with a handful of metrics and a few
years of monthly rows, but it's N+1-shaped and would need batching or a
projection well before it became multi-user. Say this before they find it.

---

## Deployment

> GitHub Pages, via Actions. I picked it mostly for what it *doesn't* need: no
> new account, no secret to store or rotate — the workflow authenticates with
> its own token. I checked the alternatives properly; every free .NET container
> host wants a credit card now, which fails the bar I set.
>
> The pipeline builds the wasm bundle, builds the SPA, publishes, and then
> smoke-tests the live URL — status code, the page body, that the runtime
> assets are actually reachable, and that a deep link serves the app. A green
> deploy step only proves an upload succeeded; the smoke test proves the site
> answered.

Two details worth volunteering, because they show you've actually deployed a
static SPA before:

> `.nojekyll` matters more than it sounds. Pages runs output through Jekyll,
> which silently drops anything starting with an underscore — and the .NET
> runtime lives in `_framework/`. It deploys green and then 404s on every
> runtime asset. That's why the smoke test fetches `dotnet.native.wasm`
> specifically.
>
> And the base path has exactly one source. A project page serves from
> `/<repo>/`; if the bundler's base and the router's basename are set
> separately they drift, and you get working assets with a 404 on every route.

**If asked about CI gating:** be straight about it.

> CI and deploy run in parallel rather than gated, so a deploy can publish
> while CI is still running. I accepted that — the deploy builds the same
> bundle, so a build failure fails both, and the smoke test catches a bad
> publish. The one-line fix is switching the deploy trigger to `workflow_run`
> on CI completion, and it's written down in the deployment doc.

---

## Testing

> 278 tests — 241 backend, 37 frontend. Domain logic is tested with no
> infrastructure at all, because Domain has no dependencies. Application
> services are tested against hand-written fakes rather than mocks; the
> interfaces are four methods each, so a fake is shorter than the mock setup
> and reads as data.
>
> What I deliberately *don't* test: React components that only map a status to
> a CSS class — that's a rename detector, not a bug detector. And I don't have
> a coverage threshold, because a number produces tests written to raise the
> number.

**The bit that lands:**

> When I started, eight tests were failing. Seven were stale expectations from
> a threshold change I hadn't finished. One was a genuine pre-existing failure
> — and it was tempting to "fix" it by changing the service, because the test's
> comment said Social should be excluded from the average when it has no data.
> But the service says the opposite explicitly, and it's right: zero friends is
> a real low score, not missing data. The comment was stale, not the code. So I
> fixed the test and wrote the reasoning into it. Changing product behaviour to
> satisfy an out-of-date assertion is how you turn a stale test into a real bug.

---

## Deliberate simplifications

Know where you *didn't* build something. It's a stronger signal than a longer
feature list.

| Not built | Why | What it would take |
| --- | --- | --- |
| Authentication | Single-user app that only ran on localhost | ASP.NET Identity or an OIDC provider; the demo would need seeded credentials |
| Multi-user / tenancy | No second user exists | An owner column on every table, and an authorization check on every read |
| Real-time updates | Data changes once a month | Nothing; polling would be over-engineering |
| Mobile apps | It's a ten-minute monthly ritual at a desk | The API is already JSON; a client is the only missing piece |
| Caching | A handful of rows, one user | Response caching or a memory cache on the dashboard read |
| Rate limiting | localhost, one user | ASP.NET's built-in rate limiter, if ever hosted |
| Soft-delete / audit history | Nothing is deleted; snapshots are already append-mostly | A deleted flag and an audit table |
| A rules DSL for metrics | Five strategies cover every metric so far | An expression parser — considered up front and rejected as premature |
| Injected clock (`TimeProvider`) | Not needed until it was | It *is* needed now — see "weakest part" below |
| Code splitting | One 580 kB chunk, mostly Recharts | Dynamic imports per route |

---

## The uncomfortable questions

**"Isn't four projects over-engineered for a personal dashboard?"**

> Probably, if you judge it on day one. But the persistence seam paid for
> itself twice — once for testing the services against fakes, and once when it
> turned out to be the only reason the whole app could be lifted into a browser
> for the demo. I didn't plan the second one. What I'd push back on is the idea
> that I added patterns for their own sake: there's no MediatR, no CQRS, no
> repository-per-entity, no DI container gymnastics. Four projects and seven
> interfaces is close to the floor for having a testable domain at all.

**"What's the weakest part?"**

> Time handling. Services call `DateTime.UtcNow` directly instead of taking an
> injected clock. It means date-sensitive behaviour isn't deterministically
> testable, and the app behaves differently either side of UTC midnight — you
> can actually see it in the demo, where logging a hangout "today" can read as
> one day ago. I found it because a test I'd pinned to a fixed date failed. I
> documented it rather than fixing it mid-deployment, because threading a
> `TimeProvider` through every date-aware service is its own change. That's the
> first thing I'd do next.

**"What would you do differently?"**

> Two things. I'd have put the demo fixture in from day one — the seeder grew
> into a marker-gated migration script that deleted rows on startup and held my
> real financial data, and that only happened because "just put my real numbers
> in for now" was easier each time. And I'd have written the frontend tests
> earlier; there was exactly one when I started, and it asserted nothing.

**"Why not just host the backend somewhere?"**

> I'd have preferred to, and if someone hands me a card-free .NET host I'll
> move it. I checked the current terms rather than going from memory — they all
> want a card now, even for a free tier. And the constraint turned out to be
> productive: it forced the question of whether the architecture could actually
> support swapping persistence, and it could.

**"Is the WebAssembly thing just a gimmick?"**

> It'd be a gimmick if I'd used Blazor to rewrite the UI. I didn't — the React
> app is unchanged, and there's no .NET UI anywhere. It's a 200-line interop
> façade over the services that already existed. The test for whether it's a
> gimmick is: did it stop me writing a second implementation of the business
> logic? It did.

**"How do I know the demo behaves like the real app?"**

> Because it's the same assemblies. And `DemoWorkspaceTests` runs the real
> services through the same `AddApplication()` composition root the API uses,
> against the in-memory repositories, asserting on real outputs. If the demo
> and the API could disagree, those tests would be where it shows.

---

## Things not to say

Each of these invites a follow-up you cannot win.

- ❌ **"It's production-ready."** It has no authentication. Say what's true:
  it's deployed, tested, and documented, and here's what it would need before
  it held anyone else's data.
- ❌ **"It's fully tested"** or any coverage percentage. You deliberately
  don't track one; say so and explain why.
- ❌ **"I used Clean Architecture."** Say what the layers actually are and what
  the boundary bought you. The label invites a purity argument; the fact
  doesn't.
- ❌ **"The demo is the real app."** It's the real *logic* with different
  persistence. The distinction is the whole point — don't blur it.
- ❌ **"It's secure."** Describe the threat model. "There's no auth, and here's
  why that's currently fine and what would change" is a much better answer.
- ❌ **"WebAssembly makes it fast."** It doesn't. It costs a 4 MB download. It
  buys you *one implementation*.
- ❌ **"I built it in a weekend."** Even if true, it invites doubt about
  everything above.
- ❌ Anything about the scoring being validated against research. The thresholds
  are reasoned defaults, retunable from the Settings page, and the design doc
  says so.
