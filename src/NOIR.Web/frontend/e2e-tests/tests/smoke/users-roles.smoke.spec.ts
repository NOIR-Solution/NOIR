import { test, expect } from '@playwright/test';
import { UsersPage, RolesPage } from '../../pages';

/**
 * Users & Roles Smoke Tests
 *
 * @smoke @users @roles @P0
 *
 * Critical user and role management flows:
 * - View users list
 * - View roles list
 * - Create dialog accessibility
 */

test.describe('Users @smoke @users @P0', () => {
  // storageState from auth.setup.ts handles authentication

  test('should display users list page', async ({ page }) => {
    const usersPage = new UsersPage(page);

    await usersPage.navigate();
    await usersPage.expectPageLoaded();

    // Verify create button and user table are visible
    await expect(usersPage.createButton).toBeVisible();
    await expect(usersPage.userTable).toBeVisible();
  });

  test('should open create user dialog', async ({ page }) => {
    const usersPage = new UsersPage(page);

    await usersPage.navigate();
    await usersPage.expectPageLoaded();
    await usersPage.openCreateDialog();

    // Verify dialog is visible with required fields
    await expect(usersPage.dialog).toBeVisible();
    await expect(usersPage.emailInput).toBeVisible();
    await expect(usersPage.passwordInput).toBeVisible();
  });

  test('should search for users', async ({ page }) => {
    const usersPage = new UsersPage(page);

    await usersPage.navigate();
    await usersPage.expectPageLoaded();

    // Enter search query
    await usersPage.search('admin');

    // Wait for page to update after search
    await usersPage.waitForPageLoad();

    // Verify admin user is in results (use main content area, not sidebar)
    const adminUser = page.locator('#main-content').getByText('admin@noir.local');
    await expect(adminUser).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Roles @smoke @roles @P0', () => {
  // storageState from auth.setup.ts handles authentication

  test('should display roles list page', async ({ page }) => {
    const rolesPage = new RolesPage(page);

    await rolesPage.navigate();
    await rolesPage.expectPageLoaded();

    // Verify create button and role table are visible
    await expect(rolesPage.createButton).toBeVisible();
    await expect(rolesPage.roleTable).toBeVisible();
  });

  test('should open create role dialog', async ({ page }) => {
    const rolesPage = new RolesPage(page);

    await rolesPage.navigate();
    await rolesPage.expectPageLoaded();
    await rolesPage.openCreateDialog();

    // Verify dialog is visible with required fields
    await expect(rolesPage.dialog).toBeVisible();
    await expect(rolesPage.nameInput).toBeVisible();
  });

  test('should display default roles', async ({ page }) => {
    const rolesPage = new RolesPage(page);

    await rolesPage.navigate();

    // Verify at least Admin role exists
    const adminRole = page.locator('text=Admin');
    await expect(adminRole.first()).toBeVisible({ timeout: 10000 });
  });
});
