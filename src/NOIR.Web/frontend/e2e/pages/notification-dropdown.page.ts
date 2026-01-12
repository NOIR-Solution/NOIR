import { Page, Locator, expect } from '@playwright/test'

export class NotificationDropdownPage {
  readonly page: Page
  readonly bellButton: Locator
  readonly bellBadge: Locator
  readonly dropdownContent: Locator
  readonly dropdownHeader: Locator
  readonly markAllReadButton: Locator
  readonly viewAllLink: Locator
  readonly emptyState: Locator
  readonly loadingSpinner: Locator

  constructor(page: Page) {
    this.page = page
    this.bellButton = page.getByRole('button', { name: /notifications/i })
    this.bellBadge = page.locator('button[aria-label*="Notifications"] .bg-destructive, button[aria-label*="notifications"] span.bg-destructive')
    this.dropdownContent = page.locator('[role="menu"]')
    this.dropdownHeader = page.getByRole('menu').getByText('Notifications', { exact: true })
    this.markAllReadButton = page.getByRole('button', { name: /mark all read/i })
    this.viewAllLink = page.getByRole('link', { name: /view all notifications/i })
    this.emptyState = page.getByText('No notifications')
    this.loadingSpinner = page.locator('.animate-spin')
  }

  async open() {
    await this.bellButton.click()
    await expect(this.dropdownContent).toBeVisible()
  }

  async close() {
    await this.page.keyboard.press('Escape')
    await expect(this.dropdownContent).not.toBeVisible()
  }

  getNotificationItem(index: number): Locator {
    return this.dropdownContent.locator('.divide-y > div').nth(index)
  }

  getNotificationByTitle(title: string): Locator {
    return this.dropdownContent.getByText(title, { exact: false })
  }

  async getUnreadCount(): Promise<number> {
    const isVisible = await this.bellBadge.isVisible()
    if (isVisible) {
      const text = await this.bellBadge.textContent()
      return text === '99+' ? 100 : parseInt(text || '0', 10)
    }
    return 0
  }

  async markAllAsRead() {
    await this.markAllReadButton.click()
  }

  async navigateToFullPage() {
    await this.viewAllLink.click()
    await expect(this.page).toHaveURL('/portal/notifications')
  }
}
