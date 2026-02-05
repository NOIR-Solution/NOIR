import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * ProductsPage - Page Object for Product Management
 *
 * Based on: src/pages/portal/ecommerce/products/ProductsPage.tsx
 * - Create button is a Link to /portal/ecommerce/products/new with Button inside
 * - Search input with placeholder "Search products..."
 * - Table or Grid view toggle
 * - Status/Category filters using Select components
 */
export class ProductsPage extends BasePage {
  // List view elements
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly productGrid: Locator;
  readonly productTable: Locator;
  readonly productRows: Locator;
  readonly statusFilter: Locator;
  readonly categoryFilter: Locator;
  readonly bulkActionsButton: Locator;
  readonly selectAllCheckbox: Locator;
  readonly pagination: Locator;
  readonly emptyState: Locator;
  readonly viewModeToggle: Locator;
  readonly pageHeader: Locator;

  constructor(page: Page) {
    super(page);

    // The create button is wrapped in a Link element
    this.createButton = page.locator('a[href="/portal/ecommerce/products/new"] button, button:has-text("New Product")');
    // Use specific placeholder to avoid matching command palette - use first() to avoid strict mode
    this.searchInput = page.locator('input[placeholder*="Search products"]').first();
    this.productGrid = page.locator('.grid.grid-cols-1');
    this.productTable = page.locator('table');
    this.productRows = page.locator('tbody tr');
    this.statusFilter = page.locator('button:has-text("Status"), [aria-label*="Status"]').first();
    this.categoryFilter = page.locator('button:has-text("Category"), [aria-label*="Category"]').first();
    this.bulkActionsButton = page.locator('button:has-text("Publish"), button:has-text("Archive")');
    this.selectAllCheckbox = page.locator('thead input[type="checkbox"], th input[type="checkbox"]');
    this.pagination = page.locator('nav[aria-label="pagination"], .pagination');
    this.emptyState = page.locator('.empty-state, [class*="EmptyState"]');
    this.viewModeToggle = page.locator('button[aria-label*="view"], button:has-text("Grid"), button:has-text("List")');
    this.pageHeader = page.locator('h1:has-text("Products"), [class*="PageHeader"]');
  }

  /**
   * Navigate to products page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/ecommerce/products');
    // Ensure we're on the products page
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify products page loaded - check for page header or page content
   */
  async expectPageLoaded(): Promise<void> {
    // Wait for page content - look for Products header or All Products section
    const pageContent = this.page.locator(
      'h1:has-text("Products"), ' +
      ':text("All Products"), ' +
      'a[href="/portal/ecommerce/products/new"]'
    );
    await expect(pageContent.first()).toBeVisible({ timeout: 15000 });
  }

  /**
   * Click create product button (Link to new product page)
   */
  async clickCreate(): Promise<void> {
    // Ensure button is visible before clicking
    await expect(this.createButton.first()).toBeVisible({ timeout: 10000 });
    await this.createButton.first().click();
    await this.waitForPageLoad();
  }

  /**
   * Search for products
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.page.keyboard.press('Enter');
    await this.waitForPageLoad();
  }

  /**
   * Get product count from table rows
   */
  async getProductCount(): Promise<number> {
    await this.page.waitForTimeout(500); // Wait for data to load
    return await this.productRows.count();
  }

  /**
   * Click on a product by name
   */
  async clickProduct(name: string): Promise<void> {
    const product = this.page.locator(`tr:has-text("${name}")`).first();
    await product.click();
    await this.waitForPageLoad();
  }

  /**
   * Open product actions menu (three dots button)
   */
  async openProductActions(productName: string): Promise<void> {
    const product = this.page.locator(`tr:has-text("${productName}")`).first();
    const actionsButton = product.locator('button:has(svg[class*="MoreHorizontal"]), button[aria-label*="action"], button:last-child');
    await actionsButton.click();
  }

  /**
   * Delete a product
   */
  async deleteProduct(productName: string): Promise<void> {
    await this.openProductActions(productName);
    const deleteButton = this.page.locator('[role="menuitem"]:has-text("Delete")');
    await deleteButton.click();
    await this.confirmAction();
    await this.expectSuccessToast();
  }

  /**
   * Filter by status using Select dropdown
   */
  async filterByStatus(status: 'Active' | 'Draft' | 'Archived' | 'All'): Promise<void> {
    await this.statusFilter.click();
    const option = this.page.locator(`[role="option"]:has-text("${status}")`);
    await option.click();
    await this.waitForPageLoad();
  }

  /**
   * Select multiple products by checkbox
   */
  async selectProducts(productNames: string[]): Promise<void> {
    for (const name of productNames) {
      const checkbox = this.page.locator(`tr:has-text("${name}") input[type="checkbox"]`).first();
      await checkbox.check();
    }
  }

  /**
   * Verify product exists in list
   */
  async expectProductInList(productName: string): Promise<void> {
    const product = this.page.locator(`tr:has-text("${productName}"), td:has-text("${productName}")`).first();
    await expect(product).toBeVisible({ timeout: 10000 });
  }

  /**
   * Verify product not in list
   */
  async expectProductNotInList(productName: string): Promise<void> {
    const product = this.page.locator(`tr:has-text("${productName}")`);
    await expect(product).toBeHidden({ timeout: 10000 });
  }
}
