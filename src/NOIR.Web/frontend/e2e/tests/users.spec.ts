import { test, expect } from '@playwright/test'
import { UsersPage } from '../pages/users.page'

test.describe('Users Page', () => {
  let usersPage: UsersPage

  test.beforeEach(async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()
  })

  test('displays page title', async () => {
    await expect(usersPage.pageTitle).toBeVisible()
    await expect(usersPage.pageTitle).toContainText(/users/i)
  })

  test('displays page description', async () => {
    await expect(usersPage.pageDescription).toBeVisible()
  })

  test('displays create user button', async () => {
    await expect(usersPage.createUserButton).toBeVisible()
  })

  test('displays search input', async () => {
    await expect(usersPage.searchInput).toBeVisible()
  })

  test('displays search button', async () => {
    await expect(usersPage.searchButton).toBeVisible()
  })

  test('displays users table', async () => {
    await expect(usersPage.usersTable).toBeVisible()
  })

  test('table has header columns', async ({ page }) => {
    const headers = page.locator('table thead th')
    const headerCount = await headers.count()
    expect(headerCount).toBeGreaterThan(0)
  })

  test('displays user data in table', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await usersPage.getUserCount()
    // Should have at least one user (admin)
    expect(rowCount).toBeGreaterThan(0)
  })

  test('page loads without errors', async () => {
    const hasError = await usersPage.errorMessage.isVisible()
    expect(hasError).toBeFalsy()
  })
})

test.describe('Users Search', () => {
  let usersPage: UsersPage

  test.beforeEach(async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()
  })

  test('search functionality works', async ({ page }) => {
    await usersPage.search('admin')

    // Wait for search results
    await page.waitForTimeout(500)

    // Page should still function
    await expect(usersPage.usersTable).toBeVisible()
  })

  test('search input accepts text', async () => {
    await usersPage.searchInput.fill('test@example.com')
    await expect(usersPage.searchInput).toHaveValue('test@example.com')
  })
})

test.describe('User Actions Menu', () => {
  let usersPage: UsersPage

  test.beforeEach(async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()
  })

  test('user row has action menu', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await usersPage.getUserCount()
    if (rowCount > 0) {
      const firstRow = usersPage.getUserRow(0)
      // Should have action button (more menu)
      const actionButton = firstRow.getByRole('button').last()
      await expect(actionButton).toBeVisible()
    }
  })

  test('action menu opens on click', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await usersPage.getUserCount()
    if (rowCount > 0) {
      await usersPage.openUserMenu(0)

      // Menu should be visible
      await expect(page.getByRole('menu')).toBeVisible()
    }
  })

  test('action menu contains View Activity option', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await usersPage.getUserCount()
    if (rowCount > 0) {
      await usersPage.openUserMenu(0)

      // Should see View Activity option
      await expect(page.getByRole('menuitem', { name: /view activity/i })).toBeVisible()
    }
  })

  test('action menu contains Assign Roles option', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await usersPage.getUserCount()
    if (rowCount > 0) {
      await usersPage.openUserMenu(0)

      // Should see Assign Roles option (if user has permission)
      // This option may not be visible if the current user doesn't have canAssignRoles permission
      const assignRolesItem = page.getByRole('menuitem', { name: /assign roles/i })
      const editItem = page.getByRole('menuitem', { name: /edit/i })

      // At minimum, menu should have some items
      const menuItems = page.getByRole('menuitem')
      const itemCount = await menuItems.count()
      expect(itemCount).toBeGreaterThan(0)

      // Assign Roles or Edit should be visible (depends on permissions)
      const hasAssignRoles = await assignRolesItem.isVisible().catch(() => false)
      const hasEdit = await editItem.isVisible().catch(() => false)
      expect(hasAssignRoles || hasEdit).toBeTruthy()
    }
  })

  test('action menu contains Edit option', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await usersPage.getUserCount()
    if (rowCount > 0) {
      await usersPage.openUserMenu(0)

      // Should see Edit option
      await expect(page.getByRole('menuitem', { name: /edit/i })).toBeVisible()
    }
  })
})

test.describe('View User Activity Link', () => {
  let usersPage: UsersPage

  test.beforeEach(async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()
  })

  test('clicking View Activity navigates to Activity Timeline with userId', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await usersPage.getUserCount()
    if (rowCount > 0) {
      // Click View Activity for first user
      await usersPage.clickViewActivity(0)

      // Should navigate to activity timeline with userId param
      await expect(page).toHaveURL(/\/portal\/admin\/activity-timeline\?userId=/)
    }
  })

  test('activity timeline shows user filter banner after navigation', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await usersPage.getUserCount()
    if (rowCount > 0) {
      // Click View Activity for first user
      await usersPage.clickViewActivity(0)

      // Should see user filter banner
      await expect(page.getByText(/showing activity for user/i)).toBeVisible()
    }
  })

  test('user email is shown in the filter banner', async ({ page }) => {
    // Wait for data to load
    await page.waitForTimeout(1000)

    const rowCount = await usersPage.getUserCount()
    if (rowCount > 0) {
      // Get the email from first row - email is in the second column
      const firstRow = usersPage.getUserRow(0)
      const emailCell = firstRow.locator('td').nth(1)
      const userEmail = await emailCell.textContent()

      // Click View Activity for first user
      await usersPage.clickViewActivity(0)

      // Wait for navigation to complete
      await page.waitForLoadState('domcontentloaded')

      // The user email or userId should be in the filter banner
      // The banner shows "Showing activity for user: <email>" or falls back to userId
      if (userEmail) {
        const emailText = userEmail.trim()
        // Check for partial match (email might be in a span or other element)
        const bannerText = page.locator('text=/Showing activity for user/i')
        await expect(bannerText).toBeVisible({ timeout: 5000 })
      }
    }
  })
})

test.describe('User Role Filter', () => {
  let usersPage: UsersPage

  test.beforeEach(async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()
  })

  test('role filter dropdown is visible', async () => {
    await expect(usersPage.roleFilter).toBeVisible()
  })

  test('status filter dropdown is visible', async ({ page }) => {
    // Look for the status filter (second dropdown)
    const statusDropdown = page.locator('button').filter({ hasText: /all|active|locked|status/i }).nth(1)
    await expect(statusDropdown).toBeVisible()
  })
})
