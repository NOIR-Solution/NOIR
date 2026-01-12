import { Page, Locator, expect } from '@playwright/test'

export class NotificationListPage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly liveIndicator: Locator
  readonly settingsLink: Locator
  readonly filterTabs: {
    all: Locator
    unread: Locator
    read: Locator
  }
  readonly refreshButton: Locator
  readonly markAllReadButton: Locator
  readonly notificationStats: Locator
  readonly notificationList: Locator
  readonly loadMoreButton: Locator
  readonly emptyState: Locator

  constructor(page: Page) {
    this.page = page
    // Use h1 specifically to avoid matching the dropdown's h3
    this.pageTitle = page.locator('h1', { hasText: 'Notifications' })
    this.liveIndicator = page.getByText('Live')
    this.settingsLink = page.getByRole('link', { name: /preferences/i })
    this.filterTabs = {
      all: page.getByRole('button', { name: 'all', exact: true }),
      unread: page.getByRole('button', { name: /unread/i }),
      read: page.getByRole('button', { name: 'read', exact: true }),
    }
    this.refreshButton = page.getByRole('button', { name: /refresh/i })
    this.markAllReadButton = page.getByRole('button', { name: /mark all read/i })
    this.notificationStats = page.getByText(/showing \d+ of \d+ notifications/i)
    this.notificationList = page.locator('.rounded-lg.border.bg-card .divide-y')
    this.loadMoreButton = page.getByRole('button', { name: /load more/i })
    this.emptyState = page.getByText('No notifications')
  }

  async goto() {
    await this.page.goto('/portal/notifications', { waitUntil: 'load', timeout: 30000 })
    await this.page.waitForURL(/\/portal\/notifications/, { timeout: 10000 })
    await expect(this.pageTitle).toBeVisible({ timeout: 15000 })
  }

  async filterBy(filter: 'all' | 'unread' | 'read') {
    await this.filterTabs[filter].click()
  }

  getNotificationItem(index: number): Locator {
    return this.notificationList.locator('> div').nth(index)
  }

  getNotificationByTitle(title: string): Locator {
    return this.notificationList.getByText(title, { exact: false })
  }

  async getNotificationCount(): Promise<{ showing: number; total: number }> {
    const text = await this.notificationStats.textContent()
    const match = text?.match(/showing (\d+) of (\d+)/i)
    return {
      showing: parseInt(match?.[1] || '0', 10),
      total: parseInt(match?.[2] || '0', 10),
    }
  }

  async deleteNotification(index: number) {
    const item = this.getNotificationItem(index)
    await item.hover()
    await item.getByRole('button', { name: /delete/i }).click()
  }

  async clickNotification(index: number) {
    await this.getNotificationItem(index).click()
  }
}
