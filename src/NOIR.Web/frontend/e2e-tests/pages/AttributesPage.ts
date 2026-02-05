import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * AttributesPage - Page Object for Product Attribute Management
 *
 * Based on: src/pages/portal/ecommerce/attributes/ProductAttributesPage.tsx
 * - Create button uses "New Attribute" text with Plus icon
 * - Uses CreateAttributeDialog component
 * - Table uses DataTable component
 *
 * Supports 13 attribute types:
 * Select, MultiSelect, Text, TextArea, Number, Decimal,
 * Boolean, Date, DateTime, Color, Range, Url, File
 */
export class AttributesPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly attributeTable: Locator;
  readonly attributeCards: Locator;
  readonly typeFilter: Locator;
  readonly pageHeader: Locator;

  // Dialog elements
  readonly dialog: Locator;
  readonly nameInput: Locator;
  readonly codeInput: Locator;
  readonly typeSelect: Locator;
  readonly descriptionInput: Locator;
  readonly requiredToggle: Locator;
  readonly filterableToggle: Locator;
  readonly visibleToggle: Locator;
  readonly valuesSection: Locator;
  readonly addValueButton: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  constructor(page: Page) {
    super(page);

    // The create button has "New Attribute" text with Plus icon
    this.createButton = page.locator('button:has-text("New Attribute"), button:has-text("Create Attribute"), button:has-text("Add Attribute")');
    // Use specific placeholder - use first() to avoid strict mode
    this.searchInput = page.locator('input[placeholder*="Search attributes"]').first();
    this.attributeTable = page.locator('table');
    this.attributeCards = page.locator('[data-testid="attribute-card"], .attribute-card');
    this.typeFilter = page.locator('button:has-text("Type"), [aria-label*="Type"]').first();
    this.pageHeader = page.locator('h1:has-text("Attributes"), h1:has-text("Product Attributes")');

    // Dialog - CreateAttributeDialog uses standard Dialog components
    this.dialog = page.locator('[role="dialog"]');
    this.nameInput = this.dialog.locator('input#name, input[name="name"]');
    this.codeInput = this.dialog.locator('input#code, input[name="code"]');
    // Type selector - use flexible selector to match various Combobox implementations
    this.typeSelect = this.dialog.locator('[role="combobox"], button[role="combobox"]').first();
    this.descriptionInput = this.dialog.locator('textarea#description, textarea[name="description"]');
    // Switch/toggle elements - use role="switch" selector
    this.requiredToggle = this.dialog.locator('button[role="switch"]').first();
    this.filterableToggle = this.dialog.locator('button[role="switch"]').nth(1);
    this.visibleToggle = this.dialog.locator('button[role="switch"]').nth(2);
    this.valuesSection = this.dialog.locator('[data-testid="values-section"], .values-section');
    this.addValueButton = this.dialog.locator('button:has-text("Add Value"), button:has-text("Add Option")');
    this.saveButton = this.dialog.locator('button[type="submit"], button:has-text("Save"), button:has-text("Create")');
    this.cancelButton = this.dialog.locator('button:has-text("Cancel")');
  }

  /**
   * Navigate to attributes page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/ecommerce/attributes');
  }

  /**
   * Verify page loaded using sequential wait pattern
   * Waits for header first (proves render), then create button (proves data loaded)
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.createButton);
  }

  /**
   * Open create attribute dialog
   */
  async openCreateDialog(): Promise<void> {
    await this.openDialogViaButton(this.createButton, this.dialog);
  }

  /**
   * Select attribute type
   */
  async selectType(type: string): Promise<void> {
    await this.typeSelect.click();
    const option = this.page.locator(`[role="option"]:has-text("${type}")`);
    await option.click();
  }

  /**
   * Create a generic attribute with just name and type
   */
  async createAttribute(data: {
    name: string;
    type: string;
    code?: string;
  }): Promise<void> {
    await this.openCreateDialog();

    await this.nameInput.fill(data.name);

    // Code field may be auto-generated, fill if provided
    if (data.code && await this.codeInput.isVisible()) {
      await this.codeInput.fill(data.code);
    }

    // Select type using Cmdk command palette (uses BasePage helper)
    await this.selectFromCmdk(this.typeSelect, data.type);

    await this.saveButton.click();

    // Wait for either dialog to close or error toast
    await Promise.race([
      this.dialog.waitFor({ state: 'hidden', timeout: 15000 }),
      this.page.locator('[data-sonner-toast][data-type="error"]').waitFor({ state: 'visible', timeout: 15000 }),
    ]);

    // Check if creation succeeded
    const dialogHidden = await this.dialog.isHidden();
    if (dialogHidden) {
      // Wait for list to refresh
      await this.waitForPageLoad();
    }
  }

  /**
   * Search attributes
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.page.keyboard.press('Enter');
    await this.waitForPageLoad();
  }

  /**
   * Create a text attribute
   */
  async createTextAttribute(data: {
    name: string;
    code: string;
    description?: string;
    required?: boolean;
    filterable?: boolean;
  }): Promise<void> {
    await this.openCreateDialog();

    await this.nameInput.fill(data.name);
    await this.codeInput.fill(data.code);
    await this.selectType('Text');

    if (data.description) {
      await this.descriptionInput.fill(data.description);
    }

    if (data.required) {
      await this.requiredToggle.click(); // Switch components use click
    }

    if (data.filterable) {
      await this.filterableToggle.click(); // Switch components use click
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Create a select attribute with values
   */
  async createSelectAttribute(data: {
    name: string;
    code: string;
    values: string[];
    required?: boolean;
    filterable?: boolean;
  }): Promise<void> {
    await this.openCreateDialog();

    await this.nameInput.fill(data.name);
    await this.codeInput.fill(data.code);
    await this.selectType('Select');

    // Add values
    for (const value of data.values) {
      await this.addValueButton.click();
      const valueInputs = this.dialog.locator('[data-testid="value-input"], input[name*="value"]');
      const lastInput = valueInputs.last();
      await lastInput.fill(value);
    }

    if (data.required) {
      await this.requiredToggle.click(); // Switch components use click
    }

    if (data.filterable) {
      await this.filterableToggle.click(); // Switch components use click
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Create a number attribute
   */
  async createNumberAttribute(data: {
    name: string;
    code: string;
    min?: number;
    max?: number;
    unit?: string;
    required?: boolean;
    filterable?: boolean;
  }): Promise<void> {
    await this.openCreateDialog();

    await this.nameInput.fill(data.name);
    await this.codeInput.fill(data.code);
    await this.selectType('Number');

    if (data.min !== undefined) {
      await this.dialog.locator('input[name="min"]').fill(String(data.min));
    }

    if (data.max !== undefined) {
      await this.dialog.locator('input[name="max"]').fill(String(data.max));
    }

    if (data.unit) {
      await this.dialog.locator('input[name="unit"]').fill(data.unit);
    }

    if (data.required) {
      await this.requiredToggle.click(); // Switch components use click
    }

    if (data.filterable) {
      await this.filterableToggle.click(); // Switch components use click
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Create a color attribute
   */
  async createColorAttribute(data: {
    name: string;
    code: string;
    colors: Array<{ name: string; hex: string }>;
  }): Promise<void> {
    await this.openCreateDialog();

    await this.nameInput.fill(data.name);
    await this.codeInput.fill(data.code);
    await this.selectType('Color');

    // Add color values
    for (const color of data.colors) {
      await this.addValueButton.click();
      const valueInputs = this.dialog.locator('[data-testid="color-name-input"]');
      const hexInputs = this.dialog.locator('[data-testid="color-hex-input"], input[type="color"]');
      await valueInputs.last().fill(color.name);
      await hexInputs.last().fill(color.hex);
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Edit an attribute
   */
  async editAttribute(attributeName: string, newData: {
    name?: string;
    description?: string;
  }): Promise<void> {
    await this.clickAttribute(attributeName);

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
   * Click attribute to edit
   */
  async clickAttribute(name: string): Promise<void> {
    const attribute = this.page.locator(`tr:has-text("${name}"), [data-testid="attribute-card"]:has-text("${name}")`).first();
    const editButton = attribute.locator('button:has-text("Edit"), [data-testid="edit-button"]');
    await editButton.click();
    await expect(this.dialog).toBeVisible();
  }

  /**
   * Delete an attribute
   */
  async deleteAttribute(name: string): Promise<void> {
    const attribute = this.page.locator(`tr:has-text("${name}"), [data-testid="attribute-card"]:has-text("${name}")`).first();
    const actionsButton = attribute.locator('[data-testid="actions-menu"], button.actions');
    await actionsButton.click();

    const deleteButton = this.page.locator('[role="menuitem"]:has-text("Delete")');
    await deleteButton.click();

    await this.confirmAction();
    await this.expectSuccessToast();
  }

  /**
   * Filter by type
   */
  async filterByType(type: string): Promise<void> {
    await this.typeFilter.click();
    const option = this.page.locator(`[role="option"]:has-text("${type}")`);
    await option.click();
    await this.waitForPageLoad();
  }

  /**
   * Verify attribute exists (searches if not immediately visible)
   */
  async expectAttributeExists(name: string): Promise<void> {
    await this.expectItemExists(name, () => this.search(name));
  }

  /**
   * Get attribute count
   */
  async getAttributeCount(): Promise<number> {
    const rows = this.page.locator('tbody tr, [data-testid="attribute-card"]');
    return await rows.count();
  }
}
