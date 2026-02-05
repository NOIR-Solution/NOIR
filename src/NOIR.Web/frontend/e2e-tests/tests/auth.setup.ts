import { test as setup, expect } from '@playwright/test';
import { TENANT_ADMIN_STATE, PLATFORM_ADMIN_STATE, STORAGE_STATE_DIR } from '../playwright.config';
import * as fs from 'fs';
import * as path from 'path';

// Ensure auth directory exists
if (!fs.existsSync(STORAGE_STATE_DIR)) {
  fs.mkdirSync(STORAGE_STATE_DIR, { recursive: true });
}

/**
 * Authentication Setup - Tenant Admin
 *
 * Logs in as tenant admin and saves the authenticated state
 * for reuse across all tests that need tenant admin access.
 *
 * Credentials: admin@noir.local / 123qwe
 *
 * Based on Login.tsx:
 * - Email input: id="email", type="email"
 * - Password input: id="password", type="password"
 * - Submit button: type="submit"
 */
setup('authenticate as tenant admin', async ({ page }) => {
  // Navigate to login page
  await page.goto('/login');
  await page.waitForLoadState('networkidle');

  // Fill in login credentials using exact selectors from Login.tsx
  await page.locator('#email').fill('admin@noir.local');
  await page.locator('#password').fill('123qwe');

  // Click login button
  await page.locator('button[type="submit"]').click();

  // Wait for successful login - should redirect to portal/dashboard
  await page.waitForURL(/\/(portal|dashboard)/, { timeout: 30000 });

  // Verify we're logged in by checking for sidebar/navbar
  await page.waitForLoadState('networkidle');
  await expect(page.locator('aside, nav, [role="navigation"]').first()).toBeVisible({ timeout: 15000 });

  // Save authentication state
  await page.context().storageState({ path: TENANT_ADMIN_STATE });

  console.log('✅ Tenant admin authentication state saved');
});

/**
 * Authentication Setup - Platform Admin
 *
 * Logs in as platform admin and saves the authenticated state
 * for reuse across platform-level tests.
 *
 * Credentials: platform@noir.local / 123qwe
 */
setup('authenticate as platform admin', async ({ page }) => {
  // Navigate to login page
  await page.goto('/login');
  await page.waitForLoadState('networkidle');

  // Fill in login credentials using exact selectors from Login.tsx
  await page.locator('#email').fill('platform@noir.local');
  await page.locator('#password').fill('123qwe');

  // Click login button
  await page.locator('button[type="submit"]').click();

  // Wait for successful login
  await page.waitForURL(/\/(portal|dashboard)/, { timeout: 30000 });

  // Verify we're logged in
  await page.waitForLoadState('networkidle');
  await expect(page.locator('aside, nav, [role="navigation"]').first()).toBeVisible({ timeout: 15000 });

  // Save authentication state
  await page.context().storageState({ path: PLATFORM_ADMIN_STATE });

  console.log('✅ Platform admin authentication state saved');
});
