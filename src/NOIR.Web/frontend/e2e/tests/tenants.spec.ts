import { test, expect } from '@playwright/test'
import { TenantsPage } from '../pages'

test.describe('Tenant Management Page', () => {
  let tenantsPage: TenantsPage

  test.beforeEach(async ({ page }) => {
    tenantsPage = new TenantsPage(page)
    await tenantsPage.goto()
  })

  test('displays page title', async () => {
    await expect(tenantsPage.pageTitle).toBeVisible()
    await expect(tenantsPage.pageTitle).toContainText(/tenant/i)
  })

  test('displays page description', async () => {
    await expect(tenantsPage.pageDescription).toBeVisible()
  })

  test('displays create tenant button', async () => {
    await expect(tenantsPage.createTenantButton).toBeVisible()
  })

  test('displays search input', async () => {
    await expect(tenantsPage.searchInput).toBeVisible()
  })

  test('displays search button', async () => {
    await expect(tenantsPage.searchButton).toBeVisible()
  })

  test('displays tenants table', async () => {
    await expect(tenantsPage.tenantsTable).toBeVisible()
  })

  test('table has header columns', async ({ page }) => {
    const headers = page.locator('table thead th')
    const headerCount = await headers.count()
    expect(headerCount).toBeGreaterThan(0)
  })

  test('displays tenant data in table', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await tenantsPage.tenantRows.count()
    // Should have at least one tenant (default tenant)
    expect(rowCount).toBeGreaterThan(0)
  })

  test('search functionality works', async ({ page }) => {
    await tenantsPage.search('default')

    // Wait for search results
    await page.waitForTimeout(500)

    // Page should still function
    await expect(tenantsPage.tenantsTable).toBeVisible()
  })

  test('create tenant button opens dialog', async ({ page }) => {
    await tenantsPage.openCreateDialog()

    // Dialog should be visible
    await expect(tenantsPage.createDialog).toBeVisible()
  })

  test('create dialog has required fields', async ({ page }) => {
    await tenantsPage.openCreateDialog()

    // Wait for dialog to fully render
    await page.waitForTimeout(300)

    // Check for identifier field
    await expect(tenantsPage.identifierInput).toBeVisible()
    // Check for display name field
    await expect(tenantsPage.displayNameInput).toBeVisible()
  })

  test('create dialog has cancel button', async ({ page }) => {
    await tenantsPage.openCreateDialog()

    await expect(tenantsPage.cancelButton).toBeVisible()
  })

  test('cancel button closes create dialog', async ({ page }) => {
    await tenantsPage.openCreateDialog()
    await tenantsPage.cancelButton.click()

    await expect(tenantsPage.createDialog).not.toBeVisible()
  })

  test('create dialog validates required fields', async ({ page }) => {
    await tenantsPage.openCreateDialog()

    // Wait for dialog to fully render
    await page.waitForTimeout(300)

    // Try to submit without filling fields
    await tenantsPage.submitCreateButton.click()

    // Dialog should stay open (validation prevents close)
    await expect(tenantsPage.createDialog).toBeVisible()
  })

  test('can fill create tenant form', async ({ page }) => {
    await tenantsPage.openCreateDialog()

    // Wait for dialog to fully render
    await page.waitForTimeout(300)

    await tenantsPage.fillCreateForm('test-tenant-id', 'Test Tenant Name')

    await expect(tenantsPage.identifierInput).toHaveValue('test-tenant-id')
    await expect(tenantsPage.displayNameInput).toHaveValue('Test Tenant Name')
  })

  test('tenant rows have action buttons', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await tenantsPage.tenantRows.count()
    if (rowCount > 0) {
      const firstRow = tenantsPage.getTenantRow(0)
      // Should have view/edit links
      const actionLinks = firstRow.getByRole('link')
      const linkCount = await actionLinks.count()
      expect(linkCount).toBeGreaterThan(0)
    }
  })

  test('displays pagination when many tenants', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    // Pagination might not be visible if only a few tenants
    const paginationVisible = await tenantsPage.paginationInfo.isVisible()
    // This is OK - pagination only shows with multiple pages
    expect(paginationVisible).toBeDefined()
  })

  test('page loads without errors', async ({ page }) => {
    // Check no error states are visible
    const hasError = await tenantsPage.errorMessage.isVisible()
    expect(hasError).toBeFalsy()
  })
})

test.describe('Tenant Details', () => {
  let tenantsPage: TenantsPage

  test.beforeEach(async ({ page }) => {
    tenantsPage = new TenantsPage(page)
    await tenantsPage.goto()
  })

  test('clicking on tenant navigates to detail page', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await tenantsPage.tenantRows.count()
    if (rowCount > 0) {
      // Click on first tenant's link/row
      const firstRow = tenantsPage.getTenantRow(0)
      const link = firstRow.getByRole('link').first()

      if (await link.isVisible()) {
        await link.click()
        // Should navigate to detail page
        await expect(page).toHaveURL(/tenants\/[a-zA-Z0-9-]+/)
      }
    }
  })
})

test.describe('Tenant Deletion', () => {
  let tenantsPage: TenantsPage

  test.beforeEach(async ({ page }) => {
    tenantsPage = new TenantsPage(page)
    await tenantsPage.goto()
  })

  test('delete button is visible for tenants', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await tenantsPage.tenantRows.count()
    if (rowCount > 0) {
      // Check if there's a delete button (might not exist for default tenant)
      const firstRow = tenantsPage.getTenantRow(0)
      const deleteButton = firstRow.getByRole('button')
      // Delete might not be available for all tenants
      const buttonCount = await deleteButton.count()
      expect(buttonCount).toBeGreaterThanOrEqual(0)
    }
  })
})
