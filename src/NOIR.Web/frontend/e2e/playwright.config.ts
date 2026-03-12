import { defineConfig, devices } from '@playwright/test';

/**
 * NOIR E2E Test Configuration
 *
 * Performance strategy (target: 56 flows in <10min):
 * 1. fullyParallel — all tests run concurrently across workers
 * 2. storageState — login once, reuse auth across all tests
 * 3. API fixtures — seed data via API, skip UI setup
 * 4. Sharding — split across 4 CI machines (--shard=1/4)
 * 5. Smart retries — only retry on CI, first-retry trace
 * 6. Single browser for dev/PR — all browsers for nightly only
 */

const CI = !!process.env.CI;
const BASE_URL = process.env.BASE_URL ?? 'http://localhost:3000';
const API_URL = process.env.API_URL ?? 'http://localhost:4000';

export default defineConfig({
  testDir: './tests',

  /* ── Performance ─────────────────────────────────────────── */
  fullyParallel: true,
  workers: CI ? '50%' : 4, // Local: 4 workers (prevents CPU hang); CI: half cores
  timeout: 30_000,                 // 30s per test (fail fast, don't hang)
  expect: { timeout: 5_000 },     // 5s assertion timeout

  /* ── Reliability ─────────────────────────────────────────── */
  retries: CI ? 2 : 0,
  forbidOnly: CI,

  /* ── Reporting ───────────────────────────────────────────── */
  reporter: CI
    ? [['html', { open: 'never' }], ['github'], ['json', { outputFile: 'test-results/results.json' }]]
    : [['html', { open: 'on-failure' }], ['list']],

  /* ── Shared config ───────────────────────────────────────── */
  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',           // Trace only on failure retry
    screenshot: 'only-on-failure',
    video: CI ? 'on-first-retry' : 'off',
    actionTimeout: 10_000,             // 10s per action
    navigationTimeout: 15_000,         // 15s per navigation

    /* Multi-tenancy header for API calls */
    extraHTTPHeaders: {
      'X-Tenant': 'default',
    },
  },

  /* ── Test Projects ───────────────────────────────────────── */
  projects: [
    // ─── Auth Setup (runs first, saves storageState) ───────
    {
      name: 'auth-setup',
      testDir: './fixtures',
      testMatch: /auth\.setup\.ts/,
    },

    // ─── Main Tests: Chromium (default for dev + PR) ───────
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        storageState: '.auth/admin.json',
      },
      dependencies: ['auth-setup'],
    },

    // ─── UI Audit: tenant admin (most pages) ────────────────
    {
      name: 'ui-audit',
      testDir: './tests/ui-audit',
      testMatch: /audit-runner\.spec\.ts|storybook-audit\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        storageState: '.auth/admin.json',
        viewport: { width: 1440, height: 900 },
        screenshot: 'off',
        video: 'off',
      },
      dependencies: ['auth-setup'],
      workers: 4,
      timeout: 60_000,
    },

    // ─── UI Audit: platform admin (elevated pages) ────────
    {
      name: 'ui-audit-platform',
      testDir: './tests/ui-audit',
      testMatch: /audit-runner-platform\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        storageState: '.auth/platform-admin.json',
        viewport: { width: 1440, height: 900 },
        screenshot: 'off',
        video: 'off',
      },
      dependencies: ['auth-setup'],
      workers: 2,
      timeout: 60_000,
    },

    // ─── Cross-browser: only in CI nightly ─────────────────
    ...(process.env.NIGHTLY ? [
      {
        name: 'firefox',
        use: {
          ...devices['Desktop Firefox'],
          storageState: '.auth/admin.json',
        },
        dependencies: ['auth-setup'],
      },
      {
        name: 'webkit',
        use: {
          ...devices['Desktop Safari'],
          storageState: '.auth/admin.json',
        },
        dependencies: ['auth-setup'],
      },
      {
        name: 'mobile-chrome',
        use: {
          ...devices['Pixel 7'],
          storageState: '.auth/admin.json',
        },
        dependencies: ['auth-setup'],
      },
    ] : []),
  ],

  /* ── Dev Servers ─────────────────────────────────────────── */
  webServer: [
    {
      command: 'dotnet run --project ../../../NOIR.Web',
      url: `${API_URL}/api/health`,
      reuseExistingServer: !CI,
      timeout: 120_000, // .NET cold start
      stdout: 'pipe',
    },
    {
      command: 'pnpm run dev',
      cwd: '..',
      url: BASE_URL,
      reuseExistingServer: !CI,
      timeout: 30_000,
      stdout: 'pipe',
    },
  ],
});

/**
 * Environment variables reference:
 *
 * CI=true          — CI mode (retries, strict, video)
 * NIGHTLY=true     — Run all browsers (firefox, webkit, mobile)
 * BASE_URL=...     — Frontend URL (default: http://localhost:3000)
 * API_URL=...      — Backend URL (default: http://localhost:4000)
 * SHARD=1/4        — Test sharding for parallel CI
 */
