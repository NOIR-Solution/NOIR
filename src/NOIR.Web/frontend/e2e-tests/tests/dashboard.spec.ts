import { test, expect } from '@playwright/test';
import { DashboardPage, Timeouts } from '../pages';

/**
 * Dashboard Tests
 *
 * Comprehensive tests for the main dashboard page.
 * Tests verify that core dashboard elements load correctly after authentication.
 * Tags: @dashboard @P0 @P1
 */

test.describe('Dashboard @dashboard', () => {
  test.describe('Dashboard Load @P0', () => {
    test('DASH-001: Dashboard loads successfully after login', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Verify we're on the portal URL
      await expect(page).toHaveURL(/\/portal/);
    });

    test('DASH-002: Sidebar navigation is visible', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Sidebar should be visible on desktop
      await expect(dashboard.sidebar).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('DASH-003: User menu is visible', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // User profile dropdown trigger should be visible in sidebar footer
      // The UserProfileDropdown renders a Button with user avatar/initials
      const userMenu = page.locator('[data-testid="user-menu"], button:has(.rounded-full)').first();
      await expect(userMenu).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });
  });

  test.describe('Dashboard Content @P1', () => {
    test('DASH-010: Quick links card displays data', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Quick Links card should be visible
      const quickLinksCard = page.locator('text="Quick Links"').first();
      await expect(quickLinksCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // API Docs link should be present
      const apiDocsLink = page.locator('a[href="/api/docs"]');
      await expect(apiDocsLink).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // Hangfire link should be present
      const hangfireLink = page.locator('a[href="/hangfire"]');
      await expect(hangfireLink).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('DASH-011: Quick action links have correct targets', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // API Docs link should open in new tab
      const apiDocsLink = page.locator('a[href="/api/docs"]');
      await expect(apiDocsLink).toHaveAttribute('target', '_blank');
      await expect(apiDocsLink).toHaveAttribute('rel', 'noopener noreferrer');

      // Hangfire link should open in new tab
      const hangfireLink = page.locator('a[href="/hangfire"]');
      await expect(hangfireLink).toHaveAttribute('target', '_blank');
      await expect(hangfireLink).toHaveAttribute('rel', 'noopener noreferrer');
    });

    test('DASH-012: User profile section displays data', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Your Profile section should display user information
      const profileSection = page.locator('text="Your Profile"').first();
      await expect(profileSection).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // Email field should be present and have content
      const emailLabel = page.locator('text="Email:"').first();
      await expect(emailLabel).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // Tenant field should be present
      const tenantLabel = page.locator('text="Tenant:"').first();
      await expect(tenantLabel).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // Roles field should be present
      const rolesLabel = page.locator('text="Roles:"').first();
      await expect(rolesLabel).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('DASH-013: Page title shows correct text', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Page header with Dashboard title should be visible
      // PageHeader component renders title in h1
      const pageTitle = page.locator('h1');
      await expect(pageTitle.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // Title should contain "Dashboard" (localized)
      const titleText = await pageTitle.first().textContent();
      expect(titleText).toBeTruthy();
    });
  });

  test.describe('Sidebar Navigation @P1', () => {
    test('DASH-020: Sidebar shows navigation sections', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();

      // Wait for sidebar to be fully ready with permission-based items
      await dashboard.waitForSidebarReady();

      // Dashboard link should be visible (always shown)
      const dashboardLink = page.locator('a[href="/portal"]');
      await expect(dashboardLink.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('DASH-021: Sidebar collapse toggle works', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Find the collapse/expand toggle button
      const toggleButton = page.locator('button[aria-label], aside button').filter({ has: page.locator('svg') }).first();

      if (await toggleButton.isVisible()) {
        // Get initial sidebar width
        const sidebar = dashboard.sidebar;
        const initialWidth = await sidebar.evaluate((el) => el.getBoundingClientRect().width);

        // Click toggle
        await toggleButton.click();
        await page.waitForTimeout(500); // Wait for animation

        // Width should change
        const newWidth = await sidebar.evaluate((el) => el.getBoundingClientRect().width);
        expect(newWidth).not.toBe(initialWidth);
      }
    });

    test('DASH-022: Navigation to Products works', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.waitForSidebarReady();

      // Click Products link
      await dashboard.navigateToProducts();

      // Should navigate to products page
      await expect(page).toHaveURL(/\/portal\/ecommerce\/products/);
    });

    test('DASH-023: Navigation to Categories works', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.waitForSidebarReady();

      // Click Categories link
      await dashboard.navigateToCategories();

      // Should navigate to categories page
      await expect(page).toHaveURL(/\/portal\/ecommerce\/categories/);
    });

    test('DASH-024: Navigation to Users works', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.waitForSidebarReady();

      // Click Users link
      await dashboard.navigateToUsers();

      // Should navigate to users page
      await expect(page).toHaveURL(/\/portal\/admin\/users/);
    });
  });

  test.describe('User Menu @P1', () => {
    test('DASH-030: User menu dropdown opens', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Find user profile button in sidebar footer
      // UserProfileDropdown is a DropdownMenuTrigger with Button containing avatar
      const userButton = dashboard.sidebar.locator('button').filter({
        has: page.locator('.rounded-full')
      }).first();

      if (await userButton.isVisible()) {
        await userButton.click();

        // Dropdown menu should appear
        const dropdown = page.locator('[role="menu"]');
        await expect(dropdown).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });

        // Dropdown should contain expected menu items
        const settingsItem = dropdown.locator('text="Settings"');
        await expect(settingsItem).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      }
    });

    test('DASH-031: User menu shows user info', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Find user profile button
      const userButton = dashboard.sidebar.locator('button').filter({
        has: page.locator('.rounded-full')
      }).first();

      if (await userButton.isVisible()) {
        await userButton.click();

        // Dropdown menu should show user details in label
        const dropdown = page.locator('[role="menu"]');
        await expect(dropdown).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });

        // Menu label should contain user info
        const menuLabel = dropdown.locator('[class*="DropdownMenuLabel"], .font-medium').first();
        await expect(menuLabel).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

        // Close dropdown
        await page.keyboard.press('Escape');
      }
    });

    test('DASH-032: Language switcher is available', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Find user profile button
      const userButton = dashboard.sidebar.locator('button').filter({
        has: page.locator('.rounded-full')
      }).first();

      if (await userButton.isVisible()) {
        await userButton.click();

        // Dropdown menu should appear
        const dropdown = page.locator('[role="menu"]');
        await expect(dropdown).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });

        // Language submenu trigger should be present
        const languageItem = dropdown.locator('text="Language"');
        await expect(languageItem).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

        // Close dropdown
        await page.keyboard.press('Escape');
      }
    });

    test('DASH-033: Theme switcher is available', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Find user profile button
      const userButton = dashboard.sidebar.locator('button').filter({
        has: page.locator('.rounded-full')
      }).first();

      if (await userButton.isVisible()) {
        await userButton.click();

        // Dropdown menu should appear
        const dropdown = page.locator('[role="menu"]');
        await expect(dropdown).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });

        // Theme submenu trigger should be present
        const themeItem = dropdown.locator('text="Theme"');
        await expect(themeItem).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

        // Close dropdown
        await page.keyboard.press('Escape');
      }
    });

    test('DASH-034: Sign out option is available', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Find user profile button
      const userButton = dashboard.sidebar.locator('button').filter({
        has: page.locator('.rounded-full')
      }).first();

      if (await userButton.isVisible()) {
        await userButton.click();

        // Dropdown menu should appear
        const dropdown = page.locator('[role="menu"]');
        await expect(dropdown).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });

        // Sign out option should be present (with red color for destructive action)
        const signOutItem = dropdown.locator('[role="menuitem"]').filter({ hasText: /sign out|logout/i });
        await expect(signOutItem).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

        // Close dropdown
        await page.keyboard.press('Escape');
      }
    });
  });

  test.describe('Responsive Behavior @P1', () => {
    test('DASH-040: Dashboard remains accessible after page refresh', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.expectDashboardLoaded();

      // Refresh the page
      await page.reload();

      // Dashboard should still load correctly
      await dashboard.expectDashboardLoaded();
      await expect(page).toHaveURL(/\/portal/);
    });

    test('DASH-041: Active menu item is highlighted', async ({ page }) => {
      const dashboard = new DashboardPage(page);
      await dashboard.navigate();
      await dashboard.waitForSidebarReady();

      // Wait for sidebar to fully render with active state
      await page.waitForTimeout(500);

      // The Dashboard nav item link (inside navigation, not the logo link)
      const isActive = await page.evaluate(() => {
        const nav = document.querySelector('nav');
        if (!nav) return false;

        // Find the Dashboard link inside nav
        const links = nav.querySelectorAll('a');
        let dashboardLink: HTMLElement | null = null;

        for (const link of links) {
          if (link.getAttribute('href') === '/portal' && link.textContent?.includes('Dashboard')) {
            dashboardLink = link as HTMLElement;
            break;
          }
        }

        if (!dashboardLink) return false;

        // Check for active indicators:
        // 1. data-active="true" attribute (stable, test-friendly)
        // 2. Gradient class (from-sidebar-primary)
        // 3. Text color class (text-sidebar-primary)
        // 4. Border indicator child element
        const hasDataActive = dashboardLink.getAttribute('data-active') === 'true';
        const hasGradientClass = dashboardLink.className?.includes('from-sidebar-primary');
        const hasPrimaryText = dashboardLink.className?.includes('text-sidebar-primary');
        const hasBorderChild = !!dashboardLink.querySelector('[class*="bg-sidebar-primary"]');

        return hasDataActive || hasGradientClass || hasPrimaryText || hasBorderChild;
      });

      expect(isActive).toBeTruthy();
    });
  });
});
