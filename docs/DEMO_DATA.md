# Demo data

The deployed demo is public. It must never contain the owner's real data —
and this repository once did, which is exactly why the guarantee below is
structural rather than a matter of being careful.

---

## The three barriers

### 1. Generate, never capture

`backend/src/Dashboard.Demo/DemoDataset.cs` is a hand-written fixture describing
a fictional person. Every number in it was invented for the purpose.

The property that matters is what *doesn't* exist: there is no export step, no
dump, no snapshot, no "anonymize the real database" script anywhere in the
pipeline. There is no code path by which real data could reach the fixture,
because nothing reads a real database on the way in. The fixture is source
code, reviewed like source code.

### 2. Separate the namespaces

The demo and the real app cannot collide because they do not share a store:

| | Real app | Demo |
| --- | --- | --- |
| Persistence | PostgreSQL, via EF Core | `DemoStore` — in-memory `List<T>`s |
| Lifetime | Durable | The browser tab |
| Reached through | `Ef*Repository` | `InMemory*Repository` |
| Bound in | `AddInfrastructure()` | `DemoWorkspace.Create()` |

The deployed bundle contains no connection string, no database driver, and no
code that could open a network connection. It could not reach a real database
if it tried.

### 3. Seed only into empty storage

Both seeding paths refuse to run against anything that already holds data:

- `DemoSeeder.FillIfEmpty(store, today)` returns `false` and does nothing
  unless the store is completely empty.
- `DevelopmentDataSeeder.SeedAsync(dbContext)` returns immediately if any
  category exists.

This is a **tested property**, not a convention —
`DemoSeederTests.FillIfEmpty_WillNotTouchAStoreHoldingDataItDidNotSeed`
puts a record into a store and asserts that seeding leaves it byte-identical.

#### Two named operations, never one flag

"Fill an empty store" and "throw everything away and start again" are separate
methods:

```csharp
DemoSeeder.FillIfEmpty(store, today);    // additive, safe, idempotent
DemoSeeder.ResetAndFill(store, today);   // destructive, demo-only
```

Not `Seed(store, today, overwrite: true)`. A boolean makes the destructive
path reachable by passing the wrong argument — one transposed flag at one call
site and real data is gone. Two names mean a caller that wants the safe
operation cannot accidentally get the dangerous one; the dangerous one has to
be typed out by name. The startup path only ever calls `FillIfEmpty`.

---

## What's in the dataset, and why

Sample data that renders an empty dashboard demonstrates nothing. The fixture
is built so a reviewer understands the app in about ten seconds, and so that
no UI state goes undemonstrated.

### Coverage

- **Both categories** (Fitness, Finance) plus Social, all populated and scored.
- **All twelve rated metrics**, so every configured rating scale appears.
- **Every metric status**: Improved, Stagnant (Emergency Fund, flat on
  purpose), Regressed (Overhead Press), and InsufficientData.
- **Multiple rating tiers**, so the four-segment tier indicator is never
  showing the same segment everywhere.
- **Both trend directions**, including Waist Measurement, the one metric where
  *lower* is better — exercising `HigherIsBetter: false` end to end.
- **A calculated metric**: Strength Total, derived from the four lifts.
- **Every social state**: comfortably recent, recent, near the overdue edge,
  overdue, badly overdue, and dropped out of the active circle entirely.
- **Both key-relationship states**: one healthy, one overdue and alerting.

### Deliberate edge cases

| Case | Where |
| --- | --- |
| **Minimal record** — only required fields | Priya, no notes |
| **Very long value** | Devon's notes, far longer than a form would encourage |
| **Boundary value** | Bench Press ends on exactly 230 lb, the Intermediate/Advanced cutoff — where off-by-one rating bugs live |
| **Missing data mid-series** | Deadlift has no reading three months ago; the trend has to survive the gap |
| **Empty state** | The **current month is deliberately unrecorded** |
| **Inactive-but-retained record** | Nadia, 402 days — outside the active window, never deleted |

The empty current month is the most deliberate choice in the fixture. The app's
whole premise is a monthly ritual, so the demo opens in exactly the state that
ritual begins from: five months of history behind you, this month still to
fill in. It also gives a reviewer something real to do — enter this month's
numbers and watch the scores move.

### Relative dates

Every date is an offset from a supplied `today`, never a literal.

A fixture pinned to absolute timestamps rots. Opened a year later it shows dead
streaks, an empty "this month", and friends who are all catastrophically
overdue — an app that looks broken through nothing but the passage of time.
Offsets keep it alive indefinitely while staying perfectly deterministic for a
given `today`, which is what makes it testable.

`DemoSeederTests.SeededDatesMoveWithToday_SoTheFixtureCannotGoStale` pins this:
seeding with `today` and `today + 1 year` must produce the same shape shifted
by exactly a year.

---

## How seeding works

**In the demo (browser).** `DemoWorkspace.Create(today)` builds a `DemoStore`,
calls `DemoSeeder.FillIfEmpty`, then wires the *real* application services to
in-memory repositories through the *same* `AddApplication()` composition root
the API uses.

**In local development.** `Program.cs` calls `DevelopmentDataSeeder.SeedAsync`
after applying migrations — only under `ASPNETCORE_ENVIRONMENT=Development`,
and only into a database with no categories.

Both read the same `DemoDataset`, so a local database and the public demo can
never show different things.

### A note on what this replaced

The previous `DevelopmentDataSeeder` was not really a seeder. It was eight
marker-gated one-time migration steps that deleted rows
(`MetricSnapshots.RemoveRange(...)`) and wrote real balances over them, running
automatically on every Development startup, covered by no test. Those steps had
long since run wherever they were going to run. They were replaced by the
single "is it empty?" guard described above.

---

## Resetting

**In the demo:** `DemoApi.Reset()` → `DemoWorkspace.Reset(today)` →
`DemoSeeder.ResetAndFill`. Exposed to the UI as `resetDemoData`, which is
`null` on the HTTP adapter — typed as a nullable capability rather than a
method that throws, so the UI can only offer the control where it genuinely
exists. A page refresh has the same effect, since nothing persists.

**Locally:** drop and recreate the database, then restart the API.

```bash
dropdb vantage_dev && createdb vantage_dev
dotnet run --project backend/src/Dashboard.Api
```

There is deliberately no "reset" endpoint on the real API. Resetting someone's
actual data is not a feature.

---

## Credentials

**There are none, and the demo has no login.** The app has no authentication at
all — it was built as a single-user tool that only ever ran on `localhost`, and
the demo gives every visitor their own throwaway in-memory copy.

This is stated here and in the README rather than inventing a demo account,
because a reviewer hunting for credentials that do not exist is a worse
experience than being told plainly that none are needed.

---

## The tests that enforce this

In `backend/tests/Dashboard.Demo.Tests/`:

| Test | What it guarantees |
| --- | --- |
| `SeededContent_ContainsNothingThatLooksPersonal` | No email, phone number, URL, credential-shaped word, or national ID pattern anywhere in the seeded content |
| `SeededContent_DoesNotContainThePreviouslyCommittedRealFigures` | The specific figures that were once in this repository cannot reappear |
| `SeededFriendNames_AreNotTheRealOnesThatWereCommitted` | Nor can the real first names |
| `FillIfEmpty_WillNotTouchAStoreHoldingDataItDidNotSeed` | Seeding cannot overwrite existing data |
| `ResetAndFill_ReplacesEverythingAndReproducesTheFixtureExactly` | Reset is complete and identity-stable |
| `SeededDatesMoveWithToday_SoTheFixtureCannotGoStale` | The fixture cannot rot |
| `TheCurrentMonthIsDeliberatelyLeftUnrecorded` | The intended empty state stays intended |

The privacy tests scan the **seeded store**, not the source file. Reading the
literals would only prove that one file looks clean; walking the store proves
that whatever actually reaches a browser is clean, including anything a future
change might compute or concatenate on the way in.
