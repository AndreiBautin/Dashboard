#!/usr/bin/env node
/**
 * Builds the public demo: the .NET WebAssembly runtime plus a Vite bundle
 * configured to talk to it.
 *
 * This is a Node script rather than a chain of npm scripts because the steps
 * involve setting environment variables and copying directories, and the
 * shell syntax for both differs between Windows and the Linux CI runner.
 * Anything that has to work identically in both places belongs here, where it
 * is written once.
 *
 * Usage:
 *   node scripts/build-demo.mjs                 # served from /
 *   VITE_BASE_PATH=/Dashboard/ node scripts/build-demo.mjs
 */

import { execFileSync } from 'node:child_process'
import { cpSync, existsSync, mkdirSync, rmSync, copyFileSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const frontendDir = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const repoRoot = resolve(frontendDir, '..')
const wasmProject = join(repoRoot, 'backend', 'src', 'Vantage.Wasm', 'Vantage.Wasm.csproj')
const appBundle = join(repoRoot, 'backend', 'src', 'Vantage.Wasm', 'bin', 'Release', 'net9.0', 'browser-wasm', 'AppBundle')

/** Where the runtime is served from, relative to the site root. Mirrored in demoApi.ts. */
const wasmPublicDir = join(frontendDir, 'public', 'dashboard-wasm')

function run(command, args, options = {}) {
  console.log(`\n> ${command} ${args.join(' ')}`)
  execFileSync(command, args, { stdio: 'inherit', ...options })
}

// 1. Publish the WebAssembly bundle. Publish rather than build: only publish
//    applies trimming, and an untrimmed bundle is roughly twice the size.
run('dotnet', ['publish', wasmProject, '-c', 'Release'])

if (!existsSync(appBundle)) {
  throw new Error(`Expected a published app bundle at ${appBundle}, but it is not there.`)
}

// 2. Stage it under public/, which Vite copies verbatim into dist/.
//    Cleared first so a removed framework file cannot survive as a stale
//    artifact and mask a broken build.
rmSync(wasmPublicDir, { recursive: true, force: true })
mkdirSync(wasmPublicDir, { recursive: true })
cpSync(join(appBundle, '_framework'), join(wasmPublicDir, '_framework'), { recursive: true })
console.log(`\nStaged the .NET runtime into ${wasmPublicDir}`)

// 3. Build the SPA in demo mode — the same `tsc -b && vite build` the
//    `build` script runs, but with VITE_DATA_SOURCE set.
//
//    Invoked through node directly rather than as `npm run build`: Node 20+
//    refuses to spawn a `.cmd` shim from execFileSync without a shell, so the
//    npm route fails on Windows and works on Linux. Calling the tools' own
//    entry points sidesteps the shell on both.
const demoEnv = { ...process.env, VITE_DATA_SOURCE: 'demo' }
const inFrontend = { cwd: frontendDir, env: demoEnv }

run(process.execPath, [join(frontendDir, 'node_modules', 'typescript', 'bin', 'tsc'), '-b'], inFrontend)
run(process.execPath, [join(frontendDir, 'node_modules', 'vite', 'bin', 'vite.js'), 'build'], inFrontend)

// 4. SPA fallback. A static host has no router, so a deep link like
//    /social is a request for a file that does not exist. GitHub Pages serves
//    404.html for those, and serving a copy of index.html there lets the
//    client-side router take over.
//
//    Worth being precise about: the response status really is 404, not 200.
//    The page renders correctly, but a crawler or an uptime check hitting a
//    deep link sees a 404. Pages offers no rewrite rule to fix this; the only
//    alternative is hash routing, which is uglier. Documented in
//    docs/DEPLOYMENT.md rather than papered over.
const dist = join(frontendDir, 'dist')
copyFileSync(join(dist, 'index.html'), join(dist, '404.html'))
console.log('\nWrote dist/404.html as the SPA fallback.')

// 5. .nojekyll. Without it GitHub Pages runs the output through Jekyll, which
//    silently drops every file and directory whose name begins with an
//    underscore. The .NET runtime lives in _framework/, so the deploy would
//    report success and then 404 on every runtime asset — a failure that only
//    appears in production and looks nothing like its cause.
writeFileSync(join(dist, '.nojekyll'), '')
console.log('Wrote dist/.nojekyll so Pages serves _framework/.')

console.log('\nDemo build complete.')
