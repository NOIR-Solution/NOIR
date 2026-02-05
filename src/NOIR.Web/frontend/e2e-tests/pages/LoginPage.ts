import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * LoginPage - Page Object for authentication flows
 */
export class LoginPage extends BasePage {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;
  readonly forgotPasswordLink: Locator;
  readonly errorMessage: Locator;
  readonly rememberMeCheckbox: Locator;
  readonly emailError: Locator;
  readonly passwordError: Locator;

  constructor(page: Page) {
    super(page);

    // Using actual selectors from Login.tsx component
    this.emailInput = page.locator('#email');
    this.passwordInput = page.locator('#password');
    this.loginButton = page.locator('button[type="submit"]');
    this.forgotPasswordLink = page.locator('a[href="/forgot-password"]');
    this.errorMessage = page.locator('.bg-destructive\\/10 .text-destructive, p.text-destructive');
    this.emailError = page.locator('p.text-destructive').first();
    this.passwordError = page.locator('p.text-destructive').last();
    this.rememberMeCheckbox = page.locator('input[type="checkbox"][name="remember"]');
  }

  /**
   * Navigate to login page
   */
  async navigate(): Promise<void> {
    await this.goto('/login');
  }

  /**
   * Perform login with given credentials
   */
  async login(email: string, password: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
  }

  /**
   * Login as tenant admin (admin@noir.local)
   */
  async loginAsTenantAdmin(): Promise<void> {
    await this.login('admin@noir.local', '123qwe');
  }

  /**
   * Login as platform admin (platform@noir.local)
   */
  async loginAsPlatformAdmin(): Promise<void> {
    await this.login('platform@noir.local', '123qwe');
  }

  /**
   * Verify login was successful (redirected to portal)
   */
  async expectLoginSuccess(): Promise<void> {
    await this.page.waitForURL(/\/(portal|dashboard)/, { timeout: 30000 });
    await expect(this.sidebar).toBeVisible({ timeout: 10000 });
  }

  /**
   * Verify login failed with error message
   * The server error appears in a div.bg-destructive/10 with p.text-destructive inside
   */
  async expectLoginFailure(errorText?: string): Promise<void> {
    // Wait for any error element to appear (server error OR validation error)
    const serverError = this.page.locator('div.rounded-xl p.text-destructive, .bg-destructive\\/10 p');
    const anyError = this.page.locator('.text-destructive');

    // Either server error or validation error should be visible
    await expect(anyError.first()).toBeVisible({ timeout: 10000 });

    if (errorText) {
      await expect(anyError.first()).toContainText(errorText);
    }
  }

  /**
   * Verify we're on the login page
   */
  async expectOnLoginPage(): Promise<void> {
    await expect(this.emailInput).toBeVisible();
    await expect(this.passwordInput).toBeVisible();
    await expect(this.loginButton).toBeVisible();
  }

  /**
   * Click forgot password link
   */
  async clickForgotPassword(): Promise<void> {
    await this.forgotPasswordLink.click();
    await this.waitForPageLoad();
  }

  /**
   * Enable remember me checkbox
   */
  async enableRememberMe(): Promise<void> {
    await this.rememberMeCheckbox.check();
  }
}
