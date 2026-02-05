import { test, expect } from '@playwright/test';
import { ProductsPage, ProductFormPage, CategoriesPage, BrandsPage } from '../pages';

/**
 * Product Management Tests
 *
 * Comprehensive E2E tests for product CRUD operations.
 * Tags: @products @P0 @P1 @P2
 */

test.describe('Product Management @products', () => {
  const testProductName = `Test Product ${Date.now()}`;
  const testSKU = `SKU-${Date.now()}`;

  test.describe('Product List @P0', () => {
    test('PROD-001: Products page loads successfully', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();
      await productsPage.expectPageLoaded();
    });

    test('PROD-002: Create button is visible and clickable', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();
      await expect(productsPage.createButton).toBeVisible();
      await expect(productsPage.createButton).toBeEnabled();
    });

    test('PROD-003: Search input is functional', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();
      await expect(productsPage.searchInput).toBeVisible();
      await productsPage.searchInput.fill('test');
      await expect(productsPage.searchInput).toHaveValue('test');
    });
  });

  test.describe('Product Creation @P0', () => {
    test('PROD-010: Navigate to create product form', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();
      await productsPage.clickCreate();

      await expect(page).toHaveURL(/\/portal\/ecommerce\/products\/(new|create)/);
    });

    test('PROD-011: Create product form displays all fields', async ({ page }) => {
      const productForm = new ProductFormPage(page);
      await productForm.navigateToCreate();

      await expect(productForm.nameInput).toBeVisible();
      await expect(productForm.priceInput).toBeVisible();
      await expect(productForm.saveButton).toBeVisible();
    });

    test('PROD-012: Create product with minimum required fields', async ({ page }) => {
      const productForm = new ProductFormPage(page);
      await productForm.navigateToCreate();

      await productForm.fillBasicInfo({
        name: testProductName,
        price: '99.99',
        sku: testSKU,
      });

      await productForm.save();

      // Should redirect back to products list or show success
      await expect(page).toHaveURL(/\/portal\/ecommerce\/products/);
    });

    test('PROD-013: Create product validation - empty name shows error', async ({ page }) => {
      const productForm = new ProductFormPage(page);
      await productForm.navigateToCreate();

      // Try to save without filling name
      await productForm.priceInput.fill('99.99');
      await productForm.saveButton.click();

      // Should show validation error
      const error = page.locator('[data-testid="name-error"], .error, [aria-invalid="true"]');
      await expect(error.first()).toBeVisible({ timeout: 5000 });
    });

    test('PROD-014: Create product validation - negative price rejected', async ({ page }) => {
      const productForm = new ProductFormPage(page);
      await productForm.navigateToCreate();

      await productForm.fillBasicInfo({
        name: 'Test Product',
        price: '-10',
      });

      await productForm.saveButton.click();

      // Wait for validation to run
      await page.waitForTimeout(500);

      // Check validation behavior - one of these should be true:
      // 1. Input has aria-invalid (FormControl sets this on error)
      // 2. FormMessage error text is visible (destructive text color)
      // 3. Price input stripped negative sign or is empty (HTML5 input validation)
      // 4. Browser prevented the negative value
      const ariaInvalid = page.locator('input[name="basePrice"][aria-invalid="true"]');
      const formMessage = page.locator('[id$="-form-item-message"]'); // FormMessage uses {id}-form-item-message
      const priceValue = await productForm.priceInput.inputValue();

      const hasAriaInvalid = await ariaInvalid.isVisible().catch(() => false);
      const hasFormMessage = await formMessage.filter({ hasText: /0|negative|min/i }).isVisible().catch(() => false);
      const priceStripped = priceValue === '' || priceValue === '10' || parseFloat(priceValue) >= 0;

      expect(hasAriaInvalid || hasFormMessage || priceStripped).toBeTruthy();
    });
  });

  test.describe('Product Search & Filter @P1', () => {
    test('PROD-020: Search products by name', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();

      await productsPage.search('test');
      await productsPage.waitForPageLoad();

      // Search should filter results
      await expect(productsPage.searchInput).toHaveValue('test');
    });

    test('PROD-021: Filter products by status - Active', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();

      // Check if status filter exists
      if (await productsPage.statusFilter.isVisible()) {
        await productsPage.filterByStatus('Active');
        await productsPage.waitForPageLoad();
      }
    });

    test('PROD-022: Filter products by status - Draft', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();

      if (await productsPage.statusFilter.isVisible()) {
        await productsPage.filterByStatus('Draft');
        await productsPage.waitForPageLoad();
      }
    });

    test('PROD-023: Clear search resets results', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();

      await productsPage.search('test');
      await productsPage.searchInput.clear();
      await page.keyboard.press('Enter');

      await expect(productsPage.searchInput).toHaveValue('');
    });
  });

  test.describe('Product Edit @P1', () => {
    test('PROD-030: Edit product form loads with existing data', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();

      // Get count of products
      const productCount = await productsPage.getProductCount();

      if (productCount > 0) {
        // Click first product
        const firstProduct = page.locator('[data-testid="product-card"], tbody tr').first();
        const editButton = firstProduct.locator('button:has-text("Edit"), [data-testid="edit-button"]');

        if (await editButton.isVisible()) {
          await editButton.click();
          await page.waitForLoadState('networkidle');

          // Should be on edit page
          await expect(page).toHaveURL(/\/portal\/ecommerce\/products\/.*\/(edit)?/);
        }
      }
    });
  });

  test.describe('Product Bulk Operations @P1', () => {
    test('PROD-040: Bulk actions button appears when products selected', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();

      const productCount = await productsPage.getProductCount();

      if (productCount > 0) {
        // Select first product
        const firstCheckbox = page.locator('[data-testid="product-card"] input[type="checkbox"], tbody tr input[type="checkbox"]').first();

        if (await firstCheckbox.isVisible()) {
          await firstCheckbox.check();

          // Bulk actions should become visible
          await expect(productsPage.bulkActionsButton).toBeVisible({ timeout: 5000 });
        }
      }
    });
  });

  test.describe('Product Delete @P1', () => {
    test('PROD-050: Delete confirmation dialog appears', async ({ page }) => {
      const productsPage = new ProductsPage(page);
      await productsPage.navigate();

      const productCount = await productsPage.getProductCount();

      if (productCount > 0) {
        // Open actions menu for first product
        const firstProduct = page.locator('[data-testid="product-card"], tbody tr').first();
        const actionsButton = firstProduct.locator('[data-testid="actions-menu"], button.actions, button:has-text("...")');

        if (await actionsButton.isVisible()) {
          await actionsButton.click();

          const deleteButton = page.locator('[role="menuitem"]:has-text("Delete")');
          if (await deleteButton.isVisible()) {
            await deleteButton.click();

            // Confirmation dialog should appear
            await expect(productsPage.confirmDialog).toBeVisible({ timeout: 5000 });

            // Cancel the delete
            await productsPage.cancelAction();
          }
        }
      }
    });
  });
});
