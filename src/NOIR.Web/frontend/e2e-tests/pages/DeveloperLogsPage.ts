import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * DeveloperLogsPage - Page Object for Developer Logs
 *
 * Based on: src/pages/portal/admin/developer-logs/DeveloperLogsPage.tsx
 * - Real-time log streaming via SignalR
 * - Multiple tabs: Live Logs, History, Statistics, Error Clusters
 * - Log level control and filtering
 * - Detail dialog for individual log entries
 */
export class DeveloperLogsPage extends BasePage {
  // Page header and connection status
  readonly pageHeader: Locator;
  readonly connectionBadge: Locator;
  readonly connectedBadge: Locator;
  readonly disconnectedBadge: Locator;
  readonly connectingBadge: Locator;

  // Main tabs
  readonly tabsList: Locator;
  readonly liveLogsTab: Locator;
  readonly historyTab: Locator;
  readonly statisticsTab: Locator;
  readonly errorClustersTab: Locator;

  // Live Logs toolbar
  readonly pauseButton: Locator;
  readonly autoScrollToggle: Locator;
  readonly sortOrderToggle: Locator;
  readonly serverLevelDropdown: Locator;
  readonly levelFilterDropdown: Locator;
  readonly searchInput: Locator;
  readonly exceptionsOnlyToggle: Locator;
  readonly clearBufferButton: Locator;

  // Log table
  readonly logTable: Locator;
  readonly logEntries: Locator;
  readonly emptyState: Locator;

  // Detail dialog
  readonly detailDialog: Locator;
  readonly dialogTitle: Locator;
  readonly dialogMessage: Locator;
  readonly dialogSource: Locator;
  readonly dialogException: Locator;
  readonly dialogProperties: Locator;
  readonly dialogRawJson: Locator;
  readonly dialogCopyButton: Locator;
  readonly dialogCloseButton: Locator;

  // History tab elements
  readonly historyFileList: Locator;
  readonly historySearchInput: Locator;
  readonly historyDateFilter: Locator;

  // Statistics tab elements
  readonly statsCards: Locator;
  readonly statsRefreshButton: Locator;

  // Error Clusters tab elements
  readonly errorClustersList: Locator;
  readonly errorClustersRefreshButton: Locator;

  constructor(page: Page) {
    super(page);

    // Page header and connection status
    this.pageHeader = page.locator('h1:has-text("Developer Logs")');
    this.connectionBadge = page.locator('header [class*="Badge"], [class*="PageHeader"] [class*="Badge"]').first();
    this.connectedBadge = page.locator('text="Connected"').first();
    this.disconnectedBadge = page.locator('text="Disconnected"').first();
    this.connectingBadge = page.locator('text="Connecting"').first();

    // Main tabs
    this.tabsList = page.locator('[role="tablist"]').first();
    this.liveLogsTab = page.locator('[role="tab"]:has-text("Live Logs")');
    this.historyTab = page.locator('[role="tab"]:has-text("History")');
    this.statisticsTab = page.locator('[role="tab"]:has-text("Statistics")');
    this.errorClustersTab = page.locator('[role="tab"]:has-text("Error Clusters")');

    // Live Logs toolbar
    this.pauseButton = page.locator('button:has-text("Pause"), button:has-text("Resume")');
    this.autoScrollToggle = page.locator('button[aria-label*="auto-scroll"], button:has-text("Auto-scroll")');
    this.sortOrderToggle = page.locator('button[aria-label*="sort"], button:has-text("Newest"), button:has-text("Oldest")');
    this.serverLevelDropdown = page.locator('button:has-text("Server Level")').first();
    this.levelFilterDropdown = page.locator('button:has-text("Filter Levels"), button:has-text("Levels")').first();
    this.searchInput = page.locator('input[placeholder*="Search"], input[placeholder*="Filter"]').first();
    this.exceptionsOnlyToggle = page.locator('button:has-text("Errors only"), [data-testid="exceptions-only"]');
    this.clearBufferButton = page.locator('button:has-text("Clear"), button:has-text("Clear Buffer")');

    // Log table
    this.logTable = page.locator('[class*="LogTable"], table, [data-testid="log-table"]');
    this.logEntries = page.locator('tr[class*="log-entry"], [data-testid="log-entry"], [class*="LogEntryRow"]');
    this.emptyState = page.locator('text="No log entries", text="Waiting for incoming logs"');

    // Detail dialog
    this.detailDialog = page.locator('[role="dialog"]');
    this.dialogTitle = this.detailDialog.locator('[class*="DialogTitle"]');
    this.dialogMessage = this.detailDialog.locator('label:has-text("Message") + div, [data-testid="log-message"]');
    this.dialogSource = this.detailDialog.locator('label:has-text("Source") + div');
    this.dialogException = this.detailDialog.locator('[class*="Exception"], [class*="red-50"]');
    this.dialogProperties = this.detailDialog.locator('label:has-text("Properties") ~ div');
    this.dialogRawJson = this.detailDialog.locator('label:has-text("Raw JSON") ~ div');
    this.dialogCopyButton = this.detailDialog.locator('button:has-text("Copy")');
    this.dialogCloseButton = this.detailDialog.locator('button[aria-label="Close"]');

    // History tab elements
    this.historyFileList = page.locator('[data-testid="history-files"], [class*="history-list"]');
    this.historySearchInput = page.locator('[data-testid="history-search"], input[placeholder*="Search history"]');
    this.historyDateFilter = page.locator('[data-testid="history-date-filter"]');

    // Statistics tab elements
    this.statsCards = page.locator('[data-testid="stats-card"], [class*="stats-card"]');
    this.statsRefreshButton = page.locator('[data-testid="stats-refresh"], button:has-text("Refresh Stats")');

    // Error Clusters tab elements
    this.errorClustersList = page.locator('[data-testid="error-clusters"], [class*="error-cluster"]');
    this.errorClustersRefreshButton = page.locator('[data-testid="clusters-refresh"], button:has-text("Refresh Clusters")');
  }

  /**
   * Navigate to developer logs page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/admin/developer-logs');
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify page loaded using sequential wait pattern
   * Waits for header first (proves render), then tabs (proves UI loaded)
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.tabsList);
  }

  /**
   * Wait for connection to be established
   */
  async waitForConnection(timeout: number = Timeouts.API_RESPONSE): Promise<boolean> {
    try {
      await this.connectedBadge.waitFor({ state: 'visible', timeout });
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Check if connected to log stream
   */
  async isConnected(): Promise<boolean> {
    return await this.connectedBadge.isVisible();
  }

  /**
   * Check if disconnected
   */
  async isDisconnected(): Promise<boolean> {
    return await this.disconnectedBadge.isVisible();
  }

  // ============================================================
  // Tab Navigation
  // ============================================================

  /**
   * Switch to Live Logs tab
   */
  async switchToLiveLogs(): Promise<void> {
    await this.liveLogsTab.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Switch to History tab
   */
  async switchToHistory(): Promise<void> {
    await this.historyTab.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Switch to Statistics tab
   */
  async switchToStatistics(): Promise<void> {
    await this.statisticsTab.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Switch to Error Clusters tab
   */
  async switchToErrorClusters(): Promise<void> {
    await this.errorClustersTab.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Get currently active tab
   */
  async getActiveTab(): Promise<string> {
    const activeTab = this.tabsList.locator('[aria-selected="true"]');
    return await activeTab.textContent() || '';
  }

  // ============================================================
  // Live Logs Controls
  // ============================================================

  /**
   * Toggle pause/resume for live logs
   */
  async togglePause(): Promise<void> {
    await this.pauseButton.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Pause live log streaming
   */
  async pause(): Promise<void> {
    const buttonText = await this.pauseButton.textContent();
    if (buttonText?.includes('Pause')) {
      await this.pauseButton.click();
    }
  }

  /**
   * Resume live log streaming
   */
  async resume(): Promise<void> {
    const buttonText = await this.pauseButton.textContent();
    if (buttonText?.includes('Resume')) {
      await this.pauseButton.click();
    }
  }

  /**
   * Toggle auto-scroll
   */
  async toggleAutoScroll(): Promise<void> {
    await this.autoScrollToggle.click();
  }

  /**
   * Toggle sort order (newest/oldest)
   */
  async toggleSortOrder(): Promise<void> {
    await this.sortOrderToggle.click();
  }

  /**
   * Change server log level
   */
  async setServerLevel(level: string): Promise<void> {
    await this.serverLevelDropdown.click();
    const option = this.page.locator(`[role="option"]:has-text("${level}"), [role="menuitem"]:has-text("${level}")`);
    await option.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Filter by specific log levels
   */
  async filterByLevel(levels: string[]): Promise<void> {
    await this.levelFilterDropdown.click();
    for (const level of levels) {
      const checkbox = this.page.locator(`[role="menuitemcheckbox"]:has-text("${level}"), label:has-text("${level}")`);
      await checkbox.click();
    }
    await this.page.keyboard.press('Escape');
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Search logs by term
   */
  async search(term: string): Promise<void> {
    await this.searchInput.fill(term);
    await this.page.waitForTimeout(Timeouts.SEARCH_WAIT);
  }

  /**
   * Clear search
   */
  async clearSearch(): Promise<void> {
    await this.searchInput.clear();
    await this.page.waitForTimeout(Timeouts.SEARCH_WAIT);
  }

  /**
   * Toggle exceptions only filter
   */
  async toggleExceptionsOnly(): Promise<void> {
    await this.exceptionsOnlyToggle.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Clear the log buffer
   */
  async clearBuffer(): Promise<void> {
    await this.clearBufferButton.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  // ============================================================
  // Log Entries
  // ============================================================

  /**
   * Get count of visible log entries
   */
  async getEntryCount(): Promise<number> {
    return await this.logEntries.count();
  }

  /**
   * Check if log table is empty
   */
  async isEmpty(): Promise<boolean> {
    return await this.emptyState.isVisible();
  }

  /**
   * Click on a log entry by index to view details
   */
  async viewEntryDetails(index: number = 0): Promise<void> {
    const entry = this.logEntries.nth(index);
    await entry.click();
    await expect(this.detailDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Click on a log entry by message content
   */
  async viewEntryByMessage(messageText: string): Promise<void> {
    const entry = this.logEntries.filter({ hasText: messageText }).first();
    await entry.click();
    await expect(this.detailDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Expand a log entry inline (without opening dialog)
   */
  async expandEntry(index: number): Promise<void> {
    const entry = this.logEntries.nth(index);
    const expandButton = entry.locator('button[aria-label*="expand"], button:has([class*="ChevronDown"])');
    if (await expandButton.isVisible()) {
      await expandButton.click();
    }
  }

  /**
   * Close the detail dialog
   */
  async closeDetailDialog(): Promise<void> {
    await this.page.keyboard.press('Escape');
    await expect(this.detailDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Copy message from detail dialog
   */
  async copyMessageFromDialog(): Promise<void> {
    await this.dialogCopyButton.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Get log level from detail dialog title
   */
  async getDialogLogLevel(): Promise<string> {
    const badge = this.detailDialog.locator('[class*="Badge"]').first();
    return await badge.textContent() || '';
  }

  // ============================================================
  // History Tab
  // ============================================================

  /**
   * Search in history files
   */
  async searchHistory(term: string): Promise<void> {
    await this.switchToHistory();
    await this.historySearchInput.fill(term);
    await this.page.waitForTimeout(Timeouts.SEARCH_WAIT);
  }

  /**
   * Get count of history files
   */
  async getHistoryFileCount(): Promise<number> {
    await this.switchToHistory();
    const files = this.historyFileList.locator('[data-testid="history-file"], [class*="file-item"]');
    return await files.count();
  }

  // ============================================================
  // Statistics Tab
  // ============================================================

  /**
   * Refresh statistics
   */
  async refreshStats(): Promise<void> {
    await this.switchToStatistics();
    const refreshButton = this.page.locator('button:has-text("Refresh")');
    if (await refreshButton.isVisible()) {
      await refreshButton.click();
      await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
    }
  }

  /**
   * Get stats card count
   */
  async getStatsCardCount(): Promise<number> {
    await this.switchToStatistics();
    return await this.statsCards.count();
  }

  // ============================================================
  // Error Clusters Tab
  // ============================================================

  /**
   * Refresh error clusters
   */
  async refreshErrorClusters(): Promise<void> {
    await this.switchToErrorClusters();
    const refreshButton = this.page.locator('button:has-text("Refresh")');
    if (await refreshButton.isVisible()) {
      await refreshButton.click();
      await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
    }
  }

  /**
   * Get error cluster count
   */
  async getErrorClusterCount(): Promise<number> {
    await this.switchToErrorClusters();
    return await this.errorClustersList.count();
  }

  // ============================================================
  // Assertions
  // ============================================================

  /**
   * Verify a log entry exists with specific level
   */
  async expectEntryWithLevel(level: string): Promise<void> {
    const entry = this.logEntries.filter({ hasText: level }).first();
    await expect(entry).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify a log entry exists with specific message
   */
  async expectEntryWithMessage(message: string): Promise<void> {
    const entry = this.logEntries.filter({ hasText: message }).first();
    await expect(entry).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify connection status is displayed
   */
  async expectConnectionStatus(status: 'Connected' | 'Disconnected' | 'Connecting'): Promise<void> {
    const badge = this.page.locator(`text="${status}"`).first();
    await expect(badge).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify exception details in dialog
   */
  async expectExceptionInDialog(): Promise<void> {
    await expect(this.dialogException).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify Live Logs tab is active
   */
  async expectLiveLogsTabActive(): Promise<void> {
    await expect(this.liveLogsTab).toHaveAttribute('aria-selected', 'true');
  }

  /**
   * Verify History tab is active
   */
  async expectHistoryTabActive(): Promise<void> {
    await expect(this.historyTab).toHaveAttribute('aria-selected', 'true');
  }

  /**
   * Verify Statistics tab is active
   */
  async expectStatisticsTabActive(): Promise<void> {
    await expect(this.statisticsTab).toHaveAttribute('aria-selected', 'true');
  }

  /**
   * Verify Error Clusters tab is active
   */
  async expectErrorClustersTabActive(): Promise<void> {
    await expect(this.errorClustersTab).toHaveAttribute('aria-selected', 'true');
  }
}
