# Security

## Threat model first

Most of this document is short because of what follows, so it comes first.

**What Vantage is:** a single-user, single-tenant application with no
authentication, no accounts, no multi-user data, no file uploads, no
user-generated HTML, no outbound integrations, and no payment data. The only
personal information in it is what the owner types about themselves. Run
locally, it binds to `localhost`.

**What is deployed:** a static bundle. No server process, no database, no
network calls after load. The .NET code runs inside the browser's WebAssembly
sandbox against an in-memory fixture.

That last point removes whole categories of vulnerability *structurally*, and
naming what is structurally absent is more honest than a checklist of items
marked N/A:

| Category | Why it does not apply |
| --- | --- |
| **CSRF** | No server session and no cookie. There is nothing for a forged request to ride on, and no server to receive one. |
| **SQL injection** | The deployed demo has no SQL and no database. In the self-hosted app, EF Core parameterizes everything and there is no raw SQL anywhere in the repository. |
| **Broken access control / IDOR** | One user, no accounts, no ownership relationships, nothing to enumerate. There is no authorization decision to get wrong. |
| **Secrets in the client** | The deployed artifact holds no credentials because it talks to nothing. Its only inputs are files served alongside it. |
| **Server-side injection (command, template, deserialization)** | No server. |
| **Session fixation / weak password storage** | No sessions and no passwords. |

What remains is a small, real list.

---

## Findings and fixes

### S1 — Personal financial data committed to the repository · **Critical** · Fixed

The committed "development seeder" contained the author's actual figures, and
said so in its own comments:

| Value | Where |
| --- | --- |
| Net Worth $236,200 | `DevelopmentDataSeeder.cs`, plus three test files |
| Credit Score 809 | seeder, tests, and a Domain code comment |
| Emergency Fund $31,193 | seeder and `CategoryDetailServiceTests` |
| Retirement Fund $74,095 | seeder |
| Four friends' first names | seeder and `SocialEndpointTests` |
| Exact dates of last contact with wife and mother | seeder |

The values had spread from the seeder into tests and comments across three
commits, so scrubbing the working tree alone would not have removed them —
they would have remained readable in history the moment the repository went
public.

**Fixed by:**

1. Replacing the seeder with one that builds from `DemoDataset`, a generated
   fixture for a fictional person (see [DEMO_DATA.md](DEMO_DATA.md)).
2. Replacing every real figure in the test suite with values chosen to produce
   the same *behaviour* under test — Net Worth `170_000` sits exactly 60%
   through its Tier2 band for a clean score of 40 — and updating the
   expectations and comments to match.
3. Renaming placeholder friend names in tests, so that no first name that was
   ever a real contact appears anywhere in the repository. This was belt and
   braces: they read as generic test names, but "none appear" is a cleaner
   property to state than "they appear but coincidentally".
4. Removing the real credit score from a `MetricRatingCalculator` doc comment.
5. **Replacing git history with a single clean initial commit** before making
   the repository public. Eleven solo commits, no collaborators depending on
   them.
6. Adding `DemoDatasetPrivacyTests`, which fails the build if any of those
   specific figures or names reappears, or if anything email-, phone-, URL-, or
   credential-shaped enters the fixture.

**Residual risk, stated plainly:** the repository was private throughout. If it
had ever been public, cloned, or forked, a history rewrite would not undo that
— the correct response then would have been to treat the figures as disclosed.
Nothing here was a credential, so there was nothing to rotate.

### S2 — `AllowedHosts: "*"` and a hardcoded dev CORS origin · **Low** · Documented

`appsettings.json` ships `AllowedHosts: "*"`, and `Program.cs` pins the CORS
policy to `http://localhost:5180`.

Both are correct for what this is: a local single-user API. Neither reaches
the deployed artifact, which has no server. If the API were ever hosted,
`AllowedHosts` should be narrowed to the real hostname and the CORS origin
moved into configuration. Left as-is deliberately rather than "hardened"
into a value that would be equally wrong somewhere else.

### S3 — No rate limiting on the API · **Low** · Accepted

Meaningless on `localhost` with one user. Would matter if hosted; noted so
that hosting it is a decision made with the gap in view.

### S4 — Injection surface · **None found** · Verified

EF Core with LINQ throughout; no raw SQL, no string-concatenated queries, no
dynamic SQL anywhere in the repository. React escapes rendered text by
default, and the app never uses `dangerouslySetInnerHTML`.

### S5 — Credentials in git history · **None found** · Verified

The full history of all eleven original commits was scanned for connection
strings, keys, tokens, and private-key blocks. Every match was a placeholder
(`<your-pg-user>`, `placeholder`, `unused`) or a PowerShell `Read-Host` prompt
that reads a password interactively and never writes it down. Nothing needed
rotating.

---

## Input validation at trust boundaries

Every point where data enters from outside:

| Boundary | Validation |
| --- | --- |
| **HTTP request bodies** | Typed request records; ASP.NET Core model binding rejects malformed JSON before a controller runs. |
| **Settings writes** | `SettingsService.SetAsync` rejects unknown keys with `KeyNotFoundException` and validates the value against the setting's declared `AppSettingValueKind` before storing. A value none of the readers could parse is never persisted. |
| **Metric entries** | `MetricEntryService` verifies the category exists *and* that every submitted metric id belongs to that category, so a well-formed request cannot write a value onto another category's metric. |
| **Domain constructors** | Reject empty names and units; `MetricDefinition` rejects a strategy/config mismatch (a `StayAbove` without a threshold) at construction rather than letting it fail confusingly inside an evaluator later. |
| **Stored settings on read** | `AppSettingReader` falls back to the declared default when a stored value won't parse. Deliberately total: one bad row must not take down the dashboard. |
| **Environment variables** | `frontend/src/lib/config.ts` is pure and total, unit-tested against hostile input. A typo cannot crash startup and — the case that actually matters — cannot silently select the wrong data source. |
| **JS → WASM interop** | `DemoApi` parses dates and the entry-values JSON explicitly, returning a structured error rather than throwing across the boundary. |

---

## Data protection

The deployed demo holds no personal data by construction — see
[DEMO_DATA.md](DEMO_DATA.md) for the three barriers. In the self-hosted app,
data lives in a local PostgreSQL database the owner controls, with no
telemetry, no analytics, and no third-party requests of any kind.

**Logging.** The app logs at framework level only; no application code logs
user content. The one log line the demo emits is counts —
`2 categories, 12 metrics, 5 months, 6 friends` — deliberately scalars, never
names or values, so it is safe to leave on in production.

**No third-party error reporting.** Not an oversight: shipping a personal
life-metrics app's errors to a vendor would mean sending exactly the data the
app exists to keep private, in exchange for information a single-user app does
not need. The platform's own logs are enough.

---

## Secrets management

- One real secret exists: the PostgreSQL connection string.
- Development: `dotnet user-secrets`, stored outside the repository.
- Hosted: the `ConnectionStrings__Vantage` environment variable.
- No default, and startup fails with an explicit message if it is missing —
  rather than silently falling back to something that half-works.
- `.gitignore` covers `.env`, `.env.local`, and `appsettings.*.local.json`.
- `.env.example` documents every variable and states at the top that the
  `VITE_` prefix inlines a value into a publicly downloadable bundle, so it
  must never hold a credential.
- **The deployed artifact has no secrets**, because it makes no requests.

---

## CI scanning

`.github/workflows/ci.yml` runs on every push and pull request:

- **`npm audit --audit-level=high`** — gated at high on purpose. This project's
  low-severity findings are in build-time transitive dependencies that never
  reach a browser; a gate that fires on those is one people learn to ignore.
- **`dotnet list package --vulnerable --include-transitive`**, failing the job
  on High or Critical. The command exits 0 even when it finds something, so the
  step inspects the output rather than trusting the exit code.
- **gitleaks over the full history** (`fetch-depth: 0`). A scan that only sees
  the tip commit would miss anything committed and later removed — which is
  precisely the case worth scanning for, and precisely what happened here.
- **Frozen lockfiles** — `npm ci` and NuGet `RestoreLockedMode`, so a
  dependency cannot silently resolve to something other than what was audited.

---

## Deliberately not done

Each of these would be **security theater** here, and shipping theater is worse
than shipping nothing, because it looks like protection:

- **CSP, HSTS, X-Frame-Options configured in application code.** GitHub Pages
  sends its own headers and ignores any the application defines. A middleware
  adding them would pass review and do literally nothing. The real gap: the
  deployed demo runs under Pages' default headers and cannot set its own.
  Stated, not papered over.
- **Input sanitizers on values React renders as text.** React escapes them
  already; a sanitizer would imply a threat that does not exist and might
  mangle legitimate input.
- **Authentication on the demo.** There is nothing to protect — every visitor
  gets their own throwaway in-memory copy of a generated fixture.
- **Encrypting the demo fixture.** It is checked into a public repository by
  design.

---

## Remaining risks

Stated plainly rather than closed off:

1. **No authentication.** If this app were ever hosted with real data, it would
   need auth before it went anywhere near a public URL. It has none today, and
   nothing in the codebase pretends otherwise.
2. **Pages controls the response headers.** No CSP, and the app cannot set one.
3. **Deep links return HTTP 404.** The SPA fallback renders correctly, but the
   status code is genuinely 404 — Pages offers no rewrite. Cosmetic here;
   would matter for SEO or uptime checks.
4. **The demo trusts its own fixture.** It is not defensive against a corrupted
   fixture, because the fixture is code in the same repository. If it were ever
   loaded from a remote source, that assumption would have to change.
5. **`AllowedHosts: "*"`** ships in the committed `appsettings.json`. Harmless
   for local use; would need narrowing before hosting the API.
6. **Dependency risk is real and ongoing.** The audit gate catches known
   high-severity advisories at build time. It cannot catch an advisory
   published after the last build, and nothing here runs on a schedule.
