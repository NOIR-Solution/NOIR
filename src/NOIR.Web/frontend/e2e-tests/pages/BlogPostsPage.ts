import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * BlogPostsPage - Page Object for Blog Posts Management
 *
 * Based on: src/pages/portal/blog/posts/BlogPostsPage.tsx
 * - Create button uses Link to "/portal/blog/posts/new" with "New Post" text
 * - Uses DataTable component with dropdown actions
 * - Status filter uses shadcn Select
 * - Category filter uses shadcn Select
 * - Pagination component for multi-page navigation
 */
export class BlogPostsPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly searchButton: Locator;
  readonly statusFilter: Locator;
  readonly categoryFilter: Locator;
  readonly postsTable: Locator;
  readonly pageHeader: Locator;
  readonly pagination: Locator;

  // Delete confirmation dialog elements
  readonly deleteDialog: Locator;
  readonly deleteConfirmButton: Locator;
  readonly deleteCancelButton: Locator;

  constructor(page: Page) {
    super(page);

    // The create button is a Link with "New Post" text
    this.createButton = page.locator('a:has-text("New Post"), button:has-text("New Post")');
    this.searchInput = page.locator('input[placeholder*="Search posts"]').first();
    this.searchButton = page.locator('button[type="submit"]:has-text("Search")');
    // Status filter Select - positioned after search form, before category filter
    // Uses w-32 class to identify it (category filter uses w-40)
    this.statusFilter = page.locator('button[role="combobox"].w-32, button[role="combobox"]:has-text("All Status"), button[role="combobox"]:has-text("Status")').first();
    this.categoryFilter = page.locator('button[role="combobox"]:has-text("All Categories"), button[role="combobox"]:has-text("Category")');
    this.postsTable = page.locator('table');
    this.pageHeader = page.locator('h1:has-text("Blog Posts")');
    this.pagination = page.locator('[data-testid="pagination"], nav[aria-label="pagination"]');

    // Delete confirmation dialog - Radix AlertDialog
    this.deleteDialog = page.locator('[role="alertdialog"], [role="dialog"]:has-text("Delete")');
    this.deleteConfirmButton = this.deleteDialog.locator('button:has-text("Delete"), button:has-text("Confirm")');
    this.deleteCancelButton = this.deleteDialog.locator('button:has-text("Cancel")');
  }

  /**
   * Navigate to blog posts page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/blog/posts');
  }

  /**
   * Verify page loaded using sequential wait pattern
   * Waits for header first (proves render), then create button (proves data loaded)
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.createButton);
  }

  /**
   * Click create button to navigate to post editor
   */
  async clickCreatePost(): Promise<void> {
    await expect(this.createButton.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.createButton.first()).toBeEnabled({ timeout: Timeouts.ELEMENT_ENABLED });
    await this.createButton.first().click();
    await this.waitForPageLoad();
  }

  /**
   * Search for posts by query
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.searchButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Clear search input
   */
  async clearSearch(): Promise<void> {
    await this.searchInput.clear();
    await this.searchButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Filter posts by status
   */
  async filterByStatus(status: 'All Status' | 'Draft' | 'Published' | 'Scheduled' | 'Archived'): Promise<void> {
    await this.statusFilter.click();
    const selectContent = this.page.locator('[role="listbox"]');
    await expect(selectContent).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    const option = selectContent.locator(`[role="option"]:has-text("${status}")`);
    await expect(option).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await option.click();

    await expect(selectContent).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.waitForPageLoad();
  }

  /**
   * Filter posts by category
   */
  async filterByCategory(categoryName: string): Promise<void> {
    await this.categoryFilter.click();
    const selectContent = this.page.locator('[role="listbox"]');
    await expect(selectContent).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    const option = selectContent.locator(`[role="option"]:has-text("${categoryName}")`);
    await expect(option).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await option.click();

    await expect(selectContent).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.waitForPageLoad();
  }

  /**
   * Get the row locator for a specific post by title
   */
  getPostRow(title: string): Locator {
    return this.page.locator(`tr:has-text("${title}")`).first();
  }

  /**
   * Click View action for a post
   */
  async viewPost(title: string): Promise<void> {
    const row = this.getPostRow(title);
    await this.openRowActionsMenu(row);

    const viewItem = this.page.locator('[role="menuitem"]:has-text("View")');
    await viewItem.click();
    await this.waitForPageLoad();
  }

  /**
   * Click Edit action for a post
   */
  async editPost(title: string): Promise<void> {
    const row = this.getPostRow(title);
    await this.openRowActionsMenu(row);

    const editItem = this.page.locator('[role="menuitem"]:has-text("Edit")');
    await editItem.click();
    await this.waitForPageLoad();
  }

  /**
   * Click Publish action for a draft post
   */
  async publishPost(title: string): Promise<void> {
    const row = this.getPostRow(title);
    await this.openRowActionsMenu(row);

    const publishItem = this.page.locator('[role="menuitem"]:has-text("Publish")');
    await publishItem.click();
    await this.waitForPageLoad();
  }

  /**
   * Delete a post
   */
  async deletePost(title: string): Promise<void> {
    const row = this.getPostRow(title);
    await this.openRowActionsMenu(row);

    const deleteItem = this.page.locator('[role="menuitem"]:has-text("Delete")');
    await deleteItem.click();

    // Wait for confirmation dialog
    await expect(this.deleteDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
    await this.deleteConfirmButton.click();
    await expect(this.deleteDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.expectSuccessToast();
  }

  /**
   * Cancel delete action
   */
  async cancelDelete(title: string): Promise<void> {
    const row = this.getPostRow(title);
    await this.openRowActionsMenu(row);

    const deleteItem = this.page.locator('[role="menuitem"]:has-text("Delete")');
    await deleteItem.click();

    // Wait for confirmation dialog
    await expect(this.deleteDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
    await this.deleteCancelButton.click();
    await expect(this.deleteDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Open row actions dropdown menu
   */
  private async openRowActionsMenu(row: Locator): Promise<void> {
    const actionsButton = row.locator('td:last-child button').first();
    await actionsButton.click();

    const dropdownMenu = this.page.locator('[role="menu"]');
    await expect(dropdownMenu).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });
  }

  /**
   * Get the total count of posts displayed
   */
  async getPostCount(): Promise<number> {
    const rows = this.postsTable.locator('tbody tr');
    return await rows.count();
  }

  /**
   * Verify a post exists in the list
   */
  async expectPostExists(title: string): Promise<void> {
    const post = this.page.locator(`td:has-text("${title}")`).first();
    await expect(post).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify a post does not exist in the list
   */
  async expectPostNotExists(title: string): Promise<void> {
    const post = this.page.locator(`td:has-text("${title}")`);
    await expect(post).toBeHidden({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify post has a specific status
   */
  async expectPostStatus(title: string, status: string): Promise<void> {
    const row = this.getPostRow(title);
    const statusBadge = row.locator(`td:has-text("${status}")`);
    await expect(statusBadge).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Navigate to a specific page in pagination
   */
  async goToPage(pageNumber: number): Promise<void> {
    const pageButton = this.pagination.locator(`button:has-text("${pageNumber}")`);
    await pageButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Navigate to next page
   */
  async goToNextPage(): Promise<void> {
    const nextButton = this.pagination.locator('button[aria-label="Go to next page"]');
    await nextButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Navigate to previous page
   */
  async goToPreviousPage(): Promise<void> {
    const prevButton = this.pagination.locator('button[aria-label="Go to previous page"]');
    await prevButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Check if empty state is displayed
   */
  async expectEmptyState(): Promise<void> {
    const emptyState = this.page.locator('text="No posts found"');
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
   * Get post information from a row
   */
  async getPostInfo(title: string): Promise<{
    title: string;
    status: string;
    category: string;
    views: string;
  }> {
    const row = this.getPostRow(title);
    const cells = row.locator('td');

    return {
      title: await cells.nth(0).textContent() || '',
      status: await cells.nth(1).textContent() || '',
      category: await cells.nth(2).textContent() || '',
      views: await cells.nth(3).textContent() || '',
    };
  }
}
