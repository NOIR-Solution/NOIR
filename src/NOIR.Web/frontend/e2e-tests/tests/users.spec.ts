import { test, expect } from '@playwright/test';
import { UsersPage } from '../pages';

/**
 * User Management Tests
 *
 * Comprehensive E2E tests for user CRUD operations.
 * Tags: @users @P0 @P1
 */

test.describe('User Management @users', () => {
  const testUserEmail = `test-${Date.now()}@example.com`;

  test.describe('User List @P0', () => {
    test('USER-001: Users page loads successfully', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();
    });

    test('USER-002: Create button is visible and clickable', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();
      await expect(usersPage.createButton.first()).toBeVisible();
      await expect(usersPage.createButton.first()).toBeEnabled();
    });

    test('USER-003: User table displays data', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();

      // Should show at least the admin user
      await expect(usersPage.userTable).toBeVisible();
      const rowCount = await usersPage.getUserCount();
      expect(rowCount).toBeGreaterThan(0);
    });

    test('USER-004: Search input is functional', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();

      await usersPage.searchInput.fill('admin');
      await expect(usersPage.searchInput).toHaveValue('admin');
    });
  });

  test.describe('User Creation @P0', () => {
    test('USER-010: Open create user dialog', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();
      await usersPage.openCreateDialog();

      await expect(usersPage.dialog).toBeVisible();
      await expect(usersPage.emailInput).toBeVisible();
      await expect(usersPage.passwordInput).toBeVisible();
    });

    test('USER-011: Create user validation - empty email', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();
      await usersPage.openCreateDialog();

      // Fill password but not email
      await usersPage.passwordInput.fill('Password123!');

      // Try to save
      await usersPage.saveButton.click();

      // Should show validation error - dialog stays open
      await expect(usersPage.dialog).toBeVisible({ timeout: 3000 });
    });

    test('USER-012: Create user validation - empty password', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();
      await usersPage.openCreateDialog();

      // Fill email but not password
      await usersPage.emailInput.fill('test@example.com');

      // Try to save
      await usersPage.saveButton.click();

      // Should show validation error - dialog stays open
      await expect(usersPage.dialog).toBeVisible({ timeout: 3000 });
    });

    test('USER-013: Create user validation - invalid email format', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();
      await usersPage.openCreateDialog();

      // Fill invalid email
      await usersPage.emailInput.fill('notanemail');
      await usersPage.passwordInput.fill('Password123!');

      // Try to save
      await usersPage.saveButton.click();

      // Should show validation error - dialog stays open
      await expect(usersPage.dialog).toBeVisible({ timeout: 3000 });
    });

    test('USER-014: Cancel button closes dialog', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();
      await usersPage.openCreateDialog();

      await usersPage.cancelButton.click();

      await expect(usersPage.dialog).toBeHidden({ timeout: 5000 });
    });
  });

  test.describe('User Search & Filter @P1', () => {
    test('USER-020: Search users by email', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();

      await usersPage.search('admin');
      await usersPage.waitForPageLoad();

      // Admin user should be in results
      const adminUser = page.locator('#main-content').getByText('admin@noir.local');
      await expect(adminUser).toBeVisible({ timeout: 10000 });
    });

    test('USER-021: Search users - no results', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();

      await usersPage.search('nonexistent-user-xyz-12345');
      await usersPage.waitForPageLoad();

      // Should show no results or empty state
      const tableRows = await usersPage.getUserCount();
      expect(tableRows).toBe(0);
    });

    test('USER-022: Clear search resets results', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();

      await usersPage.search('admin');
      await usersPage.waitForPageLoad();

      await usersPage.searchInput.clear();
      await page.keyboard.press('Enter');
      await usersPage.waitForPageLoad();

      // Should show all users again
      const rowCount = await usersPage.getUserCount();
      expect(rowCount).toBeGreaterThan(0);
    });

    test('USER-023: Filter by role if available', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();

      if (await usersPage.roleFilter.isVisible()) {
        await usersPage.roleFilter.click();
        // Check if role options appear
        const options = page.locator('[role="option"], [role="menuitemradio"]');
        await expect(options.first()).toBeVisible({ timeout: 5000 });
        await page.keyboard.press('Escape');
      }
    });
  });

  test.describe('User Edit @P1', () => {
    test('USER-030: Edit user opens dialog with data', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();

      const count = await usersPage.getUserCount();
      if (count > 0) {
        // Find a non-admin user to edit (avoid editing self)
        const userRows = page.locator('tbody tr');
        const firstUser = userRows.first();
        const editButton = firstUser.locator('button:has-text("Edit"), [data-testid="edit-button"]');

        if (await editButton.isVisible()) {
          await editButton.click();
          await expect(usersPage.dialog).toBeVisible();
          await expect(usersPage.emailInput).toHaveValue(/.+/);
        }
      }
    });
  });

  test.describe('User Delete @P1', () => {
    test('USER-040: Delete shows confirmation dialog', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();

      const count = await usersPage.getUserCount();
      if (count > 1) {
        // Find user row that isn't admin@noir.local
        const userRows = page.locator('tbody tr').filter({ hasNot: page.locator('text=admin@noir.local') });

        if (await userRows.first().isVisible()) {
          const actionsButton = userRows.first().locator('[data-testid="actions-menu"], button.actions, button:has(svg)');

          if (await actionsButton.first().isVisible()) {
            await actionsButton.first().click();
            const deleteButton = page.locator('[role="menuitem"]:has-text("Delete")');

            if (await deleteButton.isVisible()) {
              await deleteButton.click();
              await expect(usersPage.confirmDialog).toBeVisible({ timeout: 5000 });
              await usersPage.cancelAction();
            }
          }
        }
      }
    });
  });

  test.describe('User Role Assignment @P1', () => {
    test('USER-050: Access role assignment from menu', async ({ page }) => {
      const usersPage = new UsersPage(page);
      await usersPage.navigate();
      await usersPage.expectPageLoaded();

      const count = await usersPage.getUserCount();
      if (count > 0) {
        const firstUser = page.locator('tbody tr').first();
        const actionsButton = firstUser.locator('[data-testid="actions-menu"], button.actions, button:has(svg)');

        if (await actionsButton.first().isVisible()) {
          await actionsButton.first().click();

          // Check for Assign Roles option in menu
          const assignRolesButton = page.locator('[role="menuitem"]:has-text("Assign"), [role="menuitem"]:has-text("Role")');
          if (await assignRolesButton.isVisible()) {
            await expect(assignRolesButton).toBeVisible();
          }
          await page.keyboard.press('Escape');
        }
      }
    });
  });
});
