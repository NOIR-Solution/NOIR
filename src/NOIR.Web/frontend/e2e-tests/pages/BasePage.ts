import { Page, Locator, expect } from '@playwright/test';

/**
 * BasePage - Foundation for all Page Object classes
 *
 * Provides common functionality for navigation, waiting,
 * and interacting with the NOIR application.
 */
export class BasePage {
  readonly page: Page;

  // Common UI elements
  readonly sidebar: Locator;
  readonly navbar: Locator;
  readonly loadingSpinner: Locator;
  readonly toast: Locator;
  readonly confirmDialog: Locator;

  constructor(page: Page) {
    this.page = page;

    // Initialize common locators
    this.sidebar = page.locator('[data-testid="sidebar"], aside, nav.sidebar');
    this.navbar = page.locator('[data-testid="navbar"], header, nav.navbar');
    this.loadingSpinner = page.locator('[data-testid="loading"], .loading, .spinner, [role="progressbar"]');
    this.toast = page.locator('[data-sonner-toast], [role="alert"], .toast');
    this.confirmDialog = page.locator('[role="alertdialog"], [data-testid="confirm-dialog"]');
  }

  /**
   * Navigate to a path relative to base URL
   */
  async goto(path: string): Promise<void> {
    await this.page.goto(path);
    await this.waitForPageLoad();
  }

  /**
   * Wait for page to fully load (no loading spinners)
   */
  async waitForPageLoad(): Promise<void> {
    // Wait for network to be idle
    await this.page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {
      // Network idle timeout is acceptable, continue
    });

    // Wait for any loading spinners to disappear
    const spinnerCount = await this.loadingSpinner.count();
    if (spinnerCount > 0) {
      await this.loadingSpinner.first().waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {
        // Spinner timeout is acceptable
      });
    }
  }

  /**
   * Wait for and verify a success toast message
   */
  async expectSuccessToast(messagePattern?: string | RegExp): Promise<void> {
    const toast = this.page.locator('[data-sonner-toast][data-type="success"], .toast-success, [role="alert"]');
    await expect(toast.first()).toBeVisible({ timeout: 10000 });

    if (messagePattern) {
      await expect(toast.first()).toContainText(messagePattern);
    }
  }

  /**
   * Wait for and verify an error toast message
   */
  async expectErrorToast(messagePattern?: string | RegExp): Promise<void> {
    const toast = this.page.locator('[data-sonner-toast][data-type="error"], .toast-error, [role="alert"]');
    await expect(toast.first()).toBeVisible({ timeout: 10000 });

    if (messagePattern) {
      await expect(toast.first()).toContainText(messagePattern);
    }
  }

  /**
   * Click a confirmation button in a dialog
   */
  async confirmAction(): Promise<void> {
    const confirmButton = this.confirmDialog.locator('button:has-text("Confirm"), button:has-text("Yes"), button:has-text("Delete"), [data-testid="confirm-button"]');
    await confirmButton.click();
  }

  /**
   * Cancel action in a dialog
   */
  async cancelAction(): Promise<void> {
    const cancelButton = this.confirmDialog.locator('button:has-text("Cancel"), button:has-text("No"), [data-testid="cancel-button"]');
    await cancelButton.click();
  }

  /**
   * Navigate via sidebar menu
   */
  async navigateToMenu(menuText: string, submenuText?: string): Promise<void> {
    // Click main menu item
    const menuItem = this.sidebar.locator(`text="${menuText}"`).first();
    await menuItem.click();

    // Click submenu if provided
    if (submenuText) {
      const submenu = this.sidebar.locator(`text="${submenuText}"`).first();
      await submenu.click();
    }

    await this.waitForPageLoad();
  }

  /**
   * Get current URL path
   */
  getCurrentPath(): string {
    return new URL(this.page.url()).pathname;
  }

  /**
   * Take a screenshot with a descriptive name
   */
  async takeScreenshot(name: string): Promise<void> {
    await this.page.screenshot({
      path: `test-results/screenshots/${name}-${Date.now()}.png`,
      fullPage: true,
    });
  }

  /**
   * Fill a form field by label
   */
  async fillByLabel(label: string, value: string): Promise<void> {
    const field = this.page.locator(`label:has-text("${label}") + input, label:has-text("${label}") ~ input, [aria-label="${label}"]`).first();
    await field.fill(value);
  }

  /**
   * Select option from dropdown by label
   */
  async selectByLabel(label: string, optionText: string): Promise<void> {
    // Click the trigger
    const trigger = this.page.locator(`label:has-text("${label}") + button, label:has-text("${label}") ~ button, [aria-label="${label}"]`).first();
    await trigger.click();

    // Select the option
    const option = this.page.locator(`[role="option"]:has-text("${optionText}"), [role="menuitem"]:has-text("${optionText}")`).first();
    await option.click();
  }

  /**
   * Check a checkbox by label
   */
  async checkByLabel(label: string): Promise<void> {
    const checkbox = this.page.locator(`label:has-text("${label}") input[type="checkbox"], [aria-label="${label}"]`).first();
    await checkbox.check();
  }

  /**
   * Uncheck a checkbox by label
   */
  async uncheckByLabel(label: string): Promise<void> {
    const checkbox = this.page.locator(`label:has-text("${label}") input[type="checkbox"], [aria-label="${label}"]`).first();
    await checkbox.uncheck();
  }
}
