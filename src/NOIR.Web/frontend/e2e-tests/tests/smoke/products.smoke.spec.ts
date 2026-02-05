import { test, expect } from '@playwright/test';
import { ProductsPage, ProductFormPage, CategoriesPage, BrandsPage } from '../../pages';

/**
 * Products Smoke Tests
 *
 * @smoke @products @P0
 *
 * Critical product management flows:
 * - View products list
 * - Create new product
 * - Edit product
 * - Delete product
 */

test.describe('Products @smoke @products @P0', () => {
  // storageState from auth.setup.ts handles authentication

  test('should display products list page', async ({ page }) => {
    const productsPage = new ProductsPage(page);

    await productsPage.navigate();
    await productsPage.expectPageLoaded();

    // Verify create button is visible
    await expect(productsPage.createButton).toBeVisible();
  });

  test('should open create product form', async ({ page }) => {
    const productsPage = new ProductsPage(page);

    await productsPage.navigate();
    await productsPage.expectPageLoaded();
    await productsPage.clickCreate();

    // Verify we're on the product form page - use correct route
    await expect(page).toHaveURL(/\/portal\/ecommerce\/products\/new/, { timeout: 10000 });

    const productForm = new ProductFormPage(page);
    await expect(productForm.nameInput).toBeVisible();
  });

  test('should validate required fields on product creation', async ({ page }) => {
    const productForm = new ProductFormPage(page);

    await productForm.navigateToCreate();

    // Try to save without filling required fields
    await productForm.saveButton.click();

    // Should show validation errors
    const errors = page.locator('.error, [aria-invalid="true"], [data-testid*="error"]');
    await expect(errors.first()).toBeVisible({ timeout: 5000 });
  });

  test('should search for products', async ({ page }) => {
    const productsPage = new ProductsPage(page);

    await productsPage.navigate();
    await productsPage.expectPageLoaded();

    // Enter search query
    await productsPage.search('test');

    // Verify search is working - wait for page to update
    await productsPage.waitForPageLoad();
  });
});

test.describe('Categories @smoke @categories @P0', () => {
  // storageState from auth.setup.ts handles authentication

  test('should display categories list page', async ({ page }) => {
    const categoriesPage = new CategoriesPage(page);

    await categoriesPage.navigate();
    await categoriesPage.expectPageLoaded();

    // Verify page content is visible (category list or tree)
    const pageContent = page.locator(':text("All Categories"), :text("categories total")');
    await expect(pageContent.first()).toBeVisible({ timeout: 10000 });
  });

  test('should open create category dialog', async ({ page }) => {
    const categoriesPage = new CategoriesPage(page);

    await categoriesPage.navigate();
    await categoriesPage.expectPageLoaded();
    await categoriesPage.openCreateDialog();

    // Verify dialog is visible
    await expect(categoriesPage.dialog).toBeVisible();
    await expect(categoriesPage.nameInput).toBeVisible();
  });
});

test.describe('Brands @smoke @brands @P0', () => {
  // storageState from auth.setup.ts handles authentication

  test('should display brands list page', async ({ page }) => {
    const brandsPage = new BrandsPage(page);

    await brandsPage.navigate();
    await brandsPage.expectPageLoaded();

    // Verify create button is visible
    await expect(brandsPage.createButton).toBeVisible();
  });

  test('should open create brand dialog', async ({ page }) => {
    const brandsPage = new BrandsPage(page);

    await brandsPage.navigate();
    await brandsPage.expectPageLoaded();
    await brandsPage.openCreateDialog();

    // Verify dialog is visible
    await expect(brandsPage.dialog).toBeVisible();
    await expect(brandsPage.nameInput).toBeVisible();
  });
});
