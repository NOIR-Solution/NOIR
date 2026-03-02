import { type Page, type Locator, expect } from '@playwright/test';

export class UsersPage {
  readonly createButton: Locator;
  readonly userTable: Locator;
  readonly searchInput: Locator;
  readonly userRows: Locator;

  constructor(private page: Page) {
    this.createButton = page.getByRole('button', { name: /create|add|new|invite/i });
    this.userTable = page.getByRole('table');
    this.searchInput = page.getByPlaceholder(/search/i);
    this.userRows = page.getByRole('table').getByRole('row');
  }

  async goto() {
    await this.page.goto('/portal/admin/users');
    await this.page.waitForLoadState('networkidle');
  }

  async createUser(data: {
    firstName: string;
    lastName: string;
    email: string;
    password?: string;
    role?: string;
  }) {
    await this.createButton.click();
    await this.page.getByLabel(/first name/i).fill(data.firstName);
    await this.page.getByLabel(/last name/i).fill(data.lastName);
    await this.page.getByLabel(/email/i).fill(data.email);
    if (data.password) {
      // The label may include an asterisk ("Password *") so use id-based locator
      const passwordInput = this.page.locator('#password');
      if (await passwordInput.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await passwordInput.fill(data.password);
      } else {
        await this.page.getByLabel(/^password/i).first().fill(data.password);
      }
      // The form requires a Confirm Password field
      const confirmInput = this.page.locator('#confirmPassword');
      if (await confirmInput.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await confirmInput.fill(data.password);
      } else {
        const confirmLabel = this.page.getByLabel(/confirm password/i);
        if (await confirmLabel.isVisible({ timeout: 2_000 }).catch(() => false)) {
          await confirmLabel.fill(data.password);
        }
      }
    }
    if (data.role) {
      await this.page.getByRole('combobox', { name: /role/i }).click();
      await this.page.getByRole('option', { name: new RegExp(data.role, 'i') }).click();
    }
    await this.page.getByRole('button', { name: /save|create|submit|invite/i }).click();
    // Wait for success toast or error response — the API call may be intercepted
    await this.page.waitForTimeout(2_000);
  }

  async expectUserInList(email: string) {
    // Search for the user first to ensure they appear in the visible page
    const searchInput = this.searchInput;
    if (await searchInput.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await searchInput.fill(email);
      await this.page.waitForTimeout(500); // debounce
    }
    await expect(this.page.getByRole('row', { name: new RegExp(email, 'i') })).toBeVisible({ timeout: 10_000 });
  }
}
