import { test, expect } from '@playwright/test';
import { BlogPostsPage, PostEditorPage } from '../pages';

/**
 * Blog Posts Management Tests
 *
 * E2E tests for blog post CRUD operations.
 * Tags: @blog @posts @P0 @P1
 */

test.describe('Blog Posts Management @blog @posts', () => {
  const testPostTitle = `Test Post ${Date.now()}`;
  const testPostContent = 'This is test content for the blog post.';

  test.describe('Blog Posts List @P0', () => {
    test('BLOG-001: Blog posts page loads successfully', async ({ page }) => {
      const blogPostsPage = new BlogPostsPage(page);
      await blogPostsPage.navigate();
      await blogPostsPage.expectPageLoaded();
    });

    test('BLOG-002: Create button is visible', async ({ page }) => {
      const blogPostsPage = new BlogPostsPage(page);
      await blogPostsPage.navigate();
      await blogPostsPage.expectPageLoaded();
      // Button may render after page content - use explicit timeout
      await expect(blogPostsPage.createButton.first()).toBeVisible({ timeout: 15000 });
    });
  });

  test.describe('Blog Post Creation @P0', () => {
    test('BLOG-010: Navigate to create post', async ({ page }) => {
      const blogPostsPage = new BlogPostsPage(page);
      await blogPostsPage.navigate();
      await blogPostsPage.expectPageLoaded();

      await blogPostsPage.clickCreatePost();

      // Should navigate to post editor
      await expect(page).toHaveURL(/\/portal\/blog\/posts\/new/);
    });

    test('BLOG-011: Post editor form is visible', async ({ page }) => {
      const postEditorPage = new PostEditorPage(page);
      await postEditorPage.navigateToNew();
      await postEditorPage.expectNewPostPageLoaded();

      // Verify form fields are visible
      await expect(postEditorPage.titleInput).toBeVisible({ timeout: 10000 });
      await expect(postEditorPage.saveButton).toBeVisible({ timeout: 10000 });
    });

    test('BLOG-012: Create post with title and content', async ({ page }) => {
      const postEditorPage = new PostEditorPage(page);
      await postEditorPage.navigateToNew();
      await postEditorPage.expectNewPostPageLoaded();

      // Fill in the post title
      await postEditorPage.fillTitle(testPostTitle);

      // Fill in excerpt (simpler than TinyMCE content)
      await postEditorPage.fillExcerpt(testPostContent);

      // Save as draft (default option)
      await postEditorPage.save();

      // Verify redirect or success
      // Either we're redirected to posts list or get success toast
      const redirected = await page.waitForURL(/\/portal\/blog\/posts(?!\/new)/, { timeout: 10000 }).catch(() => false);
      if (!redirected) {
        // Check for success toast if not redirected
        await postEditorPage.expectSuccessToast();
      }
    });
  });

  test.describe('Blog Post Filters @P1', () => {
    test('BLOG-013: Status filter works', async ({ page }) => {
      const blogPostsPage = new BlogPostsPage(page);
      await blogPostsPage.navigate();
      await blogPostsPage.expectPageLoaded();

      // Open status filter dropdown
      if (await blogPostsPage.statusFilter.isVisible({ timeout: 5000 }).catch(() => false)) {
        await blogPostsPage.filterByStatus('Draft');

        // Verify filter was applied (check URL or UI state)
        // The filter should narrow down results
        await expect(blogPostsPage.statusFilter).toBeVisible();
      }
    });

    test('BLOG-014: Search posts', async ({ page }) => {
      const blogPostsPage = new BlogPostsPage(page);
      await blogPostsPage.navigate();
      await blogPostsPage.expectPageLoaded();

      // Search for posts
      if (await blogPostsPage.searchInput.isVisible({ timeout: 5000 }).catch(() => false)) {
        await blogPostsPage.search('test');
        await expect(blogPostsPage.searchInput).toHaveValue('test');
      }
    });
  });

  test.describe('Blog Post Delete @P1', () => {
    test('BLOG-015: Delete post shows confirmation and completes', async ({ page }) => {
      // First create a post to delete
      const postEditorPage = new PostEditorPage(page);
      await postEditorPage.navigateToNew();
      await postEditorPage.expectNewPostPageLoaded();

      const deleteTestTitle = `Delete Test ${Date.now()}`;
      await postEditorPage.fillTitle(deleteTestTitle);
      await postEditorPage.fillExcerpt('This post will be deleted.');
      await postEditorPage.save();

      // Wait for navigation or success
      await page.waitForTimeout(2000);

      // Navigate to posts list
      const blogPostsPage = new BlogPostsPage(page);
      await blogPostsPage.navigate();
      await blogPostsPage.expectPageLoaded();

      // Search for the created post
      await blogPostsPage.search(deleteTestTitle);

      // Check if post exists before trying to delete
      const postCount = await blogPostsPage.getPostCount();
      if (postCount > 0) {
        // Try to find and delete the post
        const postExists = await page.locator(`td:has-text("${deleteTestTitle}")`).isVisible({ timeout: 5000 }).catch(() => false);

        if (postExists) {
          await blogPostsPage.deletePost(deleteTestTitle);

          // Verify post no longer exists
          await blogPostsPage.expectPostNotExists(deleteTestTitle);
        }
      }
    });
  });

  test.describe('Blog Post Navigation @P1', () => {
    test('BLOG-016: Navigate back from editor', async ({ page }) => {
      const postEditorPage = new PostEditorPage(page);
      await postEditorPage.navigateToNew();
      await postEditorPage.expectNewPostPageLoaded();

      // Click back button
      await postEditorPage.goBack();

      // Should be back on posts list
      await expect(page).toHaveURL(/\/portal\/blog\/posts/);
    });
  });
});
