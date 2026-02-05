import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * ForgotPasswordPage - Page Object for the complete Forgot Password flow
 *
 * Handles all 4 steps of the password reset flow:
 * 1. /forgot-password - Email entry
 * 2. /forgot-password/verify - OTP verification
 * 3. /forgot-password/reset - New password entry
 * 4. /forgot-password/success - Confirmation
 */
export class ForgotPasswordPage extends BasePage {
  // ============================================================
  // Step 1: Email Entry Page (/forgot-password)
  // ============================================================
  readonly emailInput: Locator;
  readonly sendCodeButton: Locator;
  readonly backToLoginLink: Locator;

  // ============================================================
  // Step 2: OTP Verification Page (/forgot-password/verify)
  // ============================================================
  readonly otpInputs: Locator;
  readonly resendCodeButton: Locator;
  readonly countdownTimer: Locator;
  readonly backToEmailLink: Locator;
  readonly verifyingIndicator: Locator;

  // ============================================================
  // Step 3: Reset Password Page (/forgot-password/reset)
  // ============================================================
  readonly newPasswordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly showPasswordButton: Locator;
  readonly showConfirmPasswordButton: Locator;
  readonly resetPasswordButton: Locator;
  readonly passwordStrengthIndicator: Locator;
  readonly passwordMatchIndicator: Locator;
  readonly backToVerifyLink: Locator;

  // ============================================================
  // Step 4: Success Page (/forgot-password/success)
  // ============================================================
  readonly successIcon: Locator;
  readonly successTitle: Locator;
  readonly successDescription: Locator;
  readonly securityNotice: Locator;
  readonly goToLoginButton: Locator;

  // ============================================================
  // Common Elements (across all pages)
  // ============================================================
  readonly errorMessage: Locator;
  readonly pageTitle: Locator;
  readonly pageSubtitle: Locator;
  readonly languageSwitcher: Locator;

  constructor(page: Page) {
    super(page);

    // Step 1: Email Entry
    this.emailInput = page.locator('#email');
    this.sendCodeButton = page.locator('button[type="submit"]');
    this.backToLoginLink = page.locator('a[href="/login"]');

    // Step 2: OTP Verification
    // OTP inputs are individual text inputs with aria-label "Digit X of 6"
    this.otpInputs = page.locator('input[aria-label^="Digit"]');
    this.resendCodeButton = page.locator('button:has-text("Resend"), button:has-text("Send new code")');
    this.countdownTimer = page.locator('text=/\\d+s|Resend in/');
    this.backToEmailLink = page.locator('a[href="/forgot-password"]');
    this.verifyingIndicator = page.locator('text=/Verifying/i');

    // Step 3: Reset Password
    this.newPasswordInput = page.locator('#password');
    this.confirmPasswordInput = page.locator('#confirmPassword');
    this.showPasswordButton = page.locator('#password ~ button, button[aria-label*="password"]').first();
    this.showConfirmPasswordButton = page.locator('#confirmPassword ~ button, button[aria-label*="password"]').last();
    this.resetPasswordButton = page.locator('button[type="submit"]');
    this.passwordStrengthIndicator = page.locator('[class*="password-strength"], text=/Weak|Fair|Good|Strong/');
    this.passwordMatchIndicator = page.locator('text=/match|Match/');
    this.backToVerifyLink = page.locator('a[href="/forgot-password/verify"]');

    // Step 4: Success
    this.successIcon = page.locator('svg.text-green-600, svg.text-green-500').first();
    this.successTitle = page.locator('h1');
    this.successDescription = page.locator('h1 + p, h1 ~ p').first();
    this.securityNotice = page.locator('.bg-blue-50, .bg-blue-900\\/20');
    this.goToLoginButton = page.locator('button:has-text("Login"), button:has-text("Sign in")');

    // Common Elements
    this.errorMessage = page.locator('.bg-destructive\\/10 p, p.text-destructive');
    this.pageTitle = page.locator('h1');
    this.pageSubtitle = page.locator('h1 + p, h1 ~ p.text-muted-foreground').first();
    this.languageSwitcher = page.locator('[data-testid="language-switcher"], button:has-text("EN"), button:has-text("VI")');
  }

  // ============================================================
  // Navigation Methods
  // ============================================================

  /**
   * Navigate to the forgot password page (Step 1)
   */
  async navigate(): Promise<void> {
    await this.goto('/forgot-password');
  }

  /**
   * Navigate directly to OTP verification page (Step 2)
   * Note: Requires valid session data in sessionStorage
   */
  async navigateToVerify(): Promise<void> {
    await this.goto('/forgot-password/verify');
  }

  /**
   * Navigate directly to reset password page (Step 3)
   * Note: Requires valid session data with reset token in sessionStorage
   */
  async navigateToReset(): Promise<void> {
    await this.goto('/forgot-password/reset');
  }

  /**
   * Navigate directly to success page (Step 4)
   */
  async navigateToSuccess(): Promise<void> {
    await this.goto('/forgot-password/success');
  }

  // ============================================================
  // Step 1: Email Entry Actions
  // ============================================================

  /**
   * Enter email and submit the form (Step 1)
   */
  async requestPasswordReset(email: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.sendCodeButton.click();
  }

  /**
   * Verify we're on the email entry page
   */
  async expectOnEmailPage(): Promise<void> {
    await expect(this.emailInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.sendCodeButton).toBeVisible();
    expect(this.getCurrentPath()).toBe('/forgot-password');
  }

  /**
   * Verify email was accepted and redirected to OTP page
   */
  async expectEmailAccepted(): Promise<void> {
    await this.page.waitForURL(/\/forgot-password\/verify/, { timeout: Timeouts.PAGE_LOAD });
    await expect(this.otpInputs.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  // ============================================================
  // Step 2: OTP Verification Actions
  // ============================================================

  /**
   * Enter OTP code digit by digit
   * @param code - 6-digit OTP code as string
   */
  async enterOtp(code: string): Promise<void> {
    const digits = code.split('');
    const inputCount = await this.otpInputs.count();

    for (let i = 0; i < Math.min(digits.length, inputCount); i++) {
      await this.otpInputs.nth(i).fill(digits[i]);
    }
  }

  /**
   * Enter OTP code by pasting (simulates paste action)
   * @param code - OTP code as string
   */
  async pasteOtp(code: string): Promise<void> {
    await this.otpInputs.first().focus();
    await this.page.keyboard.type(code);
  }

  /**
   * Clear all OTP inputs
   */
  async clearOtp(): Promise<void> {
    const inputCount = await this.otpInputs.count();
    for (let i = inputCount - 1; i >= 0; i--) {
      await this.otpInputs.nth(i).fill('');
    }
  }

  /**
   * Click resend code button (when available)
   */
  async clickResendCode(): Promise<void> {
    await expect(this.resendCodeButton).toBeEnabled({ timeout: Timeouts.PAGE_LOAD });
    await this.resendCodeButton.click();
  }

  /**
   * Verify we're on the OTP verification page
   */
  async expectOnVerifyPage(): Promise<void> {
    await expect(this.otpInputs.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    expect(this.getCurrentPath()).toBe('/forgot-password/verify');
  }

  /**
   * Verify OTP was accepted and redirected to reset page
   */
  async expectOtpAccepted(): Promise<void> {
    await this.page.waitForURL(/\/forgot-password\/reset/, { timeout: Timeouts.PAGE_LOAD });
    await expect(this.newPasswordInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Wait for countdown timer to expire (resend becomes available)
   */
  async waitForResendAvailable(): Promise<void> {
    await expect(this.resendCodeButton).toBeEnabled({ timeout: 70000 }); // 60s cooldown + buffer
  }

  /**
   * Check if countdown timer is showing
   */
  async isCountdownActive(): Promise<boolean> {
    return await this.countdownTimer.isVisible().catch(() => false);
  }

  // ============================================================
  // Step 3: Reset Password Actions
  // ============================================================

  /**
   * Enter new password and confirmation, then submit
   */
  async resetPassword(password: string, confirmPassword?: string): Promise<void> {
    await this.newPasswordInput.fill(password);
    await this.confirmPasswordInput.fill(confirmPassword ?? password);
    await this.resetPasswordButton.click();
  }

  /**
   * Enter only the new password field
   */
  async enterNewPassword(password: string): Promise<void> {
    await this.newPasswordInput.fill(password);
  }

  /**
   * Enter only the confirm password field
   */
  async enterConfirmPassword(password: string): Promise<void> {
    await this.confirmPasswordInput.fill(password);
  }

  /**
   * Toggle password visibility for new password field
   */
  async toggleNewPasswordVisibility(): Promise<void> {
    await this.showPasswordButton.click();
  }

  /**
   * Toggle password visibility for confirm password field
   */
  async toggleConfirmPasswordVisibility(): Promise<void> {
    await this.showConfirmPasswordButton.click();
  }

  /**
   * Verify we're on the reset password page
   */
  async expectOnResetPage(): Promise<void> {
    await expect(this.newPasswordInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.confirmPasswordInput).toBeVisible();
    expect(this.getCurrentPath()).toBe('/forgot-password/reset');
  }

  /**
   * Verify password reset was successful and redirected to success page
   */
  async expectPasswordResetSuccess(): Promise<void> {
    await this.page.waitForURL(/\/forgot-password\/success/, { timeout: Timeouts.PAGE_LOAD });
    await expect(this.goToLoginButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Check if passwords match indicator shows positive match
   */
  async expectPasswordsMatch(): Promise<void> {
    const matchText = this.page.locator('.text-green-600:has-text("match")');
    await expect(matchText).toBeVisible({ timeout: Timeouts.QUICK_CHECK });
  }

  /**
   * Check if passwords don't match indicator is shown
   */
  async expectPasswordsDoNotMatch(): Promise<void> {
    const noMatchText = this.page.locator('.text-destructive:has-text("match")');
    await expect(noMatchText).toBeVisible({ timeout: Timeouts.QUICK_CHECK });
  }

  /**
   * Check password strength level
   */
  async getPasswordStrengthLevel(): Promise<string | null> {
    // Primary: Use stable data-testid selector
    const strengthByTestId = this.page.locator('[data-testid="password-strength-level"]');
    if (await strengthByTestId.isVisible({ timeout: 1000 }).catch(() => false)) {
      return await strengthByTestId.textContent();
    }
    // Fallback: look for colored text indicators
    const coloredStrength = this.page.locator('span.text-red-600, span.text-orange-500, span.text-yellow-500, span.text-green-500, span.text-emerald-500').first();
    if (await coloredStrength.isVisible({ timeout: 1000 }).catch(() => false)) {
      return await coloredStrength.textContent();
    }
    return null;
  }

  // ============================================================
  // Step 4: Success Page Actions
  // ============================================================

  /**
   * Click the "Go to Login" button on success page
   */
  async clickGoToLogin(): Promise<void> {
    await this.goToLoginButton.click();
    await this.page.waitForURL(/\/login/, { timeout: Timeouts.PAGE_LOAD });
  }

  /**
   * Verify we're on the success page
   */
  async expectOnSuccessPage(): Promise<void> {
    await expect(this.goToLoginButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    expect(this.getCurrentPath()).toBe('/forgot-password/success');
  }

  /**
   * Verify success page content is displayed correctly
   */
  async expectSuccessContent(): Promise<void> {
    await expect(this.successIcon).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.successTitle).toBeVisible();
    await expect(this.goToLoginButton).toBeVisible();
  }

  // ============================================================
  // Common Verification Methods
  // ============================================================

  /**
   * Verify an error message is displayed
   */
  async expectError(errorText?: string | RegExp): Promise<void> {
    await expect(this.errorMessage.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

    if (errorText) {
      await expect(this.errorMessage.first()).toContainText(errorText);
    }
  }

  /**
   * Verify no error message is displayed
   */
  async expectNoError(): Promise<void> {
    await expect(this.errorMessage).toBeHidden({ timeout: Timeouts.QUICK_CHECK });
  }

  /**
   * Get the current page title text
   */
  async getPageTitle(): Promise<string | null> {
    return await this.pageTitle.textContent();
  }

  /**
   * Get the masked email displayed on verify page
   */
  async getMaskedEmail(): Promise<string | null> {
    const subtitle = await this.pageSubtitle.textContent();
    // Extract email pattern like "t***@example.com"
    const emailMatch = subtitle?.match(/[a-z]\*+@[\w.-]+/i);
    return emailMatch ? emailMatch[0] : null;
  }

  // ============================================================
  // Complete Flow Methods
  // ============================================================

  /**
   * Complete the entire forgot password flow
   * Note: This requires mocking the backend or using test data
   * @param email - Email address
   * @param otpCode - OTP code (from test setup or mock)
   * @param newPassword - New password to set
   */
  async completeFullFlow(email: string, otpCode: string, newPassword: string): Promise<void> {
    // Step 1: Request reset
    await this.navigate();
    await this.requestPasswordReset(email);
    await this.expectEmailAccepted();

    // Step 2: Verify OTP
    await this.enterOtp(otpCode);
    await this.expectOtpAccepted();

    // Step 3: Reset password
    await this.resetPassword(newPassword);
    await this.expectPasswordResetSuccess();

    // Step 4: Verify success
    await this.expectOnSuccessPage();
  }

  /**
   * Set up session storage with mock data for testing specific steps
   * Useful for testing OTP or Reset pages directly without going through email step
   */
  async setupMockSession(data: {
    sessionToken?: string;
    maskedEmail?: string;
    expiresAt?: string;
    otpLength?: number;
    resetToken?: string;
    resetTokenExpiresAt?: string;
  }): Promise<void> {
    const sessionData = {
      sessionToken: data.sessionToken ?? 'mock-session-token',
      maskedEmail: data.maskedEmail ?? 't***@example.com',
      expiresAt: data.expiresAt ?? new Date(Date.now() + 30 * 60 * 1000).toISOString(),
      otpLength: data.otpLength ?? 6,
      ...(data.resetToken && {
        resetToken: data.resetToken,
        resetTokenExpiresAt: data.resetTokenExpiresAt ?? new Date(Date.now() + 30 * 60 * 1000).toISOString(),
      }),
    };

    await this.page.evaluate((data) => {
      sessionStorage.setItem('passwordReset', JSON.stringify(data));
    }, sessionData);

    // Verify session was set correctly
    const storedSession = await this.page.evaluate(() => {
      const item = sessionStorage.getItem('passwordReset');
      return item ? JSON.parse(item) : null;
    });

    if (!storedSession || storedSession.sessionToken !== sessionData.sessionToken) {
      throw new Error('Failed to set mock session in sessionStorage');
    }
  }

  /**
   * Clear session storage (cleanup after tests)
   */
  async clearSession(): Promise<void> {
    await this.page.evaluate(() => {
      sessionStorage.removeItem('passwordReset');
    });
  }
}
