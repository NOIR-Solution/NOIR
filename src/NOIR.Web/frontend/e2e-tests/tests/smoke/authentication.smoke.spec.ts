import { test, expect } from '@playwright/test';
import { LoginPage, DashboardPage } from '../../pages';

/**
 * Authentication Smoke Tests
 *
 * @smoke @auth @P0
 *
 * Critical authentication flows that must work:
 * - Login with valid credentials
 * - Login failure with invalid credentials
 * - Redirect to login when unauthenticated
 * - Logout functionality
 *
 * Note: These tests clear auth state to test login flows properly
 */

test.describe('Authentication @smoke @auth @P0', () => {
  // Clear auth state before each test to ensure fresh login flow
  test.beforeEach(async ({ page }) => {
    // Clear cookies and localStorage to simulate unauthenticated user
    await page.context().clearCookies();
    await page.goto('/');
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });
  });

  test('should login successfully with valid tenant admin credentials', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.navigate();
    // Wait for page to fully render
    await page.waitForLoadState('networkidle');
    await loginPage.expectOnLoginPage();

    await loginPage.loginAsTenantAdmin();
    await loginPage.expectLoginSuccess();

    // Verify dashboard is accessible
    const dashboard = new DashboardPage(page);
    await dashboard.expectDashboardLoaded();
  });

  test('should show error for invalid credentials', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.navigate();
    await page.waitForLoadState('networkidle');

    await loginPage.login('invalid@email.com', 'wrongpassword');

    // Should show error and stay on login page
    await loginPage.expectLoginFailure();
    await expect(page).toHaveURL(/\/login/);
  });

  test('should redirect unauthenticated user to login', async ({ page }) => {
    // Auth already cleared in beforeEach

    // Try to access protected route
    await page.goto('/portal');

    // Should redirect to login
    await expect(page).toHaveURL(/\/login/, { timeout: 15000 });
  });

  test('should handle empty credentials validation', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.navigate();
    await page.waitForLoadState('networkidle');

    // Clear any pre-filled values (dev mode prefills credentials)
    await loginPage.emailInput.clear();
    await loginPage.passwordInput.clear();

    // Try to submit without filling fields
    await loginPage.loginButton.click();

    // Should show validation error for empty email
    const emailError = loginPage.emailError;
    await expect(emailError).toBeVisible({ timeout: 5000 });

    // Should stay on login page
    await expect(page).toHaveURL(/\/login/);
  });

  test('should show validation error for invalid email format', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.navigate();
    await page.waitForLoadState('networkidle');

    // Enter invalid email format (missing @ and domain)
    await loginPage.emailInput.clear();
    await loginPage.emailInput.fill('notanemail');
    await loginPage.passwordInput.clear();
    await loginPage.passwordInput.fill('password123');
    await loginPage.loginButton.click();

    // The email input has type="email" which uses HTML5 native validation
    // The browser will block form submission and show a native tooltip
    // We verify the form didn't submit by checking we're still on login
    await expect(page).toHaveURL(/\/login/, { timeout: 5000 });

    // Verify the email input is marked invalid by HTML5 validation
    // by checking its validity state via JavaScript
    const isEmailInvalid = await loginPage.emailInput.evaluate((el: HTMLInputElement) => !el.checkValidity());
    expect(isEmailInvalid).toBe(true);
  });
});
