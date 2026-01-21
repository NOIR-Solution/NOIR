import { test as setup, expect } from '@playwright/test'

const TENANT_ADMIN_AUTH = 'e2e/.auth/tenant-admin.json'
const PLATFORM_ADMIN_AUTH = 'e2e/.auth/platform-admin.json'

// Most tests use tenant admin (default)
setup('authenticate as tenant admin', async ({ page }) => {
  // Navigate to login page
  await page.goto('/login')

  // Fill login form with tenant admin credentials
  await page.locator('#email').fill('admin@noir.local')
  await page.locator('#password').fill('123qwe')

  // Click submit button
  await page.getByRole('button', { name: /sign in|submit/i }).click()

  // Wait for redirect to portal
  await expect(page).toHaveURL(/\/portal/, { timeout: 30000 })

  // Wait for the page to be fully loaded
  await page.waitForLoadState('networkidle')

  // Save authentication state
  await page.context().storageState({ path: TENANT_ADMIN_AUTH })
})

// Platform-specific tests use platform admin
setup('authenticate as platform admin', async ({ page }) => {
  // Navigate to login page
  await page.goto('/login')

  // Fill login form with platform admin credentials
  await page.locator('#email').fill('platform@noir.local')
  await page.locator('#password').fill('Platform123!')

  // Click submit button
  await page.getByRole('button', { name: /sign in|submit/i }).click()

  // Wait for redirect to portal
  await expect(page).toHaveURL(/\/portal/, { timeout: 30000 })

  // Wait for the page to be fully loaded
  await page.waitForLoadState('networkidle')

  // Save authentication state
  await page.context().storageState({ path: PLATFORM_ADMIN_AUTH })
})
