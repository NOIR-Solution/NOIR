import { test, expect } from '@playwright/test';
import { LoginPage, DashboardPage } from '../../pages';
import { VisualDiffThresholds, Viewports, ScreenshotOptions } from './config';
import { expectDashboardLoadedOnMobile } from '../../utils/mobile';

/**
 * Visual Regression Tests
 *
 * Captures screenshots and compares against baselines to detect visual changes.
 * Uses Playwright's built-in toHaveScreenshot() matcher.
 *
 * Setup:
 * 1. First run generates baseline screenshots: npx playwright test visual --update-snapshots
 * 2. Subsequent runs compare against baselines: npx playwright test visual
 *
 * Baselines are stored in: e2e-tests/tests/visual/*.spec.ts-snapshots/
 *
 * Tags: @visual @regression @P2
 */

test.describe('Visual Regression Tests @visual', () => {
  test.beforeEach(async ({ page }) => {
    // Ensure consistent viewport
    await page.setViewportSize(Viewports.DESKTOP);
  });

  test('VIS-001: Dashboard page renders correctly', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.navigate();
    await dashboardPage.expectDashboardLoaded();

    // Wait for any loading states to complete
    await page.waitForLoadState('networkidle');

    // Capture full page screenshot
    await expect(page).toHaveScreenshot('dashboard-full.png', ScreenshotOptions.fullPage());
  });

  test('VIS-002: Login page renders correctly', async ({ page }) => {
    // Disable auth for login page
    const loginPage = new LoginPage(page);
    await loginPage.navigate();

    await page.waitForLoadState('networkidle');

    await expect(page).toHaveScreenshot('login-page.png', {
      animations: 'disabled',
      maxDiffPixels: 50,
    });
  });

  test('VIS-003: Dashboard card components', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.navigate();
    await dashboardPage.expectDashboardLoaded();

    await page.waitForLoadState('networkidle');

    // Capture individual card components
    const cards = await page.locator('[data-slot="card"]').all();

    for (let i = 0; i < Math.min(cards.length, 5); i++) {
      await expect(cards[i]).toHaveScreenshot(`dashboard-card-${i + 1}.png`, {
        animations: 'disabled',
        maxDiffPixels: 30,
      });
    }
  });

  test('VIS-004: Responsive - Mobile viewport', async ({ page }) => {
    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE

    const dashboardPage = new DashboardPage(page);
    await dashboardPage.navigate();

    // On mobile, sidebar is hidden by design - use mobile utility
    await expectDashboardLoadedOnMobile(page);

    await expect(page).toHaveScreenshot('dashboard-mobile.png', {
      fullPage: true,
      animations: 'disabled',
      maxDiffPixels: 100,
    });
  });

  test('VIS-005: Responsive - Tablet viewport', async ({ page }) => {
    // Test tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 }); // iPad

    const dashboardPage = new DashboardPage(page);
    await dashboardPage.navigate();

    // On tablet, sidebar is hidden by design - use mobile utility
    await expectDashboardLoadedOnMobile(page);

    await expect(page).toHaveScreenshot('dashboard-tablet.png', {
      fullPage: true,
      animations: 'disabled',
      maxDiffPixels: 100,
    });
  });

  test('VIS-006: Dark mode vs Light mode', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.navigate();
    await dashboardPage.expectDashboardLoaded();

    await page.waitForLoadState('networkidle');

    // Capture light mode
    await expect(page).toHaveScreenshot('dashboard-light.png', {
      animations: 'disabled',
      maxDiffPixels: 100,
    });

    // Toggle to dark mode (find and click theme toggle)
    const themeToggle = page.locator('[aria-label*="theme" i], [aria-label*="dark mode" i]').first();
    if (await themeToggle.isVisible()) {
      await themeToggle.click();
      await page.waitForTimeout(500); // Wait for theme transition

      // Capture dark mode
      await expect(page).toHaveScreenshot('dashboard-dark.png', {
        animations: 'disabled',
        maxDiffPixels: 100,
      });
    }
  });

  test('VIS-007: Navigation sidebar', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.navigate();
    await dashboardPage.expectDashboardLoaded();

    await page.waitForLoadState('networkidle');

    // Capture sidebar
    const sidebar = page.locator('[role="navigation"], aside, nav').first();
    if (await sidebar.isVisible()) {
      await expect(sidebar).toHaveScreenshot('sidebar.png', {
        animations: 'disabled',
        maxDiffPixels: 50,
      });
    }
  });

  test('VIS-008: Form components', async ({ page }) => {
    // Navigate to a page with forms (e.g., settings)
    await page.goto('/portal/settings');
    await page.waitForLoadState('networkidle');

    // Capture form inputs
    const inputs = await page.locator('input[type="text"], input[type="email"], textarea').all();

    for (let i = 0; i < Math.min(inputs.length, 3); i++) {
      if (await inputs[i].isVisible()) {
        await expect(inputs[i]).toHaveScreenshot(`form-input-${i + 1}.png`, {
          animations: 'disabled',
          maxDiffPixels: 20,
        });
      }
    }
  });

  test('VIS-009: Button components', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.navigate();
    await dashboardPage.expectDashboardLoaded();

    await page.waitForLoadState('networkidle');

    // Capture button variations
    const buttons = await page.locator('button').all();

    for (let i = 0; i < Math.min(buttons.length, 5); i++) {
      if (await buttons[i].isVisible()) {
        await expect(buttons[i]).toHaveScreenshot(`button-${i + 1}.png`, {
          animations: 'disabled',
          maxDiffPixels: 20,
        });
      }
    }
  });

  test('VIS-010: Modal dialogs', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.navigate();
    await dashboardPage.expectDashboardLoaded();

    await page.waitForLoadState('networkidle');

    // Try to open a modal (look for buttons that might open modals)
    const modalTriggers = await page.locator('button:has-text("Create"), button:has-text("Add"), button:has-text("New")').all();

    if (modalTriggers.length > 0) {
      await modalTriggers[0].click();
      await page.waitForTimeout(500); // Wait for modal animation

      // Capture modal
      const modal = page.locator('[role="dialog"], [role="alertdialog"]').first();
      if (await modal.isVisible()) {
        await expect(modal).toHaveScreenshot('modal-dialog.png', {
          animations: 'disabled',
          maxDiffPixels: 50,
        });
      }
    }
  });
});

/**
 * Visual Regression - Critical Pages
 *
 * Tests visual consistency of important pages across the application.
 */
test.describe('Visual Regression - Critical Pages @visual', () => {
  const criticalPages = [
    { path: '/portal/dashboard', name: 'dashboard' },
    { path: '/portal/settings', name: 'settings' },
    { path: '/portal/notifications', name: 'notifications' },
  ];

  criticalPages.forEach(({ path, name }) => {
    test(`VIS-CRITICAL-${name}: ${name} page visual consistency`, async ({ page }) => {
      await page.setViewportSize({ width: 1280, height: 720 });
      await page.goto(path);
      await page.waitForLoadState('networkidle');

      await expect(page).toHaveScreenshot(`critical-${name}.png`, {
        fullPage: true,
        animations: 'disabled',
        maxDiffPixels: 150,
      });
    });
  });
});
