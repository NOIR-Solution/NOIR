import { type Page, type Locator, expect } from '@playwright/test';

export class ProductsPage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly productTable: Locator;
  readonly productRows: Locator;
  readonly statusFilter: Locator;

  constructor(private page: Page) {
    this.createButton = page.getByRole('link', { name: /create|add|new/i }).or(
      page.getByRole('button', { name: /create|add|new/i }),
    ).first();
    this.searchInput = page.getByLabel(/search products/i).or(
      page.getByPlaceholder(/search products/i),
    ).first();
    this.productTable = page.getByRole('table');
    this.productRows = page.getByRole('table').getByRole('row');
    // Use only the combobox — the page also has clickable status-card buttons with
    // aria-label containing "status" which would cause strict-mode violations
    this.statusFilter = page.getByRole('combobox', { name: /filter by status/i }).first();
  }

  async goto() {
    await this.page.goto('/portal/ecommerce/products');
    await this.page.waitForLoadState('networkidle');
  }

  async createProduct(data: { name: string; sku: string; price?: number }) {
    await this.createButton.click();
    await this.page.waitForURL('**/products/new');
    await this.page.getByLabel(/name/i).first().fill(data.name);
    await this.page.getByLabel(/sku/i).fill(data.sku);
    if (data.price !== undefined) {
      await this.page.getByLabel(/price/i).first().fill(String(data.price));
    }
    await this.page.getByRole('button', { name: /save|create|submit/i }).click();
  }

  async editProduct(name: string) {
    await this.page.getByRole('row', { name: new RegExp(name, 'i') })
      .getByRole('link', { name: /edit/i })
      .or(this.page.getByRole('row', { name: new RegExp(name, 'i') }).getByRole('button', { name: /edit/i }))
      .click();
  }

  async searchProduct(query: string) {
    await this.searchInput.fill(query);
    await this.page.waitForResponse(resp =>
      resp.url().includes('/api/products') && resp.status() === 200,
    );
  }

  async filterByStatus(status: string) {
    await this.statusFilter.click();
    await this.page.getByRole('option', { name: new RegExp(status, 'i') }).click();
    await this.page.waitForResponse(resp =>
      resp.url().includes('/api/products') && resp.status() === 200,
    );
  }

  async expectProductInList(name: string) {
    await expect(this.page.getByRole('row', { name: new RegExp(name, 'i') })).toBeVisible();
  }

  async expectProductCount(n: number) {
    await expect(this.productRows).toHaveCount(n + 1); // +1 for header row
  }
}
