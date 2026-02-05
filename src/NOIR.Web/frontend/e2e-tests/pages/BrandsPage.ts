import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * BrandsPage - Page Object for Brand Management
 *
 * Based on: src/pages/portal/ecommerce/brands/BrandsPage.tsx
 * - Create button uses onClick handler with "New Brand" text
 * - Uses CreateBrandDialog component for the dialog
 * - Table uses DataTable component
 */
export class BrandsPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly brandTable: Locator;
  readonly brandCards: Locator;
  readonly pageHeader: Locator;

  // Dialog elements
  readonly dialog: Locator;
  readonly nameInput: Locator;
  readonly slugInput: Locator;
  readonly descriptionInput: Locator;
  readonly websiteInput: Locator;
  readonly logoUpload: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  constructor(page: Page) {
    super(page);

    // The create button has "New Brand" text with Plus icon
    this.createButton = page.locator('button:has-text("New Brand"), button:has-text("Create Brand")');
    // Use specific placeholder - use first() to avoid strict mode
    this.searchInput = page.locator('input[placeholder*="Search brands"]').first();
    this.brandTable = page.locator('table');
    this.brandCards = page.locator('[data-testid="brand-card"], .brand-card');
    this.pageHeader = page.locator('h1:has-text("Brands")');

    // Dialog - CreateBrandDialog uses standard Dialog components
    this.dialog = page.locator('[role="dialog"]');
    this.nameInput = this.dialog.locator('input#name, input[name="name"]');
    this.slugInput = this.dialog.locator('input#slug, input[name="slug"]');
    this.descriptionInput = this.dialog.locator('textarea#description, textarea[name="description"]');
    this.websiteInput = this.dialog.locator('input#website, input[name="website"], input[type="url"]');
    this.logoUpload = this.dialog.locator('input[type="file"]');
    this.saveButton = this.dialog.locator('button[type="submit"], button:has-text("Save"), button:has-text("Create")');
    this.cancelButton = this.dialog.locator('button:has-text("Cancel")');
  }

  /**
   * Navigate to brands page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/ecommerce/brands');
  }

  /**
   * Verify page loaded using sequential wait pattern
   * Waits for header first (proves render), then create button (proves data loaded)
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.createButton);
  }

  /**
   * Open create dialog
   */
  async openCreateDialog(): Promise<void> {
    await this.openDialogViaButton(this.createButton, this.dialog);
  }

  /**
   * Create a new brand
   */
  async createBrand(data: {
    name: string;
    slug?: string;
    description?: string;
    website?: string;
  }): Promise<void> {
    await this.openCreateDialog();

    await this.nameInput.fill(data.name);

    if (data.slug) {
      await this.slugInput.clear();
      await this.slugInput.fill(data.slug);
    }

    if (data.description) {
      await this.descriptionInput.fill(data.description);
    }

    if (data.website) {
      await this.websiteInput.fill(data.website);
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Edit a brand
   */
  async editBrand(brandName: string, newData: {
    name?: string;
    description?: string;
    website?: string;
  }): Promise<void> {
    await this.clickBrand(brandName);

    if (newData.name) {
      await this.nameInput.clear();
      await this.nameInput.fill(newData.name);
    }

    if (newData.description) {
      await this.descriptionInput.clear();
      await this.descriptionInput.fill(newData.description);
    }

    if (newData.website) {
      await this.websiteInput.clear();
      await this.websiteInput.fill(newData.website);
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Click brand to edit
   */
  async clickBrand(name: string): Promise<void> {
    const brand = this.page.locator(`tr:has-text("${name}"), [data-testid="brand-card"]:has-text("${name}")`).first();
    const editButton = brand.locator('button:has-text("Edit"), [data-testid="edit-button"]');
    await editButton.click();
    await expect(this.dialog).toBeVisible();
  }

  /**
   * Delete a brand
   */
  async deleteBrand(name: string): Promise<void> {
    const brand = this.page.locator(`tr:has-text("${name}"), [data-testid="brand-card"]:has-text("${name}")`).first();
    const actionsButton = brand.locator('[data-testid="actions-menu"], button.actions');
    await actionsButton.click();

    const deleteButton = this.page.locator('[role="menuitem"]:has-text("Delete")');
    await deleteButton.click();

    await this.confirmAction();
    await this.expectSuccessToast();
  }

  /**
   * Search brands
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.page.keyboard.press('Enter');
    await this.waitForPageLoad();
  }

  /**
   * Get brand count
   */
  async getBrandCount(): Promise<number> {
    const rows = this.page.locator('tbody tr, [data-testid="brand-card"]');
    return await rows.count();
  }

  /**
   * Verify brand exists (searches if not immediately visible)
   */
  async expectBrandExists(name: string): Promise<void> {
    await this.expectItemExists(name, () => this.search(name));
  }
}
