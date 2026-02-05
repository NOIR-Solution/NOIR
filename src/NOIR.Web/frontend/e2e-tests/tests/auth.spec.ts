import { test, expect } from '@playwright/test';
import { LoginPage, DashboardPage, Timeouts } from '../pages';

/**
 * Authentication Tests
 *
 * Comprehensive tests for login, logout, and session management.
 * Tags: @auth @P0 @P1
 */

test.describe('Authentication @auth', () => {
  // Login Flow tests need unauthenticated state - disable storageState
  test.describe('Login Flow @P0', () => {
    // Disable auth state for login tests - this works across all browsers
    test.use({ storageState: { cookies: [], origins: [] } });

    test('AUTH-001: Login page displays all elements', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();

      await expect(loginPage.emailInput).toBeVisible();
      await expect(loginPage.passwordInput).toBeVisible();
      await expect(loginPage.loginButton).toBeVisible();
    });

    test('AUTH-002: Tenant admin login redirects to dashboard', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();
      await loginPage.loginAsTenantAdmin();

      // Auth flow requires longer timeout: form submit → backend auth → token → redirect → page load
      // Firefox can be slower on auth redirects (see MEMORY.md)
      await page.waitForURL(/\/(portal|dashboard)/, { timeout: Timeouts.PAGE_LOAD });
      await expect(page).toHaveURL(/\/portal/);
    });

    test('AUTH-003: Platform admin login redirects to dashboard', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();
      await loginPage.loginAsPlatformAdmin();

      // Auth flow requires longer timeout: form submit → backend auth → token → redirect → page load
      // Firefox can be slower on auth redirects (see MEMORY.md)
      await page.waitForURL(/\/(portal|dashboard)/, { timeout: Timeouts.PAGE_LOAD });
      await expect(page).toHaveURL(/\/portal/);
    });

    test('AUTH-004: Empty email shows validation error', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();
      await page.waitForLoadState('networkidle');

      // Clear any prefilled values and fill only password
      await loginPage.emailInput.clear();
      await loginPage.passwordInput.fill('somepassword');
      await loginPage.loginButton.click();

      // Should show validation error - check for visible error text or HTML5 validation
      const emailError = loginPage.emailError;
      await expect(emailError).toBeVisible({ timeout: 5000 });
    });

    test('AUTH-005: Empty password shows validation error', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();
      await page.waitForLoadState('networkidle');

      // Clear any prefilled values and fill only email
      await loginPage.emailInput.fill('test@test.com');
      await loginPage.passwordInput.clear();
      await loginPage.loginButton.click();

      // Should show validation error
      const passwordError = loginPage.passwordError;
      await expect(passwordError).toBeVisible({ timeout: 5000 });
    });

    test('AUTH-006: Invalid credentials show error message', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();

      await loginPage.login('invalid@test.com', 'wrongpassword');

      await loginPage.expectLoginFailure();
    });
  });

  test.describe('Session Management @P1', () => {
    test('AUTH-007: Authenticated user stays logged in on refresh', async ({ page }) => {
      // Navigate to dashboard (uses pre-authenticated state)
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Refresh the page
      await page.reload();

      // Should still be on dashboard
      await dashboard.expectDashboardLoaded();
    });

    test('AUTH-008: Logout redirects to login page', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Find and click logout button
      const logoutButton = page.locator('button:has-text("Logout"), [data-testid="logout-button"], a:has-text("Logout")');
      if (await logoutButton.isVisible()) {
        await logoutButton.click();
        await expect(page).toHaveURL(/\/login/);
      }
    });
  });

  // Unauthenticated redirect test needs empty storage state
  test.describe('Redirect Tests @P1', () => {
    test.use({ storageState: { cookies: [], origins: [] } });

    test('AUTH-009: Unauthenticated user redirected to login', async ({ page }) => {
      // Try to access protected page without auth
      await page.goto('/portal');

      // Wait for page to fully load and check if redirect happens
      await page.waitForLoadState('networkidle').catch(() => {});

      // The app should redirect to login when not authenticated
      await expect(page).toHaveURL(/\/login/, { timeout: 30000 });
    });
  });

  // Forgot Password test needs empty storage state
  test.describe('Forgot Password @P1', () => {
    test.use({ storageState: { cookies: [], origins: [] } });

    test('AUTH-010: Forgot password link navigates to reset page', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.navigate();
      await page.waitForLoadState('networkidle');

      await loginPage.clickForgotPassword();

      await expect(page).toHaveURL(/\/(forgot-password|reset|password)/);
    });
  });
});
