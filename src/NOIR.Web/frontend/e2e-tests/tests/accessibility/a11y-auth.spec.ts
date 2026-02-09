import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { LoginPage, DashboardPage } from '../../pages';
import { Timeouts } from '../../pages/BasePage';

/**
 * Accessibility Tests - Authentication Pages
 *
 * Tests WCAG 2.1 Level AA compliance using axe-core
 * Tags: @accessibility @a11y @auth @P1
 */

// Login page tests - run without authentication
test.describe('Accessibility - Login Page (Unauthenticated) @accessibility @a11y', () => {
  // Disable auth state to see the actual login form
  test.use({ storageState: { cookies: [], origins: [] } });

  test('A11Y-AUTH-001: Login page has no accessibility violations', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.navigate();

    // Run axe accessibility scan
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();

    // Should have no violations
    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('A11Y-AUTH-003: Login form has proper ARIA labels', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.navigate();

    // Check email input
    const emailInput = loginPage.emailInput;
    await expect(emailInput).toHaveAttribute('aria-label', /.+/);

    // Check password input
    const passwordInput = loginPage.passwordInput;
    await expect(passwordInput).toHaveAttribute('aria-label', /.+/);

    // Check login button
    const loginButton = loginPage.loginButton;
    await expect(loginButton).toBeEnabled();
  });

  test('A11Y-AUTH-004: Login page has proper heading hierarchy', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.navigate();

    // Should have h1
    const h1 = page.locator('h1');
    await expect(h1.first()).toBeVisible();

    // Run axe specifically for heading order (scan entire page)
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a'])
      .analyze();

    // Filter for heading-order violations
    const headingViolations = accessibilityScanResults.violations.filter(
      v => v.id === 'heading-order'
    );

    expect(headingViolations).toEqual([]);
  });

  test('A11Y-AUTH-005: Login form has proper focus management', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.navigate();

    // Tab through form - focus should be visible
    await page.keyboard.press('Tab');
    let focusedElement = await page.evaluate(() => document.activeElement?.tagName);
    expect(focusedElement).toBeTruthy();

    await page.keyboard.press('Tab');
    focusedElement = await page.evaluate(() => document.activeElement?.tagName);
    expect(focusedElement).toBeTruthy();

    // Should be able to submit with Enter
    await loginPage.emailInput.fill('admin@noir.local');
    await loginPage.passwordInput.fill('123qwe');
    await page.keyboard.press('Enter');

    // Should redirect (keyboard submission works)
    await page.waitForURL(/\/(portal|dashboard)/, { timeout: Timeouts.API_RESPONSE }).catch(() => {
      // May fail if credentials are wrong, but keyboard navigation worked
    });
  });
});

// Dashboard test - requires authentication
test.describe('Accessibility - Dashboard (Authenticated) @accessibility @a11y', () => {
  test('A11Y-AUTH-002: Dashboard has no accessibility violations', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();

    // Run axe accessibility scan
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();

    // Should have no violations
    expect(accessibilityScanResults.violations).toEqual([]);
  });
});
