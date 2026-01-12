import { test, expect } from '@playwright/test'
import { NotificationDropdownPage } from '../pages'

test.describe('Notification Dropdown', () => {
  let dropdown: NotificationDropdownPage

  test.beforeEach(async ({ page }) => {
    await page.goto('/portal')
    dropdown = new NotificationDropdownPage(page)
  })

  test('opens dropdown on bell click', async () => {
    await dropdown.open()
    await expect(dropdown.dropdownContent).toBeVisible()
    await expect(dropdown.dropdownHeader).toBeVisible()
  })

  test('closes dropdown on escape key', async () => {
    await dropdown.open()
    await expect(dropdown.dropdownContent).toBeVisible()

    await dropdown.close()
    await expect(dropdown.dropdownContent).not.toBeVisible()
  })

  test('shows notifications header', async () => {
    await dropdown.open()
    await expect(dropdown.dropdownHeader).toHaveText('Notifications')
  })

  test('has view all notifications link', async () => {
    await dropdown.open()
    await expect(dropdown.viewAllLink).toBeVisible()
    await expect(dropdown.viewAllLink).toHaveText(/view all notifications/i)
  })

  test('view all link navigates to notifications page', async ({ page }) => {
    await dropdown.open()
    await dropdown.navigateToFullPage()
    await expect(page).toHaveURL('/portal/notifications')
  })

  test('displays loading spinner or content', async () => {
    await dropdown.open()
    // Should either show loading, empty state, or notifications
    const hasContent =
      (await dropdown.loadingSpinner.isVisible()) ||
      (await dropdown.emptyState.isVisible()) ||
      (await dropdown.dropdownContent.locator('.divide-y').isVisible())
    expect(hasContent).toBeTruthy()
  })
})
