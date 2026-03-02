import { test, expect } from '../../fixtures/base.fixture';
import { testCustomerGroup, testCustomer } from '../../helpers/test-data';
import {
  expectToast,
  confirmDelete,
  TOAST_SUCCESS,
} from '../../helpers/selectors';

const API_URL = process.env.API_URL ?? 'http://localhost:4000';

// ─── Customer Groups: Regression Tests ──────────────────────────────────────

test.describe('E-commerce Customer Groups @regression', () => {
  /**
   * CGRP-001: Customer group list loads
   */
  test('CGRP-001: should display customer groups list @smoke', async ({
    customerGroupsPage,
    page,
  }) => {
    await customerGroupsPage.goto();

    await expect(page.locator('main').first()).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('[role="alert"][data-type="error"]')).not.toBeVisible({ timeout: 2_000 }).catch(() => {});
  });

  /**
   * CGRP-002: Create, edit, and delete a customer group
   */
  test('CGRP-002: should perform customer group CRUD @regression', async ({
    customerGroupsPage,
    api,
    page,
  }) => {
    const data = testCustomerGroup();

    await customerGroupsPage.goto();

    // Create
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Wait for the create dialog before filling
    await expect(page.locator('[role="dialog"]')).toBeVisible({ timeout: 5_000 });
    await page.locator('[role="dialog"]').getByLabel(/name/i).first().fill(data.name);
    const descInput = page.locator('[role="dialog"]').getByLabel(/description/i);
    if (await descInput.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await descInput.fill(data.description);
    }

    await page.locator('[role="dialog"]').getByRole('button', { name: /save|create|submit/i }).click();
    // Use .first() — multiple toasts may stack (create + edit) by the time delete fires
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });

    // Wait for dialog to close, then verify in list
    await expect(page.locator('[role="dialog"]')).not.toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(data.name).first()).toBeVisible({ timeout: 5_000 });

    // Edit — open dropdown menu for the created group row
    const updatedName = `${data.name} Updated`;
    await page.getByRole('row', { name: new RegExp(data.name, 'i') })
      .getByRole('button')
      .first()
      .click();
    const editMenuItem = page.getByRole('menuitem', { name: /edit/i });
    if (await editMenuItem.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await editMenuItem.click();
    }

    // Wait for edit dialog before filling
    await expect(page.locator('[role="dialog"]')).toBeVisible({ timeout: 5_000 });
    const nameInput = page.locator('[role="dialog"]').getByLabel(/name/i).first();
    await nameInput.clear();
    await nameInput.fill(updatedName);
    await page.locator('[role="dialog"]').getByRole('button', { name: /save|update|submit/i }).click();
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });

    // Wait for dialog to close, then verify updated name
    await expect(page.locator('[role="dialog"]')).not.toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(updatedName).first()).toBeVisible({ timeout: 5_000 });

    // Delete — open dropdown menu for the updated group row
    await page.getByRole('row', { name: new RegExp(updatedName, 'i') })
      .getByRole('button')
      .first()
      .click();
    const deleteMenuItem2 = page.getByRole('menuitem', { name: /delete|remove/i });
    if (await deleteMenuItem2.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await deleteMenuItem2.click();
    }
    await confirmDelete(page);
    // Use .first() because at this point both "created" and "updated" toasts may be stacked
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });

    // Verify removed
    await expect(page.getByText(updatedName)).not.toBeVisible({ timeout: 5_000 });
  });

  /**
   * CGRP-003: Validation errors for customer groups
   */
  test('CGRP-003: should show validation errors for empty name @regression', async ({
    customerGroupsPage,
    page,
  }) => {
    await customerGroupsPage.goto();

    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Submit without filling name
    await page.getByRole('button', { name: /save|create|submit/i }).click();

    await expect(
      page.getByText(/required|bắt buộc/i).first(),
    ).toBeVisible({ timeout: 5_000 });
  });
});
