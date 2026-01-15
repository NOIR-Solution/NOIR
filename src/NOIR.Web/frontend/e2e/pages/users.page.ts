import { Page, Locator } from '@playwright/test'

export class UsersPage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly pageDescription: Locator
  readonly createUserButton: Locator
  readonly searchInput: Locator
  readonly searchButton: Locator
  readonly roleFilter: Locator
  readonly statusFilter: Locator
  readonly usersTable: Locator
  readonly userRows: Locator
  readonly emptyState: Locator
  readonly paginationInfo: Locator
  readonly errorMessage: Locator

  constructor(page: Page) {
    this.page = page
    this.pageTitle = page.getByRole('heading', { level: 1 })
    this.pageDescription = page.locator('p.text-muted-foreground').first()
    this.createUserButton = page.getByRole('button', { name: /create user/i })
    this.searchInput = page.getByPlaceholder(/search users/i)
    this.searchButton = page.getByRole('button', { name: /^search$/i })
    this.roleFilter = page.locator('button').filter({ hasText: /all|filter by role/i }).first()
    this.statusFilter = page.locator('button').filter({ hasText: /all|status/i }).first()
    this.usersTable = page.locator('table')
    this.userRows = page.locator('table tbody tr')
    this.emptyState = page.locator('text=No users found')
    this.paginationInfo = page.locator('text=/Page \\d+ of \\d+/')
    this.errorMessage = page.locator('.bg-destructive\\/10')
  }

  async goto() {
    await this.page.goto('/portal/admin/users')
    await this.page.waitForLoadState('networkidle')
  }

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.searchButton.click()
    await this.page.waitForLoadState('networkidle')
  }

  async openUserMenu(index: number) {
    const row = this.userRows.nth(index)
    await row.getByRole('button').last().click()
    await this.page.waitForTimeout(300)
  }

  async clickViewActivity(index: number) {
    await this.openUserMenu(index)
    await this.page.getByRole('menuitem', { name: /view activity/i }).click()
    await this.page.waitForLoadState('networkidle')
  }

  async clickEdit(index: number) {
    await this.openUserMenu(index)
    await this.page.getByRole('menuitem', { name: /edit/i }).click()
    await this.page.waitForTimeout(300)
  }

  async clickAssignRoles(index: number) {
    await this.openUserMenu(index)
    await this.page.getByRole('menuitem', { name: /assign roles/i }).click()
    await this.page.waitForTimeout(300)
  }

  getUserRow(index: number) {
    return this.userRows.nth(index)
  }

  async getUserByEmail(email: string) {
    return this.userRows.filter({ hasText: email }).first()
  }

  async getUserCount() {
    await this.page.waitForTimeout(1000)
    return this.userRows.count()
  }
}
