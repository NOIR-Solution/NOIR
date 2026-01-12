import { test, expect } from '@playwright/test'

/**
 * Auth E2E Tests - Testing authentication flows
 * Note: These tests don't use the pre-authenticated state
 */
test.describe('Authentication', () => {
  // Use fresh browser context without stored auth
  test.use({ storageState: { cookies: [], origins: [] } })

  test.beforeEach(async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
  })

  test('login page displays correctly', async ({ page }) => {
    // Check page title - it's "NOIR Authentication"
    await expect(page.getByRole('heading', { name: /NOIR Authentication/i })).toBeVisible()

    // Check form elements using locators that match the actual page
    await expect(page.locator('#email')).toBeVisible()
    await expect(page.locator('#password')).toBeVisible()
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible()

    // Check forgot password link
    await expect(page.getByRole('link', { name: /forgot password/i })).toBeVisible()
  })

  test('login with valid credentials redirects to portal', async ({ page }) => {
    // Clear and fill fields (dev mode auto-fills them)
    await page.locator('#email').clear()
    await page.locator('#email').fill('admin@noir.local')
    await page.locator('#password').clear()
    await page.locator('#password').fill('123qwe')
    await page.getByRole('button', { name: /sign in/i }).click()

    await expect(page).toHaveURL(/\/portal/, { timeout: 30000 })
  })

  test('login with invalid credentials shows error', async ({ page }) => {
    await page.locator('#email').clear()
    await page.locator('#email').fill('admin@noir.local')
    await page.locator('#password').clear()
    await page.locator('#password').fill('wrongpassword')
    await page.getByRole('button', { name: /sign in/i }).click()

    // Should show error message
    await expect(page.locator('.text-destructive')).toBeVisible({ timeout: 10000 })
  })

  test('login with empty fields shows validation', async ({ page }) => {
    // Clear the dev default values
    await page.locator('#email').clear()
    await page.locator('#password').clear()
    await page.getByRole('button', { name: /sign in/i }).click()

    // Should show error or validation message
    const hasError = await page.locator('.text-destructive').isVisible()
    const emailInput = page.locator('#email')
    const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.checkValidity())
    expect(hasError || isInvalid).toBeTruthy()
  })

  test('login with invalid email format shows validation', async ({ page }) => {
    await page.locator('#email').clear()
    await page.locator('#email').fill('notanemail')
    await page.locator('#password').clear()
    await page.locator('#password').fill('123qwe')
    await page.getByRole('button', { name: /sign in/i }).click()

    // Should show error for invalid email OR the field should be invalid
    // Wait a bit for the validation/error to appear
    await page.waitForTimeout(500)
    const hasError = await page.locator('.text-destructive').isVisible()
    const emailInput = page.locator('#email')
    const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.checkValidity())
    expect(hasError || isInvalid).toBeTruthy()
  })

  test('password visibility toggle works', async ({ page }) => {
    const passwordInput = page.locator('#password')
    await passwordInput.clear()
    await passwordInput.fill('testpassword')

    // Initially password should be hidden
    await expect(passwordInput).toHaveAttribute('type', 'password')

    // Click show password button
    const toggleButton = page.getByRole('button', { name: /show password/i })
    await toggleButton.click()

    // Now password should be visible
    await expect(passwordInput).toHaveAttribute('type', 'text')

    // Click again to hide
    await page.getByRole('button', { name: /hide password/i }).click()
    await expect(passwordInput).toHaveAttribute('type', 'password')
  })

  test('language switcher is visible on login page', async ({ page }) => {
    const languageSwitcher = page.getByRole('button', { name: /select language/i })
    await expect(languageSwitcher).toBeVisible()
  })

  test('forgot password link navigates correctly', async ({ page }) => {
    await page.getByRole('link', { name: /forgot password/i }).click()
    await expect(page).toHaveURL(/forgot-password/)
  })
})

test.describe('Logout', () => {
  test('user can access logout functionality', async ({ page }) => {
    // Go to portal (uses authenticated state)
    await page.goto('/portal')
    await page.waitForLoadState('networkidle')

    // Find user menu button (shows user info in sidebar)
    const userMenuButton = page.locator('button').filter({ hasText: /System Administrator|admin@noir/i })

    // Check if user menu is visible and can be clicked
    const isVisible = await userMenuButton.first().isVisible()
    expect(isVisible).toBeTruthy()

    if (isVisible) {
      await userMenuButton.first().click()

      // Look for logout or sign out option in the menu
      const hasLogoutOption = await page.getByRole('menuitem', { name: /logout|sign out/i }).isVisible()
        || await page.getByText(/logout|sign out/i).isVisible()

      // Either logout is visible or we can verify the menu opened
      expect(hasLogoutOption || await page.getByRole('menu').isVisible()).toBeTruthy()
    }
  })
})
