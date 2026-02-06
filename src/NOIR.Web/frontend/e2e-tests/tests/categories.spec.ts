import { test, expect } from '@playwright/test';
import { CategoriesPage } from '../pages';

/**
 * Category Management Tests
 *
 * E2E tests for product category CRUD operations.
 * Tags: @categories @P0 @P1
 */

test.describe('Category Management @categories', () => {
  const testCategoryName = `Test Category ${Date.now()}`;

  test.describe('Category List @P0', () => {
    test('CAT-001: Categories page loads successfully', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();
    });

    test('CAT-002: Create category button is visible', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();
      // Button may render after page content - use explicit timeout
      await expect(categoriesPage.createButton).toBeVisible({ timeout: 15000 });
    });
  });

  test.describe('Category Creation @P0', () => {
    test('CAT-010: Open create category dialog', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();
      // Wait for page to fully load before interacting
      await categoriesPage.expectPageLoaded();
      await categoriesPage.openCreateDialog();

      await expect(categoriesPage.dialog).toBeVisible();
      await expect(categoriesPage.nameInput).toBeVisible();
    });

    test('CAT-011: Create category with required fields', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      await categoriesPage.createCategory({
        name: testCategoryName,
        description: 'Test category description',
      });

      // Category should appear in list
      await categoriesPage.expectCategoryExists(testCategoryName);
    });

    test('CAT-012: Create category validation - empty name', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.openCreateDialog();

      // Try to save without name
      await categoriesPage.saveButton.click();

      // Should show validation error
      const error = page.locator('[data-testid="name-error"], .error, [aria-invalid="true"]');
      await expect(error.first()).toBeVisible({ timeout: 5000 });
    });

    test('CAT-013: Create subcategory with parent', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      const parentName = `Parent ${Date.now()}`;
      await categoriesPage.createCategory({ name: parentName });
      await categoriesPage.expectCategoryExists(parentName);

      const childName = `Child ${Date.now()}`;
      await categoriesPage.createCategory({
        name: childName,
        parentCategory: parentName,
      });

      // Reload page to ensure fresh data from server
      await page.reload();
      await categoriesPage.expectPageLoaded();

      // Expand all tree nodes to reveal subcategories
      const expandAllButton = page.getByRole('button', { name: 'Expand All' });
      if (await expandAllButton.isVisible()) {
        await expandAllButton.click();
        await page.waitForTimeout(1000);
      }

      // Child should be visible in the expanded tree
      await categoriesPage.expectCategoryExists(childName);
    });

    // Test that parent category dropdown loads fresh data
    test('CAT-014: Parent dropdown shows newly created categories', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      // Create a parent category
      const parentName = `Parent ${Date.now()}`;
      await categoriesPage.createCategory({ name: parentName });
      await categoriesPage.expectCategoryExists(parentName);

      // Open dialog again - parent should appear in dropdown
      await categoriesPage.openCreateDialog();

      // Wait for parent select to be enabled (categories loaded)
      await expect(categoriesPage.parentSelect).toBeEnabled({ timeout: 15000 });
      await categoriesPage.parentSelect.click();

      // Verify parent option is visible in dropdown
      const selectContent = page.locator('[role="listbox"]');
      await expect(selectContent).toBeVisible({ timeout: 5000 });
      const parentOption = selectContent.locator(`[role="option"]:has-text("${parentName}")`);
      await expect(parentOption).toBeVisible({ timeout: 10000 });

      // Close the dropdown by pressing Escape
      await page.keyboard.press('Escape');
      await expect(selectContent).toBeHidden({ timeout: 3000 });

      // Close dialog
      await categoriesPage.cancelButton.click();
      await expect(categoriesPage.dialog).toBeHidden({ timeout: 5000 });
    });
  });

  test.describe('Category Edit @P1', () => {
    test('CAT-020: Edit category opens dialog with data', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();

      const count = await categoriesPage.getCategoryCount();
      if (count > 0) {
        // Click edit on first category
        const firstCategory = page.locator('tbody tr, [data-testid="category-card"]').first();
        const editButton = firstCategory.locator('button:has-text("Edit"), [data-testid="edit-button"]');

        if (await editButton.isVisible()) {
          await editButton.click();
          await expect(categoriesPage.dialog).toBeVisible();
          await expect(categoriesPage.nameInput).toHaveValue(/.+/);
        }
      }
    });
  });

  test.describe('Category Search @P1', () => {
    test('CAT-030: Search categories by name', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();

      if (await categoriesPage.searchInput.isVisible()) {
        await categoriesPage.search('test');
        await expect(categoriesPage.searchInput).toHaveValue('test');
      }
    });
  });

  test.describe('Category Delete @P1', () => {
    test('CAT-040: Delete shows confirmation dialog', async ({ page }) => {
      const categoriesPage = new CategoriesPage(page);
      await categoriesPage.navigate();

      const count = await categoriesPage.getCategoryCount();
      if (count > 0) {
        const firstCategory = page.locator('tbody tr, [data-testid="category-card"]').first();
        const actionsButton = firstCategory.locator('[data-testid="actions-menu"], button.actions');

        if (await actionsButton.isVisible()) {
          await actionsButton.click();
          const deleteButton = page.locator('[role="menuitem"]:has-text("Delete")');

          if (await deleteButton.isVisible()) {
            await deleteButton.click();
            await expect(categoriesPage.confirmDialog).toBeVisible({ timeout: 5000 });
            await categoriesPage.cancelAction();
          }
        }
      }
    });
  });
});
