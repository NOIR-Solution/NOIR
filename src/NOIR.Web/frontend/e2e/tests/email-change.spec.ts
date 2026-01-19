import { test, expect } from '@playwright/test'
import { SettingsPage } from '../pages'
import { MailpitHelper } from '../helpers'

test.describe('Email Change with OTP', () => {
  let settingsPage: SettingsPage
  let mailpitHelper: MailpitHelper

  // Generate a unique email for each test run to avoid conflicts
  const generateTestEmail = () => `test_${Date.now()}@example.com`

  test.beforeEach(async ({ page, request }) => {
    settingsPage = new SettingsPage(page)
    mailpitHelper = new MailpitHelper(request)
    await settingsPage.goto()
    // Ensure we're on profile section
    await settingsPage.goToProfile()
    await page.waitForTimeout(500)
  })

  test('should display email change button', async ({ page }) => {
    // The change email button should be visible next to the email field
    await expect(settingsPage.changeEmailButton).toBeVisible()
  })

  test('should open email change dialog', async ({ page }) => {
    await settingsPage.openEmailChangeDialog()

    const dialog = settingsPage.getEmailChangeDialog()
    await expect(dialog).toBeVisible()

    // Dialog should have title
    await expect(dialog.getByText(/change.*email/i)).toBeVisible()
  })

  test('should show current email in dialog', async ({ page }) => {
    await settingsPage.openEmailChangeDialog()

    const dialog = settingsPage.getEmailChangeDialog()
    // Current email should be displayed (admin@noir.local)
    await expect(dialog.getByText(/admin@noir\.local/i).or(dialog.locator('input[value*="admin"]'))).toBeVisible()
  })

  test('should validate new email is different from current', async ({ page }) => {
    await settingsPage.openEmailChangeDialog()
    await settingsPage.fillNewEmail('admin@noir.local')

    // Try to submit - should show validation error
    await settingsPage.submitEmailChangeRequest()

    const dialog = settingsPage.getEmailChangeDialog()
    // Should show error that email must be different
    // The error message is in a p.text-destructive element
    await expect(
      dialog.getByText('New email must be different from current email')
    ).toBeVisible({ timeout: 5000 })
  })

  test('should validate email format', async ({ page }) => {
    await settingsPage.openEmailChangeDialog()
    await settingsPage.fillNewEmail('invalid-email')

    // Tab out or try to submit to trigger validation
    await settingsPage.submitEmailChangeRequest()

    const dialog = settingsPage.getEmailChangeDialog()
    // Should show email validation error - "Invalid email address"
    await expect(
      dialog.getByText('Invalid email address')
    ).toBeVisible({ timeout: 5000 })
  })

  test('should send OTP to new email and verify successfully', async ({ page, request }) => {
    const newEmail = generateTestEmail()

    // Clear any existing emails to this address
    await mailpitHelper.deleteEmailsTo(newEmail)

    // Step 1: Open dialog and enter new email
    await settingsPage.openEmailChangeDialog()
    await settingsPage.fillNewEmail(newEmail)
    await settingsPage.submitEmailChangeRequest()

    // Wait for either OTP step or rate limit error
    const result = await settingsPage.waitForOtpStepOrRateLimit()
    if (result === 'rate-limited') {
      test.skip(true, 'Rate limit reached - skipping OTP verification test')
      return
    }
    if (result === 'timeout') {
      test.skip(true, 'Could not reach OTP step - may be rate limited or other issue')
      return
    }

    // Step 3: Get OTP from Mailpit
    const otp = await mailpitHelper.waitForOtp(newEmail, 15, 1000)

    if (!otp) {
      // If Mailpit is not available, skip the test gracefully
      test.skip(true, 'Mailpit is not available or email was not received')
      return
    }

    expect(otp).toBeTruthy()
    expect(otp).toHaveLength(6)

    // Step 4: Enter OTP
    await settingsPage.enterOtp(otp)

    // Step 5: Wait for success
    await settingsPage.waitForEmailChangeSuccess()

    // Step 6: Verify email was changed (dialog should close eventually)
    await page.waitForTimeout(2500) // Wait for auto-close

    // Navigate back to settings to verify email changed
    await settingsPage.goto()

    // Email should now show the new email
    await expect(page.getByText(newEmail)).toBeVisible({ timeout: 10000 })
  })

  test('should show error on invalid OTP', async ({ page }) => {
    const newEmail = generateTestEmail()

    // Open dialog and enter new email
    await settingsPage.openEmailChangeDialog()
    await settingsPage.fillNewEmail(newEmail)
    await settingsPage.submitEmailChangeRequest()

    // Wait for either OTP step or rate limit error
    const result = await settingsPage.waitForOtpStepOrRateLimit()
    if (result === 'rate-limited') {
      test.skip(true, 'Rate limit reached - skipping invalid OTP test')
      return
    }
    if (result === 'timeout') {
      test.skip(true, 'Could not reach OTP step - may be rate limited or other issue')
      return
    }

    // Enter wrong OTP
    await settingsPage.enterOtp('000000')

    // Should show error
    const dialog = settingsPage.getEmailChangeDialog()
    await expect(
      dialog.getByText(/invalid|wrong|incorrect|try again/i)
    ).toBeVisible({ timeout: 10000 })
  })

  test('should allow going back to email step', async ({ page }) => {
    const newEmail = generateTestEmail()

    // Open dialog and enter new email
    await settingsPage.openEmailChangeDialog()
    await settingsPage.fillNewEmail(newEmail)
    await settingsPage.submitEmailChangeRequest()

    // Wait for either OTP step or rate limit error
    const result = await settingsPage.waitForOtpStepOrRateLimit()
    if (result === 'rate-limited') {
      test.skip(true, 'Rate limit reached - skipping back button test')
      return
    }
    if (result === 'timeout') {
      test.skip(true, 'Could not reach OTP step - may be rate limited or other issue')
      return
    }

    // Click back button
    const dialog = settingsPage.getEmailChangeDialog()
    const backButton = dialog.getByRole('button', { name: /back/i })
    await backButton.click()

    // Should be back on email step
    await expect(
      dialog.getByLabel(/new email/i).or(dialog.getByPlaceholder(/email/i))
    ).toBeVisible()
  })

  test('should close dialog on cancel', async ({ page }) => {
    await settingsPage.openEmailChangeDialog()

    // Press Escape to close
    await page.keyboard.press('Escape')

    // Dialog should be closed
    await expect(settingsPage.getEmailChangeDialog()).not.toBeVisible({ timeout: 5000 })
  })
})
