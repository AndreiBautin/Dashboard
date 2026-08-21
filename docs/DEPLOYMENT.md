# Deployment

**Live:** <https://andreibautin.github.io/Dashboard/>

Pushes to `main` build the WebAssembly runtime and the SPA, publish to GitHub
Pages, and then smoke-test the live URL.

---

## Provider: GitHub Pages

### Why

- **Genuinely free, no credit card, at any point.** Verified against GitHub's
  current published limits rather than from memory.
- **No new account and no new secret.** The repository is already on GitHub;
  the deploy workflow authenticates with its own `GITHUB_TOKEN`. Nothing to
  store, nothing to rotate, nothing to leak. This was the single strongest
  argument.
- **The artifact is static**, so a static host is not a compromise — it is the
  correct shape.
- **Deploys are ordinary CI.** No CLI to install, no dashboard to click.

### Why not the alternatives

| Option | Rejected because |
| --- | --- |
| **Frontend on Pages + API on Fly.io / Render / Railway** | Every viable .NET container host now asks for a card at signup or to leave a trial, which fails the "free means free" bar. It would also add an account, a secret, a CORS policy, a cold start, and a database to keep alive — a great deal of moving parts for a demo. |
| **Azure App Service free tier** | The F1 tier is free, but an Azure subscription requires a card even when nothing is billable. |
| **Cloudflare Pages / Netlify** | Both would work and neither needs a card. Rejected only because they add an account and a token for no gain over Pages, which is already where the code lives. Either is a drop-in replacement if Pages ever becomes unsuitable. |
| **Hugging Face Spaces (Docker)** | Genuinely free and cardless, and could run the real API. Rejected as an odd fit for a portfolio link, with sleep-on-idle latency. |
| **Reimplementing the scoring engine in TypeScript** | Not a hosting option, but the usual answer to this problem. Rejected: 1,270 lines of business logic would become a second implementation that drifts. A demo that disagrees with the app is worse than no demo. |

### The consequence, stated plainly

Pages cannot run ASP.NET Core, so **the deployed site is not running the API**.
It runs the real `Vantage.Application` and `Vantage.Domain` assemblies compiled
to WebAssembly, against in-memory repositories. Same logic, different
persistence. See [ARCHITECTURE.md](ARCHITECTURE.md).

**GitHub Pages on a free account only serves public repositories.** That is why
this repository is public, and why the git history was rewritten to remove
personal data first — see [SECURITY.md](SECURITY.md#s1).

---

## Required accounts

One: a GitHub account, which already existed. **No credit card at any point.**

---

## One-time setup

Already done for this repository; recorded so it can be reproduced.

1. **Repository → Settings → Pages → Build and deployment → Source:
   *GitHub Actions***. Not "Deploy from a branch" — the workflow publishes an
   artifact directly.
2. The repository must be **public** on a free plan.
3. Nothing else. No secrets, no environment variables, no tokens.

---

## Environment variables

Set by `.github/workflows/deploy.yml`; none are secret.

| Variable | Value in CI | Purpose |
| --- | --- | --- |
| `VITE_DATA_SOURCE` | `demo` | Set by `build-demo.mjs`. Selects the WebAssembly adapter. |
| `VITE_BASE_PATH` | `/${{ github.event.repository.name }}/` | The project-page subpath. Drives Vite's `base` *and*, through `BASE_URL`, the router's basename. |
| `VITE_COMMIT` | `${{ github.sha }}` | Build identification. |

Derived from the repository name rather than hardcoded, so a rename does not
silently produce a site whose assets 404.

**No secret is ever passed to the frontend build.** Anything prefixed `VITE_`
is inlined into a publicly downloadable bundle — `.env.example` opens with that
warning.

---

## Database and migrations

**The deployed demo has neither**, and that is not a gap to be filled: it holds
an in-memory fixture that resets on refresh.

For a self-hosted deployment of the real API, migrations are EF Core and run
automatically at startup **only under Development**:

```bash
dotnet ef migrations add <Name> --project backend/src/Vantage.Infrastructure --startup-project backend/src/Vantage.Api
dotnet ef database update --project backend/src/Vantage.Infrastructure --startup-project backend/src/Vantage.Api
```

Automatic migration-on-startup is a local-development convenience. It should
not be relied on in a hosted environment, where migrations belong in a
deliberate deploy step.

---

## Seeding and resetting

The demo seeds itself on load from `DemoDataset` and can be reset from the UI
or by refreshing. Local development seeds the same fixture into an **empty**
database only. Full detail: [DEMO_DATA.md](DEMO_DATA.md).

---

## How a deploy happens

```
push to main
   │
   ├── ci.yml ─────────────── backend build + test
   │                          frontend lint + test + build
   │                          demo build (the configuration that ships)
   │                          npm audit · NuGet audit · gitleaks (full history)
   │
   └── deploy.yml ─────────── install wasm-tools
                              npm ci
                              npm run build:demo   (VITE_BASE_PATH=/Dashboard/)
                              upload-pages-artifact
                              deploy-pages
                              smoke test ─ the live URL, the runtime, a deep link
```

### CI and deploy run in parallel, not gated

This is a deliberate trade-off and worth knowing about rather than discovering.
A push starts both workflows at once, so **a deploy can publish while CI is
still running**, and in principle a red CI run can coincide with a successful
deploy.

Accepted because: the deploy job builds the same demo bundle CI builds, so a
compile or bundling failure fails the deploy too; the smoke test verifies the
live site afterwards; and this is a single-developer portfolio project where a
few minutes of a bad demo costs nothing.

**To gate them instead**, add to `deploy.yml`:

```yaml
jobs:
  build:
    # Wait for CI on this commit before publishing anything.
    needs: [] # ← replace with a workflow_run trigger, or inline the CI jobs here
```

The one-line version: change `deploy.yml`'s trigger from
`on: push: branches: [main]` to

```yaml
on:
  workflow_run:
    workflows: [CI]
    types: [completed]
    branches: [main]
```

and guard the build job with
`if: github.event.workflow_run.conclusion == 'success'`.

### Updating the site

Push to `main`. There is no manual step. `workflow_dispatch` is also enabled on
both workflows for a re-run without a commit.

---

## Building it locally

```bash
cd frontend
npm run build:demo          # served from /
npx serve dist
```

For the project-page layout, set the base path — and note that **Git Bash on
Windows rewrites a leading-slash value into a Windows path**, which produces
asset URLs like `/C:/Program Files/Git/Dashboard/...`. Disable that conversion:

```bash
MSYS_NO_PATHCONV=1 VITE_BASE_PATH=/Dashboard/ npm run build:demo
```

PowerShell and Linux need no such workaround.

To reproduce the deployed layout exactly, serve a parent directory containing
`Dashboard/` rather than serving `dist` directly — otherwise the base path and
the served path disagree.

---

## Troubleshooting

Real failures encountered building this, with their actual fixes.

| Symptom | Cause | Fix |
| --- | --- | --- |
| Site loads, then every `_framework/*` asset 404s | Pages ran the output through Jekyll, which silently drops files and directories starting with `_`. The .NET runtime lives in `_framework/`. | Ensure `dist/.nojekyll` exists. `build-demo.mjs` writes it; the deploy smoke test asserts the runtime is reachable, because this failure only appears in production. |
| Assets load but every route 404s | Vite's `base` and the router's `basename` were configured separately and drifted. | Both derive from one value. `VITE_BASE_PATH` → Vite `base` → `BASE_URL` → `toRouterBasename()`. Never set the router's basename independently. |
| Asset URLs contain `C:/Program Files/Git/...` | Git Bash's MSYS path conversion mangled `VITE_BASE_PATH`. | `MSYS_NO_PATHCONV=1`, or build from PowerShell. Does not affect Linux CI. |
| `dotnet publish` fails: `browser-wasm` not a valid RID | The `wasm-tools` workload is missing. | `dotnet workload install wasm-tools`. The deploy workflow does this explicitly; it is not preinstalled on the runner. |
| Demo build fails on Windows with `EINVAL … spawnSync npm.cmd` | Node 20+ refuses to spawn a `.cmd` shim from `execFileSync` without a shell. | `build-demo.mjs` invokes `tsc` and `vite` through `process.execPath` instead of going via npm. |
| `error MSB4024: An XML comment cannot contain '--'` | A comment in `Directory.Build.props` contained a double hyphen. | XML comments cannot contain `--`. Reword. |
| CI fails `NU1004: ... ILLink.Tasks version has changed from [9.0.18, ) to [9.0.19, )` | Locked restore against a project whose packages are SDK-derived. `Microsoft.NET.ILLink.Tasks` tracks the installed runtime patch, so the lock records whichever machine last restored. | `Vantage.Wasm` sets `RestorePackagesWithLockFile=false`; the nine projects with real third-party dependencies keep locked restore. |
| CI fails `NETSDK1147: workloads must be installed: wasm-tools-net9` | The workload id is SDK-version dependent, and the runner resolved a newer SDK than `global.json` intended. | Pin `dotnet-version` in `setup-dotnet`, and use `dotnet workload restore <csproj>` so the requirement is read from the project rather than hardcoded. |
| Serialized properties missing only in the deployed build | Reflection-based `System.Text.Json` under a trimmed WASM publish. | `DemoJsonContext` is source-generated, so every type is a statically visible reference the trimmer keeps. |
| Deep link returns HTTP 404 in a link checker, though the page renders | Pages serves `404.html` with a genuine 404 status. There is no rewrite rule. | Cosmetic; the app renders correctly because the router takes over. Accepted rather than switching to hash routing. |
| Demo shows an unexpectedly old "this month" | The fixture is relative to `today`, but the services read `DateTime.UtcNow` — which is a day ahead of local time for part of each day. | Known and documented; see *Known issues* below. |

---

## Free-tier limits, with actual headroom

Verified against GitHub's published limits.

| Limit | Allowance | This site | Headroom |
| --- | --- | --- | --- |
| Published site size | 1 GB | ~4.7 MB | **~0.5% used** |
| Bandwidth (soft) | 100 GB/month | ~4.7 MB per cold load | **~21,000 cold loads/month** |
| Builds per hour (soft) | 10 | Not applicable — the limit does not apply when publishing with a custom Actions workflow, which this does | — |
| Actions minutes | 2,000/month on free for private repos; **unlimited for public repositories** | ~4 min per deploy | Not a constraint |

Repeat visits are far cheaper than the cold-load figure: the runtime is
cache-friendly and served with normal caching headers, so the 21,000 figure is
a conservative floor rather than a ceiling.

The bundle is 4.1 MB of that total, dominated by `dotnet.native.wasm` (1.5 MB)
and `System.Private.CoreLib.wasm` (1.3 MB). It was reduced from 8.4 MB by
enabling `InvariantGlobalization` (dropping ~2.6 MB of unused ICU locale data)
and disabling debug symbol emission.

---

## Known issues

Not blockers, but real, and better stated than discovered.

1. **Deep links return HTTP 404** with correct content, as above.
2. **First load downloads ~4 MB.** Unavoidable for a .NET runtime in the
   browser. Subsequent loads are cached.
3. **The demo does not persist.** By design — refreshing restores the fixture.
4. **UTC/local date skew.** The UI sends a browser-local date while the
   services compute against `DateTime.UtcNow`, so for part of each day an
   action logged "today" reads as one day ago. The app reads the clock directly
   rather than through an injected abstraction, which is the underlying cause.
   Visible in the demo as *"1 days since last hangout"* immediately after
   logging one — which also shows the string is not pluralized. Both are
   pre-existing behaviours of the app, left alone here rather than changed
   quietly as part of a deployment task.
