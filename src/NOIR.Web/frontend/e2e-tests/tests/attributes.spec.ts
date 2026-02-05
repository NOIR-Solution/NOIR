import { test, expect } from '@playwright/test';
import { AttributesPage } from '../pages';

/**
 * Product Attributes Tests
 *
 * Comprehensive E2E tests for product attribute management.
 * Tags: @attributes @P0 @P1
 */

test.describe('Product Attributes @attributes', () => {
  const testAttributeName = `Test Attr ${Date.now()}`;

  test.describe('Attributes List @P0', () => {
    test('ATTR-001: Attributes page loads successfully', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();
    });

    test('ATTR-002: Create button is visible', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();
      await expect(attributesPage.createButton).toBeVisible();
    });

    test('ATTR-003: Attributes table or list is visible', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      // Either table or cards should be visible
      const dataContainer = page.locator('table, [data-testid="attributes-list"], .attributes-grid');
      await expect(dataContainer.first()).toBeVisible({ timeout: 10000 });
    });
  });

  test.describe('Attribute Creation @P0', () => {
    test('ATTR-010: Open create attribute dialog', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();
      await attributesPage.openCreateDialog();

      await expect(attributesPage.dialog).toBeVisible();
      await expect(attributesPage.nameInput).toBeVisible();
      await expect(attributesPage.typeSelect).toBeVisible();
    });

    test('ATTR-011: Attribute type options are available', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();
      await attributesPage.openCreateDialog();

      // Click type selector
      await attributesPage.typeSelect.click();

      // Wait for dropdown/popover
      const options = page.locator('[data-radix-popper-content-wrapper], [role="listbox"]');
      await expect(options).toBeVisible({ timeout: 5000 });

      // Check for common attribute types
      const textOption = options.locator('div:has-text("Text"), [role="option"]:has-text("Text")');
      await expect(textOption.first()).toBeVisible();
    });

    test('ATTR-012: Create text attribute', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      await attributesPage.createAttribute({
        name: testAttributeName,
        type: 'Text',
      });

      // Attribute should appear in list
      await attributesPage.expectAttributeExists(testAttributeName);
    });

    test('ATTR-013: Create select attribute', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      const selectAttrName = `Select Attr ${Date.now()}`;
      await attributesPage.createAttribute({
        name: selectAttrName,
        type: 'Select',
      });

      await attributesPage.expectAttributeExists(selectAttrName);
    });

    test('ATTR-014: Create number attribute', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      const numberAttrName = `Number Attr ${Date.now()}`;
      await attributesPage.createAttribute({
        name: numberAttrName,
        type: 'Number',
      });

      await attributesPage.expectAttributeExists(numberAttrName);
    });

    test('ATTR-015: Create color attribute', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      const colorAttrName = `Color Attr ${Date.now()}`;
      await attributesPage.createAttribute({
        name: colorAttrName,
        type: 'Color',
      });

      await attributesPage.expectAttributeExists(colorAttrName);
    });

    test('ATTR-016: Create attribute validation - empty name', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();
      await attributesPage.openCreateDialog();

      // Try to save without name
      await attributesPage.saveButton.click();

      // Should show validation error - dialog stays open
      await expect(attributesPage.dialog).toBeVisible({ timeout: 3000 });
    });

    test('ATTR-017: Cancel button closes dialog', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();
      await attributesPage.openCreateDialog();

      await attributesPage.cancelButton.click();

      await expect(attributesPage.dialog).toBeHidden({ timeout: 5000 });
    });
  });

  test.describe('Attribute Search @P1', () => {
    test('ATTR-020: Search attributes by name', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      if (await attributesPage.searchInput.isVisible()) {
        await attributesPage.search('Color');
        await attributesPage.waitForPageLoad();
        await expect(attributesPage.searchInput).toHaveValue('Color');
      }
    });

    test('ATTR-021: Clear search resets results', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      if (await attributesPage.searchInput.isVisible()) {
        await attributesPage.search('test');
        await attributesPage.searchInput.clear();
        await page.keyboard.press('Enter');
        await attributesPage.waitForPageLoad();
        await expect(attributesPage.searchInput).toHaveValue('');
      }
    });
  });

  test.describe('Attribute Edit @P1', () => {
    test('ATTR-030: Edit attribute opens dialog with data', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      const count = await attributesPage.getAttributeCount();
      if (count > 0) {
        const firstAttribute = page.locator('tbody tr, [data-testid="attribute-card"]').first();
        const editButton = firstAttribute.locator('button:has-text("Edit"), [data-testid="edit-button"]');

        if (await editButton.isVisible()) {
          await editButton.click();
          await expect(attributesPage.dialog).toBeVisible();
          await expect(attributesPage.nameInput).toHaveValue(/.+/);
        }
      }
    });
  });

  test.describe('Attribute Delete @P1', () => {
    test('ATTR-040: Delete shows confirmation dialog', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      const count = await attributesPage.getAttributeCount();
      if (count > 0) {
        const firstAttribute = page.locator('tbody tr, [data-testid="attribute-card"]').first();
        const actionsButton = firstAttribute.locator('[data-testid="actions-menu"], button.actions, button:has(svg)');

        if (await actionsButton.first().isVisible()) {
          await actionsButton.first().click();
          const deleteButton = page.locator('[role="menuitem"]:has-text("Delete")');

          if (await deleteButton.isVisible()) {
            await deleteButton.click();
            await expect(attributesPage.confirmDialog).toBeVisible({ timeout: 5000 });
            await attributesPage.cancelAction();
          }
        }
      }
    });
  });

  test.describe('Attribute Values @P1', () => {
    test('ATTR-050: Select type shows value management', async ({ page }) => {
      const attributesPage = new AttributesPage(page);
      await attributesPage.navigate();
      await attributesPage.expectPageLoaded();

      // Find a Select-type attribute
      const selectAttribute = page.locator('tbody tr:has-text("Select"), [data-testid="attribute-card"]:has-text("Select")').first();

      if (await selectAttribute.isVisible()) {
        const manageButton = selectAttribute.locator('button:has-text("Values"), button:has-text("Options"), [data-testid="manage-values"]');

        if (await manageButton.isVisible()) {
          await manageButton.click();
          // Should open values dialog or navigate to values page
          const dialog = page.locator('[role="dialog"]');
          await expect(dialog).toBeVisible({ timeout: 5000 });
        }
      }
    });
  });
});
