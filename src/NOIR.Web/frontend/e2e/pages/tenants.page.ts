import { Page, Locator } from '@playwright/test'

export class TenantsPage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly pageDescription: Locator
  readonly createTenantButton: Locator
  readonly searchInput: Locator
  readonly searchButton: Locator
  readonly tenantsTable: Locator
  readonly tenantRows: Locator
  readonly emptyState: Locator
  readonly paginationInfo: Locator
  readonly previousButton: Locator
  readonly nextButton: Locator
  readonly errorMessage: Locator

  // Create dialog elements
  readonly createDialog: Locator
  readonly identifierInput: Locator
  readonly displayNameInput: Locator
  readonly logoUrlInput: Locator
  readonly submitCreateButton: Locator
  readonly cancelButton: Locator

  // Delete dialog elements
  readonly deleteDialog: Locator
  readonly confirmDeleteButton: Locator

  constructor(page: Page) {
    this.page = page
    this.pageTitle = page.getByRole('heading', { level: 1 })
    this.pageDescription = page.locator('p.text-muted-foreground').first()
    this.createTenantButton = page.getByRole('button', { name: /create tenant/i })
    this.searchInput = page.getByPlaceholder(/search tenants/i)
    this.searchButton = page.getByRole('button', { name: /^search$/i })
    this.tenantsTable = page.locator('table')
    this.tenantRows = page.locator('table tbody tr')
    this.emptyState = page.locator('text=No tenants found')
    this.paginationInfo = page.locator('text=/Page \\d+ of \\d+/')
    this.previousButton = page.getByRole('button', { name: /previous/i })
    this.nextButton = page.getByRole('button', { name: /next/i })
    this.errorMessage = page.locator('.bg-destructive\\/10')

    // Create dialog - use actual field labels from the form
    this.createDialog = page.getByRole('dialog')
    this.identifierInput = page.getByLabel(/^identifier$/i)
    this.displayNameInput = page.getByLabel(/display name/i)
    this.logoUrlInput = page.getByLabel(/logo url/i)
    this.submitCreateButton = page.getByRole('button', { name: /^create$/i })
    this.cancelButton = page.getByRole('button', { name: /cancel/i })

    // Delete dialog
    this.deleteDialog = page.getByRole('alertdialog')
    this.confirmDeleteButton = this.deleteDialog.getByRole('button', { name: /delete|confirm/i })
  }

  async goto() {
    await this.page.goto('/portal/admin/tenants')
    await this.page.waitForLoadState('networkidle')
  }

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.searchButton.click()
    await this.page.waitForLoadState('networkidle')
  }

  async openCreateDialog() {
    await this.createTenantButton.click()
    await this.createDialog.waitFor({ state: 'visible' })
  }

  async fillCreateForm(identifier: string, displayName: string, logoUrl?: string) {
    await this.identifierInput.fill(identifier)
    await this.displayNameInput.fill(displayName)
    if (logoUrl) {
      await this.logoUrlInput.fill(logoUrl)
    }
  }

  async deleteTenant(index: number) {
    const row = this.tenantRows.nth(index)
    await row.getByRole('button', { name: /delete|remove/i }).click()
    await this.deleteDialog.waitFor({ state: 'visible' })
    await this.confirmDeleteButton.click()
  }

  async viewTenantDetails(index: number) {
    const row = this.tenantRows.nth(index)
    await row.getByRole('link').first().click()
  }

  getTenantRow(index: number) {
    return this.tenantRows.nth(index)
  }
}
