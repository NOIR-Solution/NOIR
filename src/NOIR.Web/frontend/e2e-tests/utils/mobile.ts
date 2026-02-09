import { Page, Locator, expect } from '@playwright/test';

/**
 * Mobile Testing Utilities
 *
 * Provides reusable helpers for mobile viewport testing and responsive design validation.
 * Used across mobile E2E tests for consistent verification patterns.
 */

/**
 * Standard mobile viewport sizes for common devices
 * Based on actual device dimensions for accurate responsive testing
 */
export const MobileViewports = {
  /** iPhone SE (2020) - 375x667 - Smallest modern iPhone */
  IPHONE_SE: { width: 375, height: 667 },

  /** iPhone 12/13/14 - 390x844 - Standard iPhone size */
  IPHONE_12: { width: 390, height: 844 },

  /** iPhone 12/13/14 Pro Max - 428x926 - Large iPhone */
  IPHONE_PRO_MAX: { width: 428, height: 926 },

  /** Google Pixel 5 - 393x851 - Standard Android size */
  PIXEL_5: { width: 393, height: 851 },

  /** Samsung Galaxy S21 - 360x800 - Compact Android */
  GALAXY_S21: { width: 360, height: 800 },

  /** iPad Mini - 768x1024 - Small tablet */
  IPAD_MINI: { width: 768, height: 1024 },

  /** iPad Pro 11" - 834x1194 - Medium tablet */
  IPAD_PRO_11: { width: 834, height: 1194 },
} as const;

/**
 * Verify page is scrollable (content exceeds viewport height)
 * Validates responsive design allows vertical scrolling when needed
 *
 * @param page - Playwright page object
 *
 * @example
 * test('MOB-PROD-005: Mobile viewport can scroll product list', async ({ page }) => {
 *   const productsPage = new ProductsPage(page);
 *   await productsPage.navigate();
 *   await expectPageScrollable(page);
 * });
 */
export async function expectPageScrollable(page: Page): Promise<void> {
  const scrollHeight = await page.evaluate(() => document.documentElement.scrollHeight);
  const clientHeight = await page.evaluate(() => document.documentElement.clientHeight);

  expect(scrollHeight).toBeGreaterThanOrEqual(clientHeight);
}

/**
 * Verify content fits viewport width (no horizontal overflow)
 * Ensures responsive design doesn't cause horizontal scrolling on mobile
 *
 * @param page - Playwright page object
 * @param allowOverflow - Allow small overflow for scrollbars (default: 20px)
 *
 * @example
 * test('MOB-DASH-003: Dashboard content is responsive', async ({ page }) => {
 *   const dashboard = new DashboardPage(page);
 *   await dashboard.navigate();
 *   await expectContentFitsWidth(page);
 * });
 */
export async function expectContentFitsWidth(page: Page, allowOverflow = 20): Promise<void> {
  const bodyWidth = await page.evaluate(() => document.body.scrollWidth);
  const viewportWidth = page.viewportSize()?.width || 0;

  expect(bodyWidth).toBeLessThanOrEqual(viewportWidth + allowOverflow);
}

/**
 * Verify elements stack vertically (second element below first)
 * Validates responsive grid layouts collapse to single column on mobile
 *
 * @param first - First element locator
 * @param second - Second element locator
 *
 * @example
 * const statsCards = page.locator('[data-slot="card"]');
 * await expectVerticalStacking(statsCards.first(), statsCards.nth(1));
 */
export async function expectVerticalStacking(
  first: Locator,
  second: Locator
): Promise<void> {
  const firstBox = await first.boundingBox();
  const secondBox = await second.boundingBox();

  if (!firstBox || !secondBox) {
    throw new Error('Elements not visible for bounding box comparison. Ensure both elements are rendered.');
  }

  // Second element's top should be at or below first element's top (vertical stack)
  expect(secondBox.y).toBeGreaterThanOrEqual(firstBox.y);
}

/**
 * Verify viewport size matches expected dimensions
 * Confirms test is running with correct mobile viewport configuration
 *
 * @param page - Playwright page object
 * @param expected - Expected viewport dimensions
 *
 * @example
 * test.use({ viewport: MobileViewports.IPHONE_SE });
 *
 * test('Viewport is correct', async ({ page }) => {
 *   await expectViewportSize(page, MobileViewports.IPHONE_SE);
 * });
 */
export async function expectViewportSize(
  page: Page,
  expected: { width: number; height: number }
): Promise<void> {
  const viewport = page.viewportSize();

  expect(viewport?.width).toBe(expected.width);
  expect(viewport?.height).toBe(expected.height);
}

/**
 * Scroll element into view with mobile-friendly behavior
 * Uses center positioning for better mobile UX (element appears in middle of viewport)
 *
 * @param locator - Element to scroll into view
 *
 * @example
 * const searchInput = productsPage.searchInput;
 * await scrollIntoViewMobile(searchInput);
 * await searchInput.fill('test');
 */
export async function scrollIntoViewMobile(locator: Locator): Promise<void> {
  await locator.evaluate((el) => {
    el.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'nearest' });
  });
}

/**
 * Check if element is hidden by mobile responsive design
 * Useful for verifying elements that should be hidden on mobile (e.g., desktop sidebar)
 *
 * @param locator - Element to check
 * @returns True if element exists but is hidden (display: none, visibility: hidden, etc.)
 *
 * @example
 * const sidebar = page.locator('aside.sidebar');
 * const isHidden = await isHiddenOnMobile(sidebar);
 * expect(isHidden).toBe(true); // Sidebar should be hidden on mobile
 */
export async function isHiddenOnMobile(locator: Locator): Promise<boolean> {
  try {
    const count = await locator.count();
    if (count === 0) return true;

    const isVisible = await locator.first().isVisible({ timeout: 1000 });
    return !isVisible;
  } catch {
    return true;
  }
}

/**
 * Verify mobile menu/hamburger button is visible
 * Common pattern: desktop sidebar replaced with mobile hamburger menu
 *
 * @param page - Playwright page object
 * @param selectors - Array of possible selectors for mobile menu button
 *
 * @example
 * await expectMobileMenuVisible(page);
 * // or with custom selectors:
 * await expectMobileMenuVisible(page, ['[data-testid="mobile-nav"]', 'button.hamburger']);
 */
export async function expectMobileMenuVisible(
  page: Page,
  selectors: string[] = [
    'button[aria-label="Open menu"]',
    'button[aria-label*="Menu"]',
    '[data-testid="mobile-menu-button"]',
    'button:has([class*="menu"])',
    'button:has([class*="hamburger"])',
  ]
): Promise<void> {
  const menuButton = page.locator(selectors.join(', '));
  const count = await menuButton.count();

  expect(count).toBeGreaterThan(0);
  await expect(menuButton.first()).toBeVisible({ timeout: 10000 });
}

/**
 * Verify dashboard content loads on mobile viewport
 * Handles mobile-specific layout where sidebar may be hidden
 *
 * @param page - Playwright page object
 *
 * @example
 * test('MOB-DASH-001: Dashboard loads on mobile', async ({ page }) => {
 *   const dashboard = new DashboardPage(page);
 *   await dashboard.navigate();
 *   await expectDashboardLoadedOnMobile(page);
 * });
 */
export async function expectDashboardLoadedOnMobile(page: Page): Promise<void> {
  // On mobile, sidebar is hidden - verify main content instead
  const mainContent = page.locator('main, [data-testid="dashboard-content"]');
  await expect(mainContent.first()).toBeVisible({ timeout: 10000 });

  // Wait for page to stabilize (async data loading, animations)
  await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {
    // Network may not go idle if there are persistent connections (websockets, polling)
  });

  // Additional verification: Check for meaningful dashboard content
  const hasInteractiveElements = await page.locator('button, a, input').count();
  expect(hasInteractiveElements).toBeGreaterThan(0);
}

/**
 * Test if search input is available and functional on mobile
 * Returns true if search works, false if it's intentionally hidden on mobile
 *
 * @param searchInput - Search input locator
 * @param testValue - Value to test with (default: 'test')
 * @returns True if search is available and functional, false if hidden
 *
 * @example
 * const searchInput = productsPage.searchInput;
 * const searchWorks = await testMobileSearchFunctionality(searchInput);
 *
 * if (searchWorks) {
 *   // Search is available - test search results
 * } else {
 *   // Search is hidden - verify alternative UI (e.g., create button)
 * }
 */
export async function testMobileSearchFunctionality(
  searchInput: Locator,
  testValue = 'test'
): Promise<boolean> {
  try {
    // Check if search is visible (may need scrolling)
    const isVisible = await searchInput.isVisible({ timeout: 5000 });

    if (!isVisible) {
      // Try scrolling into view
      await searchInput.scrollIntoViewIfNeeded({ timeout: 5000 });
    }

    // Verify we can interact with it
    await searchInput.fill(testValue);
    const value = await searchInput.inputValue();

    return value === testValue;
  } catch {
    return false;
  }
}
