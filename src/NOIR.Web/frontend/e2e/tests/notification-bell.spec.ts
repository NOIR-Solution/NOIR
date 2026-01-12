import { test, expect } from '@playwright/test'
import { NotificationDropdownPage } from '../pages'

test.describe('Notification Bell Component', () => {
  let dropdown: NotificationDropdownPage

  test.beforeEach(async ({ page }) => {
    await page.goto('/portal')
    dropdown = new NotificationDropdownPage(page)
  })

  test('displays bell icon in header', async () => {
    await expect(dropdown.bellButton).toBeVisible()
  })

  test('bell button has accessible label', async () => {
    const ariaLabel = await dropdown.bellButton.getAttribute('aria-label')
    expect(ariaLabel).toMatch(/notifications/i)
  })

  test('bell icon can be clicked', async () => {
    await dropdown.bellButton.click()
    // Dropdown should open
    await expect(dropdown.dropdownContent).toBeVisible()
  })
})
