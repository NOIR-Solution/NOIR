import { Page, Locator } from '@playwright/test'

export class EmailTemplatesPage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly pageDescription: Locator
  readonly refreshButton: Locator
  readonly searchInput: Locator
  readonly languageFilter: Locator
  readonly templatesGrid: Locator
  readonly templateCards: Locator
  readonly emptyState: Locator
  readonly loadingState: Locator

  constructor(page: Page) {
    this.page = page
    this.pageTitle = page.getByRole('heading', { level: 1, name: /email templates/i })
    this.pageDescription = page.locator('p.text-muted-foreground').first()
    this.refreshButton = page.getByRole('button', { name: /refresh/i })
    this.searchInput = page.getByPlaceholder(/search/i)
    this.languageFilter = page.getByRole('button', { name: /all|english|tieng viet/i })
    this.templatesGrid = page.locator('.grid')
    this.templateCards = page.locator('[class*=Card]').filter({ has: page.locator('text=Version:') })
    this.emptyState = page.locator('text=No email templates')
    this.loadingState = page.locator('.animate-pulse')
  }

  async goto() {
    await this.page.goto('/portal/email-templates')
    await this.page.waitForLoadState('networkidle')
  }

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.page.waitForTimeout(500) // debounce
  }

  async filterByLanguage(language: 'all' | 'en' | 'vi') {
    await this.languageFilter.click()
    const option = this.page.getByRole('menuitem', {
      name: language === 'all' ? /all/i : language === 'en' ? /english/i : /tieng viet/i
    })
    await option.click()
  }

  async previewTemplate(index: number) {
    const card = this.templateCards.nth(index)
    await card.getByRole('button', { name: /preview/i }).click()
  }

  async editTemplate(index: number) {
    const card = this.templateCards.nth(index)
    await card.getByRole('button', { name: /edit/i }).click()
  }

  getTemplateCard(index: number) {
    return this.templateCards.nth(index)
  }
}
