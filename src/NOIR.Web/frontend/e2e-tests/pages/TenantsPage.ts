import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * TenantsPage - Page Object for Tenant Management List
 *
 * Based on: src/pages/portal/admin/tenants/TenantsPage.tsx
 * - Uses PageHeader with Building icon and "Create Tenant" button (Credenza dialog)
 * - Search input with Search button
 * - TenantTable component with Edit, Reset Password, Delete actions
 * - CreateTenantDialog, EditTenantDialog, DeleteTenantDialog, ResetAdminPasswordDialog
 */
export class TenantsPage extends BasePage {
  // Page elements
  readonly pageHeader: Locator;
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly searchButton: Locator;
  readonly tenantTable: Locator;

  // Create/Edit Dialog (Credenza for create, Dialog for edit)
  readonly createDialog: Locator;
  readonly editDialog: Locator;
  readonly identifierInput: Locator;
  readonly nameInput: Locator;
  readonly descriptionInput: Locator;
  readonly noteInput: Locator;
  readonly adminEmailInput: Locator;
  readonly adminPasswordInput: Locator;
  readonly adminFirstNameInput: Locator;
  readonly adminLastNameInput: Locator;
  readonly isActiveCheckbox: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  // Delete Dialog (AlertDialog)
  readonly deleteDialog: Locator;
  readonly deleteConfirmButton: Locator;
  readonly deleteCancelButton: Locator;

  // Reset Password Dialog
  readonly resetPasswordDialog: Locator;
  readonly newPasswordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly resetPasswordButton: Locator;
  readonly resetPasswordCancelButton: Locator;

  // Pagination
  readonly pagination: Locator;

  constructor(page: Page) {
    super(page);

    // Page elements
    this.pageHeader = page.locator('h1:has-text("Tenants"), h1:has-text("Tenant")');
    this.createButton = page.locator('button:has-text("Create Tenant"), button:has-text("New Tenant")');
    this.searchInput = page.locator('input[placeholder*="Search tenant"], input[placeholder*="search"]').first();
    this.searchButton = page.locator('button:has-text("Search")').first();
    this.tenantTable = page.locator('table');

    // Create Dialog (Credenza component)
    this.createDialog = page.locator('[data-state="open"][role="dialog"]').first();
    // Edit Dialog (standard Dialog component)
    this.editDialog = page.locator('[role="dialog"]');

    // Form fields - work in both create and edit dialogs
    this.identifierInput = page.locator('[role="dialog"] input[name="identifier"], [role="dialog"] input#identifier');
    this.nameInput = page.locator('[role="dialog"] input[name="name"], [role="dialog"] input#name');
    this.descriptionInput = page.locator('[role="dialog"] textarea[name="description"], [role="dialog"] textarea#description');
    this.noteInput = page.locator('[role="dialog"] textarea[name="note"], [role="dialog"] textarea#note');

    // Admin user fields (only in create dialog)
    this.adminEmailInput = page.locator('[role="dialog"] input[name="adminEmail"], [role="dialog"] input#adminEmail');
    this.adminPasswordInput = page.locator('[role="dialog"] input[name="adminPassword"], [role="dialog"] input#adminPassword');
    this.adminFirstNameInput = page.locator('[role="dialog"] input[name="adminFirstName"], [role="dialog"] input#adminFirstName');
    this.adminLastNameInput = page.locator('[role="dialog"] input[name="adminLastName"], [role="dialog"] input#adminLastName');

    // Status checkbox (only in edit dialog)
    this.isActiveCheckbox = page.locator('[role="dialog"] input#isActive, [role="dialog"] input[name="isActive"]');

    // Dialog buttons
    this.saveButton = page.locator('[role="dialog"] button[type="submit"], [role="dialog"] button:has-text("Create"), [role="dialog"] button:has-text("Update")');
    this.cancelButton = page.locator('[role="dialog"] button:has-text("Cancel")');

    // Delete Dialog (AlertDialog)
    this.deleteDialog = page.locator('[role="alertdialog"], [role="dialog"]:has-text("Delete")');
    this.deleteConfirmButton = this.deleteDialog.locator('button:has-text("Delete")');
    this.deleteCancelButton = this.deleteDialog.locator('button:has-text("Cancel")');

    // Reset Password Dialog
    this.resetPasswordDialog = page.locator('[role="dialog"]:has-text("Reset")');
    this.newPasswordInput = this.resetPasswordDialog.locator('input#newPassword, input[name="newPassword"]');
    this.confirmPasswordInput = this.resetPasswordDialog.locator('input#confirmPassword, input[name="confirmPassword"]');
    this.resetPasswordButton = this.resetPasswordDialog.locator('button:has-text("Reset Password"), button[type="submit"]');
    this.resetPasswordCancelButton = this.resetPasswordDialog.locator('button:has-text("Cancel")');

    // Pagination
    this.pagination = page.locator('[data-testid="pagination"], nav[aria-label="pagination"]');
  }

  /**
   * Navigate to tenants page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/admin/tenants');
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify page loaded using sequential wait pattern
   * Waits for header first (proves render), then create button (proves data loaded)
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.createButton);
  }

  /**
   * Open create tenant dialog
   */
  async openCreateDialog(): Promise<void> {
    await this.openDialogViaButton(this.createButton, this.createDialog);
  }

  /**
   * Create a new tenant with admin user
   */
  async createTenant(data: {
    identifier: string;
    name: string;
    description?: string;
    note?: string;
    adminEmail: string;
    adminPassword: string;
    adminFirstName?: string;
    adminLastName?: string;
  }): Promise<void> {
    await this.openCreateDialog();

    await this.identifierInput.fill(data.identifier);
    await this.nameInput.fill(data.name);

    if (data.description) {
      await this.descriptionInput.fill(data.description);
    }

    if (data.note) {
      await this.noteInput.fill(data.note);
    }

    // Admin user fields (required for create)
    await this.adminEmailInput.fill(data.adminEmail);
    await this.adminPasswordInput.fill(data.adminPassword);

    if (data.adminFirstName) {
      await this.adminFirstNameInput.fill(data.adminFirstName);
    }

    if (data.adminLastName) {
      await this.adminLastNameInput.fill(data.adminLastName);
    }

    await this.saveButton.click();
    await expect(this.createDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Search tenants
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.searchButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Clear search
   */
  async clearSearch(): Promise<void> {
    await this.searchInput.clear();
    await this.searchButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Get tenant row by identifier or name
   */
  getTenantRow(identifierOrName: string): Locator {
    return this.page.locator(`tr:has-text("${identifierOrName}")`).first();
  }

  /**
   * Click edit button for a tenant
   */
  async clickEditButton(identifierOrName: string): Promise<void> {
    const row = this.getTenantRow(identifierOrName);
    const editButton = row.locator('button[title*="Edit"], button:has([class*="edit"]), button:has(svg.lucide-edit)').first();
    await editButton.click();
    await expect(this.editDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Edit an existing tenant
   */
  async editTenant(identifierOrName: string, newData: {
    identifier?: string;
    name?: string;
    description?: string;
    note?: string;
    isActive?: boolean;
  }): Promise<void> {
    await this.clickEditButton(identifierOrName);

    if (newData.identifier !== undefined) {
      await this.identifierInput.clear();
      await this.identifierInput.fill(newData.identifier);
    }

    if (newData.name !== undefined) {
      await this.nameInput.clear();
      await this.nameInput.fill(newData.name);
    }

    if (newData.description !== undefined) {
      await this.descriptionInput.clear();
      await this.descriptionInput.fill(newData.description);
    }

    if (newData.note !== undefined) {
      await this.noteInput.clear();
      await this.noteInput.fill(newData.note);
    }

    if (newData.isActive !== undefined) {
      const isChecked = await this.isActiveCheckbox.isChecked();
      if (isChecked !== newData.isActive) {
        await this.isActiveCheckbox.click();
      }
    }

    await this.saveButton.click();
    await expect(this.editDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Click reset password button for a tenant
   */
  async clickResetPasswordButton(identifierOrName: string): Promise<void> {
    const row = this.getTenantRow(identifierOrName);
    const resetButton = row.locator('button[title*="Reset"], button:has(svg.lucide-key-round)').first();
    await resetButton.click();
    await expect(this.resetPasswordDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Reset admin password for a tenant
   */
  async resetAdminPassword(identifierOrName: string, newPassword: string): Promise<void> {
    await this.clickResetPasswordButton(identifierOrName);

    await this.newPasswordInput.fill(newPassword);
    await this.confirmPasswordInput.fill(newPassword);

    await this.resetPasswordButton.click();
    await expect(this.resetPasswordDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Click delete button for a tenant
   */
  async clickDeleteButton(identifierOrName: string): Promise<void> {
    const row = this.getTenantRow(identifierOrName);
    const deleteButton = row.locator('button[title*="Delete"], button:has(svg.lucide-trash-2), button.text-destructive').first();
    await deleteButton.click();
    await expect(this.deleteDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Delete a tenant
   */
  async deleteTenant(identifierOrName: string): Promise<void> {
    await this.clickDeleteButton(identifierOrName);
    await this.deleteConfirmButton.click();
    await expect(this.deleteDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Cancel delete dialog
   */
  async cancelDelete(): Promise<void> {
    await this.deleteCancelButton.click();
    await expect(this.deleteDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Get tenant count from table
   */
  async getTenantCount(): Promise<number> {
    const rows = this.page.locator('tbody tr');
    return await rows.count();
  }

  /**
   * Verify tenant exists in table
   */
  async expectTenantExists(identifierOrName: string): Promise<void> {
    const tenant = this.page.locator(`text="${identifierOrName}"`).first();
    await expect(tenant).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify tenant does not exist in table
   */
  async expectTenantNotExists(identifierOrName: string): Promise<void> {
    const tenant = this.page.locator(`tbody tr:has-text("${identifierOrName}")`);
    await expect(tenant).toBeHidden({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify tenant status
   */
  async expectTenantStatus(identifierOrName: string, isActive: boolean): Promise<void> {
    const row = this.getTenantRow(identifierOrName);
    const statusBadge = row.locator('[data-testid="status-badge"], .badge');
    const expectedText = isActive ? 'Active' : 'Inactive';
    await expect(statusBadge).toContainText(expectedText);
  }

  /**
   * Close any open dialog using Escape key
   */
  async closeDialog(): Promise<void> {
    await this.page.keyboard.press('Escape');
    // Wait for any dialog to close
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Navigate to tenant detail page
   */
  async goToTenantDetail(identifierOrName: string): Promise<void> {
    const row = this.getTenantRow(identifierOrName);
    // Click the identifier link or the row itself
    const link = row.locator('a, td.font-mono').first();
    await link.click();
    await this.waitForPageLoad();
  }

  /**
   * Go to specific page
   */
  async goToPage(pageNumber: number): Promise<void> {
    const pageButton = this.pagination.locator(`button:has-text("${pageNumber}")`);
    await pageButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Go to next page
   */
  async goToNextPage(): Promise<void> {
    const nextButton = this.pagination.locator('button[aria-label*="next"], button:has-text("Next")');
    await nextButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Go to previous page
   */
  async goToPreviousPage(): Promise<void> {
    const prevButton = this.pagination.locator('button[aria-label*="previous"], button:has-text("Previous")');
    await prevButton.click();
    await this.waitForPageLoad();
  }
}
