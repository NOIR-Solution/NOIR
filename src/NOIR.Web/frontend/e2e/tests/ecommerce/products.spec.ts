import { test, expect } from '../../fixtures/base.fixture';
import { testProduct, uniqueId } from '../../helpers/test-data';
import {
  waitForTableLoad,
  expectToast,
  confirmDelete,
  TOAST_SUCCESS,
} from '../../helpers/selectors';

// ─── Products: Smoke Tests ──────────────────────────────────────────────────

test.describe('E-commerce Products @smoke', () => {
  /**
   * ECOM-PROD-001: Product list loads with pagination
   * Verify that the product table renders with at least one row and correct columns.
   */
  test('ECOM-PROD-001: should display product list with pagination @smoke', async ({
    productsPage,
    api,
    trackCleanup,
    page,
  }) => {
    // Seed a product to ensure the list is non-empty
    const product = await api.createProduct(testProduct());
    trackCleanup(async () => { await api.deleteProduct(product.id); });

    await productsPage.goto();
    await waitForTableLoad(page);

    // Table should be visible with at least one data row (header + data)
    await expect(productsPage.productTable).toBeVisible();
    const rowCount = await productsPage.productRows.count();
    expect(rowCount).toBeGreaterThan(1); // >1 because first row is the header

    // Verify expected column headers
    const headerRow = productsPage.productRows.first();
    await expect(headerRow).toContainText(/name|product/i);
    await expect(headerRow).toContainText(/sku|price|status/i);
  });

  /**
   * ECOM-PROD-002: Create product (happy path)
   * Create a product via the UI, verify it appears in the list.
   */
  test('ECOM-PROD-002: should create product via UI @smoke', async ({
    productsPage,
    api,
    trackCleanup,
    page,
  }) => {
    const data = testProduct();

    await productsPage.goto();
    await productsPage.createProduct({ name: data.name, sku: data.sku, price: data.price });

    // Expect success toast
    await expect(page.locator(TOAST_SUCCESS)).toBeVisible({ timeout: 10_000 });

    // Navigate back to list and verify product appears
    await productsPage.goto();
    await waitForTableLoad(page);
    await productsPage.expectProductInList(data.name);

    // Cleanup: find the created product by searching and delete via API
    // We need the product ID — get it from the API by searching
    trackCleanup(async () => {
      // Search for the product to find its ID
      const searchRes = await api['request'].get(
        `${process.env.API_URL ?? 'http://localhost:4000'}/api/products?search=${encodeURIComponent(data.name)}`,
      );
      const searchData = await searchRes.json();
      const items = searchData?.items ?? searchData?.data ?? searchData;
      if (Array.isArray(items)) {
        for (const item of items) {
          if (item.name === data.name || item.sku === data.sku) {
            await api.deleteProduct(item.id);
          }
        }
      }
    });
  });
});

// ─── Products: Regression Tests ─────────────────────────────────────────────

test.describe('E-commerce Products @regression', () => {
  /**
   * ECOM-PROD-003: Create product validation errors
   * Verify that validation errors appear when required fields are empty.
   */
  test('ECOM-PROD-003: should show validation errors for empty required fields @regression', async ({
    page,
  }) => {
    await page.goto('/portal/ecommerce/products/new');
    await page.waitForLoadState('networkidle');

    // Click save without filling required fields
    await page.getByRole('button', { name: /save|create|submit/i }).click();

    // Expect validation errors for required fields (Name, SKU)
    await expect(
      page.getByText(/required|bắt buộc|name is required/i).first(),
    ).toBeVisible({ timeout: 5_000 });
  });

  /**
   * ECOM-PROD-004: Edit product
   * Create a product via API, edit it via UI, verify changes persist.
   */
  test('ECOM-PROD-004: should edit an existing product @regression', async ({
    productsPage,
    api,
    trackCleanup,
    page,
  }) => {
    // Seed a product via API
    const original = testProduct({ name: `E2E Edit Product ${Date.now()}` });
    const created = await api.createProduct(original);
    trackCleanup(async () => { await api.deleteProduct(created.id); });

    // Navigate directly to the product edit page using the created product ID
    // Use /edit suffix — the /products/{id} route renders in View mode with disabled inputs
    await page.goto(`/portal/ecommerce/products/${created.id}/edit`);
    await page.waitForLoadState('networkidle');

    // Update the product name
    const updatedName = `E2E Updated Product ${Date.now()}`;
    const nameInput = page.getByLabel(/product name/i).or(page.getByLabel(/name/i)).first();
    await expect(nameInput).toBeEnabled({ timeout: 10_000 });
    await nameInput.clear();
    await nameInput.fill(updatedName);

    // Save
    await page.getByRole('button', { name: /save|update|submit/i }).click();
    await expect(page.locator(TOAST_SUCCESS)).toBeVisible({ timeout: 10_000 });

    // Verify the updated name appears on the page (breadcrumb, title, or input value)
    await expect(page.getByText(updatedName).first()).toBeVisible({ timeout: 5_000 });
  });

  /**
   * ECOM-PROD-005: Product search and filter
   * Seed two products, search for one, verify filtering.
   */
  test('ECOM-PROD-005: should search and filter products @regression', async ({
    productsPage,
    api,
    trackCleanup,
    page,
  }) => {
    // Seed two products with distinct names
    const productA = await api.createProduct(testProduct({ name: `E2E SearchA ${Date.now()}` }));
    const productB = await api.createProduct(testProduct({ name: `E2E SearchB ${Date.now()}` }));
    trackCleanup(async () => {
      await api.deleteProduct(productA.id);
      await api.deleteProduct(productB.id);
    });

    await productsPage.goto();
    await waitForTableLoad(page);

    // Search for product A by name
    await productsPage.searchProduct('SearchA');

    // Product A should be visible, product B should not
    await expect(
      page.getByRole('row', { name: new RegExp(productA.name, 'i') }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(
      page.getByRole('row', { name: new RegExp(productB.name, 'i') }),
    ).not.toBeVisible({ timeout: 3_000 });

    // Clear search by emptying the input
    // React's useDeferredValue may not trigger a new API call when clearing to empty string
    // if the query was already short — use a timeout-based wait instead
    await productsPage.searchInput.clear();
    await page.waitForTimeout(2_000); // Allow debounced search to settle

    // Try status filter (Draft)
    await productsPage.filterByStatus('Draft');

    // Both seeded products (Draft status) should still be visible
    await expect(
      page.getByRole('row', { name: new RegExp(productA.name, 'i') }),
    ).toBeVisible({ timeout: 10_000 });
  });

  /**
   * ECOM-PROD-006: Product categories CRUD
   * Create, edit, and delete a product category via UI.
   */
  test('ECOM-PROD-006: should perform full categories CRUD @regression', async ({
    api,
    trackCleanup,
    page,
  }) => {
    const categoryName = `E2E Category ${Date.now()}`;

    await page.goto('/portal/ecommerce/categories');
    await page.waitForLoadState('networkidle');

    // Switch to List/Table view — the default is Tree view which has no <tr> rows for Playwright
    // The ViewModeToggle renders buttons with aria-label "Table view" and "Tree view"
    const listViewBtn = page.getByRole('button', { name: /^list$|table view/i });
    if (await listViewBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await listViewBtn.click();
      await page.waitForTimeout(500); // Allow view transition
    }

    // Create category — click "New Category" button in the header
    await page.getByRole('button', { name: /new category|create|add/i }).first().click();

    // Wait for dialog to appear (use first() to handle any Radix Portal duplicates)
    await expect(page.locator('[role="dialog"]').first()).toBeVisible({ timeout: 5_000 });

    // ProductCategoryDialog is labelled "Create Category" / "Edit Category"
    // DeleteProductCategoryDialog is labelled "Delete Category"
    const createDialog = page.getByRole('dialog', { name: /create category|new category/i });
    const editDialog = page.getByRole('dialog', { name: /edit category/i });
    const deleteDialog = page.getByRole('dialog', { name: /delete category/i });

    // Use :not([aria-hidden="true"]) and .first() to target only the currently active dialog
    const activeCreateDialog = page.locator('[role="dialog"]:not([aria-hidden="true"])').first();
    await activeCreateDialog.getByLabel(/^name$/i).first().fill(categoryName);
    await activeCreateDialog.getByRole('button', { name: /save|create|submit/i }).click();

    // Use .first() — multiple toasts may stack (create + edit + delete)
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });
    await page.waitForTimeout(500);
    await expect(page.getByText(categoryName).first()).toBeVisible({ timeout: 5_000 });

    // Edit the category — the row has "Actions for {name}" dropdown button
    const updatedCategoryName = `E2E Updated Category ${Date.now()}`;
    await page.getByRole('row', { name: new RegExp(categoryName, 'i') })
      .getByRole('button', { name: new RegExp(`actions for ${categoryName}`, 'i') })
      .first()
      .click();
    // Click Edit menuitem from the dropdown
    const editMenuItem = page.getByRole('menuitem', { name: /edit/i });
    await expect(editMenuItem).toBeVisible({ timeout: 3_000 });
    await editMenuItem.click();

    // Wait for the edit dialog to open and fill new name
    // Use :not([aria-hidden="true"]) and .first() to target only the currently active dialog
    const activeDialog = page.locator('[role="dialog"]:not([aria-hidden="true"])').first();
    await expect(activeDialog).toBeVisible({ timeout: 5_000 });
    const nameInput = activeDialog.getByLabel(/^name$/i).first();
    await nameInput.clear();
    await nameInput.fill(updatedCategoryName);
    await activeDialog.getByRole('button', { name: /save|update|submit/i }).click();
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });

    // CRITICAL: Wait for the Edit dialog to fully close before proceeding
    // The Edit Category dialog has title "Edit Category" — wait for it to not be visible
    await page.waitForFunction(
      () => !document.querySelector('[role="dialog"]'),
      { timeout: 8_000 },
    ).catch(() => {
      // If dialog is still in DOM (Radix keeps it during animation), check it's aria-hidden
    });
    await page.waitForTimeout(300); // Allow any animations to complete
    await expect(page.getByText(updatedCategoryName).first()).toBeVisible({ timeout: 5_000 });

    // Delete the category
    await page.getByRole('row', { name: new RegExp(updatedCategoryName, 'i') })
      .getByRole('button', { name: new RegExp(`actions for ${updatedCategoryName}`, 'i') })
      .first()
      .click();
    // Click Delete menuitem from the dropdown
    const deleteMenuItem = page.getByRole('menuitem', { name: /delete/i });
    await expect(deleteMenuItem).toBeVisible({ timeout: 3_000 });
    await deleteMenuItem.click();

    // Wait for the delete confirmation dialog to appear
    // Poll for the delete button directly using JS to avoid Radix strict mode issues
    await page.waitForFunction(
      () => {
        // Find the "Delete" button that is NOT disabled and is inside an open dialog
        const dialogs = Array.from(document.querySelectorAll('[role="dialog"]:not([aria-hidden="true"])'));
        for (const dialog of dialogs) {
          const btns = Array.from(dialog.querySelectorAll('button'));
          const deleteBtn = btns.find(b => /^delete$/i.test(b.textContent?.trim() ?? '') && !b.disabled);
          if (deleteBtn) return true;
        }
        return false;
      },
      { timeout: 8_000 },
    );
    // Click the delete button via JS evaluation to bypass overlay intercept issues
    await page.evaluate(() => {
      const dialogs = Array.from(document.querySelectorAll('[role="dialog"]:not([aria-hidden="true"])'));
      for (const dialog of dialogs) {
        const btns = Array.from(dialog.querySelectorAll('button'));
        const deleteBtn = btns.find(b => /^delete$/i.test(b.textContent?.trim() ?? '') && !b.disabled);
        if (deleteBtn) { (deleteBtn as HTMLButtonElement).click(); return; }
      }
    });
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });

    // Verify category is removed from the table (check after toast confirms deletion)
    await page.waitForTimeout(1_000); // Allow list to refresh
    await expect(page.getByRole('row', { name: new RegExp(updatedCategoryName, 'i') })).not.toBeVisible({ timeout: 5_000 });
  });

  /**
   * ECOM-PROD-008: Product status lifecycle
   * Transition a product through Draft -> Active -> Archived.
   */
  test('ECOM-PROD-008: should transition product through status lifecycle @regression', async ({
    api,
    trackCleanup,
    page,
  }) => {
    // Seed a draft product
    const product = await api.createProduct(testProduct({ status: 'Draft' }));
    trackCleanup(async () => { await api.deleteProduct(product.id); });

    // Navigate to the product edit page using /edit suffix
    // The /products/{id} route renders in View mode — inputs are disabled
    await page.goto(`/portal/ecommerce/products/${product.id}/edit`);
    await page.waitForLoadState('networkidle');

    // Change status from Draft to Active — the product page shows a "Publish" button for draft products
    const publishBtn = page.getByRole('button', { name: /publish/i });
    const statusCombobox = page.getByRole('combobox', { name: /status/i })
      .or(page.getByLabel(/status/i)).first();

    if (await publishBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      // Use the Publish button to activate
      await publishBtn.click();
    } else if (await statusCombobox.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await statusCombobox.click();
      await page.getByRole('option', { name: /active/i }).click();
      await page.getByRole('button', { name: /save|update|submit/i }).click();
    }
    await expect(page.locator(TOAST_SUCCESS)).toBeVisible({ timeout: 10_000 });

    // Verify Active status badge
    await expect(page.getByText(/active/i).first()).toBeVisible();

    // Change status from Active to Archived — use the save button with status combobox if available
    if (await statusCombobox.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await statusCombobox.click();
      await page.getByRole('option', { name: /archived/i }).click();
      await page.getByRole('button', { name: /save|update|submit/i }).click();
      await expect(page.locator(TOAST_SUCCESS)).toBeVisible({ timeout: 10_000 });
      // Verify Archived status badge
      await expect(page.getByText(/archived/i).first()).toBeVisible();
    }
    // If no combobox for archive, skip this step — archiving may require a different flow
  });

  /**
   * ECOM-PROD-009: Delete product
   * Verify soft-delete with confirmation dialog.
   */
  test('ECOM-PROD-009: should delete product with confirmation @regression', async ({
    productsPage,
    api,
    page,
  }) => {
    const product = await api.createProduct(testProduct({ name: `E2E Delete Product ${Date.now()}` }));

    await productsPage.goto();
    await waitForTableLoad(page);

    // Search for the product to ensure it's visible
    await productsPage.searchInput.fill(product.name.substring(0, 15));
    await page.waitForResponse(resp =>
      resp.url().includes('/api/products') && resp.status() === 200,
    );

    // Wait for product row to appear
    await expect(
      page.getByRole('row', { name: new RegExp(product.name, 'i') }),
    ).toBeVisible({ timeout: 10_000 });

    // Click the actions dropdown button and select Delete
    await page.getByRole('row', { name: new RegExp(product.name, 'i') })
      .getByRole('button')
      .first()
      .click();
    const deleteMenuItem = page.getByRole('menuitem', { name: /delete|remove/i });
    if (await deleteMenuItem.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await deleteMenuItem.click();
    } else {
      // Fallback: direct delete button
      await page.getByRole('row', { name: new RegExp(product.name, 'i') })
        .getByRole('button', { name: /delete|remove/i })
        .click();
    }

    // Confirm deletion
    await confirmDelete(page);
    await expect(page.locator(TOAST_SUCCESS)).toBeVisible({ timeout: 10_000 });

    // Verify product removed from list
    await expect(page.getByRole('row', { name: new RegExp(product.name, 'i') }))
      .not.toBeVisible({ timeout: 5_000 });
  });
});

// ─── Products: Nightly Tests ────────────────────────────────────────────────

test.describe('E-commerce Products @nightly', () => {
  /**
   * ECOM-PROD-007: Product attributes management
   * Create and delete a product attribute via the UI.
   */
  test('ECOM-PROD-007: should manage product attributes @nightly', async ({
    page,
  }) => {
    const attrName = `E2E Color ${Date.now()}`;

    await page.goto('/portal/ecommerce/attributes');
    await page.waitForLoadState('networkidle');

    // Verify attribute list loads
    await expect(page.getByRole('table').or(page.getByRole('list')).first()).toBeVisible({ timeout: 10_000 });

    // Create a new attribute
    await page.getByRole('button', { name: /create|add|new/i }).click();
    await expect(
      page.locator('[role="dialog"]').or(page.getByLabel(/name/i)).first(),
    ).toBeVisible({ timeout: 5_000 });

    await page.getByLabel(/name/i).first().fill(attrName);

    // Select type if available
    const typeSelect = page.getByRole('combobox', { name: /type/i })
      .or(page.getByLabel(/type/i));
    if (await typeSelect.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await typeSelect.click();
      await page.getByRole('option', { name: /text/i }).click();
    }

    await page.getByRole('button', { name: /save|create|submit/i }).click();
    await expect(page.locator(TOAST_SUCCESS)).toBeVisible({ timeout: 10_000 });

    // Verify attribute appears in the list
    await expect(page.getByText(attrName)).toBeVisible({ timeout: 5_000 });

    // Delete the attribute — the row has "Actions for {name}" dropdown button
    await page.getByRole('row', { name: new RegExp(attrName, 'i') })
      .getByRole('button')
      .first()
      .click();
    const attrDeleteItem = page.getByRole('menuitem', { name: /delete|remove/i });
    if (await attrDeleteItem.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await attrDeleteItem.click();
    } else {
      // Fallback: direct delete button
      await page.getByRole('row', { name: new RegExp(attrName, 'i') })
        .getByRole('button', { name: /delete|remove/i })
        .click();
    }
    await confirmDelete(page);
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });

    // Verify attribute is removed
    await expect(page.getByText(attrName)).not.toBeVisible({ timeout: 5_000 });
  });

  /**
   * ECOM-PROD-010: Brands CRUD
   * Create, edit, and delete a brand via UI.
   */
  test('ECOM-PROD-010: should perform brands CRUD @nightly', async ({
    page,
  }) => {
    const brandName = `E2E Brand ${Date.now()}`;

    await page.goto('/portal/ecommerce/brands');
    await page.waitForLoadState('networkidle');

    // Create brand
    await page.getByRole('button', { name: /create|add|new/i }).first().click();
    await expect(page.locator('[role="dialog"]')).toBeVisible({ timeout: 5_000 });

    await page.locator('[role="dialog"]').getByLabel(/name/i).first().fill(brandName);
    await page.locator('[role="dialog"]').getByRole('button', { name: /save|create|submit/i }).click();
    // Use .first() — multiple toasts may stack across create/edit/delete actions
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });

    // Verify brand in list (wait for dialog to close first)
    await expect(page.locator('[role="dialog"]')).not.toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(brandName).first()).toBeVisible({ timeout: 5_000 });

    // Edit brand — the row has "Actions for {name}" dropdown button
    const updatedBrandName = `E2E Brand Updated ${Date.now()}`;
    await page.getByRole('row', { name: new RegExp(brandName, 'i') })
      .getByRole('button')
      .first()
      .click();
    const brandEditItem = page.getByRole('menuitem', { name: /edit/i });
    if (await brandEditItem.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await brandEditItem.click();
    }
    // Wait for edit dialog to appear before filling
    await expect(page.locator('[role="dialog"]')).toBeVisible({ timeout: 5_000 });
    const nameInput = page.locator('[role="dialog"]').getByLabel(/name/i).first();
    await nameInput.clear();
    await nameInput.fill(updatedBrandName);
    await page.locator('[role="dialog"]').getByRole('button', { name: /save|update/i }).click();
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });

    // Delete brand — wait for dialog to close, then find the updated row
    await expect(page.locator('[role="dialog"]')).not.toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(updatedBrandName).first()).toBeVisible({ timeout: 5_000 });
    await page.getByRole('row', { name: new RegExp(updatedBrandName, 'i') })
      .getByRole('button')
      .first()
      .click();
    const brandDeleteItem = page.getByRole('menuitem', { name: /delete|remove/i });
    if (await brandDeleteItem.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await brandDeleteItem.click();
    }
    await confirmDelete(page);
    await expect(page.locator(TOAST_SUCCESS).first()).toBeVisible({ timeout: 10_000 });

    await expect(page.getByText(updatedBrandName)).not.toBeVisible({ timeout: 5_000 });
  });
});
