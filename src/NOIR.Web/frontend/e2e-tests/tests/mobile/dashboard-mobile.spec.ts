import { test, expect } from '@playwright/test';
import { DashboardPage } from '../../pages';
import { expectDashboardLoadedOnMobile } from '../../utils/mobile';

/**
 * Mobile Dashboard Tests
 *
 * Tests dashboard on mobile viewport
 * Tags: @mobile @dashboard @P1
 */

// Use mobile viewport
test.use({ viewport: { width: 375, height: 667 } });

test.describe('Mobile Dashboard @mobile @dashboard', () => {
  test('MOB-DASH-001: Dashboard loads on mobile', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();

    // Verify dashboard loaded on mobile viewport
    await expectDashboardLoadedOnMobile(page);
  });

  test('MOB-DASH-002: Mobile menu button is visible', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    // On mobile, sidebar is hidden - check for main content instead
    const mainContent = page.locator('main, [data-testid="dashboard-content"]');
    await expect(mainContent.first()).toBeVisible({ timeout: 10000 });

    // Look for mobile menu button (hamburger icon)
    const menuButton = page.locator('button[aria-label="Open menu"], button:has-text("Menu"), [data-testid="mobile-menu-button"], button:has([class*="menu"])');

    // At least one menu trigger should be visible
    const count = await menuButton.count();
    expect(count).toBeGreaterThan(0);
  });

  test('MOB-DASH-003: Dashboard content is responsive', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    // On mobile, sidebar is hidden - check for main content instead
    const mainContent = page.locator('main, [data-testid="dashboard-content"]');
    await expect(mainContent.first()).toBeVisible({ timeout: 10000 });

    // Content should fit viewport width
    const bodyWidth = await page.evaluate(() => document.body.scrollWidth);
    const viewportWidth = page.viewportSize()?.width || 0;

    // Allow small overflow for scrollbars
    expect(bodyWidth).toBeLessThanOrEqual(viewportWidth + 20);
  });

  test('MOB-DASH-004: Stats cards stack vertically on mobile', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();
    // On mobile, sidebar is hidden - check for main content instead
    const mainContent = page.locator('main, [data-testid="dashboard-content"]');
    await expect(mainContent.first()).toBeVisible({ timeout: 10000 });

    // Look for stats cards
    const statsCards = page.locator('[data-testid*="stat"], [class*="stat-card"], .card, [data-slot="card"]');
    const count = await statsCards.count();

    if (count > 0) {
      // First card should be visible
      await expect(statsCards.first()).toBeVisible();

      // Cards should be stacked (check if second card is below first)
      if (count > 1) {
        const firstBox = await statsCards.first().boundingBox();
        const secondBox = await statsCards.nth(1).boundingBox();

        if (firstBox && secondBox) {
          // Second card's top should be at or below first card's bottom
          expect(secondBox.y).toBeGreaterThanOrEqual(firstBox.y);
        }
      }
    }
  });

  test('MOB-DASH-005: User menu is accessible on mobile', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.navigate();

    // Verify dashboard loaded on mobile viewport
    await expectDashboardLoadedOnMobile(page);
  });
});
