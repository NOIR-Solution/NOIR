import { test, expect } from '../../fixtures/base.fixture';
import { testBlogPost, testBlogCategory, testBlogTag } from '../../helpers/test-data';
import { confirmDelete, expectToast } from '../../helpers/selectors';

test.describe('Blog Posts @smoke', () => {
  test('BLOG-001: should display blog post list @smoke', async ({
    blogPostsPage,
    page,
  }) => {
    await blogPostsPage.goto();
    await expect(blogPostsPage.postTable.or(page.locator('main')).first()).toBeVisible();
    // Page should load without error states
    await expect(page.locator('[role="alert"][data-type="error"]')).not.toBeVisible();
  });

  test('BLOG-002: should create blog post via UI @smoke', async ({
    blogPostsPage,
    api,
    page,
  }) => {
    const data = testBlogPost();
    let postId: string | undefined;

    try {
      await blogPostsPage.gotoNew();

      // Fill title
      await page.getByLabel('Title', { exact: true }).fill(data.title);

      // Wait for Tiptap editor to be ready
      const editableDiv = page.locator('.tiptap[contenteditable="true"]').first();
      if (await editableDiv.isVisible({ timeout: 5_000 }).catch(() => false)) {
        await editableDiv.fill('E2E test blog content');
      }

      await page.getByRole('button', { name: /save|publish|create/i }).click();

      // Verify success
      await expect(page.locator('[data-sonner-toast][data-type="success"]')).toBeVisible({ timeout: 10_000 });

      // Get created post ID from URL if redirected
      const url = page.url();
      const match = url.match(/posts\/([a-f0-9-]+)/);
      if (match) postId = match[1];
    } finally {
      if (postId) await api.deleteBlogPost(postId).catch(() => {});
    }
  });
});

test.describe('Blog Posts @regression', () => {
  test('BLOG-003: should edit blog post with rich content @regression', async ({
    api,
    page,
    blogPostsPage,
  }) => {
    const data = testBlogPost();
    const created = await api.createBlogPost(data);
    const postId = created.id ?? created.Id;

    try {
      await blogPostsPage.gotoEdit(postId);

      // Wait for the edit page to load (not redirect to list)
      await expect(page).toHaveURL(new RegExp(`posts/${postId}/edit`), { timeout: 10_000 });

      // Modify title — FormLabel renders as <label> linked to input via react-hook-form
      const titleInput = page.getByLabel('Title', { exact: true });
      await expect(titleInput).toBeVisible({ timeout: 10_000 });
      await titleInput.clear();
      await titleInput.fill(`${data.title} Updated`);

      await page.getByRole('button', { name: /save|update/i }).click();
      await expect(page.locator('[data-sonner-toast][data-type="success"]')).toBeVisible({ timeout: 10_000 });
    } finally {
      await api.deleteBlogPost(postId).catch(() => {});
    }
  });

  test('BLOG-004: should perform blog categories CRUD @regression', async ({
    api,
    page,
    blogPostsPage,
  }) => {
    await blogPostsPage.gotoCategories();

    const catData = testBlogCategory();

    // Create via the "New Category" button in page header
    await page.getByRole('button', { name: /create|add|new/i }).click();
    await page.locator('[role="dialog"]').waitFor({ state: 'visible', timeout: 5_000 });
    await page.getByLabel(/name/i).first().fill(catData.name);
    // Click create via evaluate() — CredenzaFooter button in Radix portal
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('[role="dialog"] button'));
      const createBtn = btns.find(b => /^create$/i.test(b.textContent?.trim() ?? ''));
      if (createBtn) (createBtn as HTMLButtonElement).click();
    });
    await expect(page.locator('[data-sonner-toast][data-type="success"]')).toBeVisible({ timeout: 10_000 });
    // Wait for dialog to close and list to refresh
    await page.waitForTimeout(500);

    // Verify created category is visible in the tree view
    await expect(page.getByText(catData.name).first()).toBeVisible({ timeout: 5_000 });

    // Switch to table/list view — ViewModeToggle button has aria-label "Table view"
    const tableViewBtn = page.getByRole('button', { name: /table view/i }).first();
    if (await tableViewBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await tableViewBtn.click();
      await page.waitForTimeout(500);
    }

    // Search for the category to ensure it's visible in the viewport
    const searchInput = page.getByLabel(/search categories/i);
    await searchInput.fill(catData.name);
    await page.waitForTimeout(500); // debounce

    // Find the category row in table view
    const catRow = page.getByRole('row', { name: new RegExp(catData.name, 'i') });
    await expect(catRow).toBeVisible({ timeout: 10_000 });

    // Click the EllipsisVertical button (first button in the row)
    const actionBtn = catRow.getByRole('button').first();
    await expect(actionBtn).toBeVisible({ timeout: 5_000 });
    await actionBtn.click();

    // Wait for the DropdownMenu to open and click Delete
    const deleteMenuItem = page.getByRole('menuitem', { name: /^delete$/i });
    await deleteMenuItem.waitFor({ state: 'visible', timeout: 3_000 });
    await deleteMenuItem.click();

    // Wait for delete confirmation dialog and confirm via evaluate()
    await page.waitForTimeout(300);
    await page.evaluate(() => {
      const dialogs = Array.from(document.querySelectorAll('[role="dialog"]'));
      for (const dialog of dialogs) {
        const btns = Array.from(dialog.querySelectorAll('button'));
        const deleteBtn = btns.find(b => /^delete$/i.test(b.textContent?.trim() ?? ''));
        if (deleteBtn && !(deleteBtn as HTMLButtonElement).disabled) {
          (deleteBtn as HTMLButtonElement).click();
          return;
        }
      }
    });
  });

  test('BLOG-005: should perform blog tags CRUD @regression', async ({
    api,
    page,
    blogPostsPage,
  }) => {
    await blogPostsPage.gotoTags();

    const tagData = testBlogTag();

    // Create via the "New Tag" button in page header
    await page.getByRole('button', { name: /create|add|new/i }).click();
    await page.locator('[role="dialog"]').waitFor({ state: 'visible', timeout: 5_000 });
    await page.getByLabel(/name/i).first().fill(tagData.name);
    // Click create via evaluate() — CredenzaFooter button in Radix portal
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('[role="dialog"] button'));
      const createBtn = btns.find(b => /^create$/i.test(b.textContent?.trim() ?? ''));
      if (createBtn) (createBtn as HTMLButtonElement).click();
    });
    await expect(page.locator('[data-sonner-toast][data-type="success"]')).toBeVisible({ timeout: 10_000 });
    // Wait for dialog to close and list to refresh
    await page.waitForTimeout(500);

    // Search for the newly created tag to bring it into view
    const searchInput = page.getByLabel(/search tags/i);
    await searchInput.fill(tagData.name);
    await page.waitForTimeout(500); // debounce

    // Verify the tag appears in the filtered table
    const tagRow = page.getByRole('row', { name: new RegExp(tagData.name, 'i') });
    await expect(tagRow).toBeVisible({ timeout: 5_000 });

    // Click the EllipsisVertical button (first button in the row) with Playwright trusted click
    // The button has data-no-row-click on its parent cell which prevents row onClick
    const actionBtn = tagRow.getByRole('button').first();
    await expect(actionBtn).toBeVisible({ timeout: 5_000 });
    await actionBtn.click();

    // Wait for the DropdownMenu to open and click Delete
    const deleteMenuItem = page.getByRole('menuitem', { name: /^delete$/i });
    await deleteMenuItem.waitFor({ state: 'visible', timeout: 3_000 });
    await deleteMenuItem.click();

    // Wait for delete confirmation dialog and confirm via evaluate()
    await page.waitForTimeout(300);
    await page.evaluate(() => {
      const dialogs = Array.from(document.querySelectorAll('[role="dialog"]'));
      for (const dialog of dialogs) {
        const btns = Array.from(dialog.querySelectorAll('button'));
        const deleteBtn = btns.find(b => /^delete$/i.test(b.textContent?.trim() ?? ''));
        if (deleteBtn && !(deleteBtn as HTMLButtonElement).disabled) {
          (deleteBtn as HTMLButtonElement).click();
          return;
        }
      }
    });
  });

  test('BLOG-006: should delete blog post with confirmation @regression', async ({
    api,
    page,
    blogPostsPage,
  }) => {
    const data = testBlogPost();
    const created = await api.createBlogPost(data);
    const postId = created.id ?? created.Id;

    await blogPostsPage.goto();
    await expect(blogPostsPage.postTable).toBeVisible({ timeout: 15_000 });

    // Find and delete the post via the row dropdown (ellipsis button → Delete)
    // Use the unique aria-label "Actions for {title}" on the ellipsis button
    const postActionBtn = page.getByRole('button', { name: new RegExp(`actions for ${data.title}`, 'i') });
    await expect(postActionBtn).toBeVisible({ timeout: 10_000 });
    await postActionBtn.click();
    // Wait for dropdown menu to open
    await page.getByRole('menuitem', { name: /^delete$/i }).waitFor({ state: 'visible', timeout: 3_000 });
    await page.getByRole('menuitem', { name: /^delete$/i }).click();

    await confirmDelete(page);
    await expect(page.locator('[data-sonner-toast][data-type="success"]')).toBeVisible({ timeout: 10_000 });

    // Verify removed from list
    await expect(page.getByRole('row', { name: new RegExp(data.title, 'i') })).not.toBeVisible();
  });
});
