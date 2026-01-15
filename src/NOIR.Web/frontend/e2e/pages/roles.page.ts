import { Page, Locator } from '@playwright/test'

export class RolesPage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly pageDescription: Locator
  readonly createRoleButton: Locator
  readonly searchInput: Locator
  readonly searchButton: Locator
  readonly rolesTable: Locator
  readonly roleRows: Locator
  readonly emptyState: Locator
  readonly paginationInfo: Locator
  readonly previousButton: Locator
  readonly nextButton: Locator
  readonly errorMessage: Locator

  // Create dialog elements
  readonly createDialog: Locator
  readonly roleNameInput: Locator
  readonly descriptionInput: Locator
  readonly parentRoleSelect: Locator
  readonly colorSelect: Locator
  readonly submitCreateButton: Locator
  readonly cancelButton: Locator

  // Edit dialog elements
  readonly editDialog: Locator
  readonly editRoleNameInput: Locator
  readonly editDescriptionInput: Locator
  readonly editSubmitButton: Locator

  // Delete dialog elements
  readonly deleteDialog: Locator
  readonly confirmDeleteButton: Locator

  // Permissions dialog elements
  readonly permissionsDialog: Locator
  readonly permissionsSearchInput: Locator
  readonly applyTemplateButton: Locator
  readonly templateDropdown: Locator
  readonly selectAllButton: Locator
  readonly clearAllButton: Locator
  readonly savePermissionsButton: Locator
  readonly permissionCheckboxes: Locator
  readonly selectedCountText: Locator

  constructor(page: Page) {
    this.page = page
    this.pageTitle = page.getByRole('heading', { level: 1 })
    this.pageDescription = page.locator('p.text-muted-foreground').first()
    this.createRoleButton = page.getByRole('button', { name: /create role/i })
    this.searchInput = page.getByPlaceholder(/search roles/i)
    this.searchButton = page.getByRole('button', { name: /^search$/i })
    this.rolesTable = page.locator('table')
    this.roleRows = page.locator('table tbody tr')
    this.emptyState = page.locator('text=No roles found')
    this.paginationInfo = page.locator('text=/Page \\d+ of \\d+/')
    this.previousButton = page.getByRole('button', { name: /previous/i })
    this.nextButton = page.getByRole('button', { name: /next/i })
    this.errorMessage = page.locator('.bg-destructive\\/10')

    // Create dialog - use actual field labels from the form
    this.createDialog = page.getByRole('dialog')
    this.roleNameInput = page.getByLabel(/role name/i)
    this.descriptionInput = page.getByRole('textbox', { name: /description/i })
    this.parentRoleSelect = page.getByRole('combobox', { name: /parent role/i })
    this.colorSelect = page.locator('[data-radix-select-trigger]').filter({ hasText: /color|gray|red|blue/i })
    this.submitCreateButton = page.getByRole('button', { name: /^create$/i })
    this.cancelButton = page.getByRole('button', { name: /cancel/i })

    // Edit dialog
    this.editDialog = page.getByRole('dialog')
    this.editRoleNameInput = page.getByLabel(/role name/i)
    this.editDescriptionInput = page.getByRole('textbox', { name: /description/i })
    this.editSubmitButton = page.getByRole('button', { name: /^save$/i })

    // Delete dialog
    this.deleteDialog = page.getByRole('alertdialog')
    this.confirmDeleteButton = this.deleteDialog.getByRole('button', { name: /delete|confirm/i })

    // Permissions dialog
    this.permissionsDialog = page.getByRole('dialog')
    this.permissionsSearchInput = page.getByPlaceholder(/search permissions/i)
    this.applyTemplateButton = page.getByRole('button', { name: /apply template/i })
    this.templateDropdown = page.locator('[role="menu"]')
    this.selectAllButton = page.getByRole('button', { name: /select all/i })
    this.clearAllButton = page.getByRole('button', { name: /clear all/i })
    this.savePermissionsButton = page.getByRole('button', { name: /save permissions/i })
    this.permissionCheckboxes = page.locator('[role="checkbox"]')
    this.selectedCountText = page.locator('text=/\\d+ permissions selected/')
  }

  async goto() {
    await this.page.goto('/portal/admin/roles')
    await this.page.waitForLoadState('networkidle')
  }

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.searchButton.click()
    await this.page.waitForLoadState('networkidle')
  }

  async openCreateDialog() {
    await this.createRoleButton.click()
    await this.createDialog.waitFor({ state: 'visible' })
  }

  async fillCreateForm(name: string, description?: string) {
    await this.roleNameInput.fill(name)
    if (description) {
      await this.descriptionInput.fill(description)
    }
  }

  async createRole(name: string, description?: string) {
    await this.openCreateDialog()
    await this.fillCreateForm(name, description)
    await this.submitCreateButton.click()
  }

  async editRole(index: number) {
    const row = this.roleRows.nth(index)
    await row.getByRole('button', { name: /edit/i }).click()
    await this.editDialog.waitFor({ state: 'visible' })
  }

  async openPermissionsDialog(index: number) {
    const row = this.roleRows.nth(index)
    await row.getByRole('button', { name: /permissions|key/i }).click()
    await this.permissionsDialog.waitFor({ state: 'visible' })
  }

  async deleteRole(index: number) {
    const row = this.roleRows.nth(index)
    await row.getByRole('button', { name: /delete|trash/i }).click()
    await this.deleteDialog.waitFor({ state: 'visible' })
    await this.confirmDeleteButton.click()
  }

  getRoleRow(index: number) {
    return this.roleRows.nth(index)
  }

  async getRoleByName(name: string) {
    return this.roleRows.filter({ hasText: name }).first()
  }
}
