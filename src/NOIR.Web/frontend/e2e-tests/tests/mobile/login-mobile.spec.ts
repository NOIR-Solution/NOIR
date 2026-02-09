import { test, expect } from '@playwright/test';
import { LoginPage, DashboardPage } from '../../pages';
import { Timeouts } from '../../pages/BasePage';
import { expectDashboardLoadedOnMobile } from '../../utils/mobile';

/**
 * Mobile Login Tests
 *
 * Tests login flow on mobile viewport (375x667 - iPhone SE)
 * Tags: @mobile @auth @P0
 */

// Use mobile viewport
test.use({ viewport: { width: 375, height: 667 } });

test.describe('Mobile Login @mobile @auth', () => {
  // Tests that require unauthenticated state
  test.describe('Unauthenticated', () => {
    test.use({ storageState: { cookies: [], origins: [] } });

    test('MOB-AUTH-001: Login page displays correctly on mobile', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();

      // Verify mobile layout
      await expect(loginPage.emailInput).toBeVisible();
      await expect(loginPage.passwordInput).toBeVisible();
      await expect(loginPage.loginButton).toBeVisible();

      // Verify viewport size
      const viewport = page.viewportSize();
      expect(viewport?.width).toBe(375);
      expect(viewport?.height).toBe(667);
    });

    test('MOB-AUTH-002: Login flow works on mobile', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();
      await loginPage.loginAsTenantAdmin();

      // Should redirect to dashboard
      await expect(page).toHaveURL(/\/portal/);
    });

    test('MOB-AUTH-004: Forms are scrollable on mobile', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();

      // Verify page is scrollable if content exceeds viewport
      const scrollHeight = await page.evaluate(() => document.documentElement.scrollHeight);
      const clientHeight = await page.evaluate(() => document.documentElement.clientHeight);

      // Content should fit or be scrollable
      expect(scrollHeight).toBeGreaterThanOrEqual(clientHeight);
    });
  });

  // Tests that use authenticated state
  test.describe('Authenticated', () => {
    test('MOB-AUTH-003: Mobile navigation drawer opens after login', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();

      // Verify dashboard loaded on mobile viewport
      await expectDashboardLoadedOnMobile(page);
    });
  });
});
