import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * ProductFormPage - Page Object for Create/Edit Product
 */
export class ProductFormPage extends BasePage {
  // Form fields
  readonly nameInput: Locator;
  readonly slugInput: Locator;
  readonly descriptionInput: Locator;
  readonly skuInput: Locator;
  readonly priceInput: Locator;
  readonly comparePriceInput: Locator;
  readonly costInput: Locator;
  readonly stockInput: Locator;
  readonly categorySelect: Locator;
  readonly brandSelect: Locator;
  readonly statusSelect: Locator;

  // Sections
  readonly basicInfoSection: Locator;
  readonly pricingSection: Locator;
  readonly inventorySection: Locator;
  readonly imagesSection: Locator;
  readonly variantsSection: Locator;
  readonly attributesSection: Locator;
  readonly seoSection: Locator;

  // Actions
  readonly saveButton: Locator;
  readonly saveDraftButton: Locator;
  readonly publishButton: Locator;
  readonly cancelButton: Locator;
  readonly deleteButton: Locator;

  // Image upload
  readonly imageUploadZone: Locator;
  readonly imageGallery: Locator;

  // Variants
  readonly addVariantButton: Locator;
  readonly variantTable: Locator;
  readonly generateVariantsButton: Locator;

  constructor(page: Page) {
    super(page);

    // Form fields
    this.nameInput = page.locator('input[name="name"], [data-testid="product-name-input"]');
    this.slugInput = page.locator('input[name="slug"], [data-testid="product-slug-input"]');
    this.descriptionInput = page.locator('textarea[name="description"], [data-testid="product-description-input"], .tiptap, .ProseMirror');
    this.skuInput = page.locator('input[name="sku"], [data-testid="product-sku-input"]');
    // Product form uses "basePrice" field name (see ProductFormPage.tsx:1070)
    this.priceInput = page.locator('input[name="basePrice"]');
    this.comparePriceInput = page.locator('input[name="compareAtPrice"], [data-testid="compare-price-input"]');
    this.costInput = page.locator('input[name="cost"], [data-testid="cost-input"]');
    this.stockInput = page.locator('input[name="stock"], input[name="quantity"], [data-testid="stock-input"]');
    this.categorySelect = page.locator('[data-testid="category-select"], button:has-text("Category")');
    this.brandSelect = page.locator('[data-testid="brand-select"], button:has-text("Brand")');
    this.statusSelect = page.locator('[data-testid="status-select"]');

    // Sections
    this.basicInfoSection = page.locator('[data-testid="basic-info-section"]');
    this.pricingSection = page.locator('[data-testid="pricing-section"]');
    this.inventorySection = page.locator('[data-testid="inventory-section"]');
    this.imagesSection = page.locator('[data-testid="images-section"]');
    this.variantsSection = page.locator('[data-testid="variants-section"]');
    this.attributesSection = page.locator('[data-testid="attributes-section"]');
    this.seoSection = page.locator('[data-testid="seo-section"]');

    // Actions
    this.saveButton = page.locator('button:has-text("Save"), [data-testid="save-button"]');
    this.saveDraftButton = page.locator('button:has-text("Save Draft"), [data-testid="save-draft-button"]');
    this.publishButton = page.locator('button:has-text("Publish"), [data-testid="publish-button"]');
    this.cancelButton = page.locator('button:has-text("Cancel"), [data-testid="cancel-button"]');
    this.deleteButton = page.locator('button:has-text("Delete"), [data-testid="delete-button"]');

    // Images
    this.imageUploadZone = page.locator('[data-testid="image-upload-zone"], .dropzone, input[type="file"]');
    this.imageGallery = page.locator('[data-testid="image-gallery"]');

    // Variants
    this.addVariantButton = page.locator('button:has-text("Add Variant"), [data-testid="add-variant-button"]');
    this.variantTable = page.locator('[data-testid="variant-table"], table');
    this.generateVariantsButton = page.locator('button:has-text("Generate"), [data-testid="generate-variants-button"]');
  }

  /**
   * Navigate to create product page
   */
  async navigateToCreate(): Promise<void> {
    await this.goto('/portal/ecommerce/products/new');
    // Wait for page to load - look for form header or name input
    await this.page.waitForLoadState('networkidle').catch(() => {});
    const pageHeader = this.page.locator('h1:has-text("New Product")');
    await expect(pageHeader).toBeVisible({ timeout: 15000 });
    // Ensure form is interactive
    await expect(this.nameInput).toBeVisible({ timeout: 10000 });
  }

  /**
   * Navigate to edit product page
   */
  async navigateToEdit(productId: string): Promise<void> {
    await this.goto(`/portal/ecommerce/products/${productId}/edit`);
  }

  /**
   * Fill basic product information
   */
  async fillBasicInfo(data: {
    name: string;
    description?: string;
    sku?: string;
    price: string;
    comparePrice?: string;
    cost?: string;
  }): Promise<void> {
    await this.nameInput.fill(data.name);

    if (data.description) {
      await this.descriptionInput.fill(data.description);
    }

    if (data.sku) {
      await this.skuInput.fill(data.sku);
    }

    await this.priceInput.fill(data.price);

    if (data.comparePrice) {
      await this.comparePriceInput.fill(data.comparePrice);
    }

    if (data.cost) {
      await this.costInput.fill(data.cost);
    }
  }

  /**
   * Set inventory quantity
   */
  async setStock(quantity: string): Promise<void> {
    await this.stockInput.fill(quantity);
  }

  /**
   * Select category
   */
  async selectCategory(categoryName: string): Promise<void> {
    await this.categorySelect.click();
    const option = this.page.locator(`[role="option"]:has-text("${categoryName}")`).first();
    await option.click();
  }

  /**
   * Select brand
   */
  async selectBrand(brandName: string): Promise<void> {
    await this.brandSelect.click();
    const option = this.page.locator(`[role="option"]:has-text("${brandName}")`).first();
    await option.click();
  }

  /**
   * Save product
   */
  async save(): Promise<void> {
    await this.saveButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Save as draft
   */
  async saveAsDraft(): Promise<void> {
    await this.saveDraftButton.click();
    await this.expectSuccessToast();
  }

  /**
   * Publish product
   */
  async publish(): Promise<void> {
    await this.publishButton.click();
    await this.expectSuccessToast();
  }

  /**
   * Cancel and go back
   */
  async cancel(): Promise<void> {
    await this.cancelButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Verify form has validation error
   */
  async expectValidationError(fieldName: string): Promise<void> {
    const error = this.page.locator(`[data-testid="${fieldName}-error"], .error:near(:text("${fieldName}")), [aria-invalid="true"]`).first();
    await expect(error).toBeVisible({ timeout: 5000 });
  }

  /**
   * Upload product image
   */
  async uploadImage(filePath: string): Promise<void> {
    const fileInput = this.page.locator('input[type="file"]').first();
    await fileInput.setInputFiles(filePath);
    await this.waitForPageLoad();
  }

  /**
   * Add a variant
   */
  async addVariant(data: {
    sku: string;
    price: string;
    stock: string;
    options?: Record<string, string>;
  }): Promise<void> {
    await this.addVariantButton.click();

    // Fill variant form in dialog/row
    const variantDialog = this.page.locator('[role="dialog"], .variant-form').first();

    await variantDialog.locator('input[name="sku"]').fill(data.sku);
    await variantDialog.locator('input[name="price"]').fill(data.price);
    await variantDialog.locator('input[name="stock"]').fill(data.stock);

    // Save variant
    await variantDialog.locator('button:has-text("Save"), button:has-text("Add")').click();
  }

  /**
   * Verify product was saved successfully
   */
  async expectSaveSuccess(): Promise<void> {
    await this.expectSuccessToast();
    // Should redirect to products list or stay on edit page
    await expect(this.page).toHaveURL(/\/portal\/ecommerce\/products/);
  }
}
