import { test, expect } from '@playwright/test'
import { NotificationListPage } from '../pages'

test.describe('Notification List Page', () => {
  let listPage: NotificationListPage

  test.beforeEach(async ({ page }) => {
    listPage = new NotificationListPage(page)
    await listPage.goto()
  })

  test('displays page title', async () => {
    await expect(listPage.pageTitle).toBeVisible()
    await expect(listPage.pageTitle).toHaveText('Notifications')
  })

  test('displays filter tabs', async () => {
    await expect(listPage.filterTabs.all).toBeVisible()
    await expect(listPage.filterTabs.unread).toBeVisible()
    await expect(listPage.filterTabs.read).toBeVisible()
  })

  test('all filter tab is active by default', async () => {
    await expect(listPage.filterTabs.all).toHaveClass(/bg-background|shadow/)
  })

  test('filter tabs switch views', async () => {
    // Click unread
    await listPage.filterBy('unread')
    await expect(listPage.filterTabs.unread).toHaveClass(/bg-background|shadow/)

    // Click read
    await listPage.filterBy('read')
    await expect(listPage.filterTabs.read).toHaveClass(/bg-background|shadow/)

    // Click all
    await listPage.filterBy('all')
    await expect(listPage.filterTabs.all).toHaveClass(/bg-background|shadow/)
  })

  test('displays refresh button', async () => {
    await expect(listPage.refreshButton).toBeVisible()
    await expect(listPage.refreshButton).toHaveText(/refresh/i)
  })

  test('refresh button can be clicked', async () => {
    await listPage.refreshButton.click()
    // Should not throw error, may show loading state
    await expect(listPage.refreshButton).toBeVisible()
  })

  test('displays notification stats', async () => {
    await expect(listPage.notificationStats).toBeVisible()
    const text = await listPage.notificationStats.textContent()
    expect(text).toMatch(/showing \d+ of \d+ notifications/i)
  })

  test('displays notification list or empty state', async ({ page }) => {
    // Should either show notifications or empty state
    const hasEmptyState = await listPage.emptyState.isVisible()
    const hasNotifications = await listPage.notificationList.isVisible()

    expect(hasEmptyState || hasNotifications).toBeTruthy()
  })

  test('settings link navigates to preferences', async ({ page }) => {
    // Look for the Preferences link/button in the header
    const preferencesLink = page.getByRole('link', { name: /preferences/i })
    await expect(preferencesLink).toBeVisible()
    await preferencesLink.click()
    await expect(page).toHaveURL(/settings\/notifications/)
  })
})
