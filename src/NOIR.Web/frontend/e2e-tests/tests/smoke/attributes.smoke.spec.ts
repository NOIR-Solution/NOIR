import { test, expect } from '@playwright/test';
import { AttributesPage } from '../../pages';

/**
 * Product Attributes Smoke Tests
 *
 * @smoke @attributes @P0
 *
 * Critical attribute management flows:
 * - View attributes list
 * - Create attribute dialog
 * - Attribute type selection
 */

test.describe('Product Attributes @smoke @attributes @P0', () => {
  test('should display attributes list page', async ({ page }) => {
    const attributesPage = new AttributesPage(page);

    await attributesPage.navigate();
    await attributesPage.expectPageLoaded();

    // Verify create button is visible
    await expect(attributesPage.createButton).toBeVisible();
  });

  test('should open create attribute dialog', async ({ page }) => {
    const attributesPage = new AttributesPage(page);

    await attributesPage.navigate();
    await attributesPage.openCreateDialog();

    // Verify dialog is visible with required fields
    await expect(attributesPage.dialog).toBeVisible();
    await expect(attributesPage.nameInput).toBeVisible();
    await expect(attributesPage.typeSelect).toBeVisible();
  });

  test('should show attribute type options', async ({ page }) => {
    const attributesPage = new AttributesPage(page);

    await attributesPage.navigate();
    await attributesPage.openCreateDialog();

    // Click type selector (Combobox trigger)
    await attributesPage.typeSelect.click();

    // Wait for Combobox popover to open
    const popover = page.locator('[data-radix-popper-content-wrapper]');
    await expect(popover).toBeVisible({ timeout: 5000 });

    // Verify attribute types are available in the Combobox dropdown
    // The Combobox uses <div> elements with text, not [role="option"]
    const selectOption = popover.locator('div:has-text("Select")').first();
    const textOption = popover.locator('div:has-text("Text")').first();
    const numberOption = popover.locator('div:has-text("Number")').first();
    const colorOption = popover.locator('div:has-text("Color")').first();

    await expect(selectOption).toBeVisible();
    await expect(textOption).toBeVisible();
    await expect(numberOption).toBeVisible();
    await expect(colorOption).toBeVisible();
  });

  test('should validate required fields', async ({ page }) => {
    const attributesPage = new AttributesPage(page);

    await attributesPage.navigate();
    await attributesPage.openCreateDialog();

    // Try to save without filling fields
    await attributesPage.saveButton.click();

    // Should show validation error or dialog remains open
    await expect(attributesPage.dialog).toBeVisible({ timeout: 3000 });
  });
});
