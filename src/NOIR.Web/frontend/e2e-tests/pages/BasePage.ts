import { Page, Locator, expect } from '@playwright/test';
import * as nodePath from 'path';

/**
 * Standardized timeout values for consistent test behavior
 * CI environment gets 2x timeouts due to slower Docker containers, SQL Server, and backend compilation
 */
const CI_MULTIPLIER = process.env.CI ? 2 : 1;

export const Timeouts = {
  PAGE_LOAD: 30000 * CI_MULTIPLIER,
  ELEMENT_VISIBLE: 15000 * CI_MULTIPLIER,
  ELEMENT_ENABLED: 5000 * CI_MULTIPLIER,
  DIALOG_OPEN: 10000 * CI_MULTIPLIER,
  DIALOG_CLOSE: 10000 * CI_MULTIPLIER,
  QUICK_CHECK: 3000 * CI_MULTIPLIER,
  TOAST: 10000 * CI_MULTIPLIER,
  API_RESPONSE: 15000 * CI_MULTIPLIER,
  STABILITY_WAIT: 200, // Don't scale stability waits
  SEARCH_WAIT: 500, // Don't scale search waits
  REDIRECT: 1000, // Don't scale redirects
} as const;

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
    // Radix AlertDialog uses role="dialog", not "alertdialog" - include both for compatibility
    this.confirmDialog = page.locator('[role="alertdialog"], [role="dialog"]:has-text("Delete"), [data-testid="confirm-dialog"]');
  }

  /**
   * Open the user profile dropdown menu at the bottom of the sidebar.
   * Used for accessing theme, language, and settings options.
   */
  async openUserProfileMenu(): Promise<void> {
    const userProfileTrigger = this.page.locator(
      'button:has(svg.lucide-chevrons-up-down), button:has(svg.lucide-chevron-up)'
    );
    const fallbackTrigger = this.page.locator('button:has-text("admin@noir.local")');
    const trigger = userProfileTrigger.or(fallbackTrigger);
    await expect(trigger.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await trigger.first().click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Navigate to a path relative to base URL.
   * Includes auth recovery: if the page redirects to login (expired token),
   * re-authenticates and retries navigation.
   */
  async goto(path: string): Promise<void> {
    await this.page.goto(path);
    await this.waitForPageLoad();

    // Auth recovery: detect if redirected to login due to expired tokens
    // Skip if we're intentionally navigating to login-related pages
    const isLoginPath = path.includes('/login') || path.includes('/forgot-password');
    if (!isLoginPath && await this.isOnLoginPage()) {
      await this.reAuthenticate();
      await this.page.goto(path);
      await this.waitForPageLoad();
    }
  }

  /**
   * Check if the current page is the login page (auth expired)
   */
  private async isOnLoginPage(): Promise<boolean> {
    return await this.page.locator('h1:has-text("NOIR Authentication")').isVisible();
  }

  /**
   * Re-authenticate when auth state expires during long test runs.
   * Updates the storage state file so subsequent tests get fresh tokens.
   */
  private async reAuthenticate(): Promise<void> {
    if (!this.page.url().includes('/login')) {
      await this.page.goto('/login');
      await this.page.waitForLoadState('networkidle');
    }

    await this.page.locator('#email').fill('admin@noir.local');
    await this.page.locator('#password').fill('123qwe');
    await this.page.locator('button[type="submit"]').click();
    await this.page.waitForURL(/\/(portal|dashboard)/, { timeout: 30000 });
    await this.page.waitForLoadState('networkidle');

    // Save updated auth state for subsequent tests
    const authStatePath = nodePath.join(__dirname, '..', '.auth', 'tenant-admin.json');
    await this.page.context().storageState({ path: authStatePath });
  }

  /**
   * Wait for page to fully load (no loading spinners)
   */
  async waitForPageLoad(): Promise<void> {
    // Wait for DOM content to be loaded first
    await this.page.waitForLoadState('domcontentloaded', { timeout: 30000 }).catch(() => {});

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

    // Small stability wait for React hydration (helps Firefox)
    await this.page.waitForTimeout(200);
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

  // ============================================================
  // Reusable Dialog & Interaction Patterns
  // ============================================================

  /**
   * Standard pattern for opening dialogs via button click
   * Waits for button readiness, clicks, and verifies dialog appears
   */
  async openDialogViaButton(button: Locator, dialog: Locator): Promise<void> {
    await expect(button.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(button.first()).toBeEnabled({ timeout: Timeouts.ELEMENT_ENABLED });
    await button.first().click();
    await expect(dialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Close dialog using Escape key (most reliable for Radix dialogs)
   * Falls back to clicking cancel button if Escape doesn't work
   */
  async closeDialog(dialog: Locator, cancelButton?: Locator): Promise<void> {
    await this.page.keyboard.press('Escape');
    const isClosed = await dialog.isHidden({ timeout: Timeouts.QUICK_CHECK }).catch(() => false);

    if (!isClosed && cancelButton) {
      await cancelButton.first().click();
    }

    await expect(dialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Click element with scroll handling for elements inside scroll containers (e.g., tall dialogs)
   * Uses JavaScript scrollIntoView which works within nested scroll contexts
   * @param locator - The element to click
   */
  async clickWithScroll(locator: Locator): Promise<void> {
    await locator.evaluate((el) => {
      el.scrollIntoView({ behavior: 'instant', block: 'center' });
    });
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
    await locator.click();
  }

  /**
   * Verify an item exists, searching if not immediately visible
   * Handles pagination by searching for the item
   * @param name - Text to find on the page
   * @param searchFn - Optional function to call to search for the item
   */
  async expectItemExists(name: string, searchFn?: () => Promise<void>): Promise<void> {
    let item = this.page.locator(`text="${name}"`).first();
    const visible = await item.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false);

    if (!visible && searchFn) {
      await searchFn();
      await this.waitForPageLoad();
      item = this.page.locator(`text="${name}"`).first();
    }

    await expect(item).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Select option from Cmdk command palette dialog
   * Handles nested dialog structure with multiple fallback strategies
   * @param triggerButton - Button that opens the command palette
   * @param optionText - Exact text of the option to select
   */
  async selectFromCmdk(triggerButton: Locator, optionText: string): Promise<void> {
    await triggerButton.click();
    await this.page.waitForTimeout(Timeouts.SEARCH_WAIT);

    // Cmdk opens as a second dialog on the page
    const cmdkDialog = this.page.locator('[role="dialog"]').nth(1);
    await expect(cmdkDialog).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    // Try cursor pointer divs first (Cmdk option pattern)
    const cmdkOptions = cmdkDialog.locator('[cursor=pointer], div[class*="cursor-pointer"]');
    const optionCount = await cmdkOptions.count();
    let clicked = false;

    // Find exact text match
    for (let i = 0; i < optionCount; i++) {
      const optText = await cmdkOptions.nth(i).textContent();
      if (optText?.trim() === optionText) {
        await cmdkOptions.nth(i).click();
        clicked = true;
        break;
      }
    }

    // Fallback: try exact text locator
    if (!clicked) {
      const exactOption = cmdkDialog.locator(`text="${optionText}"`).first();
      if (await exactOption.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false)) {
        await exactOption.click();
        clicked = true;
      }
    }

    // Final fallback: filter by regex
    if (!clicked) {
      const genericOption = cmdkDialog.locator(`div:has-text("${optionText}")`).filter({ hasText: new RegExp(`^${optionText}$`) }).first();
      await genericOption.click();
    }

    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Standard page load verification for CRUD pages
   * Implements sequential wait pattern: header first (proves render), then action button (proves data loaded)
   *
   * Why sequential waits:
   * React renders in phases: layout → data fetch → interactive elements.
   * By waiting for the action button, we ensure data loading completed.
   *
   * @param headerSelector - Locator for page header (h1)
   * @param actionButtonSelector - Locator for primary action button or interactive element
   */
  async expectStandardPageLoaded(
    headerSelector: Locator,
    actionButtonSelector: Locator
  ): Promise<void> {
    // First wait for header (proves initial page render)
    await expect(headerSelector.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    // Then wait for action button (proves data loaded and page is interactive)
    await expect(actionButtonSelector.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Open delete confirmation dialog from row actions menu
   * Returns the confirmation dialog locator for further assertions
   * @param rowLocator - Locator for the row containing the item to delete
   */
  async openDeleteConfirmation(rowLocator: Locator): Promise<Locator> {
    const actionsButton = rowLocator.locator('td:last-child button').first();
    await actionsButton.click();

    const dropdownMenu = this.page.locator('[role="menu"]');
    await expect(dropdownMenu).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    const deleteMenuItem = dropdownMenu.locator('[role="menuitem"]:has-text("Delete")');
    await deleteMenuItem.click();

    await expect(this.confirmDialog.first()).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });
    return this.confirmDialog.first();
  }
}
