import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * UserSettingsPage - Page Object for User Settings
 *
 * Based on: src/pages/portal/Settings.tsx
 * - Has two navigation sections: Profile and Security
 * - Profile section shows ProfileForm component
 * - Security section shows ChangePasswordForm and SessionManagement
 */
export class UserSettingsPage extends BasePage {
  // Page header
  readonly pageHeader: Locator;
  readonly pageDescription: Locator;

  // Sidebar navigation
  readonly profileNavItem: Locator;
  readonly securityNavItem: Locator;

  // Profile section
  readonly profileForm: Locator;
  readonly firstNameInput: Locator;
  readonly lastNameInput: Locator;
  readonly emailInput: Locator;
  readonly phoneInput: Locator;
  readonly saveProfileButton: Locator;

  // Security section - Change Password
  readonly changePasswordForm: Locator;
  readonly currentPasswordInput: Locator;
  readonly newPasswordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly changePasswordButton: Locator;

  // Security section - Session Management
  readonly sessionManagement: Locator;
  readonly sessionsList: Locator;
  readonly revokeAllSessionsButton: Locator;

  constructor(page: Page) {
    super(page);

    // Page header
    this.pageHeader = page.locator('h1:has-text("Settings"), h1:has-text("Account Settings")');
    this.pageDescription = page.locator('p.text-muted-foreground').first();

    // Sidebar navigation
    this.profileNavItem = page.locator('button:has-text("Personal Information"), button:has-text("Profile"), nav button').filter({ hasText: /Personal|Profile/i }).first();
    this.securityNavItem = page.locator('button:has-text("Security"), nav button').filter({ hasText: /Security/i }).first();

    // Profile section
    this.profileForm = page.locator('form').filter({ has: page.locator('input[name="firstName"], input#firstName') }).first();
    this.firstNameInput = page.locator('input[name="firstName"], input#firstName').first();
    this.lastNameInput = page.locator('input[name="lastName"], input#lastName').first();
    this.emailInput = page.locator('input[name="email"], input#email, input[type="email"]').first();
    this.phoneInput = page.locator('input[name="phone"], input#phone, input[type="tel"]').first();
    this.saveProfileButton = page.locator('button:has-text("Save"), button:has-text("Update Profile"), button[type="submit"]').first();

    // Security section - Change Password
    this.changePasswordForm = page.locator('form').filter({ has: page.locator('input[type="password"]') }).first();
    this.currentPasswordInput = page.locator('input[name="currentPassword"], input#currentPassword').first();
    this.newPasswordInput = page.locator('input[name="newPassword"], input#newPassword').first();
    this.confirmPasswordInput = page.locator('input[name="confirmPassword"], input#confirmPassword').first();
    this.changePasswordButton = page.locator('button:has-text("Change Password"), button:has-text("Update Password")').first();

    // Security section - Session Management
    this.sessionManagement = page.locator('[data-testid="session-management"], div:has-text("Active Sessions")').first();
    this.sessionsList = page.locator('[data-testid="sessions-list"], table, .sessions-list').first();
    this.revokeAllSessionsButton = page.locator('button:has-text("Revoke All"), button:has-text("Sign Out All")').first();
  }

  /**
   * Navigate to user settings page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/settings');
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify page loaded using sequential wait pattern
   */
  async expectPageLoaded(): Promise<void> {
    await expect(this.pageHeader).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    // Wait for navigation items to be visible (proves page is interactive)
    await expect(this.profileNavItem).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Navigate to Profile section
   */
  async goToProfile(): Promise<void> {
    await this.profileNavItem.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
    await expect(this.firstNameInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Navigate to Security section
   */
  async goToSecurity(): Promise<void> {
    await this.securityNavItem.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
    await expect(this.currentPasswordInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Check if Profile section is active
   */
  async isProfileSectionActive(): Promise<boolean> {
    const profileNavClasses = await this.profileNavItem.getAttribute('class') || '';
    return profileNavClasses.includes('bg-blue') || profileNavClasses.includes('text-blue');
  }

  /**
   * Check if Security section is active
   */
  async isSecuritySectionActive(): Promise<boolean> {
    const securityNavClasses = await this.securityNavItem.getAttribute('class') || '';
    return securityNavClasses.includes('bg-blue') || securityNavClasses.includes('text-blue');
  }

  /**
   * Update profile information
   */
  async updateProfile(data: {
    firstName?: string;
    lastName?: string;
    phone?: string;
  }): Promise<void> {
    await this.goToProfile();

    if (data.firstName) {
      await this.firstNameInput.clear();
      await this.firstNameInput.fill(data.firstName);
    }

    if (data.lastName) {
      await this.lastNameInput.clear();
      await this.lastNameInput.fill(data.lastName);
    }

    if (data.phone) {
      await this.phoneInput.clear();
      await this.phoneInput.fill(data.phone);
    }

    await this.saveProfileButton.click();
    await this.expectSuccessToast();
  }

  /**
   * Change password
   */
  async changePassword(data: {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
  }): Promise<void> {
    await this.goToSecurity();

    await this.currentPasswordInput.fill(data.currentPassword);
    await this.newPasswordInput.fill(data.newPassword);
    await this.confirmPasswordInput.fill(data.confirmPassword);

    await this.changePasswordButton.click();
  }

  /**
   * Get current profile values
   */
  async getProfileValues(): Promise<{
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
  }> {
    await this.goToProfile();

    return {
      firstName: await this.firstNameInput.inputValue(),
      lastName: await this.lastNameInput.inputValue(),
      email: await this.emailInput.inputValue(),
      phone: await this.phoneInput.inputValue(),
    };
  }

  /**
   * Verify profile form is visible
   */
  async expectProfileFormVisible(): Promise<void> {
    await expect(this.firstNameInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.lastNameInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify security section is visible
   */
  async expectSecuritySectionVisible(): Promise<void> {
    await expect(this.currentPasswordInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.newPasswordInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Revoke all sessions
   */
  async revokeAllSessions(): Promise<void> {
    await this.goToSecurity();
    await expect(this.revokeAllSessionsButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await this.revokeAllSessionsButton.click();

    // Wait for confirmation dialog if any
    const confirmButton = this.confirmDialog.locator('button:has-text("Confirm"), button:has-text("Yes")');
    const hasConfirmDialog = await confirmButton.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false);

    if (hasConfirmDialog) {
      await confirmButton.click();
    }
  }
}
