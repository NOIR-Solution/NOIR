import { Page, Locator, expect } from '@playwright/test'

export class NotificationPreferencesPage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly backButton: Locator
  readonly saveButton: Locator
  readonly categoryCards: Locator
  readonly successToast: Locator

  constructor(page: Page) {
    this.page = page
    this.pageTitle = page.getByRole('heading', { name: 'Notification Preferences' })
    // Back button is an icon-only button (h-8 w-8) with ArrowLeft, next to the page title
    this.backButton = page.locator('a[href="/portal/notifications"]').filter({ has: page.locator('svg') })
    this.saveButton = page.getByRole('button', { name: /save/i })
    this.categoryCards = page.locator('[data-slot="card"], .rounded-lg.border.bg-card').filter({ has: page.locator('[role="switch"]') })
    this.successToast = page.getByText('Preferences saved successfully')
  }

  async goto() {
    await this.page.goto('/portal/settings/notifications', { waitUntil: 'load', timeout: 30000 })
    await this.page.waitForURL(/\/portal\/settings\/notifications/, { timeout: 10000 })
    await expect(this.pageTitle).toBeVisible({ timeout: 15000 })
  }

  getCategoryCard(category: 'system' | 'userAction' | 'workflow' | 'security' | 'integration'): Locator {
    const labels: Record<string, string> = {
      system: 'System',
      userAction: 'User Actions',
      workflow: 'Workflow',
      security: 'Security',
      integration: 'Integration',
    }
    return this.page.locator('.rounded-lg.border.bg-card, [data-slot="card"]').filter({ hasText: labels[category] })
  }

  async toggleInAppNotification(category: 'system' | 'userAction' | 'workflow' | 'security' | 'integration') {
    const card = this.getCategoryCard(category)
    await card.getByRole('switch').click()
  }

  async selectEmailFrequency(
    category: 'system' | 'userAction' | 'workflow' | 'security' | 'integration',
    frequency: 'Never' | 'Immediate' | 'Daily digest' | 'Weekly digest'
  ) {
    const card = this.getCategoryCard(category)
    await card.getByRole('button', { name: frequency, exact: true }).click()
  }

  async save() {
    await this.saveButton.click()
    await expect(this.successToast).toBeVisible({ timeout: 10000 })
  }

  async isSaveEnabled(): Promise<boolean> {
    return await this.saveButton.isEnabled()
  }

  async navigateBack() {
    await this.backButton.click()
    await expect(this.page).toHaveURL('/portal/notifications')
  }
}
