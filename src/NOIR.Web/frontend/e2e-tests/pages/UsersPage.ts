import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * UsersPage - Page Object for User Management
 *
 * Based on: src/pages/portal/admin/users/UsersPage.tsx
 * - Create button uses "Create User" text
 * - Uses CreateUserDialog component for the dialog
 * - Table uses DataTable component
 */
export class UsersPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly userTable: Locator;
  readonly roleFilter: Locator;
  readonly statusFilter: Locator;
  readonly pageHeader: Locator;

  // Create/Edit dialog
  readonly dialog: Locator;
  readonly emailInput: Locator;
  readonly firstNameInput: Locator;
  readonly lastNameInput: Locator;
  readonly passwordInput: Locator;
  readonly roleSelect: Locator;
  readonly activeToggle: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  constructor(page: Page) {
    super(page);

    // The create button has "Create User" text with Plus icon
    this.createButton = page.locator('button:has-text("Create User"), button:has-text("New User"), button:has-text("Add User")');
    // Use specific placeholder to avoid matching command palette - use first() to avoid strict mode
    this.searchInput = page.locator('input[placeholder*="Search users"]').first();
    this.userTable = page.locator('table');
    this.roleFilter = page.locator('button:has-text("Role"), [aria-label*="Role"]').first();
    this.statusFilter = page.locator('button:has-text("Status"), [aria-label*="Status"]').first();
    this.pageHeader = page.locator('h1:has-text("Users")');

    // Dialog - CreateUserDialog uses standard Dialog components
    this.dialog = page.locator('[role="dialog"]');
    this.emailInput = this.dialog.locator('input#email, input[name="email"], input[type="email"]');
    this.firstNameInput = this.dialog.locator('input#firstName, input[name="firstName"]');
    this.lastNameInput = this.dialog.locator('input#lastName, input[name="lastName"]');
    // Use #password to get first password field (not confirmPassword)
    this.passwordInput = this.dialog.locator('input#password');
    this.roleSelect = this.dialog.locator('button[role="combobox"], [aria-label*="role"]');
    this.activeToggle = this.dialog.locator('button[role="switch"], input[name="isActive"]');
    this.saveButton = this.dialog.locator('button[type="submit"], button:has-text("Save"), button:has-text("Create")');
    this.cancelButton = this.dialog.locator('button:has-text("Cancel")');
  }

  /**
   * Navigate to users page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/admin/users');
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify page loaded - check for page header or page content
   */
  async expectPageLoaded(): Promise<void> {
    // Wait for the Users page content
    const pageContent = this.page.locator(
      'h1:has-text("Users"), ' +
      'button:has-text("Create User"), ' +
      ':text("All Users"), ' +
      'table'
    );
    await expect(pageContent.first()).toBeVisible({ timeout: 15000 });
  }

  /**
   * Open create user dialog
   */
  async openCreateDialog(): Promise<void> {
    await expect(this.createButton.first()).toBeVisible({ timeout: 10000 });
    await this.createButton.first().click();
    await expect(this.dialog).toBeVisible();
  }

  /**
   * Create a new user
   */
  async createUser(data: {
    email: string;
    firstName: string;
    lastName: string;
    password: string;
    roles?: string[];
  }): Promise<void> {
    await this.openCreateDialog();

    await this.emailInput.fill(data.email);
    await this.firstNameInput.fill(data.firstName);
    await this.lastNameInput.fill(data.lastName);
    await this.passwordInput.fill(data.password);

    if (data.roles && data.roles.length > 0) {
      for (const role of data.roles) {
        await this.roleSelect.click();
        const option = this.page.locator(`[role="option"]:has-text("${role}"), [role="menuitemcheckbox"]:has-text("${role}")`);
        await option.click();
      }
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Edit user
   */
  async editUser(email: string, newData: {
    firstName?: string;
    lastName?: string;
  }): Promise<void> {
    await this.clickUser(email);

    if (newData.firstName) {
      await this.firstNameInput.clear();
      await this.firstNameInput.fill(newData.firstName);
    }

    if (newData.lastName) {
      await this.lastNameInput.clear();
      await this.lastNameInput.fill(newData.lastName);
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Click user to edit
   */
  async clickUser(email: string): Promise<void> {
    const user = this.page.locator(`tr:has-text("${email}")`).first();
    const editButton = user.locator('button:has-text("Edit"), [data-testid="edit-button"]');
    await editButton.click();
    await expect(this.dialog).toBeVisible();
  }

  /**
   * Delete user
   */
  async deleteUser(email: string): Promise<void> {
    const user = this.page.locator(`tr:has-text("${email}")`).first();
    const actionsButton = user.locator('[data-testid="actions-menu"], button.actions');
    await actionsButton.click();

    const deleteButton = this.page.locator('[role="menuitem"]:has-text("Delete")');
    await deleteButton.click();

    await this.confirmAction();
    await this.expectSuccessToast();
  }

  /**
   * Assign roles to user
   */
  async assignRoles(email: string, roles: string[]): Promise<void> {
    const user = this.page.locator(`tr:has-text("${email}")`).first();
    const actionsButton = user.locator('[data-testid="actions-menu"], button.actions');
    await actionsButton.click();

    const assignRolesButton = this.page.locator('[role="menuitem"]:has-text("Assign Roles")');
    await assignRolesButton.click();

    // Select roles in dialog
    for (const role of roles) {
      const roleCheckbox = this.page.locator(`[role="dialog"] label:has-text("${role}") input[type="checkbox"]`);
      await roleCheckbox.check();
    }

    await this.page.locator('[role="dialog"] button:has-text("Save")').click();
    await this.expectSuccessToast();
  }

  /**
   * Search users
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.page.keyboard.press('Enter');
    await this.waitForPageLoad();
  }

  /**
   * Get user count
   */
  async getUserCount(): Promise<number> {
    const rows = this.page.locator('tbody tr');
    return await rows.count();
  }

  /**
   * Verify user exists
   */
  async expectUserExists(email: string): Promise<void> {
    const user = this.page.locator(`text="${email}"`).first();
    await expect(user).toBeVisible({ timeout: 10000 });
  }
}
