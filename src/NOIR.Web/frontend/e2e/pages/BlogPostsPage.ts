import { type Page, type Locator, expect } from '@playwright/test';

export class BlogPostsPage {
  readonly createButton: Locator;
  readonly postTable: Locator;
  readonly postRows: Locator;
  readonly searchInput: Locator;

  constructor(private page: Page) {
    this.createButton = page.getByRole('link', { name: /new post|create/i }).or(
      page.getByRole('button', { name: /new post|create/i }),
    );
    this.postTable = page.getByRole('table');
    this.postRows = page.getByRole('table').getByRole('row');
    this.searchInput = page.getByPlaceholder(/search/i);
  }

  async goto() {
    await this.page.goto('/portal/blog/posts');
    await this.page.waitForLoadState('networkidle');
  }

  async gotoNew() {
    await this.page.goto('/portal/blog/posts/new');
    await this.page.waitForLoadState('networkidle');
  }

  async gotoEdit(postId: string) {
    await this.page.goto(`/portal/blog/posts/${postId}/edit`);
    await this.page.waitForLoadState('networkidle');
  }

  async gotoCategories() {
    await this.page.goto('/portal/blog/categories');
    await this.page.waitForLoadState('networkidle');
  }

  async gotoTags() {
    await this.page.goto('/portal/blog/tags');
    await this.page.waitForLoadState('networkidle');
  }

  async fillPostForm(data: { title: string; content?: string }) {
    await this.page.getByLabel('Title', { exact: true }).fill(data.title);
    // Tiptap editor — contenteditable div (no iframe)
    if (data.content) {
      const editableDiv = this.page.locator('.tiptap[contenteditable="true"]').first();
      await editableDiv.fill(data.content);
    }
  }

  async savePost() {
    await this.page.getByRole('button', { name: /save|publish|create/i }).click();
  }

  async expectPostInList(title: string) {
    await expect(this.page.getByRole('row', { name: new RegExp(title, 'i') })).toBeVisible();
  }

  async deletePost(title: string) {
    await this.page.getByRole('row', { name: new RegExp(title, 'i') })
      .getByRole('button', { name: /delete|remove/i })
      .click();
  }
}
