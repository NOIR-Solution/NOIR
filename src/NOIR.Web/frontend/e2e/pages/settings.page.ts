import { Page, Locator } from '@playwright/test'

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
}
