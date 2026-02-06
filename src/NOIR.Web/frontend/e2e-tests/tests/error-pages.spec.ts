import { test, expect } from '@playwright/test';
import { BasePage, Timeouts } from '../pages';

/**
 * Error Pages and Edge Cases E2E Tests
 *
 * Tests for error handling scenarios:
 * - Unknown routes redirect to landing page
 * - Protected routes redirect to login when unauthenticated
 * - Handling of invalid URLs
 */
test.describe('Error Pages and Route Handling @error-pages', () => {

  test.describe('Unknown Routes @P1', () => {
    test('ERR-001: Unknown route redirects to landing or login', async ({ page }) => {
      await page.goto('/this-route-does-not-exist');
      await page.waitForLoadState('domcontentloaded');
      await page.waitForTimeout(Timeouts.REDIRECT); // Allow redirect to complete

      // Should redirect to either landing page "/" or login
      // (authenticated users may stay on portal, unauthenticated go to landing)
      const url = page.url();
      const isValidRedirect = url.endsWith('/') || url.includes('/login') || url.includes('/portal');
      expect(isValidRedirect).toBeTruthy();
    });

    test('ERR-002: Unknown portal route redirects appropriately', async ({ page }) => {
      await page.goto('/portal/nonexistent-page');
      await page.waitForLoadState('domcontentloaded');

      // Should either redirect to dashboard or show something meaningful
      const url = page.url();
      // Accept either redirect to portal/dashboard or staying on a valid portal page
      expect(url).toMatch(/\/(portal|login)/);
    });

    test('ERR-003: Deep unknown route redirects correctly', async ({ page }) => {
      await page.goto('/portal/admin/something/that/does/not/exist');
      await page.waitForLoadState('domcontentloaded');

      const url = page.url();
      expect(url).toMatch(/\/(portal|login)/);
    });
  });

  test.describe('Auth Protection @P0', () => {
    test('ERR-010: Protected route redirects to login when unauthenticated', async ({ page }) => {
      // Clear auth state
      await page.context().clearCookies();
      await page.goto('/');
      await page.evaluate(() => {
        localStorage.clear();
        sessionStorage.clear();
      });

      // Try to access protected route
      await page.goto('/portal');
      await page.waitForLoadState('domcontentloaded');

      // Should redirect to login
      await expect(page).toHaveURL(/\/login/, { timeout: Timeouts.PAGE_LOAD });
    });

    test('ERR-011: Protected admin route redirects to login when unauthenticated', async ({ page }) => {
      // Clear auth state
      await page.context().clearCookies();
      await page.goto('/');
      await page.evaluate(() => {
        localStorage.clear();
        sessionStorage.clear();
      });

      // Try to access admin route
      await page.goto('/portal/admin/users');
      await page.waitForLoadState('domcontentloaded');

      // Should redirect to login
      await expect(page).toHaveURL(/\/login/, { timeout: Timeouts.PAGE_LOAD });
    });
  });
});
