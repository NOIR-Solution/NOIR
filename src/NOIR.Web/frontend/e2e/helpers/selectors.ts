import { type Page, expect } from '@playwright/test';

/**
 * Shared selectors and UI interaction helpers.
 *
 * The frontend uses:
 * - Sonner for toast notifications
 * - Credenza (Radix-based) for dialogs
 * - Radix UI for dropdowns, selects, tabs
 */

// ─── Toast selectors (Sonner) ──────────────────────────────────
export const TOAST_SUCCESS = '[data-sonner-toast][data-type="success"]';
export const TOAST_ERROR = '[data-sonner-toast][data-type="error"]';
export const TOAST_INFO = '[data-sonner-toast][data-type="info"]';

// ─── Dialog selectors (Credenza / Radix) ───────────────────────
export const CONFIRM_DIALOG = '[role="alertdialog"], [role="dialog"]';
export const DIALOG_CONTENT = '[data-state="open"][role="dialog"]';

// ─── Loading indicators ────────────────────────────────────────
export const LOADING_SPINNER = '[data-testid="loading"], [role="progressbar"], .animate-spin';
export const TABLE_SKELETON = '[data-testid="table-skeleton"]';

// ─── Helpers ───────────────────────────────────────────────────

/**
 * Click the confirm/yes button in a destructive confirmation dialog.
 * Waits for the dialog to appear before clicking.
 */
export async function confirmDelete(page: Page) {
  // Wait for a dialog with a destructive action button to be visible
  // Credenza (Radix Dialog) renders role="dialog" with data-state="open"
  // Use page.waitForSelector for reliability
  await page.waitForSelector('[role="dialog"]:visible', { timeout: 5_000 });
  await page.waitForTimeout(300); // let dialog animation settle
  const btn = page.locator('[role="dialog"]').last()
    .getByRole('button', { name: /confirm|yes|delete|ok/i }).first();
  await btn.waitFor({ state: 'visible', timeout: 5_000 });
  await btn.click();
}

/**
 * Wait for table data to finish loading.
 * Waits for skeleton/spinner to disappear and rows to be present.
 */
export async function waitForTableLoad(page: Page) {
  // Wait for any loading indicator to disappear
  const spinner = page.locator(LOADING_SPINNER);
  if (await spinner.isVisible({ timeout: 1_000 }).catch(() => false)) {
    await spinner.waitFor({ state: 'hidden', timeout: 15_000 });
  }

  // Wait for at least one data row to appear
  await expect(page.getByRole('table').getByRole('row')).not.toHaveCount(0);
}

/**
 * Expect a toast notification with the given text.
 * Matches against success toasts by default.
 */
export async function expectToast(page: Page, text: string | RegExp, type: 'success' | 'error' | 'info' = 'success') {
  const selectorMap = {
    success: TOAST_SUCCESS,
    error: TOAST_ERROR,
    info: TOAST_INFO,
  };
  // Use .filter() to find the specific toast with matching text — avoids strict mode violations
  // when multiple toasts of the same type are stacked
  const toast = page.locator(selectorMap[type]).filter({ hasText: text });
  await expect(toast.first()).toBeVisible({ timeout: 10_000 });
}

/**
 * Dismiss all visible toast notifications.
 */
export async function dismissToasts(page: Page) {
  const toasts = page.locator('[data-sonner-toast]');
  const count = await toasts.count();
  for (let i = 0; i < count; i++) {
    const closeBtn = toasts.nth(i).getByRole('button', { name: /close|dismiss/i });
    if (await closeBtn.isVisible({ timeout: 500 }).catch(() => false)) {
      await closeBtn.click();
    }
  }
}

/**
 * Close an open dialog by clicking its close button or pressing Escape.
 */
export async function closeDialog(page: Page) {
  const closeBtn = page.locator(DIALOG_CONTENT).getByRole('button', { name: /close/i });
  if (await closeBtn.isVisible({ timeout: 1_000 }).catch(() => false)) {
    await closeBtn.click();
  } else {
    await page.keyboard.press('Escape');
  }
}

/**
 * Select an option from a combobox/select dropdown.
 */
export async function selectOption(page: Page, label: string | RegExp, option: string | RegExp) {
  await page.getByRole('combobox', { name: label }).click();
  await page.getByRole('option', { name: option }).click();
}
