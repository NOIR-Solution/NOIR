import { test as setup, expect } from '@playwright/test'

const AUTH_FILE = 'e2e/.auth/admin.json'

setup('authenticate as admin', async ({ page }) => {
  // Navigate to login page
  await page.goto('/login')

  // Fill login form - use specific selectors to avoid ambiguity
  await page.locator('#email').fill('admin@noir.local')
  await page.locator('#password').fill('123qwe')

  // Click submit button
  await page.getByRole('button', { name: /sign in|submit/i }).click()

  // Wait for redirect to portal
  await expect(page).toHaveURL(/\/portal/, { timeout: 30000 })

  // Wait for the page to be fully loaded
  await page.waitForLoadState('networkidle')

  // Save authentication state
  await page.context().storageState({ path: AUTH_FILE })
})
