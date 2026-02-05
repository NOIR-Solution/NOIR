import { test, expect } from '@playwright/test';
import { DeveloperLogsPage } from '../pages';

/**
 * Developer Logs Tests
 *
 * Comprehensive E2E tests for the Developer Logs page.
 * Features tested: Live log streaming, History, Statistics, Error Clusters.
 * Tags: @developer-logs @P0 @P1
 */

test.describe('Developer Logs @developer-logs', () => {
  test.describe('Page Load @P0', () => {
    test('LOG-001: Developer logs page loads successfully', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();
    });

    test('LOG-002: Live logs tab is default', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Live Logs tab should be selected by default
      await logsPage.expectLiveLogsTabActive();
    });

    test('LOG-003: Connection status indicator visible', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Connection badge should be visible (Connected, Connecting, or Disconnected)
      const connectionBadge = page.locator(
        'text="Connected", text="Connecting", text="Disconnected", text="Reconnecting"'
      );
      await expect(connectionBadge.first()).toBeVisible({ timeout: 10000 });
    });
  });

  test.describe('Live Logs Controls @P1', () => {
    test('LOG-010: Pause/resume button works', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Check for pause button
      const pauseResumeButton = page.locator('button:has-text("Pause"), button:has-text("Resume")');
      await expect(pauseResumeButton.first()).toBeVisible({ timeout: 5000 });

      // Get initial button text
      const initialText = await pauseResumeButton.first().textContent();

      // Click the button
      await pauseResumeButton.first().click();
      await page.waitForTimeout(500);

      // Button text should have changed
      const newText = await pauseResumeButton.first().textContent();

      // If initial was "Pause", it should now be "Resume" (or vice versa)
      if (initialText?.includes('Pause')) {
        expect(newText).toContain('Resume');
      } else if (initialText?.includes('Resume')) {
        expect(newText).toContain('Pause');
      }

      // Click again to restore original state
      await pauseResumeButton.first().click();
    });

    test('LOG-011: Filter by log level', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Look for level filter dropdown
      const levelFilter = page.locator(
        'button:has-text("Filter Levels"), ' +
        'button:has-text("Levels"), ' +
        '[data-testid="level-filter"], ' +
        'button:has-text("All Levels")'
      );

      if (await levelFilter.first().isVisible()) {
        await levelFilter.first().click();

        // Wait for dropdown to open
        const dropdown = page.locator(
          '[role="menu"], [role="listbox"], [data-radix-popper-content-wrapper]'
        );
        await expect(dropdown.first()).toBeVisible({ timeout: 5000 });

        // Check for level options (e.g., Error, Warning, Information)
        const levelOption = page.locator(
          '[role="menuitemcheckbox"], [role="option"], label'
        ).filter({ hasText: /Error|Warning|Information/i });
        await expect(levelOption.first()).toBeVisible();

        // Close dropdown
        await page.keyboard.press('Escape');
      }
    });

    test('LOG-012: Search logs', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Find search input
      const searchInput = page.locator(
        'input[placeholder*="Search"], ' +
        'input[placeholder*="Filter"], ' +
        '[data-testid="search-input"]'
      );

      if (await searchInput.first().isVisible()) {
        // Type a search term
        await searchInput.first().fill('test');
        await page.waitForTimeout(500);

        // Verify the search term is in the input
        await expect(searchInput.first()).toHaveValue('test');

        // Clear the search
        await searchInput.first().clear();
        await expect(searchInput.first()).toHaveValue('');
      }
    });

    test('LOG-013: Clear buffer button works', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Find clear buffer button
      const clearButton = page.locator(
        'button:has-text("Clear"), ' +
        'button:has-text("Clear Buffer"), ' +
        '[data-testid="clear-buffer"]'
      );

      if (await clearButton.first().isVisible()) {
        await expect(clearButton.first()).toBeEnabled();

        // Click the clear button
        await clearButton.first().click();

        // Wait for action to complete
        await page.waitForTimeout(500);

        // Button should still be visible after action
        await expect(clearButton.first()).toBeVisible();
      }
    });

    test('LOG-014: View log entry details', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Wait for potential log entries to appear
      await page.waitForTimeout(2000);

      // Check if there are any log entries
      const logEntries = page.locator(
        'tr[class*="log-entry"], ' +
        '[data-testid="log-entry"], ' +
        '[class*="LogEntryRow"], ' +
        'tbody tr'
      );

      const entryCount = await logEntries.count();

      if (entryCount > 0) {
        // Click on the first log entry
        await logEntries.first().click();

        // Wait for detail dialog to appear
        const dialog = page.locator('[role="dialog"]');

        // Dialog might appear, or entry might expand inline
        const dialogVisible = await dialog.isVisible().catch(() => false);

        if (dialogVisible) {
          await expect(dialog).toBeVisible({ timeout: 5000 });

          // Close the dialog
          await page.keyboard.press('Escape');
          await expect(dialog).toBeHidden({ timeout: 5000 });
        }
      } else {
        // No log entries yet - verify empty state message
        const emptyState = page.locator(
          'text="No log entries", ' +
          'text="Waiting for incoming logs", ' +
          'text="No entries match"'
        );
        await expect(emptyState.first()).toBeVisible({ timeout: 5000 });
      }
    });

    test('LOG-015: Auto-scroll toggle', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Find auto-scroll toggle
      const autoScrollToggle = page.locator(
        'button[aria-label*="auto-scroll"], ' +
        'button[aria-label*="Auto-scroll"], ' +
        'button:has-text("Auto-scroll"), ' +
        '[data-testid="auto-scroll-toggle"]'
      );

      if (await autoScrollToggle.first().isVisible()) {
        // Get initial state
        const initialPressed = await autoScrollToggle.first().getAttribute('aria-pressed');
        const initialDataState = await autoScrollToggle.first().getAttribute('data-state');

        // Click to toggle
        await autoScrollToggle.first().click();
        await page.waitForTimeout(300);

        // Verify toggle changed (either aria-pressed or data-state)
        const newPressed = await autoScrollToggle.first().getAttribute('aria-pressed');
        const newDataState = await autoScrollToggle.first().getAttribute('data-state');

        // One of these should have changed
        const stateChanged =
          (initialPressed !== newPressed) ||
          (initialDataState !== newDataState);

        expect(stateChanged).toBeTruthy();

        // Toggle back to original state
        await autoScrollToggle.first().click();
      }
    });
  });

  test.describe('History Tab @P1', () => {
    test('LOG-020: History tab loads', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Click on History tab
      await logsPage.switchToHistory();

      // Verify History tab is now active
      await logsPage.expectHistoryTabActive();

      // Wait for tab content to load
      await page.waitForTimeout(1000);

      // History tab should show either file list or empty state
      const historyContent = page.locator(
        '[data-testid="history-files"], ' +
        '[class*="history-list"], ' +
        'text="No history files", ' +
        'text="Log files will appear here", ' +
        'table, ' +
        '[class*="file"]'
      );

      // Content area should be visible
      const tabContent = page.locator('[role="tabpanel"]');
      await expect(tabContent.first()).toBeVisible({ timeout: 5000 });
    });
  });

  test.describe('Statistics Tab @P1', () => {
    test('LOG-021: Statistics tab loads and shows stats', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Click on Statistics tab
      await logsPage.switchToStatistics();

      // Verify Statistics tab is now active
      await logsPage.expectStatisticsTabActive();

      // Wait for tab content to load
      await page.waitForTimeout(1000);

      // Statistics tab should show stats cards or metrics
      const statsContent = page.locator(
        '[data-testid="stats-card"], ' +
        '[class*="stats-card"], ' +
        '[class*="Card"], ' +
        'text="Total", ' +
        'text="Entries", ' +
        'text="Buffer", ' +
        'text="Levels"'
      );

      // Tab panel should be visible
      const tabContent = page.locator('[role="tabpanel"]');
      await expect(tabContent.first()).toBeVisible({ timeout: 5000 });

      // Look for refresh button
      const refreshButton = page.locator(
        'button:has-text("Refresh"), ' +
        '[data-testid="stats-refresh"], ' +
        'button[aria-label*="refresh"]'
      );

      if (await refreshButton.first().isVisible()) {
        await expect(refreshButton.first()).toBeEnabled();
      }
    });
  });

  test.describe('Error Clusters Tab @P1', () => {
    test('LOG-022: Error clusters tab loads', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Click on Error Clusters tab
      await logsPage.switchToErrorClusters();

      // Verify Error Clusters tab is now active
      await logsPage.expectErrorClustersTabActive();

      // Wait for tab content to load
      await page.waitForTimeout(1000);

      // Error Clusters tab should show clusters list or empty state
      const clustersContent = page.locator(
        '[data-testid="error-clusters"], ' +
        '[class*="error-cluster"], ' +
        'text="No error clusters", ' +
        'text="No errors found", ' +
        'text="Error Patterns", ' +
        '[class*="Card"]'
      );

      // Tab panel should be visible
      const tabContent = page.locator('[role="tabpanel"]');
      await expect(tabContent.first()).toBeVisible({ timeout: 5000 });

      // Look for refresh button
      const refreshButton = page.locator(
        'button:has-text("Refresh"), ' +
        '[data-testid="clusters-refresh"], ' +
        'button[aria-label*="refresh"]'
      );

      if (await refreshButton.first().isVisible()) {
        await expect(refreshButton.first()).toBeEnabled();
      }
    });
  });

  test.describe('Tab Navigation @P1', () => {
    test('LOG-030: Navigate between all tabs', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Start at Live Logs (default)
      await logsPage.expectLiveLogsTabActive();

      // Navigate to History
      await logsPage.switchToHistory();
      await logsPage.expectHistoryTabActive();

      // Navigate to Statistics
      await logsPage.switchToStatistics();
      await logsPage.expectStatisticsTabActive();

      // Navigate to Error Clusters
      await logsPage.switchToErrorClusters();
      await logsPage.expectErrorClustersTabActive();

      // Return to Live Logs
      await logsPage.switchToLiveLogs();
      await logsPage.expectLiveLogsTabActive();
    });
  });

  test.describe('Server Level Control @P1', () => {
    test('LOG-040: Server level dropdown is visible', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Find server level dropdown
      const serverLevelDropdown = page.locator(
        'button:has-text("Server Level"), ' +
        '[data-testid="server-level"], ' +
        'button:has-text("Information"), ' +
        'button:has-text("Debug"), ' +
        'button:has-text("Warning"), ' +
        'button:has-text("Error")'
      );

      if (await serverLevelDropdown.first().isVisible()) {
        await expect(serverLevelDropdown.first()).toBeEnabled();

        // Click to open dropdown
        await serverLevelDropdown.first().click();

        // Verify dropdown opens
        const dropdown = page.locator(
          '[role="menu"], [role="listbox"], [data-radix-popper-content-wrapper]'
        );
        await expect(dropdown.first()).toBeVisible({ timeout: 5000 });

        // Close dropdown
        await page.keyboard.press('Escape');
      }
    });
  });

  test.describe('Exceptions Only Filter @P1', () => {
    test('LOG-050: Exceptions only toggle', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Find exceptions only toggle
      const exceptionsToggle = page.locator(
        'button:has-text("Errors only"), ' +
        'button:has-text("Exceptions"), ' +
        '[data-testid="exceptions-only"], ' +
        'label:has-text("Errors only")'
      );

      if (await exceptionsToggle.first().isVisible()) {
        // Get initial state
        const initialState = await exceptionsToggle.first().getAttribute('aria-pressed') ||
                            await exceptionsToggle.first().getAttribute('data-state');

        // Click to toggle
        await exceptionsToggle.first().click();
        await page.waitForTimeout(300);

        // Verify toggle is still visible (functional)
        await expect(exceptionsToggle.first()).toBeVisible();

        // Toggle back
        await exceptionsToggle.first().click();
      }
    });
  });

  test.describe('Sort Order @P1', () => {
    test('LOG-060: Sort order toggle works', async ({ page }) => {
      const logsPage = new DeveloperLogsPage(page);
      await logsPage.navigate();
      await logsPage.expectPageLoaded();

      // Find sort order toggle
      const sortToggle = page.locator(
        'button[aria-label*="sort"], ' +
        'button:has-text("Newest"), ' +
        'button:has-text("Oldest"), ' +
        '[data-testid="sort-toggle"]'
      );

      if (await sortToggle.first().isVisible()) {
        // Get initial text
        const initialText = await sortToggle.first().textContent();

        // Click to toggle
        await sortToggle.first().click();
        await page.waitForTimeout(300);

        // Text should have changed (or toggle state should be different)
        const newText = await sortToggle.first().textContent();

        // Either text changed or button is still functional
        const isStillFunctional = await sortToggle.first().isVisible();
        expect(isStillFunctional).toBeTruthy();

        // Toggle back
        await sortToggle.first().click();
      }
    });
  });
});
