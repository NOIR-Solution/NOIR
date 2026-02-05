import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * CategoriesPage - Page Object for Category Management
 *
 * Based on: src/pages/portal/ecommerce/categories/ProductCategoriesPage.tsx
 * - Create button uses onClick handler with "New Category" text
 * - Uses CreateCategoryDialog component for the dialog
 * - Table uses DataTable component
 */
export class CategoriesPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly categoryTree: Locator;
  readonly categoryTable: Locator;
  readonly categoryCards: Locator;
  readonly pageHeader: Locator;

  // Dialog elements
  readonly dialog: Locator;
  readonly nameInput: Locator;
  readonly slugInput: Locator;
  readonly descriptionInput: Locator;
  readonly parentSelect: Locator;
  readonly imageUpload: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  constructor(page: Page) {
    super(page);

    // The create button has "New Category" text with Plus icon
    this.createButton = page.locator('button:has-text("New Category"), button:has-text("Create Category")');
    // Use specific placeholder - use first() to avoid strict mode
    this.searchInput = page.locator('input[placeholder*="Search categories"]').first();
    this.categoryTree = page.locator('[data-testid="category-tree"]');
    this.categoryTable = page.locator('table');
    this.categoryCards = page.locator('[data-testid="category-card"], .category-card');
    this.pageHeader = page.locator('h1:has-text("Categories"), h1:has-text("Product Categories")');

    // Dialog - CreateCategoryDialog uses standard Dialog components
    this.dialog = page.locator('[role="dialog"]');
    this.nameInput = this.dialog.locator('input#name, input[name="name"]');
    this.slugInput = this.dialog.locator('input#slug, input[name="slug"]');
    this.descriptionInput = this.dialog.locator('textarea#description, textarea[name="description"]');
    // Parent category uses shadcn Select - look for trigger with placeholder or default value
    this.parentSelect = this.dialog.locator('button[role="combobox"]:has-text("Select parent"), button[role="combobox"]:has-text("No parent")');
    this.imageUpload = this.dialog.locator('input[type="file"]');
    this.saveButton = this.dialog.locator('button[type="submit"], button:has-text("Save"), button:has-text("Create")');
    this.cancelButton = this.dialog.locator('button:has-text("Cancel")');
  }

  /**
   * Navigate to categories page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/ecommerce/categories');
  }

  /**
   * Verify page loaded using sequential wait pattern
   * Waits for header first (proves render), then create button (proves data loaded)
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.createButton);
  }

  /**
   * Open create category dialog
   */
  async openCreateDialog(): Promise<void> {
    await this.openDialogViaButton(this.createButton, this.dialog);
  }

  /**
   * Create a new category
   */
  async createCategory(data: {
    name: string;
    slug?: string;
    description?: string;
    parentCategory?: string;
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

    if (data.parentCategory) {
      // Wait for parent Select to be enabled (it's disabled while categories are loading)
      await expect(this.parentSelect).toBeEnabled({ timeout: 15000 });
      await this.parentSelect.click();
      // Wait for Select content to appear - shadcn Select uses [role="listbox"]
      const selectContent = this.page.locator('[role="listbox"]');
      await expect(selectContent).toBeVisible({ timeout: 5000 });

      // Select the option - shadcn Select uses standard [role="option"]
      const option = selectContent.locator(`[role="option"]:has-text("${data.parentCategory}")`);
      await expect(option).toBeVisible({ timeout: 10000 });
      await option.click();

      // Wait for dropdown to close
      await expect(selectContent).toBeHidden({ timeout: 5000 });

      // Verify the selection was registered - find any combobox in the parent field area
      const parentSelectTrigger = this.dialog.locator('button[role="combobox"]').filter({ hasText: data.parentCategory });
      await expect(parentSelectTrigger).toBeVisible({ timeout: 5000 });
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: 10000 });
    await this.expectSuccessToast();
  }

  /**
   * Edit an existing category
   */
  async editCategory(categoryName: string, newData: {
    name?: string;
    description?: string;
  }): Promise<void> {
    await this.clickCategory(categoryName);

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
   * Click on a category to edit
   */
  async clickCategory(name: string): Promise<void> {
    const category = this.page.locator(`tr:has-text("${name}"), [data-testid="category-card"]:has-text("${name}")`).first();
    const editButton = category.locator('button:has-text("Edit"), [data-testid="edit-button"]');
    await editButton.click();
    await expect(this.dialog).toBeVisible();
  }

  /**
   * Delete a category
   */
  async deleteCategory(name: string): Promise<void> {
    const category = this.page.locator(`tr:has-text("${name}"), [data-testid="category-card"]:has-text("${name}")`).first();
    const actionsButton = category.locator('[data-testid="actions-menu"], button.actions');
    await actionsButton.click();

    const deleteButton = this.page.locator('[role="menuitem"]:has-text("Delete")');
    await deleteButton.click();

    await this.confirmAction();
    await this.expectSuccessToast();
  }

  /**
   * Search categories
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.page.keyboard.press('Enter');
    await this.waitForPageLoad();
  }

  /**
   * Get category count
   */
  async getCategoryCount(): Promise<number> {
    const rows = this.page.locator('tbody tr, [data-testid="category-card"]');
    return await rows.count();
  }

  /**
   * Verify category exists
   */
  async expectCategoryExists(name: string): Promise<void> {
    const category = this.page.locator(`text="${name}"`).first();
    await expect(category).toBeVisible({ timeout: 10000 });
  }

  /**
   * Verify category does not exist
   */
  async expectCategoryNotExists(name: string): Promise<void> {
    const category = this.page.locator(`text="${name}"`);
    await expect(category).toBeHidden({ timeout: 10000 });
  }
}
