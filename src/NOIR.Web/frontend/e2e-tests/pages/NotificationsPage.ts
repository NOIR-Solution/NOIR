import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * NotificationsPage - Page Object for Notifications List
 *
 * Based on: src/pages/portal/Notifications.tsx
 * - Full notification history with filtering and pagination
 * - Uses NotificationList component with filter tabs
 * - Has bulk actions (mark all read, refresh)
 * - Shows live connection status indicator
 */
export class NotificationsPage extends BasePage {
  // Page header
  readonly pageHeader: Locator;
  readonly pageDescription: Locator;
  readonly preferencesButton: Locator;
  readonly liveIndicator: Locator;

  // Filter tabs
  readonly filterTabs: Locator;
  readonly allFilterTab: Locator;
  readonly unreadFilterTab: Locator;
  readonly readFilterTab: Locator;

  // Actions
  readonly refreshButton: Locator;
  readonly markAllReadButton: Locator;

  // Notification list
  readonly notificationList: Locator;
  readonly notificationItems: Locator;
  readonly emptyState: Locator;
  readonly loadMoreButton: Locator;
  readonly statsText: Locator;

  // Loading states
  readonly notificationSkeleton: Locator;

  constructor(page: Page) {
    super(page);

    // Page header
    this.pageHeader = page.locator('h1:has-text("Notifications")');
    this.pageDescription = page.locator('p.text-muted-foreground').first();
    this.preferencesButton = page.locator('a:has-text("Preferences"), button:has-text("Preferences")');
    this.liveIndicator = page.locator('span:has-text("Live"), .animate-pulse').first();

    // Filter tabs - tabs are inside a muted background container
    this.filterTabs = page.locator('.bg-muted.rounded-lg, [role="tablist"]').first();
    this.allFilterTab = page.locator('button:has-text("all")').first();
    this.unreadFilterTab = page.locator('button:has-text("unread")').first();
    this.readFilterTab = page.locator('button:has-text("read")').first();

    // Actions
    this.refreshButton = page.locator('button:has-text("Refresh")');
    this.markAllReadButton = page.locator('button:has-text("Mark all read")');

    // Notification list
    this.notificationList = page.locator('.rounded-lg.border.bg-card, [data-testid="notification-list"]').first();
    this.notificationItems = page.locator('.divide-y > div, [data-testid="notification-item"]');
    this.emptyState = page.locator('[data-testid="notification-empty"], .notification-empty, div:has-text("No notifications")').first();
    this.loadMoreButton = page.locator('button:has-text("Load more")');
    this.statsText = page.locator('p.text-sm.text-muted-foreground:has-text("Showing")');

    // Loading states
    this.notificationSkeleton = page.locator('.divide-y .animate-pulse, [data-testid="notification-skeleton"]').first();
  }

  /**
   * Navigate to notifications page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/notifications');
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify page loaded using sequential wait pattern
   */
  async expectPageLoaded(): Promise<void> {
    await expect(this.pageHeader).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    // Wait for filter tabs to be visible (proves page is interactive)
    await expect(this.allFilterTab).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Filter notifications by type
   */
  async filterBy(filter: 'all' | 'unread' | 'read'): Promise<void> {
    switch (filter) {
      case 'all':
        await this.allFilterTab.click();
        break;
      case 'unread':
        await this.unreadFilterTab.click();
        break;
      case 'read':
        await this.readFilterTab.click();
        break;
    }
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Get current active filter
   */
  async getActiveFilter(): Promise<string> {
    // Active filter has 'bg-background shadow-sm' classes
    for (const filter of ['all', 'unread', 'read']) {
      const tab = this.page.locator(`button:has-text("${filter}")`).first();
      const classes = await tab.getAttribute('class') || '';
      if (classes.includes('bg-background') || classes.includes('shadow')) {
        return filter;
      }
    }
    return 'all';
  }

  /**
   * Refresh notifications
   */
  async refresh(): Promise<void> {
    await this.refreshButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Mark all notifications as read
   */
  async markAllAsRead(): Promise<void> {
    const isVisible = await this.markAllReadButton.isVisible();
    if (isVisible) {
      await this.markAllReadButton.click();
      await this.expectSuccessToast();
    }
  }

  /**
   * Load more notifications
   */
  async loadMore(): Promise<void> {
    const isVisible = await this.loadMoreButton.isVisible();
    if (isVisible) {
      await this.loadMoreButton.click();
      await this.waitForPageLoad();
    }
  }

  /**
   * Get notification count
   */
  async getNotificationCount(): Promise<number> {
    const count = await this.notificationItems.count();
    return count;
  }

  /**
   * Get unread badge count from filter tab
   */
  async getUnreadBadgeCount(): Promise<number> {
    const badge = this.unreadFilterTab.locator('span.rounded-full');
    const isVisible = await badge.isVisible();
    if (!isVisible) return 0;

    const text = await badge.textContent();
    return parseInt(text || '0', 10);
  }

  /**
   * Check if notifications list is empty
   */
  async isListEmpty(): Promise<boolean> {
    const emptyVisible = await this.emptyState.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false);
    const itemCount = await this.notificationItems.count();
    return emptyVisible || itemCount === 0;
  }

  /**
   * Check if live indicator is showing
   */
  async isLiveIndicatorVisible(): Promise<boolean> {
    return await this.liveIndicator.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false);
  }

  /**
   * Click on a notification by index
   */
  async clickNotification(index: number): Promise<void> {
    const notification = this.notificationItems.nth(index);
    await notification.click();
  }

  /**
   * Mark a specific notification as read
   */
  async markNotificationAsRead(index: number): Promise<void> {
    const notification = this.notificationItems.nth(index);
    const markReadButton = notification.locator('button:has-text("Mark as read"), button[aria-label*="mark"]').first();
    const isVisible = await markReadButton.isVisible();
    if (isVisible) {
      await markReadButton.click();
    }
  }

  /**
   * Delete a specific notification
   */
  async deleteNotification(index: number): Promise<void> {
    const notification = this.notificationItems.nth(index);
    const deleteButton = notification.locator('button:has-text("Delete"), button[aria-label*="delete"]').first();
    const isVisible = await deleteButton.isVisible();
    if (isVisible) {
      await deleteButton.click();

      // Handle confirmation if needed
      const confirmButton = this.confirmDialog.locator('button:has-text("Delete"), button:has-text("Confirm")');
      const hasConfirm = await confirmButton.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false);
      if (hasConfirm) {
        await confirmButton.click();
      }
    }
  }

  /**
   * Navigate to preferences page
   */
  async goToPreferences(): Promise<void> {
    await this.preferencesButton.click();
    await this.page.waitForURL('**/settings/notifications');
  }

  /**
   * Get notification stats text
   */
  async getStatsText(): Promise<string> {
    const text = await this.statsText.textContent();
    return text || '';
  }

  /**
   * Wait for notifications to load (skeleton to disappear)
   */
  async waitForNotificationsLoaded(): Promise<void> {
    await this.notificationSkeleton.waitFor({ state: 'hidden', timeout: Timeouts.API_RESPONSE }).catch(() => {});
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Verify notification list is visible
   */
  async expectNotificationListVisible(): Promise<void> {
    await expect(this.notificationList).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify empty state is shown
   */
  async expectEmptyState(): Promise<void> {
    await expect(this.emptyState).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Check if load more button is available
   */
  async hasLoadMore(): Promise<boolean> {
    return await this.loadMoreButton.isVisible();
  }

  /**
   * Check if mark all read button is available
   */
  async canMarkAllAsRead(): Promise<boolean> {
    return await this.markAllReadButton.isVisible();
  }
}
