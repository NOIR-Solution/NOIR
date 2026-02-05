import { test, expect } from '@playwright/test';
import { DashboardPage } from '../../pages';

/**
 * Navigation Smoke Tests
 *
 * @smoke @navigation @P0
 *
 * Critical navigation flows:
 * - Dashboard loads correctly
 * - Sidebar navigation works
 * - Key pages are accessible
 *
 * Routes (from Sidebar.tsx):
 * - Dashboard: /portal
 * - Products: /portal/ecommerce/products
 * - Categories: /portal/ecommerce/categories
 * - Brands: /portal/ecommerce/brands
 * - Attributes: /portal/ecommerce/attributes
 * - Users: /portal/admin/users
 * - Roles: /portal/admin/roles
 */

test.describe('Navigation @smoke @navigation @P0', () => {
  test.beforeEach(async ({ page }) => {
    // Tests use pre-authenticated state from auth.setup.ts
  });

  test('should load dashboard successfully', async ({ page }) => {
    const dashboard = new DashboardPage(page);

    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();

    // Verify sidebar is visible
    await expect(dashboard.sidebar).toBeVisible();
  });

  test('should navigate to Products page via sidebar', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();

    // Wait for specific link to be visible
    const productsLink = page.locator('a[href="/portal/ecommerce/products"]').first();
    await expect(productsLink).toBeVisible({ timeout: 30000 });
    await productsLink.click();

    // Verify navigation
    await expect(page).toHaveURL(/\/portal\/ecommerce\/products/, { timeout: 10000 });
  });

  test('should navigate to Categories page via sidebar', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();

    // Wait for specific link to be visible
    const categoriesLink = page.locator('a[href="/portal/ecommerce/categories"]').first();
    await expect(categoriesLink).toBeVisible({ timeout: 30000 });
    await categoriesLink.click();

    // Verify navigation
    await expect(page).toHaveURL(/\/portal\/ecommerce\/categories/, { timeout: 10000 });
  });

  test('should navigate to Brands page via sidebar', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();

    // Wait for specific link to be visible
    const brandsLink = page.locator('a[href="/portal/ecommerce/brands"]').first();
    await expect(brandsLink).toBeVisible({ timeout: 30000 });
    await brandsLink.click();

    // Verify navigation
    await expect(page).toHaveURL(/\/portal\/ecommerce\/brands/, { timeout: 10000 });
  });

  test('should navigate to Users page via sidebar', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();

    // Wait for specific link to be visible
    const usersLink = page.locator('a[href="/portal/admin/users"]').first();
    await expect(usersLink).toBeVisible({ timeout: 30000 });
    await usersLink.click();

    // Verify navigation
    await expect(page).toHaveURL(/\/portal\/admin\/users/, { timeout: 10000 });
  });

  test('should navigate to Roles page via sidebar', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();

    // Wait for specific link to be visible
    const rolesLink = page.locator('a[href="/portal/admin/roles"]').first();
    await expect(rolesLink).toBeVisible({ timeout: 30000 });
    await rolesLink.click();

    // Verify navigation
    await expect(page).toHaveURL(/\/portal\/admin\/roles/, { timeout: 10000 });
  });

  test('should navigate to Tenant Settings page', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();

    // Wait for specific link to be visible
    const settingsLink = page.locator('a[href="/portal/admin/tenant-settings"]').first();
    await expect(settingsLink).toBeVisible({ timeout: 30000 });
    await settingsLink.click();

    // Verify navigation
    await expect(page).toHaveURL(/\/portal\/admin\/tenant-settings/, { timeout: 10000 });
  });
});
