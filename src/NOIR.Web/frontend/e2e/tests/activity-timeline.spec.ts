import { test, expect } from '@playwright/test'
import { ActivityTimelinePage } from '../pages/activity-timeline.page'

test.describe('Activity Timeline Page', () => {
  let activityPage: ActivityTimelinePage

  test.beforeEach(async ({ page }) => {
    activityPage = new ActivityTimelinePage(page)
    await activityPage.goto()
  })

  test('displays page title', async () => {
    await expect(activityPage.pageTitle).toBeVisible()
    await expect(activityPage.pageTitle).toContainText(/activity timeline/i)
  })

  test('displays page description', async () => {
    await expect(activityPage.pageDescription).toBeVisible()
  })

  test('displays refresh button', async () => {
    await expect(activityPage.refreshButton).toBeVisible()
  })

  test('displays search input', async () => {
    await expect(activityPage.searchInput).toBeVisible()
  })

  test('displays search button', async () => {
    await expect(activityPage.searchButton).toBeVisible()
  })

  test('displays context filter dropdown', async () => {
    await expect(activityPage.contextSelect).toBeVisible()
  })

  test('displays action filter dropdown', async () => {
    await expect(activityPage.actionSelect).toBeVisible()
  })

  test('displays failed only toggle', async () => {
    await expect(activityPage.failedOnlyToggle).toBeVisible()
  })

  test('displays date range picker', async () => {
    await expect(activityPage.dateRangePicker).toBeVisible()
  })

  test('page loads without errors', async () => {
    const hasError = await activityPage.errorMessage.isVisible()
    expect(hasError).toBeFalsy()
  })
})

test.describe('Activity Timeline Filters', () => {
  let activityPage: ActivityTimelinePage

  test.beforeEach(async ({ page }) => {
    activityPage = new ActivityTimelinePage(page)
    await activityPage.goto()
  })

  test('search functionality filters results', async ({ page }) => {
    // Wait for initial load
    await page.waitForTimeout(1000)

    // Search for something
    await activityPage.search('admin')

    // Page should still function after search
    await expect(activityPage.searchInput).toHaveValue('admin')
  })

  test('context filter dropdown opens and shows options', async ({ page }) => {
    await activityPage.contextSelect.click()
    await page.waitForTimeout(300)

    // Should see "All Contexts" option
    await expect(page.getByRole('option', { name: /all contexts/i })).toBeVisible()
  })

  test('action filter dropdown opens and shows operations', async ({ page }) => {
    await activityPage.actionSelect.click()
    await page.waitForTimeout(300)

    // Should see operation types
    await expect(page.getByRole('option', { name: /all actions/i })).toBeVisible()
    await expect(page.getByRole('option', { name: /create/i })).toBeVisible()
    await expect(page.getByRole('option', { name: /update/i })).toBeVisible()
    await expect(page.getByRole('option', { name: /delete/i })).toBeVisible()
  })

  test('failed only toggle can be switched', async ({ page }) => {
    // Initially should be unchecked
    const initialState = await activityPage.failedOnlyToggle.isChecked()

    // Toggle it
    await activityPage.toggleFailedOnly()

    // Should be in opposite state
    const newState = await activityPage.failedOnlyToggle.isChecked()
    expect(newState).toBe(!initialState)
  })

  test('clear button appears when filters are active', async ({ page }) => {
    // Initially clear button might not be visible
    const initialVisible = await activityPage.clearButton.isVisible()

    // Apply a filter
    await activityPage.toggleFailedOnly()

    // Clear button should now be visible
    await expect(activityPage.clearButton).toBeVisible()
  })

  test('clear button resets all filters', async ({ page }) => {
    // Apply some filters
    await activityPage.searchInput.fill('test')
    await activityPage.toggleFailedOnly()
    await page.waitForTimeout(300)

    // Click clear
    await activityPage.clearFilters()

    // Search input should be empty
    await expect(activityPage.searchInput).toHaveValue('')

    // Failed only should be unchecked
    const failedOnly = await activityPage.failedOnlyToggle.isChecked()
    expect(failedOnly).toBeFalsy()
  })
})

test.describe('Date Range Picker', () => {
  let activityPage: ActivityTimelinePage

  test.beforeEach(async ({ page }) => {
    activityPage = new ActivityTimelinePage(page)
    await activityPage.goto()
  })

  test('date range picker opens calendar popover', async ({ page }) => {
    await activityPage.openDateRangePicker()

    // Calendar should be visible
    await expect(page.locator('[role="dialog"]').or(page.locator('.rdp'))).toBeVisible()
  })

  test('date range picker shows calendar with navigation', async ({ page }) => {
    await activityPage.openDateRangePicker()
    await page.waitForTimeout(500)

    // Calendar popover should be visible with navigation buttons
    const popover = page.locator('[role="dialog"], .rdp, [class*="calendar"]').filter({ has: page.locator('button') })
    await expect(popover).toBeVisible()

    // Should have navigation buttons (chevrons) for changing months
    const navButtons = page.locator('button').filter({ has: page.locator('svg') })
    const navCount = await navButtons.count()
    expect(navCount).toBeGreaterThan(0)
  })

  test('date range picker has clear and apply buttons', async ({ page }) => {
    await activityPage.openDateRangePicker()
    await page.waitForTimeout(300)

    // Should have Clear and Apply buttons
    await expect(page.getByRole('button', { name: /clear/i }).last()).toBeVisible()
    await expect(page.getByRole('button', { name: /apply/i })).toBeVisible()
  })

  test('clicking apply closes the date picker', async ({ page }) => {
    await activityPage.openDateRangePicker()
    await page.waitForTimeout(300)

    // Click Apply
    await page.getByRole('button', { name: /apply/i }).click()
    await page.waitForTimeout(300)

    // Popover should close
    const popover = page.locator('[role="dialog"]').filter({ has: page.locator('.rdp, [class*="calendar"]') })
    await expect(popover).not.toBeVisible()
  })
})

test.describe('User Filter from URL', () => {
  let activityPage: ActivityTimelinePage

  test.beforeEach(async ({ page }) => {
    activityPage = new ActivityTimelinePage(page)
  })

  test('displays user filter banner when userId is in URL', async ({ page }) => {
    await activityPage.gotoWithUserId('test-user-id', 'test@example.com')

    // User filter banner should be visible
    await expect(activityPage.userFilterBanner).toBeVisible()
    await expect(page.getByText('test@example.com')).toBeVisible()
  })

  test('user filter banner has clear button', async ({ page }) => {
    await activityPage.gotoWithUserId('test-user-id', 'test@example.com')

    // Clear user filter button should be visible
    await expect(activityPage.clearUserFilterButton).toBeVisible()
  })

  test('clearing user filter removes URL params', async ({ page }) => {
    await activityPage.gotoWithUserId('test-user-id', 'test@example.com')

    // Clear user filter
    await activityPage.clearUserFilter()

    // URL should no longer have userId param
    await expect(page).not.toHaveURL(/userId=/)

    // User filter banner should not be visible
    await expect(activityPage.userFilterBanner).not.toBeVisible()
  })
})

test.describe('Activity Timeline Details', () => {
  let activityPage: ActivityTimelinePage

  test.beforeEach(async ({ page }) => {
    activityPage = new ActivityTimelinePage(page)
    await activityPage.goto()
  })

  test('clicking entry opens details dialog', async ({ page }) => {
    // Wait for entries to load
    await page.waitForTimeout(2000)

    const entryCount = await activityPage.getEntryCount()

    if (entryCount > 0) {
      // Click first entry
      await activityPage.clickEntry(0)

      // Details dialog should be visible
      await expect(activityPage.detailsDialog).toBeVisible()
    }
  })

  test('details dialog has tabs for HTTP, Handler, Database, Raw', async ({ page }) => {
    // Wait for entries to load
    await page.waitForTimeout(2000)

    const entryCount = await activityPage.getEntryCount()

    if (entryCount > 0) {
      await activityPage.clickEntry(0)

      // Should have tabs
      await expect(activityPage.httpTab).toBeVisible()
      await expect(activityPage.handlerTab).toBeVisible()
      await expect(activityPage.databaseTab).toBeVisible()
      await expect(activityPage.rawTab).toBeVisible()
    }
  })

  test('clicking different tabs shows different content', async ({ page }) => {
    // Wait for entries to load
    await page.waitForTimeout(2000)

    const entryCount = await activityPage.getEntryCount()

    if (entryCount > 0) {
      await activityPage.clickEntry(0)

      // Click Handler tab
      await activityPage.handlerTab.click()
      await page.waitForTimeout(300)

      // Handler tab should be selected
      await expect(activityPage.handlerTab).toHaveAttribute('data-state', 'active')

      // Click Database tab
      await activityPage.databaseTab.click()
      await page.waitForTimeout(300)

      // Database tab should be selected
      await expect(activityPage.databaseTab).toHaveAttribute('data-state', 'active')
    }
  })
})

test.describe('Activity Timeline Refresh', () => {
  let activityPage: ActivityTimelinePage

  test.beforeEach(async ({ page }) => {
    activityPage = new ActivityTimelinePage(page)
    await activityPage.goto()
  })

  test('refresh button triggers data reload', async ({ page }) => {
    // Wait for initial load
    await page.waitForTimeout(1000)

    // Click refresh
    await activityPage.refreshButton.click()

    // Page should still function
    await expect(activityPage.pageTitle).toBeVisible()
  })
})
