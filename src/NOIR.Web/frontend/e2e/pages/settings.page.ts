import { Page, Locator, expect } from '@playwright/test'

export class SettingsPage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly pageDescription: Locator
  readonly profileNavButton: Locator
  readonly securityNavButton: Locator

  // Profile form elements
  readonly firstNameInput: Locator
  readonly lastNameInput: Locator
  readonly emailInput: Locator
  readonly avatarSection: Locator
  readonly saveProfileButton: Locator

  // Email change dialog elements
  readonly changeEmailButton: Locator

  // Security/Change Password form elements
  readonly currentPasswordInput: Locator
  readonly newPasswordInput: Locator
  readonly confirmPasswordInput: Locator
  readonly changePasswordButton: Locator

  constructor(page: Page) {
    this.page = page
    this.pageTitle = page.getByRole('heading', { level: 1 })
    this.pageDescription = page.locator('p.text-muted-foreground').first()
    this.profileNavButton = page.getByRole('button', { name: /personal info|profile/i })
    this.securityNavButton = page.getByRole('button', { name: /security/i })

    // Profile form
    this.firstNameInput = page.getByLabel(/first name/i)
    this.lastNameInput = page.getByLabel(/last name/i)
    this.emailInput = page.getByLabel(/email/i)
    this.avatarSection = page.locator('[class*=avatar]').first()
    this.saveProfileButton = page.getByRole('button', { name: /save|update profile/i })

    // Email change button (opens dialog) - target the one next to email field
    // The email input has id="email", and the Change button is its sibling in a flex container
    this.changeEmailButton = page.locator('#email').locator('..').locator('..').getByRole('button', { name: /change/i })

    // Security form
    this.currentPasswordInput = page.getByLabel(/current password/i)
    this.newPasswordInput = page.getByLabel(/^new password$/i)
    this.confirmPasswordInput = page.getByLabel(/confirm.*password/i)
    this.changePasswordButton = page.getByRole('button', { name: /change password|update password/i })
  }

  async goto() {
    await this.page.goto('/portal/settings', { waitUntil: 'load', timeout: 30000 })
    // Wait for URL to confirm navigation completed
    await this.page.waitForURL(/\/portal\/settings/, { timeout: 10000 })
    // Small delay to let React hydrate
    await this.page.waitForTimeout(500)
  }

  async goToProfile() {
    await this.profileNavButton.click()
  }

  async goToSecurity() {
    await this.securityNavButton.click()
  }

  async fillProfileForm(firstName: string, lastName: string) {
    await this.firstNameInput.fill(firstName)
    await this.lastNameInput.fill(lastName)
  }

  async fillChangePasswordForm(currentPassword: string, newPassword: string, confirmPassword: string) {
    await this.currentPasswordInput.fill(currentPassword)
    await this.newPasswordInput.fill(newPassword)
    await this.confirmPasswordInput.fill(confirmPassword)
  }

  /**
   * Opens the email change dialog.
   */
  async openEmailChangeDialog() {
    await this.changeEmailButton.click()
    // Wait for dialog to be visible
    await expect(this.page.getByRole('dialog')).toBeVisible()
  }

  /**
   * Gets the email change dialog locator.
   */
  getEmailChangeDialog() {
    return this.page.getByRole('dialog')
  }

  /**
   * Fills in the new email address in the email change dialog.
   */
  async fillNewEmail(newEmail: string) {
    const dialog = this.getEmailChangeDialog()
    const newEmailInput = dialog.getByLabel(/new email/i).or(dialog.getByPlaceholder(/email/i))
    await newEmailInput.fill(newEmail)
  }

  /**
   * Submits the email change request (step 1).
   */
  async submitEmailChangeRequest() {
    const dialog = this.getEmailChangeDialog()
    const submitButton = dialog.getByRole('button', { name: /send|submit|continue/i })
    await submitButton.click()
  }

  /**
   * Checks if the dialog is on the OTP verification step.
   * The OTP step has a "Back" button that's not present in step 1.
   */
  async isOnOtpStep(): Promise<boolean> {
    const dialog = this.getEmailChangeDialog()
    const backButton = dialog.getByRole('button', { name: /back/i })
    return backButton.isVisible({ timeout: 1000 }).catch(() => false)
  }

  /**
   * Waits for the OTP step to appear, checking for rate limit errors.
   * Returns 'otp' if OTP step reached, 'rate-limited' if rate limit error shown,
   * or 'timeout' if neither condition met within timeout.
   */
  async waitForOtpStepOrRateLimit(timeoutMs = 10000): Promise<'otp' | 'rate-limited' | 'timeout'> {
    const dialog = this.getEmailChangeDialog()
    const pollInterval = 500
    const maxAttempts = Math.ceil(timeoutMs / pollInterval)

    for (let i = 0; i < maxAttempts; i++) {
      // Check if we're on OTP step (Back button is visible)
      if (await this.isOnOtpStep()) {
        return 'otp'
      }
      // Check for rate limit error
      const rateLimitError = dialog.getByText(/too many|rate limit|try again later/i)
      if (await rateLimitError.isVisible({ timeout: 100 }).catch(() => false)) {
        return 'rate-limited'
      }
      await this.page.waitForTimeout(pollInterval)
    }

    return 'timeout'
  }

  /**
   * Enters the OTP code in the verification step.
   * Uses the OtpInput component's inputs (individual digit inputs with inputmode="numeric").
   */
  async enterOtp(otp: string) {
    const dialog = this.getEmailChangeDialog()
    // OtpInput component uses inputs with inputmode="numeric" and they're NOT disabled
    // The OTP inputs are inside a flex container, distinct from the email inputs
    const otpInputs = dialog.locator('input[inputmode="numeric"]:not([disabled])')
    const count = await otpInputs.count()

    if (count >= 6) {
      // Multiple single-digit inputs (OtpInput component)
      for (let i = 0; i < 6; i++) {
        await otpInputs.nth(i).fill(otp[i])
      }
    } else {
      // Fallback: type all digits into the first enabled input
      const enabledInput = dialog.locator('input:not([disabled]):not([type="email"])')
      await enabledInput.first().fill(otp)
    }
  }

  /**
   * Waits for the email change success message.
   */
  async waitForEmailChangeSuccess() {
    const dialog = this.getEmailChangeDialog()
    // Look for success indicator
    await expect(
      dialog.getByText(/success|changed|updated/i).or(dialog.locator('[class*="success"], [class*="check"]'))
    ).toBeVisible({ timeout: 10000 })
  }

  /**
   * Closes the email change dialog.
   */
  async closeEmailChangeDialog() {
    const closeButton = this.page.getByRole('button', { name: /close/i }).or(
      this.page.locator('[aria-label*="close"]')
    )
    if (await closeButton.isVisible()) {
      await closeButton.click()
    }
    // Wait for dialog to close
    await expect(this.getEmailChangeDialog()).not.toBeVisible({ timeout: 5000 })
  }
}
