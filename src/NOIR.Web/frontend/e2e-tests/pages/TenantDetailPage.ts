import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * TenantDetailPage - Page Object for Tenant Detail View
 *
 * Based on: src/pages/portal/admin/tenants/TenantDetailPage.tsx
 * - Shows tenant details with Back button, Edit button, Delete button
 * - Two cards: Basic Info and Timestamps
 * - Edit dialog with TenantFormValidated (identifier, name, description, note, isActive)
 * - Delete confirmation dialog (AlertDialog)
 */
export class TenantDetailPage extends BasePage {
  // Page elements
  readonly backButton: Locator;
  readonly tenantName: Locator;
  readonly tenantIdentifier: Locator;
  readonly editButton: Locator;
  readonly deleteButton: Locator;

  // Info cards
  readonly basicInfoCard: Locator;
  readonly timestampsCard: Locator;

  // Basic Info fields
  readonly identifierValue: Locator;
  readonly nameValue: Locator;
  readonly statusBadge: Locator;

  // Timestamp fields
  readonly createdAtValue: Locator;
  readonly updatedAtValue: Locator;

  // Edit Dialog
  readonly editDialog: Locator;
  readonly identifierInput: Locator;
  readonly nameInput: Locator;
  readonly descriptionInput: Locator;
  readonly noteInput: Locator;
  readonly isActiveCheckbox: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  // Delete Dialog (AlertDialog)
  readonly deleteDialog: Locator;
  readonly deleteConfirmButton: Locator;
  readonly deleteCancelButton: Locator;

  // Error state
  readonly errorMessage: Locator;
  readonly loadingIndicator: Locator;

  constructor(page: Page) {
    super(page);

    // Page elements
    this.backButton = page.locator('a:has-text("Back"), button:has-text("Back")').first();
    this.tenantName = page.locator('h1').first();
    this.tenantIdentifier = page.locator('p.font-mono, .text-muted-foreground.font-mono').first();
    this.editButton = page.locator('button:has-text("Edit")').first();
    this.deleteButton = page.locator('button:has-text("Delete")').first();

    // Info cards
    this.basicInfoCard = page.locator('[class*="card"]:has-text("Basic Info"), .card:has-text("Identifier")').first();
    this.timestampsCard = page.locator('[class*="card"]:has-text("Timestamps"), .card:has-text("Created")').first();

    // Basic Info field values (within the card)
    this.identifierValue = this.basicInfoCard.locator('p.font-mono').first();
    this.nameValue = this.basicInfoCard.locator('div:has-text("Name") + p, div:has-text("Name") ~ p').first();
    this.statusBadge = this.basicInfoCard.locator('[data-testid="status-badge"], .badge, span:has-text("Active"), span:has-text("Inactive")');

    // Timestamp field values
    this.createdAtValue = this.timestampsCard.locator('div:has-text("Created") + p, div:has-text("Created") ~ p').first();
    this.updatedAtValue = this.timestampsCard.locator('div:has-text("Updated") + p, div:has-text("Updated") ~ p').first();

    // Edit Dialog
    this.editDialog = page.locator('[role="dialog"]:has-text("Edit")');
    this.identifierInput = this.editDialog.locator('input[name="identifier"], input#identifier');
    this.nameInput = this.editDialog.locator('input[name="name"], input#name');
    this.descriptionInput = this.editDialog.locator('textarea[name="description"], textarea#description');
    this.noteInput = this.editDialog.locator('textarea[name="note"], textarea#note');
    this.isActiveCheckbox = this.editDialog.locator('input#isActive, input[name="isActive"]');
    this.saveButton = this.editDialog.locator('button[type="submit"], button:has-text("Update")');
    this.cancelButton = this.editDialog.locator('button:has-text("Cancel")');

    // Delete Dialog (AlertDialog)
    this.deleteDialog = page.locator('[role="alertdialog"], [role="dialog"]:has-text("Delete")');
    this.deleteConfirmButton = this.deleteDialog.locator('button:has-text("Delete")');
    this.deleteCancelButton = this.deleteDialog.locator('button:has-text("Cancel")');

    // Error and loading states
    this.errorMessage = page.locator('.bg-destructive\\/10, [class*="error"], .text-destructive');
    this.loadingIndicator = page.locator('text="Loading", .loading, [role="progressbar"]');
  }

  /**
   * Navigate to tenant detail page by ID
   */
  async navigate(tenantId: string): Promise<void> {
    await this.goto(`/portal/admin/tenants/${tenantId}`);
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Navigate to tenant detail page with edit dialog open
   */
  async navigateWithEdit(tenantId: string): Promise<void> {
    await this.goto(`/portal/admin/tenants/${tenantId}?edit=true`);
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify page loaded successfully
   * Waits for tenant name and edit button to be visible
   */
  async expectPageLoaded(): Promise<void> {
    await expect(this.tenantName).toBeVisible({ timeout: Timeouts.PAGE_LOAD });
    await expect(this.editButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify loading state
   */
  async expectLoading(): Promise<void> {
    await expect(this.loadingIndicator).toBeVisible({ timeout: Timeouts.QUICK_CHECK });
  }

  /**
   * Verify error state
   */
  async expectError(message?: string): Promise<void> {
    await expect(this.errorMessage).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    if (message) {
      await expect(this.errorMessage).toContainText(message);
    }
  }

  /**
   * Go back to tenants list
   */
  async goBack(): Promise<void> {
    await this.backButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Open edit dialog
   */
  async openEditDialog(): Promise<void> {
    await this.openDialogViaButton(this.editButton, this.editDialog);
  }

  /**
   * Edit tenant details
   */
  async editTenant(newData: {
    identifier?: string;
    name?: string;
    description?: string;
    note?: string;
    isActive?: boolean;
  }): Promise<void> {
    await this.openEditDialog();

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
   * Cancel edit dialog
   */
  async cancelEdit(): Promise<void> {
    await this.closeDialog(this.editDialog, this.cancelButton);
  }

  /**
   * Open delete dialog
   */
  async openDeleteDialog(): Promise<void> {
    await this.openDialogViaButton(this.deleteButton, this.deleteDialog);
  }

  /**
   * Delete tenant and verify redirect to list
   */
  async deleteTenant(): Promise<void> {
    await this.openDeleteDialog();
    await this.deleteConfirmButton.click();
    await expect(this.deleteDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
    // Should redirect to tenants list
    await this.page.waitForURL('**/portal/admin/tenants', { timeout: Timeouts.PAGE_LOAD });
  }

  /**
   * Cancel delete dialog
   */
  async cancelDelete(): Promise<void> {
    await this.deleteCancelButton.click();
    await expect(this.deleteDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Get tenant name from page header
   */
  async getTenantName(): Promise<string> {
    return await this.tenantName.textContent() || '';
  }

  /**
   * Get tenant identifier from page
   */
  async getTenantIdentifier(): Promise<string> {
    return await this.tenantIdentifier.textContent() || '';
  }

  /**
   * Verify tenant name matches expected value
   */
  async expectTenantName(expectedName: string): Promise<void> {
    await expect(this.tenantName).toContainText(expectedName);
  }

  /**
   * Verify tenant identifier matches expected value
   */
  async expectTenantIdentifier(expectedIdentifier: string): Promise<void> {
    await expect(this.tenantIdentifier).toContainText(expectedIdentifier);
  }

  /**
   * Verify tenant status
   */
  async expectStatus(isActive: boolean): Promise<void> {
    const expectedText = isActive ? 'Active' : 'Inactive';
    await expect(this.statusBadge).toContainText(expectedText);
  }

  /**
   * Verify basic info card content
   */
  async expectBasicInfo(data: {
    identifier?: string;
    name?: string;
    isActive?: boolean;
  }): Promise<void> {
    if (data.identifier) {
      await expect(this.identifierValue).toContainText(data.identifier);
    }

    if (data.name) {
      await expect(this.basicInfoCard).toContainText(data.name);
    }

    if (data.isActive !== undefined) {
      await this.expectStatus(data.isActive);
    }
  }

  /**
   * Verify timestamps are present
   */
  async expectTimestampsPresent(): Promise<void> {
    await expect(this.timestampsCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.createdAtValue).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify edit dialog is open with expected values
   */
  async expectEditDialogValues(data: {
    identifier?: string;
    name?: string;
    description?: string;
    note?: string;
    isActive?: boolean;
  }): Promise<void> {
    await expect(this.editDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });

    if (data.identifier !== undefined) {
      await expect(this.identifierInput).toHaveValue(data.identifier);
    }

    if (data.name !== undefined) {
      await expect(this.nameInput).toHaveValue(data.name);
    }

    if (data.description !== undefined) {
      await expect(this.descriptionInput).toHaveValue(data.description);
    }

    if (data.note !== undefined) {
      await expect(this.noteInput).toHaveValue(data.note);
    }

    if (data.isActive !== undefined) {
      if (data.isActive) {
        await expect(this.isActiveCheckbox).toBeChecked();
      } else {
        await expect(this.isActiveCheckbox).not.toBeChecked();
      }
    }
  }

  /**
   * Close any open dialog using Escape key
   */
  async closeAnyDialog(): Promise<void> {
    await this.page.keyboard.press('Escape');
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Verify the delete dialog shows correct tenant info
   */
  async expectDeleteDialogContent(tenantNameOrIdentifier: string): Promise<void> {
    await expect(this.deleteDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
    await expect(this.deleteDialog).toContainText(tenantNameOrIdentifier);
  }

  /**
   * Get current URL to extract tenant ID
   */
  getTenantIdFromUrl(): string {
    const url = this.page.url();
    const match = url.match(/\/tenants\/([^/?]+)/);
    return match ? match[1] : '';
  }

  /**
   * Verify URL contains expected tenant ID
   */
  async expectTenantIdInUrl(tenantId: string): Promise<void> {
    await expect(this.page).toHaveURL(new RegExp(`/tenants/${tenantId}`));
  }
}
