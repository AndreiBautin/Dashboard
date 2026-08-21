/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'
import { execSync } from 'node:child_process'

/**
 * The base path the app is served from. A GitHub Pages project page serves
 * from `/<repo>/`, a local dev server from `/`.
 *
 * This is the *only* place the value is configured. Vite exposes it at
 * runtime as `import.meta.env.BASE_URL`, which is what `config.ts` reads and
 * what the router's basename is derived from. Configuring the bundler and the
 * router separately is the classic way to end up with assets that load
 * correctly and a 404 on every route.
 */
function normalizeBasePath(raw: string | undefined): string {
  if (!raw) return '/'
  const trimmed = raw.trim()
  if (trimmed === '' || trimmed === '/') return '/'
  const withLeading = trimmed.startsWith('/') ? trimmed : `/${trimmed}`
  return withLeading.endsWith('/') ? withLeading : `${withLeading}/`
}

/**
 * Build identification, so a deployed page can be tied back to a commit.
 * Falls back to "dev" rather than failing the build when git is unavailable
 * or the source is a tarball — a missing SHA is not a reason to be unable to
 * build.
 */
function currentCommit(): string {
  if (process.env.VITE_COMMIT) return process.env.VITE_COMMIT
  try {
    return execSync('git rev-parse --short HEAD', { stdio: ['ignore', 'pipe', 'ignore'] })
      .toString()
      .trim()
  } catch {
    return 'dev'
  }
}

export default defineConfig(() => {
  const base = normalizeBasePath(process.env.VITE_BASE_PATH)

  return {
    base,
    plugins: [react(), tailwindcss()],
    define: {
      'import.meta.env.VITE_COMMIT': JSON.stringify(currentCommit()),
      'import.meta.env.VITE_BUILT_AT': JSON.stringify(new Date().toISOString()),
    },
    server: {
      // Pinned, and deliberately off Vite's default 5173 so a second Vite app
      // can't take it first. The API's CORS policy allows exactly this origin,
      // so drifting to the next free port breaks every request — fail instead.
      // Kept in sync with start-app.bat and Program.cs.
      port: 5180,
      strictPort: true,
    },
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
    },
  }
})
