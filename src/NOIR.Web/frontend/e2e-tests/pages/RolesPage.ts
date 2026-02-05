import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * RolesPage - Page Object for Role Management
 *
 * Based on: src/pages/portal/admin/roles/RolesPage.tsx
 * - Uses CreateRoleDialog component directly (not just a button)
 * - Table uses DataTable component
 */
export class RolesPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly roleTable: Locator;
  readonly pageHeader: Locator;

  // Dialog elements
  readonly dialog: Locator;
  readonly nameInput: Locator;
  readonly descriptionInput: Locator;
  readonly permissionsSection: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  constructor(page: Page) {
    super(page);

    // The create button is part of CreateRoleDialog trigger - usually "Create Role" or "New Role"
    this.createButton = page.locator('button:has-text("Create Role"), button:has-text("New Role"), button:has-text("Add Role")');
    // Use specific placeholder - use first() to avoid strict mode
    this.searchInput = page.locator('input[placeholder*="Search roles"]').first();
    this.roleTable = page.locator('table');
    this.pageHeader = page.locator('h1:has-text("Roles")');

    // Dialog - CreateRoleDialog uses standard Dialog components
    this.dialog = page.locator('[role="dialog"]');
    this.nameInput = this.dialog.locator('input#name, input[name="name"]');
    this.descriptionInput = this.dialog.locator('textarea#description, textarea[name="description"]');
    this.permissionsSection = this.dialog.locator('[data-testid="permissions-section"], .permissions-section');
    this.saveButton = this.dialog.locator('button[type="submit"], button:has-text("Save"), button:has-text("Create")');
    this.cancelButton = this.dialog.locator('button:has-text("Cancel")');
  }

  /**
   * Navigate to roles page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/admin/roles');
  }

  /**
   * Verify page loaded - check for page header or create button
   */
  async expectPageLoaded(): Promise<void> {
    // Wait for either the page header or create button to be visible
    const pageContent = this.page.locator('h1:has-text("Roles"), button:has-text("Create Role")');
    await expect(pageContent.first()).toBeVisible({ timeout: 15000 });
  }

  /**
   * Open create role dialog
   */
  async openCreateDialog(): Promise<void> {
    await this.createButton.click();
    await expect(this.dialog).toBeVisible();
  }

  /**
   * Create a new role
   */
  async createRole(data: {
    name: string;
    description?: string;
    permissions?: string[];
  }): Promise<void> {
    await this.openCreateDialog();

    await this.nameInput.fill(data.name);

    if (data.description) {
      await this.descriptionInput.fill(data.description);
    }

    if (data.permissions && data.permissions.length > 0) {
      for (const permission of data.permissions) {
        const checkbox = this.permissionsSection.locator(`label:has-text("${permission}") input[type="checkbox"], input[value="${permission}"]`);
        await checkbox.check();
      }
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Edit a role
   */
  async editRole(roleName: string, newData: {
    name?: string;
    description?: string;
    permissions?: string[];
  }): Promise<void> {
    await this.clickRole(roleName);

    if (newData.name) {
      await this.nameInput.clear();
      await this.nameInput.fill(newData.name);
    }

    if (newData.description) {
      await this.descriptionInput.clear();
      await this.descriptionInput.fill(newData.description);
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Click role to edit
   */
  async clickRole(name: string): Promise<void> {
    const role = this.page.locator(`tr:has-text("${name}")`).first();
    const editButton = role.locator('button:has-text("Edit"), [data-testid="edit-button"]');
    await editButton.click();
    await expect(this.dialog).toBeVisible();
  }

  /**
   * Delete a role
   */
  async deleteRole(name: string): Promise<void> {
    const role = this.page.locator(`tr:has-text("${name}")`).first();
    const actionsButton = role.locator('[data-testid="actions-menu"], button.actions');
    await actionsButton.click();

    const deleteButton = this.page.locator('[role="menuitem"]:has-text("Delete")');
    await deleteButton.click();

    await this.confirmAction();
    await this.expectSuccessToast();
  }

  /**
   * View role permissions
   */
  async viewPermissions(roleName: string): Promise<void> {
    const role = this.page.locator(`tr:has-text("${roleName}")`).first();
    const viewButton = role.locator('button:has-text("Permissions"), [data-testid="view-permissions"]');
    await viewButton.click();
    await expect(this.page.locator('[role="dialog"]')).toBeVisible();
  }

  /**
   * Search roles
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.page.keyboard.press('Enter');
    await this.waitForPageLoad();
  }

  /**
   * Get role count
   */
  async getRoleCount(): Promise<number> {
    const rows = this.page.locator('tbody tr');
    return await rows.count();
  }

  /**
   * Verify role exists
   */
  async expectRoleExists(name: string): Promise<void> {
    const role = this.page.locator(`text="${name}"`).first();
    await expect(role).toBeVisible({ timeout: 10000 });
  }
}
