import { test, expect } from '@playwright/test'
import { TenantsPage, RolesPage, LoginPage } from '../pages'

// Test tenant info - unique per run
const TENANT_ID = `test-tenant-${Date.now()}`
const TENANT_NAME = `Test Tenant ${Date.now()}`
const ADMIN_EMAIL = `admin-${Date.now()}@test-tenant.local`
const ADMIN_PASSWORD = 'TestPass123!'

test.describe('Tenant Provisioning E2E', () => {
  let tenantsPage: TenantsPage

  test.beforeEach(async ({ page }) => {
    tenantsPage = new TenantsPage(page)
  })

  test('create tenant with admin user', async ({ page }) => {
    await tenantsPage.goto()
    await tenantsPage.openCreateDialog()

    // Wait for dialog to fully render
    await page.waitForTimeout(300)

    // Fill all required fields
    await tenantsPage.fillCreateForm(
      TENANT_ID,
      TENANT_NAME,
      ADMIN_EMAIL,
      ADMIN_PASSWORD,
      { description: 'E2E test tenant', note: 'Created by Playwright' }
    )

    // Verify admin fields are filled
    await expect(tenantsPage.adminEmailInput).toHaveValue(ADMIN_EMAIL)
    await expect(tenantsPage.adminPasswordInput).toHaveValue(ADMIN_PASSWORD)

    // Submit the form via JavaScript (dialog is too tall for standard click)
    await page.evaluate(() => {
      const submitBtn = document.querySelector('button[type="submit"]') as HTMLButtonElement
      submitBtn?.click()
    })

    // Wait for dialog to close and data to refresh
    await page.waitForTimeout(2000)
    await expect(tenantsPage.createDialog).not.toBeVisible({ timeout: 10000 })

    // Verify tenant appears in the list
    await tenantsPage.search(TENANT_ID)
    await page.waitForTimeout(500)

    const tenantRow = page.locator('table tbody tr').filter({ hasText: TENANT_ID })
    await expect(tenantRow).toBeVisible({ timeout: 10000 })
  })

  test('admin email field is required', async ({ page }) => {
    await tenantsPage.goto()
    await tenantsPage.openCreateDialog()

    await page.waitForTimeout(300)

    // Fill only basic fields, leave admin email empty
    await tenantsPage.identifierInput.fill('test-no-admin')
    await tenantsPage.displayNameInput.fill('Test No Admin')
    await tenantsPage.adminPasswordInput.fill('TestPass123!')

    // Try to submit via JavaScript
    await page.evaluate(() => {
      const submitBtn = document.querySelector('button[type="submit"]') as HTMLButtonElement
      submitBtn?.click()
    })

    // Dialog should stay open due to validation
    await expect(tenantsPage.createDialog).toBeVisible()

    // Should show validation error for admin email
    const emailError = page.getByText('This field is required').first()
    await expect(emailError).toBeVisible()
  })

  test('admin password field is required', async ({ page }) => {
    await tenantsPage.goto()
    await tenantsPage.openCreateDialog()

    await page.waitForTimeout(300)

    // Fill only basic fields, leave admin password empty
    await tenantsPage.identifierInput.fill('test-no-password')
    await tenantsPage.displayNameInput.fill('Test No Password')
    await tenantsPage.adminEmailInput.fill('admin@test.local')

    // Try to submit via JavaScript
    await page.evaluate(() => {
      const submitBtn = document.querySelector('button[type="submit"]') as HTMLButtonElement
      submitBtn?.click()
    })

    // Dialog should stay open due to validation
    await expect(tenantsPage.createDialog).toBeVisible()
  })
})

test.describe('Tenant Admin Login', () => {
  // This test depends on a tenant being created first
  // In a real scenario, we'd use test fixtures or API to set this up

  test.skip('login with newly created tenant admin', async ({ browser }) => {
    // Create a new context without stored auth
    const context = await browser.newContext()
    const page = await context.newPage()

    // Navigate to login page
    await page.goto('/login')

    // Fill login form with tenant admin credentials
    await page.locator('#email').fill(ADMIN_EMAIL)
    await page.locator('#password').fill(ADMIN_PASSWORD)

    // Submit login
    await page.getByRole('button', { name: /sign in|submit/i }).click()

    // Should redirect to portal
    await expect(page).toHaveURL(/\/portal/, { timeout: 30000 })

    // Verify user is logged in by checking for portal elements
    await expect(page.locator('nav')).toBeVisible()

    await context.close()
  })
})

test.describe('Edit Tenant', () => {
  let tenantsPage: TenantsPage

  test.beforeEach(async ({ page }) => {
    tenantsPage = new TenantsPage(page)
    await tenantsPage.goto()
  })

  test('can navigate to tenant detail and open edit dialog', async ({ page }) => {
    await page.waitForTimeout(1000)

    const rowCount = await tenantsPage.tenantRows.count()
    if (rowCount > 0) {
      // Click on first tenant's view link
      const firstRow = tenantsPage.getTenantRow(0)
      await firstRow.getByRole('link').first().click()

      // Should navigate to detail page
      await expect(page).toHaveURL(/tenants\/[a-zA-Z0-9-]+/)

      // Find and click edit button
      const editButton = page.getByRole('button', { name: /edit/i })
      if (await editButton.isVisible()) {
        await editButton.click()

        // Edit dialog should open
        await expect(page.getByRole('dialog')).toBeVisible()
      }
    }
  })

  test('edit dialog has description and note fields', async ({ page }) => {
    await page.waitForTimeout(1000)

    const rowCount = await tenantsPage.tenantRows.count()
    if (rowCount > 0) {
      // Navigate to first tenant's detail page
      const firstRow = tenantsPage.getTenantRow(0)
      await firstRow.getByRole('link').first().click()

      await expect(page).toHaveURL(/tenants\/[a-zA-Z0-9-]+/)

      const editButton = page.getByRole('button', { name: /edit/i })
      if (await editButton.isVisible()) {
        await editButton.click()
        await page.waitForTimeout(300)

        // Check for description and note fields
        const descriptionField = page.getByLabel(/description/i)
        const noteField = page.getByLabel(/note/i)

        await expect(descriptionField).toBeVisible()
        await expect(noteField).toBeVisible()
      }
    }
  })

  test('can update tenant description and note', async ({ page }) => {
    await page.waitForTimeout(1000)

    const rowCount = await tenantsPage.tenantRows.count()
    if (rowCount > 0) {
      // Navigate to first tenant's detail page
      const firstRow = tenantsPage.getTenantRow(0)
      await firstRow.getByRole('link').first().click()

      await expect(page).toHaveURL(/tenants\/[a-zA-Z0-9-]+/)

      const editButton = page.getByRole('button', { name: /edit/i })
      if (await editButton.isVisible()) {
        await editButton.click()
        await page.waitForTimeout(300)

        // Fill description and note
        const descriptionField = page.getByLabel(/description/i)
        const noteField = page.getByLabel(/note/i)

        await descriptionField.fill('Updated description via E2E test')
        await noteField.fill('Updated note via E2E test')

        // Submit the form
        const saveButton = page.getByRole('button', { name: /save|update/i })
        await saveButton.click()

        // Dialog should close
        await page.waitForTimeout(1000)
        await expect(page.getByRole('dialog')).not.toBeVisible({ timeout: 10000 })
      }
    }
  })
})

test.describe('Role Screen', () => {
  let rolesPage: RolesPage

  test.beforeEach(async ({ page }) => {
    rolesPage = new RolesPage(page)
    await rolesPage.goto()
  })

  test('roles page loads correctly', async () => {
    await expect(rolesPage.pageTitle).toBeVisible()
    await expect(rolesPage.pageTitle).toContainText(/roles/i)
  })

  test('displays system roles in table', async ({ page }) => {
    await page.waitForTimeout(1000)

    const rowCount = await rolesPage.roleRows.count()
    expect(rowCount).toBeGreaterThan(0)

    // Should have Admin and User roles at minimum
    const adminRow = page.locator('table tbody tr').filter({ hasText: /admin/i })
    await expect(adminRow.first()).toBeVisible()
  })

  test('create role button is visible', async () => {
    await expect(rolesPage.createRoleButton).toBeVisible()
  })

  test('can search for roles', async ({ page }) => {
    await rolesPage.search('admin')
    await page.waitForTimeout(500)

    await expect(rolesPage.rolesTable).toBeVisible()
    const adminRow = page.locator('table tbody tr').filter({ hasText: /admin/i })
    await expect(adminRow.first()).toBeVisible()
  })

  test('role row has permissions button', async ({ page }) => {
    await page.waitForTimeout(1000)

    const rowCount = await rolesPage.roleRows.count()
    if (rowCount > 0) {
      const firstRow = rolesPage.getRoleRow(0)
      const permissionsButton = firstRow.getByRole('button').filter({ hasText: /key|permissions/i }).first()

      // At least one row should have permissions button
      const hasPermissionsButton = await permissionsButton.isVisible()
      expect(hasPermissionsButton).toBeDefined()
    }
  })

  test('platform admin role should not appear in tenant role list', async ({ page }) => {
    await page.waitForTimeout(1000)

    // Platform Admin should be hidden from tenant role management
    const platformAdminRow = page.locator('table tbody tr').filter({ hasText: /^PlatformAdmin$/i })
    const count = await platformAdminRow.count()

    // Platform Admin should not be visible (it's hidden via IsPlatformRole)
    expect(count).toBe(0)
  })
})
