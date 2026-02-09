import { defineConfig, devices } from '@playwright/test';
import * as path from 'path';

/**
 * NOIR E2E Test Configuration
 * Updated: 2026-02-09 - Fixed workflow module resolution
 *
 * Run with:
 *   npx playwright test              # Run all tests
 *   npx playwright test --headed     # Run with browser visible
 *   npx playwright test --ui         # Open interactive UI mode
 *   npx playwright test --grep @smoke # Run smoke tests only
 */

// Storage state paths for authenticated sessions
export const STORAGE_STATE_DIR = path.join(__dirname, '.auth');
export const TENANT_ADMIN_STATE = path.join(STORAGE_STATE_DIR, 'tenant-admin.json');
export const PLATFORM_ADMIN_STATE = path.join(STORAGE_STATE_DIR, 'platform-admin.json');

export default defineConfig({
  // Test directory
  testDir: './tests',

  // Run tests in parallel files - reduced for stability
  fullyParallel: false,

  // Fail the build on CI if you accidentally left test.only in the source code
  forbidOnly: !!process.env.CI,

  // Retry failed tests to handle flaky sidebar loading
  retries: process.env.CI ? 2 : 1,

  // Use 1 worker for consistent auth state handling
  workers: 1,

  // Reporter configuration
  reporter: [
    ['list'],
    ['html', { open: 'never' }],
    ['json', { outputFile: 'test-results/results.json' }],
  ],

  // Shared settings for all projects
  use: {
    // Base URL for all tests
    baseURL: process.env.BASE_URL || 'http://localhost:3000',

    // Collect trace when retrying the failed test
    trace: 'on-first-retry',

    // Take screenshot on failure
    screenshot: 'only-on-failure',

    // Record video on first retry
    video: 'on-first-retry',

    // Timeout for actions like click, fill, etc.
    actionTimeout: 10000,

    // Navigation timeout
    navigationTimeout: 30000,
  },

  // Global timeout for each test
  timeout: 60000,

  // Expect timeout
  expect: {
    timeout: 10000,
  },

  // Configure projects for different browsers and roles
  projects: [
    // Setup projects - run first to authenticate
    {
      name: 'auth-setup',
      testMatch: /auth\.setup\.ts/,
    },

    // Smoke tests - quick validation of critical paths
    {
      name: 'smoke',
      testMatch: /.*smoke.*\.spec\.ts/,
      dependencies: ['auth-setup'],
      use: {
        ...devices['Desktop Chrome'],
        storageState: TENANT_ADMIN_STATE,
      },
    },

    // Chrome tests - primary browser
    {
      name: 'chromium',
      testMatch: /.*\.spec\.ts/,
      testIgnore: /.*smoke.*\.spec\.ts/,
      dependencies: ['auth-setup'],
      use: {
        ...devices['Desktop Chrome'],
        storageState: TENANT_ADMIN_STATE,
      },
    },

    // Firefox auth re-setup - refresh tokens before Firefox tests
    // (tokens from initial auth-setup may expire during long Chromium run)
    {
      name: 'firefox-auth-setup',
      testMatch: /auth\.setup\.ts/,
      dependencies: ['chromium'],
    },

    // Firefox tests - cross-browser validation
    {
      name: 'firefox',
      testMatch: /.*\.spec\.ts/,
      testIgnore: /.*smoke.*\.spec\.ts/,
      dependencies: ['firefox-auth-setup'],
      use: {
        ...devices['Desktop Firefox'],
        storageState: TENANT_ADMIN_STATE,
      },
    },

    // Mobile Chrome tests
    {
      name: 'mobile-chrome',
      testMatch: /.*mobile.*\.spec\.ts/,
      dependencies: ['auth-setup'],
      use: {
        ...devices['Pixel 5'],
        storageState: TENANT_ADMIN_STATE,
      },
    },

    // Platform admin tests - uses platform admin auth
    // Depends on firefox-auth-setup which refreshes both tenant + platform tokens
    {
      name: 'platform-admin',
      testMatch: /.*platform.*\.spec\.ts/,
      dependencies: ['firefox-auth-setup'],
      use: {
        ...devices['Desktop Chrome'],
        storageState: PLATFORM_ADMIN_STATE,
      },
    },

    // Mobile viewport tests - iPhone SE responsive design validation
    {
      name: 'mobile-viewport',
      testMatch: /tests\/mobile\/.*\.spec\.ts/,
      dependencies: ['auth-setup'],
      use: {
        ...devices['iPhone SE'],
        storageState: TENANT_ADMIN_STATE,
      },
    },

    // Accessibility tests - WCAG 2.1 Level AA compliance
    {
      name: 'accessibility',
      testMatch: /tests\/accessibility\/.*\.spec\.ts/,
      dependencies: ['auth-setup'],
      use: {
        ...devices['Desktop Chrome'],
        storageState: TENANT_ADMIN_STATE,
      },
    },

    // Visual regression tests - Screenshot comparison
    {
      name: 'visual',
      testMatch: /tests\/visual\/.*\.spec\.ts/,
      dependencies: ['auth-setup'],
      use: {
        ...devices['Desktop Chrome'],
        storageState: TENANT_ADMIN_STATE,
      },
    },
  ],

  // Run your local dev server before starting the tests
  webServer: process.env.CI ? undefined : {
    command: 'npm run dev',
    cwd: path.join(__dirname, '..'),
    url: 'http://localhost:3000',
    reuseExistingServer: true,
    timeout: 120000,
  },
});
