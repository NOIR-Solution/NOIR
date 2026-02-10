import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * ActivityTimelinePage - Page Object for Activity Timeline
 *
 * Based on: src/pages/portal/admin/activity-timeline/ActivityTimelinePage.tsx
 * - Timeline view of user activities
 * - Filtering by date range, context, operation type, user
 * - Details dialog with HTTP, Handler, Database, Raw tabs
 */
export class ActivityTimelinePage extends BasePage {
  // Page header and actions
  readonly pageHeader: Locator;
  readonly refreshButton: Locator;

  // Filters
  readonly searchInput: Locator;
  readonly dateRangePicker: Locator;
  readonly contextDropdown: Locator;
  readonly operationDropdown: Locator;
  readonly failedOnlySwitch: Locator;
  readonly clearFiltersButton: Locator;
  readonly searchButton: Locator;

  // User filter banner (when filtering by user from Users page)
  readonly userFilterBanner: Locator;
  readonly clearUserFilterButton: Locator;

  // Timeline content
  readonly timelineContainer: Locator;
  readonly timelineEntries: Locator;
  readonly emptyState: Locator;
  readonly loadingSkeletons: Locator;

  // Pagination
  readonly pagination: Locator;
  readonly paginationPrevious: Locator;
  readonly paginationNext: Locator;

  // Details dialog
  readonly detailsDialog: Locator;
  readonly dialogTitle: Locator;
  readonly dialogTabs: Locator;
  readonly httpTab: Locator;
  readonly handlerTab: Locator;
  readonly databaseTab: Locator;
  readonly rawTab: Locator;
  readonly dialogCloseButton: Locator;

  constructor(page: Page) {
    super(page);

    // Page header and actions
    this.pageHeader = page.locator('h1:has-text("Activity Timeline")');
    this.refreshButton = page.locator('button:has-text("Refresh")');

    // Filters
    this.searchInput = page.locator('input[placeholder*="Search by ID"]').first();
    this.dateRangePicker = page.locator('button:has-text("Date range"), [data-testid="date-range-picker"]').first();
    this.contextDropdown = page.locator('button:has-text("All Contexts")').first();
    this.operationDropdown = page.locator('button:has-text("All Actions")').first();
    this.failedOnlySwitch = page.locator('#only-failed, [data-testid="failed-only-switch"]');
    this.clearFiltersButton = page.locator('button:has-text("Clear")').first();
    this.searchButton = page.locator('button[type="submit"]:has-text("Search")');

    // User filter banner
    this.userFilterBanner = page.locator('div:has-text("Showing activity for user")');
    this.clearUserFilterButton = page.locator('button:has-text("Clear user filter")');

    // Timeline content
    this.timelineContainer = page.locator('[class*="pl-2"]').first();
    this.timelineEntries = page.locator('button[type="button"][class*="rounded-lg border"]');
    this.emptyState = page.locator('div.border-dashed.border-2.rounded-xl');
    this.loadingSkeletons = page.locator('.animate-pulse, [class*="Skeleton"]');

    // Pagination
    this.pagination = page.locator('[class*="pagination"], nav[aria-label="pagination"]');
    this.paginationPrevious = page.locator('button[aria-label="Go to previous page"]');
    this.paginationNext = page.locator('button[aria-label="Go to next page"]');

    // Details dialog
    this.detailsDialog = page.locator('[role="dialog"]');
    this.dialogTitle = this.detailsDialog.locator('[class*="DialogTitle"]');
    this.dialogTabs = this.detailsDialog.locator('[role="tablist"]');
    this.httpTab = this.detailsDialog.locator('[role="tab"]:has-text("HTTP")');
    this.handlerTab = this.detailsDialog.locator('[role="tab"]:has-text("Handler")');
    this.databaseTab = this.detailsDialog.locator('[role="tab"]:has-text("Database")');
    this.rawTab = this.detailsDialog.locator('[role="tab"]:has-text("Raw")');
    this.dialogCloseButton = this.detailsDialog.locator('button[aria-label="Close"]');
  }

  /**
   * Navigate to activity timeline page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/activity-timeline');
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Navigate with user filter (from Users page "View user activity" link)
   */
  async navigateWithUserFilter(userId: string, userEmail?: string): Promise<void> {
    let url = `/portal/activity-timeline?userId=${userId}`;
    if (userEmail) {
      url += `&userEmail=${encodeURIComponent(userEmail)}`;
    }
    await this.goto(url);
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify page loaded using sequential wait pattern
   * Waits for header first (proves render), then refresh button (proves data loaded)
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.refreshButton);
  }

  /**
   * Wait for timeline entries to load
   */
  async waitForEntriesLoaded(): Promise<void> {
    // Wait for loading skeletons to disappear
    await this.loadingSkeletons.first().waitFor({ state: 'hidden', timeout: Timeouts.API_RESPONSE }).catch(() => {});
    // Wait for either entries or empty state
    await Promise.race([
      this.timelineEntries.first().waitFor({ state: 'visible', timeout: Timeouts.API_RESPONSE }),
      this.emptyState.waitFor({ state: 'visible', timeout: Timeouts.API_RESPONSE }),
    ]).catch(() => {});
  }

  /**
   * Search activities by term
   */
  async search(term: string): Promise<void> {
    await this.searchInput.fill(term);
    await this.searchButton.click();
    await this.waitForEntriesLoaded();
  }

  /**
   * Clear search input
   */
  async clearSearch(): Promise<void> {
    await this.searchInput.clear();
    await this.searchButton.click();
    await this.waitForEntriesLoaded();
  }

  /**
   * Filter by date range using the date picker
   */
  async filterByDateRange(): Promise<void> {
    await this.dateRangePicker.click();
    // Date picker opens - user can select dates
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Select a preset date range (Today, Last 7 days, etc.)
   */
  async selectDatePreset(preset: string): Promise<void> {
    await this.dateRangePicker.click();
    const presetButton = this.page.locator(`button:has-text("${preset}")`);
    await presetButton.click();
    await this.waitForEntriesLoaded();
  }

  /**
   * Filter by page context
   */
  async filterByContext(context: string): Promise<void> {
    await this.contextDropdown.click();
    const option = this.page.locator(`[role="option"]:has-text("${context}")`);
    await option.click();
    await this.waitForEntriesLoaded();
  }

  /**
   * Get available contexts from dropdown
   */
  async getAvailableContexts(): Promise<string[]> {
    await this.contextDropdown.click();
    const options = this.page.locator('[role="option"]');
    const contexts = await options.allTextContents();
    await this.page.keyboard.press('Escape');
    return contexts.filter(c => c !== 'All Contexts');
  }

  /**
   * Filter by operation type (Create, Update, Delete)
   */
  async filterByOperationType(operationType: 'Create' | 'Update' | 'Delete'): Promise<void> {
    await this.operationDropdown.click();
    const option = this.page.locator(`[role="option"]:has-text("${operationType}")`);
    await option.click();
    await this.waitForEntriesLoaded();
  }

  /**
   * Toggle "Failed only" filter
   */
  async toggleFailedOnly(): Promise<void> {
    await this.failedOnlySwitch.click();
    await this.waitForEntriesLoaded();
  }

  /**
   * Set "Failed only" filter to specific state
   */
  async setFailedOnly(enabled: boolean): Promise<void> {
    const isChecked = await this.failedOnlySwitch.isChecked();
    if (isChecked !== enabled) {
      await this.failedOnlySwitch.click();
      await this.waitForEntriesLoaded();
    }
  }

  /**
   * Clear all filters
   */
  async clearAllFilters(): Promise<void> {
    const isVisible = await this.clearFiltersButton.isVisible();
    if (isVisible) {
      await this.clearFiltersButton.click();
      await this.waitForEntriesLoaded();
    }
  }

  /**
   * Clear user filter (when navigated from Users page)
   */
  async clearUserFilter(): Promise<void> {
    const isVisible = await this.clearUserFilterButton.isVisible();
    if (isVisible) {
      await this.clearUserFilterButton.click();
      await this.waitForEntriesLoaded();
    }
  }

  /**
   * Refresh the timeline
   */
  async refresh(): Promise<void> {
    await this.refreshButton.click();
    await this.waitForEntriesLoaded();
  }

  /**
   * Get count of visible timeline entries
   */
  async getEntryCount(): Promise<number> {
    return await this.timelineEntries.count();
  }

  /**
   * Check if timeline is empty
   */
  async isEmpty(): Promise<boolean> {
    return await this.emptyState.isVisible();
  }

  /**
   * Click on an entry by index to view details
   */
  async viewEntryDetails(index: number = 0): Promise<void> {
    const entry = this.timelineEntries.nth(index);
    await entry.click();
    await expect(this.detailsDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Click on an entry by text content
   */
  async viewEntryByText(text: string): Promise<void> {
    const entry = this.timelineEntries.filter({ hasText: text }).first();
    await entry.click();
    await expect(this.detailsDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Close the details dialog
   */
  async closeDetailsDialog(): Promise<void> {
    await this.page.keyboard.press('Escape');
    await expect(this.detailsDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Switch to HTTP tab in details dialog
   */
  async switchToHttpTab(): Promise<void> {
    await this.httpTab.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Switch to Handler tab in details dialog
   */
  async switchToHandlerTab(): Promise<void> {
    await this.handlerTab.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Switch to Database tab in details dialog
   */
  async switchToDatabaseTab(): Promise<void> {
    await this.databaseTab.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Switch to Raw tab in details dialog
   */
  async switchToRawTab(): Promise<void> {
    await this.rawTab.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Get entry details from the open dialog
   */
  async getEntryDetailsText(): Promise<string> {
    return await this.detailsDialog.textContent() || '';
  }

  /**
   * Navigate to next page
   */
  async goToNextPage(): Promise<void> {
    await this.paginationNext.click();
    await this.waitForEntriesLoaded();
  }

  /**
   * Navigate to previous page
   */
  async goToPreviousPage(): Promise<void> {
    await this.paginationPrevious.click();
    await this.waitForEntriesLoaded();
  }

  /**
   * Check if pagination is visible
   */
  async hasPagination(): Promise<boolean> {
    return await this.pagination.isVisible();
  }

  /**
   * Verify user filter banner is visible
   */
  async expectUserFilterVisible(userEmail?: string): Promise<void> {
    await expect(this.userFilterBanner.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    if (userEmail) {
      await expect(this.userFilterBanner).toContainText(userEmail);
    }
  }

  /**
   * Verify an entry exists with specific text
   */
  async expectEntryExists(text: string): Promise<void> {
    const entry = this.timelineEntries.filter({ hasText: text }).first();
    await expect(entry).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify an entry has specific operation type badge
   */
  async expectEntryHasOperation(index: number, operationType: 'Create' | 'Update' | 'Delete'): Promise<void> {
    const entry = this.timelineEntries.nth(index);
    const badge = entry.locator(`text="${operationType}"`);
    await expect(badge).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify entry shows success status
   */
  async expectEntryIsSuccess(index: number): Promise<void> {
    const entry = this.timelineEntries.nth(index);
    const successIndicator = entry.locator('[class*="bg-green-500"]');
    await expect(successIndicator).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify entry shows failure status
   */
  async expectEntryIsFailure(index: number): Promise<void> {
    const entry = this.timelineEntries.nth(index);
    const failureIndicator = entry.locator('[class*="bg-red-500"]');
    await expect(failureIndicator).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }
}
