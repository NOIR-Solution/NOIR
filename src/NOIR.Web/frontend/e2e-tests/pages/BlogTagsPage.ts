import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * BlogTagsPage - Page Object for Blog Tags Management
 *
 * Based on: src/pages/portal/blog/tags/BlogTagsPage.tsx
 * - Create button uses onClick handler with "New Tag" text
 * - Uses TagDialog component for create/edit
 * - Uses DeleteTagDialog for delete confirmation
 * - Features color picker for tag colors
 */
export class BlogTagsPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly searchButton: Locator;
  readonly tagsTable: Locator;
  readonly pageHeader: Locator;

  // Dialog elements (TagDialog)
  readonly dialog: Locator;
  readonly nameInput: Locator;
  readonly slugInput: Locator;
  readonly descriptionInput: Locator;
  readonly colorPicker: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  // Delete confirmation dialog
  readonly deleteDialog: Locator;
  readonly deleteConfirmButton: Locator;
  readonly deleteCancelButton: Locator;

  constructor(page: Page) {
    super(page);

    // The create button has "New Tag" text with Plus icon
    this.createButton = page.locator('button:has-text("New Tag")');
    this.searchInput = page.locator('input[placeholder*="Search tags"]').first();
    this.searchButton = page.locator('button[type="submit"]:has-text("Search")');
    this.tagsTable = page.locator('table');
    this.pageHeader = page.locator('h1:has-text("Tags")');

    // Dialog - TagDialog uses standard Dialog components
    this.dialog = page.locator('[role="dialog"]');
    this.nameInput = this.dialog.locator('input[placeholder*="JavaScript"]');
    this.slugInput = this.dialog.locator('input[placeholder*="javascript"]');
    this.descriptionInput = this.dialog.locator('textarea[placeholder*="Describe"]');
    this.colorPicker = this.dialog.locator('[data-testid="color-picker"], input[type="color"], .color-picker');
    this.saveButton = this.dialog.locator('button[type="submit"], button:has-text("Create"), button:has-text("Update")');
    this.cancelButton = this.dialog.locator('button:has-text("Cancel")');

    // Delete confirmation dialog
    this.deleteDialog = page.locator('[role="alertdialog"], [role="dialog"]:has-text("Delete")');
    this.deleteConfirmButton = this.deleteDialog.locator('button:has-text("Delete"), button:has-text("Confirm")');
    this.deleteCancelButton = this.deleteDialog.locator('button:has-text("Cancel")');
  }

  /**
   * Navigate to blog tags page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/blog/tags');
  }

  /**
   * Verify page loaded using sequential wait pattern
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.createButton);
  }

  /**
   * Open create tag dialog
   */
  async openCreateDialog(): Promise<void> {
    await this.openDialogViaButton(this.createButton, this.dialog);
  }

  /**
   * Create a new tag
   */
  async createTag(data: {
    name: string;
    slug?: string;
    description?: string;
    color?: string;
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

    if (data.color) {
      await this.selectColor(data.color);
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Select a color in the color picker
   */
  private async selectColor(color: string): Promise<void> {
    // The ColorPicker component may have different implementations
    // Try common patterns
    const colorInput = this.dialog.locator('input[type="color"]');
    if (await colorInput.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false)) {
      await colorInput.fill(color);
      return;
    }

    // Try clicking a color swatch
    const colorSwatch = this.dialog.locator(`[data-color="${color}"], button[style*="${color}"]`);
    if (await colorSwatch.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false)) {
      await colorSwatch.click();
      return;
    }

    // Try text input for hex color
    const hexInput = this.dialog.locator('input[placeholder*="#"]');
    if (await hexInput.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false)) {
      await hexInput.fill(color);
    }
  }

  /**
   * Edit an existing tag
   */
  async editTag(tagName: string, newData: {
    name?: string;
    slug?: string;
    description?: string;
    color?: string;
  }): Promise<void> {
    await this.clickEditButton(tagName);

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

    if (newData.color) {
      await this.selectColor(newData.color);
    }

    await this.saveButton.click();
    await expect(this.dialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Click edit button for a tag
   */
  private async clickEditButton(tagName: string): Promise<void> {
    const row = this.page.locator(`tr:has-text("${tagName}")`).first();
    const actionsButton = row.locator('td:last-child button').first();
    await actionsButton.click();

    const dropdownMenu = this.page.locator('[role="menu"]');
    await expect(dropdownMenu).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    const editItem = dropdownMenu.locator('[role="menuitem"]:has-text("Edit")');
    await editItem.click();

    await expect(this.dialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Delete a tag
   */
  async deleteTag(name: string): Promise<void> {
    const row = this.page.locator(`tr:has-text("${name}")`).first();
    const actionsButton = row.locator('td:last-child button').first();
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
    const actionsButton = row.locator('td:last-child button').first();
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
   * Search tags
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
   * Get tag count
   */
  async getTagCount(): Promise<number> {
    const rows = this.tagsTable.locator('tbody tr');
    return await rows.count();
  }

  /**
   * Verify tag exists
   */
  async expectTagExists(name: string): Promise<void> {
    const tag = this.page.locator(`td:has-text("${name}")`).first();
    await expect(tag).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify tag does not exist
   */
  async expectTagNotExists(name: string): Promise<void> {
    const tag = this.page.locator(`td:has-text("${name}")`);
    await expect(tag).toBeHidden({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Get tag information from table row
   */
  async getTagInfo(name: string): Promise<{
    name: string;
    slug: string;
    color: string;
    postCount: number;
  }> {
    const row = this.page.locator(`tr:has-text("${name}")`).first();
    const cells = row.locator('td');

    return {
      name: await cells.nth(0).textContent() || '',
      slug: await cells.nth(1).textContent() || '',
      color: await cells.nth(2).textContent() || '',
      postCount: parseInt(await cells.nth(3).textContent() || '0', 10),
    };
  }

  /**
   * Verify tag has specific post count
   */
  async expectTagPostCount(name: string, count: number): Promise<void> {
    const row = this.page.locator(`tr:has-text("${name}")`).first();
    const postCountBadge = row.locator(`td:nth-child(4):has-text("${count}")`);
    await expect(postCountBadge).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify tag has specific color
   */
  async expectTagColor(name: string, color: string): Promise<void> {
    const row = this.page.locator(`tr:has-text("${name}")`).first();
    // Color is shown either as a colored dot or as text
    const colorIndicator = row.locator(`[style*="${color}"], code:has-text("${color}")`);
    await expect(colorIndicator).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Close the dialog without saving
   */
  async closeDialogWithoutSaving(): Promise<void> {
    await this.closeDialog(this.dialog, this.cancelButton);
  }

  /**
   * Check if empty state is displayed
   */
  async expectEmptyState(): Promise<void> {
    const emptyState = this.page.locator('text="No tags found"');
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
   * Verify dialog title matches create mode
   */
  async expectCreateDialogTitle(): Promise<void> {
    const title = this.dialog.locator('text="Create New Tag"');
    await expect(title).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify dialog title matches edit mode
   */
  async expectEditDialogTitle(): Promise<void> {
    const title = this.dialog.locator('text="Edit Tag"');
    await expect(title).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Get the row locator for a specific tag
   */
  getTagRow(name: string): Locator {
    return this.page.locator(`tr:has-text("${name}")`).first();
  }

  /**
   * Verify tag row displays color indicator
   */
  async expectTagHasColorIndicator(name: string): Promise<void> {
    const row = this.getTagRow(name);
    const colorDot = row.locator('div.rounded-full[style*="background"]');
    await expect(colorDot).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Open delete confirmation via row actions and verify dialog content
   */
  async openDeleteConfirmation(name: string): Promise<Locator> {
    const row = this.getTagRow(name);
    return await this.openDeleteConfirmation(row);
  }

  /**
   * Verify the tag count displayed in the card description
   */
  async expectTotalTagCount(count: number): Promise<void> {
    const countText = this.page.locator(`text="${count} tags"`);
    await expect(countText).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify auto-generated slug from name
   */
  async expectAutoGeneratedSlug(expectedSlug: string): Promise<void> {
    await expect(this.slugInput).toHaveValue(expectedSlug, { timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Fill name and verify slug is auto-generated
   */
  async fillNameAndVerifySlug(name: string): Promise<void> {
    await this.nameInput.fill(name);
    // Wait for slug to be auto-generated
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
    const expectedSlug = name.toLowerCase().replace(/[^a-z0-9\s-]/g, '').replace(/\s+/g, '-');
    await this.expectAutoGeneratedSlug(expectedSlug);
  }
}
