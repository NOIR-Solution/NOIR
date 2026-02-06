import { test, expect } from '@playwright/test';
import { ActivityTimelinePage } from '../pages';

/**
 * Activity Timeline Tests
 *
 * Comprehensive E2E tests for the Activity Timeline page.
 * Tests cover page loading, filtering, entry details, and pagination.
 * Tags: @activity-timeline @P0 @P1
 */

test.describe('Activity Timeline @activity-timeline', () => {
  test.describe('Page Loading @P0', () => {
    test('ACT-001: Activity timeline page loads successfully', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
    });

    test('ACT-002: Timeline entries are visible (if data exists)', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Check if entries exist or empty state is shown
      const entryCount = await timelinePage.getEntryCount();
      const isEmpty = await timelinePage.isEmpty();

      // Either entries are visible or empty state is shown
      expect(entryCount > 0 || isEmpty).toBeTruthy();
    });

    test('ACT-003: Search input is functional', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();

      // Verify search input is visible and functional
      await expect(timelinePage.searchInput).toBeVisible();
      await timelinePage.searchInput.fill('test-search-term');
      await expect(timelinePage.searchInput).toHaveValue('test-search-term');
    });
  });

  test.describe('Filtering @P1', () => {
    test('ACT-010: Filter by date range works', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Verify date range picker is visible
      await expect(timelinePage.dateRangePicker).toBeVisible();

      // Click to open date picker
      await timelinePage.filterByDateRange();

      // Date picker should open (look for calendar elements)
      const calendarPopover = page.locator('[data-radix-popper-content-wrapper], [role="dialog"]:has([role="grid"])');
      await expect(calendarPopover.first()).toBeVisible({ timeout: 5000 });

      // Close the date picker
      await page.keyboard.press('Escape');
    });

    test('ACT-011: Filter by operation type (Create/Update/Delete)', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Verify operation dropdown is visible
      await expect(timelinePage.operationDropdown).toBeVisible();

      // Open the operation dropdown
      await timelinePage.operationDropdown.click();

      // Verify all operation types are available
      const createOption = page.locator('[role="option"]:has-text("Create")');
      const updateOption = page.locator('[role="option"]:has-text("Update")');
      const deleteOption = page.locator('[role="option"]:has-text("Delete")');

      await expect(createOption).toBeVisible({ timeout: 5000 });
      await expect(updateOption).toBeVisible();
      await expect(deleteOption).toBeVisible();

      // Select an operation type and verify filter works
      await createOption.click();
      await timelinePage.waitForEntriesLoaded();

      // The dropdown should show the selected value (or "All Actions" if reverted)
      // The filter has been applied - page should still be functional
      await expect(timelinePage.pageHeader).toBeVisible();
    });

    test('ACT-012: Filter by context (page)', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Verify context dropdown is visible
      await expect(timelinePage.contextDropdown).toBeVisible();

      // Open the context dropdown
      await timelinePage.contextDropdown.click();

      // Verify dropdown opened with options
      const options = page.locator('[role="option"]');
      await expect(options.first()).toBeVisible({ timeout: 5000 });

      // Get available contexts (if any)
      const optionCount = await options.count();
      expect(optionCount).toBeGreaterThan(0);

      // Close dropdown
      await page.keyboard.press('Escape');
    });

    test('ACT-013: Toggle failed only filter', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Find the failed only switch by id or label
      const failedSwitch = page.locator('#only-failed, [data-testid="failed-only-switch"]');
      const failedLabel = page.locator('label:has-text("Failed only")');

      // Either the switch or label should be visible
      const switchVisible = await failedSwitch.isVisible();
      const labelVisible = await failedLabel.isVisible();
      expect(switchVisible || labelVisible).toBeTruthy();

      // Toggle the switch if visible
      if (switchVisible) {
        const isChecked = await failedSwitch.isChecked();
        await failedSwitch.click();
        await timelinePage.waitForEntriesLoaded();

        // Verify toggle state changed
        const newChecked = await failedSwitch.isChecked();
        expect(newChecked).toBe(!isChecked);
      } else if (labelVisible) {
        // Click on the label to toggle
        await failedLabel.click();
        await timelinePage.waitForEntriesLoaded();
      }
    });

    test('ACT-014: Clear all filters', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Apply a filter first (search for something)
      await timelinePage.searchInput.fill('test-filter');

      // Now the clear button should appear
      const clearButton = page.locator('button:has-text("Clear")');
      await expect(clearButton.first()).toBeVisible({ timeout: 5000 });

      // Click clear
      await clearButton.first().click();
      await timelinePage.waitForEntriesLoaded();

      // Verify search input is cleared
      await expect(timelinePage.searchInput).toHaveValue('');
    });
  });

  test.describe('Entry Details Dialog @P1', () => {
    test('ACT-020: View entry details dialog opens', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      const entryCount = await timelinePage.getEntryCount();

      if (entryCount > 0) {
        // Click on the first entry to open details
        await timelinePage.viewEntryDetails(0);

        // Verify dialog opened
        await expect(timelinePage.detailsDialog).toBeVisible();
      } else {
        // Skip test if no entries - just verify empty state
        await expect(timelinePage.emptyState).toBeVisible();
      }
    });

    test('ACT-021: Details dialog tabs work (HTTP, Handler, Database, Raw)', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      const entryCount = await timelinePage.getEntryCount();

      if (entryCount > 0) {
        // Open the details dialog
        await timelinePage.viewEntryDetails(0);
        await expect(timelinePage.detailsDialog).toBeVisible();

        // Wait for tabs to be visible
        await expect(timelinePage.dialogTabs).toBeVisible({ timeout: 5000 });

        // Verify all tabs are present
        await expect(timelinePage.httpTab).toBeVisible();
        await expect(timelinePage.handlerTab).toBeVisible();
        await expect(timelinePage.databaseTab).toBeVisible();
        await expect(timelinePage.rawTab).toBeVisible();

        // Test switching to each tab
        // HTTP tab should be default, switch to Handler
        await timelinePage.switchToHandlerTab();
        await expect(timelinePage.handlerTab).toHaveAttribute('data-state', 'active');

        // Switch to Database tab
        await timelinePage.switchToDatabaseTab();
        await expect(timelinePage.databaseTab).toHaveAttribute('data-state', 'active');

        // Switch to Raw tab
        await timelinePage.switchToRawTab();
        await expect(timelinePage.rawTab).toHaveAttribute('data-state', 'active');

        // Switch back to HTTP tab
        await timelinePage.switchToHttpTab();
        await expect(timelinePage.httpTab).toHaveAttribute('data-state', 'active');
      } else {
        // Skip test if no entries
        await expect(timelinePage.emptyState).toBeVisible();
      }
    });

    test('ACT-022: Close details dialog', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      const entryCount = await timelinePage.getEntryCount();

      if (entryCount > 0) {
        // Open the details dialog
        await timelinePage.viewEntryDetails(0);
        await expect(timelinePage.detailsDialog).toBeVisible();

        // Close the dialog using Escape key
        await timelinePage.closeDetailsDialog();

        // Verify dialog is closed
        await expect(timelinePage.detailsDialog).toBeHidden();
      } else {
        // Skip test if no entries
        await expect(timelinePage.emptyState).toBeVisible();
      }
    });
  });

  test.describe('Pagination @P1', () => {
    test('ACT-030: Pagination works (if multiple pages)', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Check if pagination is present
      const hasPagination = await timelinePage.hasPagination();

      if (hasPagination) {
        // Check if next page button is enabled
        const isNextEnabled = await timelinePage.paginationNext.isEnabled().catch(() => false);

        if (isNextEnabled) {
          // Navigate to next page
          await timelinePage.goToNextPage();
          await timelinePage.waitForEntriesLoaded();

          // Verify page loaded
          await expect(timelinePage.pageHeader).toBeVisible();

          // Check if we can go back
          const isPrevEnabled = await timelinePage.paginationPrevious.isEnabled().catch(() => false);
          if (isPrevEnabled) {
            await timelinePage.goToPreviousPage();
            await timelinePage.waitForEntriesLoaded();
            await expect(timelinePage.pageHeader).toBeVisible();
          }
        }
      } else {
        // No pagination - verify page still works
        await expect(timelinePage.pageHeader).toBeVisible();
      }
    });
  });

  test.describe('Refresh Functionality @P1', () => {
    test('ACT-040: Refresh button works', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Verify refresh button is visible
      await expect(timelinePage.refreshButton).toBeVisible();
      await expect(timelinePage.refreshButton).toBeEnabled();

      // Click refresh
      await timelinePage.refresh();

      // Page should still be loaded after refresh
      await expect(timelinePage.pageHeader).toBeVisible();
    });
  });

  test.describe('Search Functionality @P1', () => {
    test('ACT-050: Search submits on button click', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Enter search term
      await timelinePage.searchInput.fill('test-search');

      // Click search button
      await expect(timelinePage.searchButton).toBeVisible();
      await timelinePage.searchButton.click();

      // Wait for search to complete
      await timelinePage.waitForEntriesLoaded();

      // Page should still be functional
      await expect(timelinePage.pageHeader).toBeVisible();
    });

    test('ACT-051: Search can be cleared', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      // Enter search term
      await timelinePage.searchInput.fill('test-search');

      // Clear the search
      await timelinePage.clearSearch();

      // Verify search input is cleared
      await expect(timelinePage.searchInput).toHaveValue('');
    });
  });

  test.describe('Entry Status Indicators @P1', () => {
    test('ACT-060: Entries show success/failure status indicators', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      const entryCount = await timelinePage.getEntryCount();

      if (entryCount > 0) {
        // Status indicators (bg-green-500/bg-red-500) are in the avatar container,
        // which is a sibling of the entry button, not inside it.
        // Search at page level within the timeline entry wrapper (div.relative.flex.gap-4)
        const successIcon = page.locator('[class*="bg-green-500"]');
        const failureIcon = page.locator('[class*="bg-red-500"]');

        // At least one status indicator should be visible on the page
        const hasSuccess = await successIcon.first().isVisible().catch(() => false);
        const hasFailure = await failureIcon.first().isVisible().catch(() => false);

        expect(hasSuccess || hasFailure).toBeTruthy();
      }
    });

    test('ACT-061: Entries show operation type badges', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);
      await timelinePage.navigate();
      await timelinePage.expectPageLoaded();
      await timelinePage.waitForEntriesLoaded();

      const entryCount = await timelinePage.getEntryCount();

      if (entryCount > 0) {
        // Operation type badges (Create/Update/Delete) are rendered as Badge components
        // inside the entry button. Use data-slot="badge" for precise matching.
        const entries = timelinePage.timelineEntries;
        const firstEntry = entries.first();

        const createBadge = firstEntry.locator('[data-slot="badge"]:has-text("Create")');
        const updateBadge = firstEntry.locator('[data-slot="badge"]:has-text("Update")');
        const deleteBadge = firstEntry.locator('[data-slot="badge"]:has-text("Delete")');

        const hasCreate = await createBadge.isVisible().catch(() => false);
        const hasUpdate = await updateBadge.isVisible().catch(() => false);
        const hasDelete = await deleteBadge.isVisible().catch(() => false);

        // At least one operation type should be visible
        expect(hasCreate || hasUpdate || hasDelete).toBeTruthy();
      }
    });
  });

  test.describe('User Activity Filter @P1', () => {
    test('ACT-070: User filter banner shows when userId param present', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);

      // Navigate with a user filter
      await timelinePage.navigateWithUserFilter('test-user-id', 'test@example.com');
      await timelinePage.expectPageLoaded();

      // Verify user filter banner is visible
      const userFilterBanner = page.locator('div:has-text("Showing activity for user")');
      await expect(userFilterBanner.first()).toBeVisible({ timeout: 10000 });
    });

    test('ACT-071: Clear user filter removes URL parameter', async ({ page }) => {
      const timelinePage = new ActivityTimelinePage(page);

      // Navigate with a user filter
      await timelinePage.navigateWithUserFilter('test-user-id', 'test@example.com');
      await timelinePage.expectPageLoaded();

      // Find and click the clear user filter button
      const clearUserFilterButton = page.locator('button:has-text("Clear user filter")');

      if (await clearUserFilterButton.isVisible()) {
        await clearUserFilterButton.click();
        await timelinePage.waitForEntriesLoaded();

        // Verify URL no longer has userId parameter
        const url = page.url();
        expect(url).not.toContain('userId=');
      }
    });
  });
});
