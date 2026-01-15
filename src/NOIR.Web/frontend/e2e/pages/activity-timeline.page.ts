import { Page, Locator } from '@playwright/test'

export class ActivityTimelinePage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly pageDescription: Locator
  readonly refreshButton: Locator
  readonly searchInput: Locator
  readonly searchButton: Locator
  readonly contextSelect: Locator
  readonly actionSelect: Locator
  readonly failedOnlyToggle: Locator
  readonly dateRangePicker: Locator
  readonly clearButton: Locator
  readonly timelineEntries: Locator
  readonly emptyState: Locator
  readonly paginationInfo: Locator
  readonly userFilterBanner: Locator
  readonly clearUserFilterButton: Locator
  readonly errorMessage: Locator
  readonly loadingSkeletons: Locator

  // Details dialog elements
  readonly detailsDialog: Locator
  readonly detailsDialogTitle: Locator
  readonly httpTab: Locator
  readonly handlerTab: Locator
  readonly databaseTab: Locator
  readonly rawTab: Locator

  constructor(page: Page) {
    this.page = page
    this.pageTitle = page.getByRole('heading', { level: 1 })
    this.pageDescription = page.locator('p.text-muted-foreground').first()
    this.refreshButton = page.getByRole('button', { name: /refresh/i })
    this.searchInput = page.getByPlaceholder(/search by id, user/i)
    this.searchButton = page.getByRole('button', { name: /^search$/i })
    this.contextSelect = page.locator('button').filter({ hasText: /all contexts/i }).first()
    this.actionSelect = page.locator('button').filter({ hasText: /all actions/i }).first()
    this.failedOnlyToggle = page.locator('#only-failed')
    this.dateRangePicker = page.getByRole('button', { name: /date range|pick a date/i })
    this.clearButton = page.getByRole('button', { name: /^clear$/i })
    this.timelineEntries = page.locator('button').filter({ has: page.locator('.font-medium') }).filter({ hasNot: page.locator('[role="dialog"]') })
    this.emptyState = page.locator('text=No activity found')
    this.paginationInfo = page.locator('text=/Page \\d+ of \\d+/')
    this.userFilterBanner = page.locator('text=Showing activity for user')
    this.clearUserFilterButton = page.getByRole('button', { name: /clear user filter/i })
    this.errorMessage = page.locator('.bg-destructive\\/10')
    this.loadingSkeletons = page.locator('.animate-pulse')

    // Details dialog
    this.detailsDialog = page.getByRole('dialog')
    this.detailsDialogTitle = this.detailsDialog.getByRole('heading')
    this.httpTab = page.getByRole('tab', { name: /http/i })
    this.handlerTab = page.getByRole('tab', { name: /handler/i })
    this.databaseTab = page.getByRole('tab', { name: /database/i })
    this.rawTab = page.getByRole('tab', { name: /raw/i })
  }

  async goto() {
    await this.page.goto('/portal/admin/activity-timeline')
    await this.page.waitForLoadState('networkidle')
  }

  async gotoWithUserId(userId: string, userEmail?: string) {
    let url = `/portal/admin/activity-timeline?userId=${encodeURIComponent(userId)}`
    if (userEmail) {
      url += `&userEmail=${encodeURIComponent(userEmail)}`
    }
    await this.page.goto(url)
    await this.page.waitForLoadState('networkidle')
  }

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.searchButton.click()
    await this.page.waitForTimeout(500)
  }

  async selectContext(context: string) {
    await this.contextSelect.click()
    await this.page.getByRole('option', { name: context }).click()
    await this.page.waitForTimeout(500)
  }

  async selectAction(action: string) {
    await this.actionSelect.click()
    await this.page.getByRole('option', { name: action }).click()
    await this.page.waitForTimeout(500)
  }

  async toggleFailedOnly() {
    await this.failedOnlyToggle.click()
    await this.page.waitForTimeout(500)
  }

  async openDateRangePicker() {
    await this.dateRangePicker.click()
    await this.page.waitForTimeout(300)
  }

  async selectDateRange(startDate: Date, endDate: Date) {
    await this.openDateRangePicker()
    // The calendar should be visible now
    // Click on start date
    const startDay = startDate.getDate().toString()
    const endDay = endDate.getDate().toString()

    // Click on start day
    await this.page.locator('button[name="day"]').filter({ hasText: new RegExp(`^${startDay}$`) }).first().click()

    // Click on end day (if different from start)
    if (startDate.getDate() !== endDate.getDate()) {
      await this.page.locator('button[name="day"]').filter({ hasText: new RegExp(`^${endDay}$`) }).first().click()
    }

    // Click Apply button
    await this.page.getByRole('button', { name: /apply/i }).click()
    await this.page.waitForTimeout(500)
  }

  async clearFilters() {
    const clearVisible = await this.clearButton.isVisible()
    if (clearVisible) {
      await this.clearButton.click()
      await this.page.waitForTimeout(500)
    }
  }

  async clearUserFilter() {
    await this.clearUserFilterButton.click()
    await this.page.waitForTimeout(500)
  }

  async clickEntry(index: number) {
    const entries = this.page.locator('.rounded-lg.border.transition-all').filter({ has: this.page.locator('.font-medium') })
    await entries.nth(index).click()
    await this.detailsDialog.waitFor({ state: 'visible', timeout: 5000 })
  }

  async getEntryCount() {
    // Wait for loading to complete
    await this.page.waitForTimeout(1000)
    const entries = this.page.locator('.rounded-lg.border.transition-all').filter({ has: this.page.locator('.font-medium') })
    return entries.count()
  }

  getEntry(index: number) {
    return this.page.locator('.rounded-lg.border.transition-all').filter({ has: this.page.locator('.font-medium') }).nth(index)
  }
}
