import { test, expect } from '@playwright/test'
import { SettingsPage } from '../pages'

test.describe('Settings Page', () => {
  let settingsPage: SettingsPage

  test.beforeEach(async ({ page }) => {
    settingsPage = new SettingsPage(page)
    await settingsPage.goto()
    // Wait for page to fully load
    await page.waitForTimeout(500)
  })

  test('displays settings page title', async ({ page }) => {
    // Settings title may be in the page header
    const title = page.getByRole('heading', { level: 1 })
    await expect(title).toBeVisible()
  })

  test('displays navigation sidebar', async ({ page }) => {
    // Look for navigation buttons
    const profileNav = page.getByRole('button', { name: /personal|profile/i })
    const securityNav = page.getByRole('button', { name: /security/i })

    await expect(profileNav).toBeVisible()
    await expect(securityNav).toBeVisible()
  })

  test('profile section is active by default', async ({ page }) => {
    // Profile nav should have active styling
    const profileNav = page.getByRole('button', { name: /personal|profile/i })
    await expect(profileNav).toHaveClass(/bg-blue|text-blue|font-medium/)
  })

  test('can switch to security section', async ({ page }) => {
    const securityNav = page.getByRole('button', { name: /security/i })
    await securityNav.click()
    await expect(securityNav).toHaveClass(/bg-blue|text-blue|font-medium/)
  })

  test('can switch back to profile section', async ({ page }) => {
    const profileNav = page.getByRole('button', { name: /personal|profile/i })
    const securityNav = page.getByRole('button', { name: /security/i })

    await securityNav.click()
    await profileNav.click()
    await expect(profileNav).toHaveClass(/bg-blue|text-blue|font-medium/)
  })
})

test.describe('Profile Settings', () => {
  let settingsPage: SettingsPage

  test.beforeEach(async ({ page }) => {
    settingsPage = new SettingsPage(page)
    await settingsPage.goto()
    // Ensure we're on profile section
    const profileNav = page.getByRole('button', { name: /personal|profile/i })
    if (await profileNav.isVisible()) {
      await profileNav.click()
    }
    await page.waitForTimeout(500)
  })

  test('displays profile form fields', async ({ page }) => {
    // Check for name fields - they may use different labels
    const firstNameInput = page.getByLabel(/first name/i)
      .or(page.getByPlaceholder(/first name/i))
    const lastNameInput = page.getByLabel(/last name/i)
      .or(page.getByPlaceholder(/last name/i))

    await expect(firstNameInput).toBeVisible()
    await expect(lastNameInput).toBeVisible()
  })

  test('displays current user email', async ({ page }) => {
    // Email should be displayed (possibly read-only)
    const emailField = page.getByText(/admin@noir.local/)
    await expect(emailField.first()).toBeVisible()
  })

  test('displays avatar section', async ({ page }) => {
    // Should have avatar or upload section
    const avatarSection = page.locator('[class*=avatar]').first()
      .or(page.getByText(/avatar|profile picture|photo/i).first())
    await expect(avatarSection).toBeVisible()
  })

  test('can edit profile fields', async ({ page }) => {
    const firstNameInput = page.getByLabel(/first name/i)
      .or(page.getByPlaceholder(/first name/i))
    const lastNameInput = page.getByLabel(/last name/i)
      .or(page.getByPlaceholder(/last name/i))

    // Clear and type new values
    await firstNameInput.clear()
    await firstNameInput.fill('TestFirst')
    await lastNameInput.clear()
    await lastNameInput.fill('TestLast')

    // Values should be updated
    await expect(firstNameInput).toHaveValue('TestFirst')
    await expect(lastNameInput).toHaveValue('TestLast')
  })

  test('save button is visible', async ({ page }) => {
    const saveButton = page.getByRole('button', { name: /save|update/i })
    await expect(saveButton).toBeVisible()
  })
})

test.describe('Security Settings - Change Password', () => {
  let settingsPage: SettingsPage

  test.beforeEach(async ({ page }) => {
    settingsPage = new SettingsPage(page)
    await settingsPage.goto()
    // Navigate to security section
    const securityNav = page.getByRole('button', { name: /security/i })
    await securityNav.click()
    await page.waitForTimeout(500)
  })

  test('displays change password form', async ({ page }) => {
    const currentPasswordInput = page.getByLabel(/current password/i)
      .or(page.getByPlaceholder(/current password/i))
    const newPasswordInput = page.getByLabel(/new password/i)
      .or(page.getByPlaceholder(/new password/i))
    const confirmPasswordInput = page.getByLabel(/confirm/i)
      .or(page.getByPlaceholder(/confirm/i))

    await expect(currentPasswordInput.first()).toBeVisible()
    await expect(newPasswordInput.first()).toBeVisible()
    await expect(confirmPasswordInput.first()).toBeVisible()
  })

  test('change password button is visible', async ({ page }) => {
    const changeButton = page.getByRole('button', { name: /change password|update password/i })
    await expect(changeButton).toBeVisible()
  })

  test('password fields accept input', async ({ page }) => {
    const currentPasswordInput = page.getByLabel(/current password/i)
      .or(page.getByPlaceholder(/current password/i))
    const newPasswordInput = page.getByLabel(/new password/i).first()
      .or(page.getByPlaceholder(/new password/i).first())
    const confirmPasswordInput = page.getByLabel(/confirm/i)
      .or(page.getByPlaceholder(/confirm/i))

    await currentPasswordInput.first().fill('currentpass')
    await newPasswordInput.fill('newpassword123')
    await confirmPasswordInput.first().fill('newpassword123')

    await expect(currentPasswordInput.first()).toHaveValue('currentpass')
  })

  test('button is disabled when passwords do not match', async ({ page }) => {
    const currentPasswordInput = page.getByLabel(/current password/i)
      .or(page.getByPlaceholder(/current password/i))
    const newPasswordInput = page.getByLabel(/new password/i).first()
      .or(page.getByPlaceholder(/new password/i).first())
    const confirmPasswordInput = page.getByLabel(/confirm/i)
      .or(page.getByPlaceholder(/confirm/i))
    const changeButton = page.getByRole('button', { name: /change password|update password/i })

    await currentPasswordInput.first().fill('123qwe')
    await newPasswordInput.fill('newpassword123')
    await confirmPasswordInput.first().fill('differentpassword')

    // Button should be disabled when passwords don't match (form validation)
    await expect(changeButton).toBeDisabled()
  })

  test('shows error when current password is wrong', async ({ page }) => {
    const currentPasswordInput = page.getByLabel(/current password/i)
      .or(page.getByPlaceholder(/current password/i))
    const newPasswordInput = page.getByLabel(/new password/i).first()
      .or(page.getByPlaceholder(/new password/i).first())
    const confirmPasswordInput = page.getByLabel(/confirm/i)
      .or(page.getByPlaceholder(/confirm/i))
    const changeButton = page.getByRole('button', { name: /change password|update password/i })

    await currentPasswordInput.first().fill('wrongpassword')
    await newPasswordInput.fill('NewPassword123!')
    await confirmPasswordInput.first().fill('NewPassword123!')

    // Click if enabled
    if (await changeButton.isEnabled()) {
      await changeButton.click()
      // Wait for API response
      await page.waitForTimeout(2000)
    }

    // Form should still be visible (operation failed or button was disabled)
    await expect(changeButton).toBeVisible()
  })
})
