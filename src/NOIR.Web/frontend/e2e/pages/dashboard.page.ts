import { Page, Locator } from '@playwright/test'

export class DashboardPage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly welcomeMessage: Locator
  readonly quickLinksCard: Locator
  readonly apiDocsLink: Locator
  readonly hangfireLink: Locator
  readonly profileSection: Locator
  readonly userEmail: Locator
  readonly userTenant: Locator
  readonly userRoles: Locator

  constructor(page: Page) {
    this.page = page
    this.pageTitle = page.getByRole('heading', { level: 1 })
    this.welcomeMessage = page.locator('p.text-muted-foreground').first()
    this.quickLinksCard = page.locator('.max-w-md').first()
    this.apiDocsLink = page.getByRole('link', { name: /api documentation|api docs/i })
    this.hangfireLink = page.getByRole('link', { name: /hangfire|background jobs/i })
    this.profileSection = page.locator('text=Your Profile').locator('..')
    this.userEmail = page.locator('text=Email:').locator('..').locator('span.font-medium')
    this.userTenant = page.locator('text=Tenant:').locator('..').locator('span.font-medium')
    this.userRoles = page.locator('text=Roles:').locator('..').locator('span.font-medium')
  }

  async goto() {
    await this.page.goto('/portal')
    await this.page.waitForLoadState('networkidle')
  }
}
