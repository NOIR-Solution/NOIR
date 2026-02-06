import { test, expect } from '@playwright/test';
import { CommandPalettePage, DashboardPage, Timeouts } from '../pages';
import { ROUTES, ROUTE_PATTERNS } from '../constants/routes';

/**
 * Command Palette E2E Tests
 *
 * Tests for the global command palette (Cmd+K / Ctrl+K).
 * Features tested:
 * - Opening/closing via keyboard and backdrop
 * - Search/filtering functionality
 * - Navigation to pages
 * - Quick actions (theme toggle, create actions)
 * - Keyboard navigation
 * - "Current" page indicator
 */
test.describe('Command Palette @command-palette', () => {
  let dashboard: DashboardPage;

  test.beforeEach(async ({ page }) => {
    dashboard = new DashboardPage(page);
    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();
  });

  test.describe('Opening and Closing @P0', () => {
    test('CMD-001: Open command palette with Ctrl+K', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();
      await cmdPalette.expectPaletteOpen();
    });

    test('CMD-002: Close command palette with Escape', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();
      await cmdPalette.expectPaletteOpen();

      await cmdPalette.closeViaEscape();
      await cmdPalette.expectPaletteClosed();
    });

    test('CMD-003: Close command palette by clicking backdrop', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();
      await cmdPalette.expectPaletteOpen();

      await cmdPalette.closeViaBackdrop();
      await cmdPalette.expectPaletteClosed();
    });

    test('CMD-004: Toggle command palette open and closed', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);

      // Open
      await cmdPalette.open();
      await cmdPalette.expectPaletteOpen();

      // Close
      await cmdPalette.closeViaEscape();
      await cmdPalette.expectPaletteClosed();

      // Open again
      await cmdPalette.open();
      await cmdPalette.expectPaletteOpen();
    });
  });

  test.describe('Search Functionality @P0', () => {
    test('CMD-010: Search input is focused when palette opens', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // The input should be focused
      await expect(cmdPalette.searchInput).toBeFocused({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('CMD-011: Search filters navigation items', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // Search for "Products"
      await cmdPalette.search('Products');

      // Products should be visible
      const productsItem = cmdPalette.getNavigationItem('Products');
      await expect(productsItem).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // Unrelated items like "Users" should be filtered out
      const usersItem = cmdPalette.getNavigationItem('Users');
      await expect(usersItem).not.toBeVisible({ timeout: Timeouts.QUICK_CHECK });
    });

    test('CMD-012: Search shows no results for non-matching query', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      await cmdPalette.search('xyznonexistent');
      await cmdPalette.expectNoResults();
    });

    test('CMD-013: Clear search restores all items', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // Search to filter
      await cmdPalette.search('Products');
      const usersItem = cmdPalette.getNavigationItem('Users');
      await expect(usersItem).not.toBeVisible({ timeout: Timeouts.QUICK_CHECK });

      // Clear search
      await cmdPalette.clearSearch();

      // All items should be visible again
      await expect(usersItem).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('CMD-014: Search by keywords (e.g., "shop" matches Products)', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // "shop" is a keyword for Products
      await cmdPalette.search('shop');

      const productsItem = cmdPalette.getNavigationItem('Products');
      await expect(productsItem).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });
  });

  test.describe('Navigation @P0', () => {
    test('CMD-020: Navigate to Products page via command palette', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();
      await cmdPalette.selectNavigationItem('Products');

      // Palette should close after selection
      await cmdPalette.expectPaletteClosed();

      // Should navigate to products page
      await expect(page).toHaveURL(ROUTE_PATTERNS.PRODUCTS_LIST, { timeout: Timeouts.PAGE_LOAD });
    });

    test('CMD-021: Navigate to Users page via command palette', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();
      await cmdPalette.selectNavigationItem('Users');

      await cmdPalette.expectPaletteClosed();
      await expect(page).toHaveURL(ROUTE_PATTERNS.USERS_LIST, { timeout: Timeouts.PAGE_LOAD });
    });

    test('CMD-022: Navigate to Dashboard shows "Current" badge', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // We're on /portal (Dashboard), so Dashboard should show "Current"
      await cmdPalette.expectCurrentBadge('Dashboard');
    });

    test('CMD-023: Navigate to Blog Posts via command palette', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();
      await cmdPalette.selectNavigationItem('Blog Posts');

      await cmdPalette.expectPaletteClosed();
      await expect(page).toHaveURL(ROUTE_PATTERNS.BLOG_POSTS, { timeout: Timeouts.PAGE_LOAD });
    });
  });

  test.describe('Quick Actions @P1', () => {
    test('CMD-030: Theme toggle action is visible', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // Should show either "Switch to dark mode" or "Switch to light mode"
      const themeAction = page.locator('[cmdk-item]:has-text("Switch to")');
      await expect(themeAction.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('CMD-031: System theme action is visible', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      const systemThemeAction = cmdPalette.getActionItem('Use system theme');
      await expect(systemThemeAction).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('CMD-032: Create new product action navigates to product form', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();
      await cmdPalette.selectActionItem('Create new product');

      await cmdPalette.expectPaletteClosed();
      await expect(page).toHaveURL(ROUTE_PATTERNS.PRODUCTS_NEW, { timeout: Timeouts.PAGE_LOAD });
    });

    test('CMD-033: Create new blog post action navigates to post editor', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();
      await cmdPalette.selectActionItem('Create new blog post');

      await cmdPalette.expectPaletteClosed();
      await expect(page).toHaveURL(ROUTE_PATTERNS.BLOG_POSTS_NEW, { timeout: Timeouts.PAGE_LOAD });
    });

    test('CMD-034: Search for "theme" shows theme actions', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      await cmdPalette.search('theme');

      // Theme-related actions should appear
      const themeItems = page.locator('[cmdk-item]:has-text("theme"), [cmdk-item]:has-text("mode")');
      await expect(themeItems.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });
  });

  test.describe('UI Structure @P1', () => {
    test('CMD-040: Shows navigation and quick actions groups', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // Both groups should be visible
      const navigationHeading = page.locator('[cmdk-group-heading]:has-text("Navigation")');
      const actionsHeading = page.locator('[cmdk-group-heading]:has-text("Quick Actions")');

      await expect(navigationHeading).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      await expect(actionsHeading).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('CMD-041: Shows keyboard hints in footer', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      await expect(cmdPalette.footer).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // Footer should contain navigate, select, close hints
      await expect(cmdPalette.footer.locator('text=navigate')).toBeVisible();
      await expect(cmdPalette.footer.locator('text=select')).toBeVisible();
      await expect(cmdPalette.footer.locator('text=close')).toBeVisible();
    });

    test('CMD-042: Shows Esc keyboard hint', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // Esc hint may be in footer or search area - check either location
      const escHint = page.locator('kbd:has-text("esc"), kbd:has-text("Esc")');
      await expect(escHint.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('CMD-043: Search placeholder text is visible', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // The placeholder should be visible
      await expect(cmdPalette.searchInput).toHaveAttribute('placeholder', /command|search/i);
    });
  });

  test.describe('Keyboard Navigation @P1', () => {
    test('CMD-050: Arrow keys navigate through items', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // Press ArrowDown - first item should be selected
      await page.keyboard.press('ArrowDown');

      // There should be an aria-selected or data-selected item
      const selectedItem = page.locator('[cmdk-item][aria-selected="true"], [cmdk-item][data-selected="true"]');
      await expect(selectedItem.first()).toBeVisible({ timeout: Timeouts.QUICK_CHECK });
    });

    test('CMD-051: Enter selects the highlighted item and navigates', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // Search for a specific page to narrow down
      await cmdPalette.search('Roles');

      // Press Enter on the first result
      await page.keyboard.press('Enter');

      // Should have navigated to Roles page
      await cmdPalette.expectPaletteClosed();
      await expect(page).toHaveURL(ROUTE_PATTERNS.ROLES_LIST, { timeout: Timeouts.PAGE_LOAD });
    });
  });

  test.describe('Route Change Behavior @P1', () => {
    test('CMD-060: Palette closes when selecting a navigation item', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // Navigate via command palette item (clicking sidebar is blocked by backdrop)
      await cmdPalette.selectNavigationItem('Products');

      // Palette should close after navigation
      await cmdPalette.expectPaletteClosed();
      await expect(page).toHaveURL(ROUTE_PATTERNS.PRODUCTS_LIST, { timeout: Timeouts.PAGE_LOAD });
    });

    test('CMD-061: Search is cleared when palette reopens', async ({ page }) => {
      const cmdPalette = new CommandPalettePage(page);
      await cmdPalette.open();

      // Type something
      await cmdPalette.search('test search');

      // Close and reopen
      await cmdPalette.closeViaEscape();
      await cmdPalette.open();

      // Search should be cleared
      await expect(cmdPalette.searchInput).toHaveValue('');
    });
  });
});
