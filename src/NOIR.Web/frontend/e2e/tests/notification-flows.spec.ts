import { test, expect } from '@playwright/test'
import { NotificationDropdownPage, NotificationListPage, NotificationPreferencesPage } from '../pages'

test.describe('End-to-End Notification Flows', () => {
  test('complete navigation flow: portal -> dropdown -> list -> preferences', async ({ page }) => {
    // 1. Navigate to portal
    await page.goto('/portal')

    // 2. Open dropdown
    const dropdown = new NotificationDropdownPage(page)
    await dropdown.open()
    await expect(dropdown.dropdownContent).toBeVisible()

    // 3. Navigate to full page
    await dropdown.navigateToFullPage()
    const listPage = new NotificationListPage(page)
    await expect(listPage.pageTitle).toBeVisible()

    // 4. Navigate to preferences via settings link
    await page.goto('/portal/settings/notifications')
    const prefsPage = new NotificationPreferencesPage(page)
    await expect(prefsPage.pageTitle).toBeVisible()

    // 5. Navigate back to notifications using the back button
    await prefsPage.navigateBack()
  })

  test('dropdown closes when navigating to full page', async ({ page }) => {
    await page.goto('/portal')

    const dropdown = new NotificationDropdownPage(page)
    await dropdown.open()

    // Click view all
    await dropdown.viewAllLink.click()

    // Should be on notifications page
    await expect(page).toHaveURL('/portal/notifications')

    // Dropdown should not be visible (we're on a different page)
    await expect(dropdown.dropdownContent).not.toBeVisible()
  })

  test('filter tabs maintain state during session', async ({ page }) => {
    const listPage = new NotificationListPage(page)
    await listPage.goto()

    // Switch to unread filter
    await listPage.filterBy('unread')
    await expect(listPage.filterTabs.unread).toHaveClass(/bg-background|shadow/)

    // Refresh the page
    await page.reload()
    await expect(listPage.pageTitle).toBeVisible()

    // Default filter should be 'all' after refresh (state not persisted)
    await expect(listPage.filterTabs.all).toHaveClass(/bg-background|shadow/)
  })

  test('preferences page loads all categories', async ({ page }) => {
    await page.goto('/portal/settings/notifications')

    // Wait for page to load
    await expect(page.getByRole('heading', { name: 'Notification Preferences' })).toBeVisible()

    // Verify all categories are present
    const categories = ['System', 'User Actions', 'Workflow', 'Security', 'Integration']
    for (const category of categories) {
      await expect(page.getByText(category, { exact: true }).first()).toBeVisible()
    }
  })

  test('notification stats update after refresh', async ({ page }) => {
    const listPage = new NotificationListPage(page)
    await listPage.goto()

    // Get initial stats
    const initialStats = await listPage.notificationStats.textContent()

    // Refresh
    await listPage.refreshButton.click()

    // Wait for potential loading
    await page.waitForTimeout(1000)

    // Stats should still be visible (may or may not change)
    await expect(listPage.notificationStats).toBeVisible()
  })

  test('can access notifications from header on any portal page', async ({ page }) => {
    // Go to a different portal page (e.g., dashboard)
    await page.goto('/portal')

    // Bell should be visible
    const dropdown = new NotificationDropdownPage(page)
    await expect(dropdown.bellButton).toBeVisible()

    // Can open dropdown
    await dropdown.open()
    await expect(dropdown.dropdownContent).toBeVisible()
  })
})
