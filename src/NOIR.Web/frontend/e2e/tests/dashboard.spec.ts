import { test, expect } from '@playwright/test'
import { DashboardPage } from '../pages'

test.describe('Dashboard', () => {
  let dashboardPage: DashboardPage

  test.beforeEach(async ({ page }) => {
    dashboardPage = new DashboardPage(page)
    await dashboardPage.goto()
  })

  test('displays dashboard page title', async () => {
    await expect(dashboardPage.pageTitle).toBeVisible()
    await expect(dashboardPage.pageTitle).toContainText(/dashboard/i)
  })

  test('displays welcome message with user name', async () => {
    await expect(dashboardPage.welcomeMessage).toBeVisible()
    const text = await dashboardPage.welcomeMessage.textContent()
    expect(text).toMatch(/welcome/i)
  })

  test('displays quick links card', async () => {
    await expect(dashboardPage.quickLinksCard).toBeVisible()
  })

  test('displays API docs link', async ({ page }) => {
    const apiLink = page.getByRole('link', { name: /api/i })
    await expect(apiLink).toBeVisible()
    await expect(apiLink).toHaveAttribute('href', /api\/docs/)
  })

  test('displays Hangfire link', async ({ page }) => {
    const hangfireLink = page.getByRole('link', { name: /hangfire/i })
    await expect(hangfireLink).toBeVisible()
    await expect(hangfireLink).toHaveAttribute('href', /hangfire/)
  })

  test('displays user profile information', async ({ page }) => {
    // Check profile section exists
    const profileSection = page.locator('text=Your Profile').locator('..')
    await expect(profileSection).toBeVisible()

    // Check email is displayed
    const emailRow = page.locator('text=Email:')
    await expect(emailRow).toBeVisible()

    // Check tenant is displayed
    const tenantRow = page.locator('text=Tenant:')
    await expect(tenantRow).toBeVisible()

    // Check roles are displayed
    const rolesRow = page.locator('text=Roles:')
    await expect(rolesRow).toBeVisible()
  })

  test('displays admin user email in profile', async ({ page }) => {
    // The admin user should see their email somewhere on the page
    // Check in the profile section or sidebar
    const emailText = page.locator('text=admin@noir.local')
    await expect(emailText.first()).toBeVisible()
  })

  test('API docs link opens in new tab', async ({ page }) => {
    const apiLink = page.getByRole('link', { name: /api/i })
    await expect(apiLink).toHaveAttribute('target', '_blank')
  })

  test('Hangfire link opens in new tab', async ({ page }) => {
    const hangfireLink = page.getByRole('link', { name: /hangfire/i })
    await expect(hangfireLink).toHaveAttribute('target', '_blank')
  })

  test('dashboard loads without errors', async ({ page }) => {
    // Check no error states are visible
    const errorElements = page.locator('.text-destructive, .bg-destructive')
    const errorCount = await errorElements.count()
    expect(errorCount).toBe(0)
  })
})
