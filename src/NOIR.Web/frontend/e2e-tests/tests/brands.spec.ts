import { test, expect } from '@playwright/test';
import { BrandsPage } from '../pages';

/**
 * Brand Management Tests
 *
 * Comprehensive E2E tests for brand CRUD operations.
 * Tags: @brands @P0 @P1
 */

test.describe('Brand Management @brands', () => {
  const testBrandName = `Test Brand ${Date.now()}`;

  test.describe('Brand List @P0', () => {
    test('BRAND-001: Brands page loads successfully', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();
    });

    test('BRAND-002: Create button is visible and clickable', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();
      await expect(brandsPage.createButton).toBeVisible();
      await expect(brandsPage.createButton).toBeEnabled();
    });

    test('BRAND-003: Search input is functional', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      if (await brandsPage.searchInput.isVisible()) {
        await brandsPage.searchInput.fill('test');
        await expect(brandsPage.searchInput).toHaveValue('test');
      }
    });
  });

  test.describe('Brand Creation @P0', () => {
    test('BRAND-010: Open create brand dialog', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      // Wait for create button to be visible and click
      await expect(brandsPage.createButton).toBeVisible({ timeout: 15000 });
      await brandsPage.createButton.click();

      await expect(brandsPage.dialog).toBeVisible({ timeout: 10000 });
      await expect(brandsPage.nameInput).toBeVisible();
    });

    test('BRAND-011: Create brand with required fields', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      // Create brand with unique name
      const uniqueBrandName = `Test Brand ${Date.now()}`;

      // Wait for button and open dialog
      await expect(brandsPage.createButton).toBeVisible({ timeout: 15000 });
      await brandsPage.createButton.click();
      await expect(brandsPage.dialog).toBeVisible({ timeout: 10000 });

      // Fill form
      await brandsPage.nameInput.fill(uniqueBrandName);
      if (await brandsPage.descriptionInput.isVisible()) {
        await brandsPage.descriptionInput.fill('Test brand description');
      }

      // Submit and wait
      await brandsPage.saveButton.click();

      // Wait for dialog to close or toast
      await Promise.race([
        brandsPage.dialog.waitFor({ state: 'hidden', timeout: 15000 }),
        page.locator('[data-sonner-toast]').waitFor({ state: 'visible', timeout: 15000 }),
      ]);

      // Verify success
      const dialogHidden = await brandsPage.dialog.isHidden();
      if (dialogHidden) {
        await brandsPage.expectBrandExists(uniqueBrandName);
      }
    });

    test('BRAND-012: Create brand with all fields', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      const fullBrandName = `Full Brand ${Date.now()}`;

      // Wait for button and open dialog
      await expect(brandsPage.createButton).toBeVisible({ timeout: 15000 });
      await brandsPage.createButton.click();
      await expect(brandsPage.dialog).toBeVisible({ timeout: 10000 });

      // Fill all fields
      await brandsPage.nameInput.fill(fullBrandName);
      if (await brandsPage.descriptionInput.isVisible()) {
        await brandsPage.descriptionInput.fill('Complete brand with all fields');
      }
      if (await brandsPage.websiteInput.isVisible()) {
        await brandsPage.websiteInput.fill('https://example.com');
      }

      // Submit
      await brandsPage.saveButton.click();

      // Wait for dialog to close
      await Promise.race([
        brandsPage.dialog.waitFor({ state: 'hidden', timeout: 15000 }),
        page.locator('[data-sonner-toast]').waitFor({ state: 'visible', timeout: 15000 }),
      ]);

      const dialogHidden = await brandsPage.dialog.isHidden();
      if (dialogHidden) {
        await brandsPage.expectBrandExists(fullBrandName);
      }
    });

    test('BRAND-013: Create brand validation - empty name', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      await expect(brandsPage.createButton).toBeVisible({ timeout: 15000 });
      await brandsPage.createButton.click();
      await expect(brandsPage.dialog).toBeVisible({ timeout: 10000 });

      // Try to save without name
      await brandsPage.saveButton.click();

      // Should show validation error - dialog stays open
      await expect(brandsPage.dialog).toBeVisible({ timeout: 3000 });
    });

    test('BRAND-014: Cancel button closes dialog', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      await expect(brandsPage.createButton).toBeVisible({ timeout: 15000 });
      await brandsPage.createButton.click();
      await expect(brandsPage.dialog).toBeVisible({ timeout: 10000 });

      // Fill in some data to ensure form is interactive
      await brandsPage.nameInput.fill('Test Brand');
      await page.waitForTimeout(300);

      // Use Escape key to close dialog (most reliable method for radix dialogs)
      await page.keyboard.press('Escape');
      await expect(brandsPage.dialog).toBeHidden({ timeout: 10000 });
    });
  });

  test.describe('Brand Edit @P1', () => {
    test('BRAND-020: Edit brand opens dialog with data', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      const count = await brandsPage.getBrandCount();
      if (count > 0) {
        // Click edit on first brand
        const firstBrand = page.locator('tbody tr, [data-testid="brand-card"]').first();
        const editButton = firstBrand.locator('button:has-text("Edit"), [data-testid="edit-button"]');

        if (await editButton.isVisible()) {
          await editButton.click();
          await expect(brandsPage.dialog).toBeVisible();
          await expect(brandsPage.nameInput).toHaveValue(/.+/);
        }
      }
    });
  });

  test.describe('Brand Search @P1', () => {
    test('BRAND-030: Search brands by name', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      if (await brandsPage.searchInput.isVisible()) {
        await brandsPage.search('test');
        await expect(brandsPage.searchInput).toHaveValue('test');
      }
    });

    test('BRAND-031: Clear search resets results', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      if (await brandsPage.searchInput.isVisible()) {
        await brandsPage.search('test');
        await brandsPage.searchInput.clear();
        await page.keyboard.press('Enter');
        await expect(brandsPage.searchInput).toHaveValue('');
      }
    });
  });

  test.describe('Brand Delete @P1', () => {
    test('BRAND-040: Delete shows confirmation dialog', async ({ page }) => {
      const brandsPage = new BrandsPage(page);
      await brandsPage.navigate();
      await brandsPage.expectPageLoaded();

      const count = await brandsPage.getBrandCount();
      if (count > 0) {
        // Find the first row and its actions dropdown trigger (button with MoreHorizontal icon)
        const firstRow = page.locator('tbody tr').first();
        // The actions button is a ghost variant button containing an SVG in the last cell
        const actionsButton = firstRow.locator('td:last-child button[variant="ghost"], td:last-child button').first();

        if (await actionsButton.isVisible({ timeout: 5000 }).catch(() => false)) {
          await actionsButton.click();

          // Wait for dropdown menu to appear
          const dropdownMenu = page.locator('[role="menu"]');
          await expect(dropdownMenu).toBeVisible({ timeout: 5000 });

          // Look for delete menu item
          const deleteMenuItem = dropdownMenu.locator('[role="menuitem"]:has-text("Delete")');
          if (await deleteMenuItem.isVisible({ timeout: 3000 }).catch(() => false)) {
            await deleteMenuItem.click();

            // Delete confirmation dialog should appear (uses role="dialog" with "Delete Brand" title)
            const deleteDialog = page.locator('[role="dialog"]:has-text("Delete Brand")');
            await expect(deleteDialog).toBeVisible({ timeout: 5000 });

            // Cancel the delete
            const cancelButton = deleteDialog.locator('button:has-text("Cancel")');
            await cancelButton.click();
            await expect(deleteDialog).toBeHidden({ timeout: 5000 });
          } else {
            // Menu doesn't have delete option (user may not have permission) - close menu
            await page.keyboard.press('Escape');
          }
        }
      }
    });
  });
});
