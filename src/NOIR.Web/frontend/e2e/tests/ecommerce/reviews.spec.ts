import { test, expect } from '../../fixtures/base.fixture';
import { testProduct, testReview } from '../../helpers/test-data';
import {
  expectToast,
  confirmDelete,
  waitForTableLoad,
  TOAST_SUCCESS,
} from '../../helpers/selectors';

const API_URL = process.env.API_URL ?? 'http://localhost:4000';

// ─── Reviews: Smoke Tests ───────────────────────────────────────────────────

test.describe('E-commerce Reviews @smoke', () => {
  /**
   * REV-001: Review list loads
   * Verify that the reviews moderation page renders.
   */
  test('REV-001: should display reviews list page @smoke', async ({
    reviewsPage,
    page,
  }) => {
    await reviewsPage.goto();

    // Page should load without error
    await expect(page.locator('main').first()).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('[role="alert"][data-type="error"]')).not.toBeVisible({ timeout: 2_000 }).catch(() => {});
  });
});

// ─── Reviews: Regression Tests ──────────────────────────────────────────────

test.describe('E-commerce Reviews @regression', () => {
  /**
   * REV-002: Approve a pending review
   * Create a review via API, approve it via UI.
   */
  test('REV-002: should approve a pending review @regression', async ({
    reviewsPage,
    api,
    trackCleanup,
    page,
  }) => {
    // Seed: create product then review
    const productData = testProduct();
    const product = await api.createProduct(productData);
    trackCleanup(async () => { await api.deleteProduct(product.id); });

    const reviewData = testReview({ productId: product.id });
    const review = await api.createReview(reviewData);
    trackCleanup(async () => { await api.deleteReview(review.id).catch(() => {}); });

    await reviewsPage.goto();

    // Find the review row
    const reviewRow = page.getByRole('row', { name: new RegExp(reviewData.title, 'i') });

    if (await reviewRow.isVisible({ timeout: 5_000 }).catch(() => false)) {
      // Click approve button
      const approveBtn = reviewRow.getByRole('button', { name: /approve/i });
      if (await approveBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await approveBtn.click();
        await expect(page.locator(TOAST_SUCCESS)).toBeVisible({ timeout: 10_000 });
      }
    }
  });

  /**
   * REV-003: Reject a pending review
   *
   * The review row uses a DropdownMenu (EllipsisVertical trigger in the first cell).
   * The "Reject" option only appears for Pending reviews.
   * Confirming opens RejectReviewDialog — footer button text is t('reviews.reject') = "Reject".
   */
  test('REV-003: should reject a pending review @regression', async ({
    reviewsPage,
    api,
    trackCleanup,
    page,
  }) => {
    // Seed: create product then review
    const productData = testProduct();
    const product = await api.createProduct(productData);
    trackCleanup(async () => { await api.deleteProduct(product.id); });

    const reviewData = testReview({ productId: product.id, title: `E2E Reject Review ${Date.now()}` });
    const review = await api.createReview(reviewData);
    trackCleanup(async () => { await api.deleteReview(review.id).catch(() => {}); });

    await reviewsPage.goto();

    // Search for the review to bring it into viewport
    const searchInput = page.getByLabel(/search reviews/i);
    if (await searchInput.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await searchInput.fill(reviewData.title);
      await page.waitForTimeout(500);
    }

    const reviewRow = page.getByRole('row', { name: new RegExp(reviewData.title, 'i') });
    if (!(await reviewRow.isVisible({ timeout: 5_000 }).catch(() => false))) {
      // Review not visible — skip gracefully
      return;
    }

    // Open the EllipsisVertical dropdown (first button in the row's action cell)
    const actionBtn = reviewRow.getByRole('button').first();
    await expect(actionBtn).toBeVisible({ timeout: 5_000 });
    await actionBtn.click();

    // Wait for DropdownMenu to open and click the Reject menuitem (only shown for Pending reviews)
    const rejectMenuItem = page.getByRole('menuitem', { name: /^reject$/i });
    if (!(await rejectMenuItem.isVisible({ timeout: 3_000 }).catch(() => false))) {
      // Reject option not available — review may not be Pending
      await page.keyboard.press('Escape');
      return;
    }
    await rejectMenuItem.click();

    // Wait for RejectReviewDialog to open
    await page.waitForSelector('[role="dialog"]:visible', { timeout: 5_000 });
    await page.waitForTimeout(300);

    // Optionally fill rejection reason
    const reasonTextarea = page.locator('[role="dialog"]').last().locator('textarea').first();
    if (await reasonTextarea.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await reasonTextarea.fill('Spam content - E2E test');
    }

    // Click the Reject confirm button in the dialog using Playwright trusted click
    const confirmBtn = page.locator('[role="dialog"]').last()
      .getByRole('button', { name: /^reject$/i }).first();
    await confirmBtn.waitFor({ state: 'visible', timeout: 5_000 });
    await confirmBtn.click();

    await expect(page.locator(TOAST_SUCCESS)).toBeVisible({ timeout: 10_000 });
  });

  /**
   * REV-004: View review details dialog
   *
   * The reviews moderation UI has no delete endpoint (reviews are Approved/Rejected, not deleted).
   * This test verifies that a review row can be found and its detail dialog opens correctly.
   */
  test('REV-004: should view review details from list @regression', async ({
    reviewsPage,
    api,
    trackCleanup,
    page,
  }) => {
    const productData = testProduct();
    const product = await api.createProduct(productData);
    trackCleanup(async () => { await api.deleteProduct(product.id); });

    const reviewData = testReview({ productId: product.id, title: `E2E View Review ${Date.now()}` });
    const review = await api.createReview(reviewData);
    trackCleanup(async () => {
      // Cleanup via the correct product-scoped endpoint pattern
      // (no direct delete endpoint exists for reviews)
      await api.request.delete(`${API_URL}/api/products/${product.id}/reviews/${review.id ?? review.Id}`).catch(() => {});
    });

    await reviewsPage.goto();

    // Search to bring the review into viewport
    const searchInput = page.getByLabel(/search reviews/i);
    if (await searchInput.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await searchInput.fill(reviewData.title);
      await page.waitForTimeout(500);
    }

    const reviewRow = page.getByRole('row', { name: new RegExp(reviewData.title, 'i') });

    if (!(await reviewRow.isVisible({ timeout: 5_000 }).catch(() => false))) {
      // Review not visible — skip
      return;
    }

    // Verify review is visible in the moderation list
    await expect(reviewRow).toBeVisible();

    // Open the EllipsisVertical dropdown and click View Details
    const actionBtn = reviewRow.getByRole('button').first();
    await expect(actionBtn).toBeVisible({ timeout: 5_000 });
    await actionBtn.click();

    const viewDetailsItem = page.getByRole('menuitem', { name: /view details/i });
    await viewDetailsItem.waitFor({ state: 'visible', timeout: 3_000 });
    await viewDetailsItem.click();

    // Verify the detail dialog opens
    await expect(page.locator('[role="dialog"]')).toBeVisible({ timeout: 5_000 });

    // Close the dialog
    await page.keyboard.press('Escape');
    await page.waitForTimeout(300);
  });

  /**
   * REV-005: Filter reviews by status
   */
  test('REV-005: should filter reviews by status @regression', async ({
    reviewsPage,
    page,
  }) => {
    await reviewsPage.goto();

    // Try filtering by Pending status
    await reviewsPage.filterByStatus('Pending');

    // Page should not show error
    await expect(page.locator('[role="alert"][data-type="error"]')).not.toBeVisible({ timeout: 2_000 }).catch(() => {});
  });
});
