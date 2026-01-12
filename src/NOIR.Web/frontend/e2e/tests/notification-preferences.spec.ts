import { test, expect } from '@playwright/test'
import { NotificationPreferencesPage } from '../pages'

test.describe('Notification Preferences Page', () => {
  let prefsPage: NotificationPreferencesPage

  test.beforeEach(async ({ page }) => {
    prefsPage = new NotificationPreferencesPage(page)
    await prefsPage.goto()
  })

  test('displays page title', async () => {
    await expect(prefsPage.pageTitle).toBeVisible()
    await expect(prefsPage.pageTitle).toHaveText('Notification Preferences')
  })

  test('displays back button', async () => {
    await expect(prefsPage.backButton).toBeVisible()
  })

  test('displays save button', async () => {
    await expect(prefsPage.saveButton).toBeVisible()
  })

  test('save button is disabled initially', async () => {
    await expect(prefsPage.saveButton).toBeDisabled()
  })

  test('displays all 5 category cards', async ({ page }) => {
    // Check for each category - use first() since description also contains text
    const categories = ['System', 'User Actions', 'Workflow', 'Security', 'Integration']
    for (const category of categories) {
      await expect(page.getByText(category, { exact: true }).first()).toBeVisible()
    }
  })

  test('category cards have in-app toggle', async ({ page }) => {
    const switches = page.getByRole('switch')
    const count = await switches.count()
    expect(count).toBeGreaterThanOrEqual(5)
  })

  test('category cards have email frequency options', async ({ page }) => {
    // Check for frequency buttons
    await expect(page.getByRole('button', { name: 'Never' }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: 'Immediate' }).first()).toBeVisible()
  })

  test('toggling in-app enables save button', async ({ page }) => {
    // Toggle the first switch
    const firstSwitch = page.getByRole('switch').first()
    await firstSwitch.click()

    // Save button should now be enabled
    await expect(prefsPage.saveButton).toBeEnabled()

    // Toggle back to reset
    await firstSwitch.click()
  })

  test('changing email frequency enables save button', async ({ page }) => {
    // Get a frequency button that's not currently selected
    const dailyButton = page.getByRole('button', { name: 'Daily digest' }).first()
    await dailyButton.click()

    // Save button should be enabled
    await expect(prefsPage.saveButton).toBeEnabled()
  })

  test('back navigation works', async () => {
    // Use the page object's navigateBack method
    await prefsPage.navigateBack()
  })
})
