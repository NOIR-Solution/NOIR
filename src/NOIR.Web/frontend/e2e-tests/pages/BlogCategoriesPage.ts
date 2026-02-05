import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * BlogCategoriesPage - Page Object for Blog Categories Management
 *
 * Based on: src/pages/portal/blog/categories/BlogCategoriesPage.tsx
 * - Create button uses onClick handler with "New Category" text
 * - Supports tree view and table view modes
 * - Uses CategoryDialog component for create/edit
 * - Uses DeleteCategoryDialog for delete confirmation
 */
export class BlogCategoriesPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly categoriesTable: Locator;
  readonly categoryTree: Locator;
  readonly pageHeader: Locator;

  // View mode toggle
  readonly tableViewButton: Locator;
  readonly treeViewButton: Locator;

  // Dialog elements (CategoryDialog)
  readonly dialog: Locator;
  readonly nameInput: Locator;
  readonly slugInput: Locator;
  readonly descriptionInput: Locator;
  readonly sortOrderInput: Locator;
  readonly parentSelect: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  // Delete confirmation dialog
  readonly deleteDialog: Locator;
  readonly deleteConfirmButton: Locator;
  readonly deleteCancelButton: Locator;

  constructor(page: Page) {
    super(page);

    // The create button has "New Category" text with Plus icon
    this.createButton = page.locator('button:has-text("New Category")');
    this.searchInput = page.locator('input[placeholder*="Search categories"]').first();
    this.categoriesTable = page.locator('table');
    this.categoryTree = page.locator('[data-testid="category-tree-view"], .category-tree');
    this.pageHeader = page.locator('h1:has-text("Categories")');

    // View mode toggle buttons
    this.tableViewButton = page.locator('button[aria-label*="Table view"], button:has(svg.lucide-list)');
    this.treeViewButton = page.locator('button[aria-label*="Tree view"], button:has(svg.lucide-git-branch)');

    // Dialog - CategoryDialog uses standard Dialog components
    this.dialog = page.locator('[role="dialog"]');
    this.nameInput = this.dialog.locator('input[placeholder*="Technology"]');
    this.slugInput = this.dialog.locator('input[placeholder*="technology"]');
    this.descriptionInput = this.dialog.locator('textarea[placeholder*="Describe"]');
    this.sortOrderInput = this.dialog.locator('input[type="number"]');
    this.parentSelect = this.dialog.locator('button[role="combobox"]:has-text("No parent"), button[role="combobox"]:has-text("Select parent")');
    this.saveButton = this.dialog.locator('button[type="submit"], button:has-text("Create"), button:has-text("Update")');
    this.cancelButton = this.dialog.locator('button:has-text("Cancel")');

    // Delete confirmation dialog
    this.deleteDialog = page.locator('[role="alertdialog"], [role="dialog"]:has-text("Delete")');
    this.deleteConfirmButton = this.deleteDialog.locator('button:has-text("Delete"), button:has-text("Confirm")');
    this.deleteCancelButton = this.deleteDialog.locator('button:has-text("Cancel")');
  }

  /**
   * Navigate to blog categories page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/blog/categories');
  }

  /**
   * Verify page loaded using sequential wait pattern
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.createButton);
  }

  /**
   * Switch to table view mode
   */
  async switchToTableView(): Promise<void> {
    await this.tableViewButton.click();
    await expect(this.categoriesTable).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Switch to tree view mode
   */
  async switchToTreeView(): Promise<void> {
    await this.treeViewButton.click();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
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
    sortOrder?: number;
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

    if (data.sortOrder !== undefined) {
      await this.sortOrderInput.clear();
      await this.sortOrderInput.fill(String(data.sortOrder));
    }

    if (data.parentCategory) {
      await this.selectParentCategory(data.parentCategory);
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Select parent category in the dropdown
   */
  private async selectParentCategory(parentName: string): Promise<void> {
    await this.parentSelect.click();
    const selectContent = this.page.locator('[role="listbox"]');
    await expect(selectContent).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    const option = selectContent.locator(`[role="option"]:has-text("${parentName}")`);
    await expect(option).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await option.click();

    await expect(selectContent).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Edit an existing category (table view)
   */
  async editCategory(categoryName: string, newData: {
    name?: string;
    slug?: string;
    description?: string;
    sortOrder?: number;
    parentCategory?: string;
  }): Promise<void> {
    await this.clickEditButton(categoryName);

    if (newData.name) {
      await this.nameInput.clear();
      await this.nameInput.fill(newData.name);
    }

    if (newData.slug) {
      await this.slugInput.clear();
      await this.slugInput.fill(newData.slug);
    }

    if (newData.description) {
      await this.descriptionInput.clear();
      await this.descriptionInput.fill(newData.description);
    }

    if (newData.sortOrder !== undefined) {
      await this.sortOrderInput.clear();
      await this.sortOrderInput.fill(String(newData.sortOrder));
    }

    if (newData.parentCategory) {
      await this.selectParentCategory(newData.parentCategory);
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Click edit button for a category
   */
  private async clickEditButton(categoryName: string): Promise<void> {
    const row = this.page.locator(`tr:has-text("${categoryName}")`).first();
    const actionsButton = row.locator('button:has(svg.lucide-more-horizontal)');
    await actionsButton.click();

    const dropdownMenu = this.page.locator('[role="menu"]');
    await expect(dropdownMenu).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    const editItem = dropdownMenu.locator('[role="menuitem"]:has-text("Edit")');
    await editItem.click();

    await expect(this.dialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Delete a category
   */
  async deleteCategory(name: string): Promise<void> {
    const row = this.page.locator(`tr:has-text("${name}")`).first();
    const actionsButton = row.locator('button:has(svg.lucide-more-horizontal)');
    await actionsButton.click();

    const dropdownMenu = this.page.locator('[role="menu"]');
    await expect(dropdownMenu).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    const deleteItem = dropdownMenu.locator('[role="menuitem"]:has-text("Delete")');
    await deleteItem.click();

    await expect(this.deleteDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
    await this.deleteConfirmButton.click();
    await expect(this.deleteDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Cancel delete action
   */
  async cancelDelete(name: string): Promise<void> {
    const row = this.page.locator(`tr:has-text("${name}")`).first();
    const actionsButton = row.locator('button:has(svg.lucide-more-horizontal)');
    await actionsButton.click();

    const dropdownMenu = this.page.locator('[role="menu"]');
    await expect(dropdownMenu).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    const deleteItem = dropdownMenu.locator('[role="menuitem"]:has-text("Delete")');
    await deleteItem.click();

    await expect(this.deleteDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
    await this.deleteCancelButton.click();
    await expect(this.deleteDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Search categories
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    // Debounced search - wait for results
    await this.page.waitForTimeout(Timeouts.SEARCH_WAIT);
    await this.waitForPageLoad();
  }

  /**
   * Clear search
   */
  async clearSearch(): Promise<void> {
    await this.searchInput.clear();
    await this.page.waitForTimeout(Timeouts.SEARCH_WAIT);
    await this.waitForPageLoad();
  }

  /**
   * Get category count (table view)
   */
  async getCategoryCount(): Promise<number> {
    const rows = this.categoriesTable.locator('tbody tr');
    return await rows.count();
  }

  /**
   * Verify category exists
   */
  async expectCategoryExists(name: string): Promise<void> {
    const category = this.page.locator(`text="${name}"`).first();
    await expect(category).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify category does not exist
   */
  async expectCategoryNotExists(name: string): Promise<void> {
    const category = this.page.locator(`text="${name}"`);
    await expect(category).toBeHidden({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Get category information from table row
   */
  async getCategoryInfo(name: string): Promise<{
    name: string;
    slug: string;
    parent: string;
    postCount: number;
    childCount: number;
    sortOrder: number;
  }> {
    const row = this.page.locator(`tr:has-text("${name}")`).first();
    const cells = row.locator('td');

    return {
      name: await cells.nth(0).textContent() || '',
      slug: await cells.nth(1).textContent() || '',
      parent: await cells.nth(2).textContent() || '',
      postCount: parseInt(await cells.nth(3).textContent() || '0', 10),
      childCount: parseInt(await cells.nth(4).textContent() || '0', 10),
      sortOrder: parseInt(await cells.nth(5).textContent() || '0', 10),
    };
  }

  /**
   * Verify category has specific post count
   */
  async expectCategoryPostCount(name: string, count: number): Promise<void> {
    const row = this.page.locator(`tr:has-text("${name}")`).first();
    const postCountBadge = row.locator(`td:nth-child(4):has-text("${count}")`);
    await expect(postCountBadge).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify category has specific parent
   */
  async expectCategoryParent(name: string, parentName: string): Promise<void> {
    const row = this.page.locator(`tr:has-text("${name}")`).first();
    const parentCell = row.locator(`td:has-text("${parentName}")`);
    await expect(parentCell).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Close the dialog without saving
   */
  async closeDialog(): Promise<void> {
    await this.closeDialog(this.dialog, this.cancelButton);
  }

  /**
   * Check if empty state is displayed
   */
  async expectEmptyState(): Promise<void> {
    const emptyState = this.page.locator('text="No categories found"');
    await expect(emptyState).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Check if loading state is displayed
   */
  async expectLoading(): Promise<void> {
    const loading = this.page.locator('text="Loading..."');
    await expect(loading).toBeVisible({ timeout: Timeouts.QUICK_CHECK });
  }

  /**
   * Expand a category in tree view
   */
  async expandCategoryInTree(name: string): Promise<void> {
    const categoryNode = this.page.locator(`[data-testid="category-tree"] >> text="${name}"`).first();
    const expandButton = categoryNode.locator('..').locator('button[aria-label="Expand"]');
    await expandButton.click();
  }

  /**
   * Collapse a category in tree view
   */
  async collapseCategoryInTree(name: string): Promise<void> {
    const categoryNode = this.page.locator(`[data-testid="category-tree"] >> text="${name}"`).first();
    const collapseButton = categoryNode.locator('..').locator('button[aria-label="Collapse"]');
    await collapseButton.click();
  }

  /**
   * Edit category from tree view
   */
  async editCategoryFromTree(name: string): Promise<void> {
    const categoryNode = this.page.locator(`text="${name}"`).first();
    const editButton = categoryNode.locator('..').locator('button:has(svg.lucide-pencil)');
    await editButton.click();
    await expect(this.dialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Delete category from tree view
   */
  async deleteCategoryFromTree(name: string): Promise<void> {
    const categoryNode = this.page.locator(`text="${name}"`).first();
    const deleteButton = categoryNode.locator('..').locator('button:has(svg.lucide-trash-2)');
    await deleteButton.click();
    await expect(this.deleteDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
    await this.deleteConfirmButton.click();
    await expect(this.deleteDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Verify dialog title matches create mode
   */
  async expectCreateDialogTitle(): Promise<void> {
    const title = this.dialog.locator('text="Create New Category"');
    await expect(title).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify dialog title matches edit mode
   */
  async expectEditDialogTitle(): Promise<void> {
    const title = this.dialog.locator('text="Edit Category"');
    await expect(title).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }
}
