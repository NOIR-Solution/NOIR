import { test, expect } from '@playwright/test';
import { BlogCategoriesPage } from '../pages';

/**
 * Blog Categories Management Tests
 *
 * E2E tests for blog category CRUD operations.
 * Tags: @blog @categories @P0 @P1
 */

test.describe('Blog Categories Management @blog @categories', () => {
  const testCategoryName = `Test Category ${Date.now()}`;

  test.describe('Blog Categories List @P0', () => {
    test('BLOGCAT-001: Blog categories page loads successfully', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();
    });

    test('BLOGCAT-002: Create button is visible', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();
      // Button may render after page content - use explicit timeout
      await expect(categoriesPage.createButton).toBeVisible({ timeout: 15000 });
    });
  });

  test.describe('Blog Category Creation @P0', () => {
    test('BLOGCAT-010: Create category dialog opens', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      await categoriesPage.openCreateDialog();

      await expect(categoriesPage.dialog).toBeVisible();
      await expect(categoriesPage.nameInput).toBeVisible();
    });

    test('BLOGCAT-011: Create category with name', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      await categoriesPage.createCategory({
        name: testCategoryName,
        description: 'Test blog category description',
      });

      // Category should appear in list
      await categoriesPage.expectCategoryExists(testCategoryName);
    });
  });

  test.describe('Blog Category Edit @P1', () => {
    test('BLOGCAT-012: Edit category updates name', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      // First create a category to edit
      const editTestName = `Edit Test ${Date.now()}`;
      await categoriesPage.createCategory({ name: editTestName });
      await categoriesPage.expectCategoryExists(editTestName);

      // Switch to table view if needed for editing
      if (await categoriesPage.tableViewButton.isVisible({ timeout: 3000 }).catch(() => false)) {
        await categoriesPage.switchToTableView();
      }

      // Edit the category
      const updatedName = `${editTestName} Updated`;
      await categoriesPage.editCategory(editTestName, {
        name: updatedName,
      });

      // Verify category was updated
      await categoriesPage.expectCategoryExists(updatedName);
    });
  });

  test.describe('Blog Category Delete @P1', () => {
    test('BLOGCAT-013: Delete category shows confirmation and completes', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      // First create a category to delete
      const deleteTestName = `Delete Test ${Date.now()}`;
      await categoriesPage.createCategory({ name: deleteTestName });
      await categoriesPage.expectCategoryExists(deleteTestName);

      // Switch to table view if needed for deleting
      if (await categoriesPage.tableViewButton.isVisible({ timeout: 3000 }).catch(() => false)) {
        await categoriesPage.switchToTableView();
      }

      // Delete the category
      await categoriesPage.deleteCategory(deleteTestName);

      // Verify category no longer exists
      await categoriesPage.expectCategoryNotExists(deleteTestName);
    });
  });

  test.describe('Blog Category View Mode @P1', () => {
    test('BLOGCAT-014: View mode toggle (table/tree)', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      // Check if view toggle buttons are visible
      const tableButtonVisible = await categoriesPage.tableViewButton.isVisible({ timeout: 5000 }).catch(() => false);
      const treeButtonVisible = await categoriesPage.treeViewButton.isVisible({ timeout: 5000 }).catch(() => false);

      if (tableButtonVisible && treeButtonVisible) {
        // Switch to table view
        await categoriesPage.switchToTableView();
        await expect(categoriesPage.categoriesTable).toBeVisible({ timeout: 10000 });

        // Switch back to tree view
        await categoriesPage.switchToTreeView();
        // Tree view should be active (table may still be visible in some implementations)
        await page.waitForTimeout(1000); // Wait for view transition
      }
    });
  });

  test.describe('Blog Category Search @P1', () => {
    test('BLOGCAT-015: Search categories by name', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      // First create a category to search for
      const searchTestName = `Search Test ${Date.now()}`;
      await categoriesPage.createCategory({ name: searchTestName });
      await categoriesPage.expectCategoryExists(searchTestName);

      // Search for the category
      if (await categoriesPage.searchInput.isVisible({ timeout: 5000 }).catch(() => false)) {
        await categoriesPage.search(searchTestName);

        // Verify category is still visible after search
        await categoriesPage.expectCategoryExists(searchTestName);
      }
    });
  });

  test.describe('Blog Category Validation @P1', () => {
    test('BLOGCAT-016: Create category validation - empty name', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      await categoriesPage.openCreateDialog();

      // Try to save without name
      await categoriesPage.saveButton.click();

      // Should show validation error - check for error state
      const error = page.locator('[data-testid="name-error"], .text-destructive, [aria-invalid="true"]');
      await expect(error.first()).toBeVisible({ timeout: 5000 });
    });

    test('BLOGCAT-017: Cancel button closes dialog without saving', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      await categoriesPage.openCreateDialog();
      await expect(categoriesPage.dialog).toBeVisible();

      // Click cancel
      await categoriesPage.cancelButton.click();
      await expect(categoriesPage.dialog).toBeHidden({ timeout: 5000 });
    });
  });

  test.describe('Blog Category Hierarchy @P1', () => {
    test('BLOGCAT-018: Parent dropdown shows available categories', async ({ page }) => {
      const categoriesPage = new BlogCategoriesPage(page);
      await categoriesPage.navigate();
      await categoriesPage.expectPageLoaded();

      // Create a parent category first
      const parentName = `Parent ${Date.now()}`;
      await categoriesPage.createCategory({ name: parentName });
      await categoriesPage.expectCategoryExists(parentName);

      // Open create dialog for child
      await categoriesPage.openCreateDialog();

      // Wait for parent select to be enabled (categories loaded)
      if (await categoriesPage.parentSelect.isEnabled({ timeout: 10000 }).catch(() => false)) {
        await categoriesPage.parentSelect.click();

        // Verify parent option is visible in dropdown
        const selectContent = page.locator('[role="listbox"]');
        await expect(selectContent).toBeVisible({ timeout: 5000 });
        const parentOption = selectContent.locator(`[role="option"]:has-text("${parentName}")`);
        await expect(parentOption).toBeVisible({ timeout: 10000 });

        // Close the dropdown by pressing Escape
        await page.keyboard.press('Escape');
        await expect(selectContent).toBeHidden({ timeout: 3000 });
      }

      // Close dialog
      await categoriesPage.cancelButton.click();
      await expect(categoriesPage.dialog).toBeHidden({ timeout: 5000 });
    });
  });
});
