import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * PostEditorPage - Page Object for Blog Post Editor
 *
 * Based on: src/pages/portal/blog/posts/PostEditorPage.tsx
 * - Used for both creating new posts (/portal/blog/posts/new)
 *   and editing existing posts (/portal/blog/posts/:id/edit)
 * - TinyMCE editor for content
 * - Publishing options: Draft, Publish Now, Schedule
 * - Organization: Category select, Tags multi-select
 * - Featured image upload
 * - SEO settings
 */
export class PostEditorPage extends BasePage {
  // Header elements
  readonly backButton: Locator;
  readonly pageTitle: Locator;
  readonly saveButton: Locator;
  readonly statusBadge: Locator;

  // Content form fields
  readonly titleInput: Locator;
  readonly slugInput: Locator;
  readonly excerptInput: Locator;
  readonly contentEditor: Locator;
  readonly tinymceIframe: Locator;

  // Publishing options
  readonly draftOption: Locator;
  readonly publishOption: Locator;
  readonly scheduleOption: Locator;
  readonly scheduleDatePicker: Locator;
  readonly scheduleTimePicker: Locator;

  // Organization
  readonly categorySelect: Locator;
  readonly tagsContainer: Locator;

  // Featured image
  readonly imageUploadArea: Locator;
  readonly imagePreview: Locator;
  readonly removeImageButton: Locator;
  readonly replaceImageButton: Locator;
  readonly imageAltInput: Locator;

  // SEO fields
  readonly metaTitleInput: Locator;
  readonly metaDescriptionInput: Locator;
  readonly canonicalUrlInput: Locator;
  readonly allowIndexingSwitch: Locator;

  constructor(page: Page) {
    super(page);

    // Header elements - back button is an icon-only button with ArrowLeft icon
    // Selector priority: aria-label (best) → SVG class (fallback) → position-based (last resort)
    this.backButton = page.locator('button[aria-label*="Back"], button[aria-label*="back"], button:has(svg[class*="lucide-arrow-left"]), header button[variant="ghost"]:first-child, .flex.items-center.gap-3 > button:first-child').first();
    this.pageTitle = page.locator('h1:has-text("New Post"), h1:has-text("Edit Post")');
    this.saveButton = page.locator('button:has-text("Save")');
    this.statusBadge = page.locator('[class*="badge"]');

    // Content form fields
    this.titleInput = page.locator('input[placeholder*="Enter post title"]');
    this.slugInput = page.locator('input[placeholder*="post-url-slug"]');
    this.excerptInput = page.locator('textarea[placeholder*="brief summary"]');
    this.contentEditor = page.locator('.tox-tinymce');
    this.tinymceIframe = page.locator('iframe[id*="tinymce"]');

    // Publishing options - Radio buttons with labels
    this.draftOption = page.locator('input[type="radio"][value="draft"], #draft');
    this.publishOption = page.locator('input[type="radio"][value="publish"], #publish');
    this.scheduleOption = page.locator('input[type="radio"][value="schedule"], #schedule');
    this.scheduleDatePicker = page.locator('button[role="combobox"]:has-text("Select date")');
    this.scheduleTimePicker = page.locator('button[role="combobox"]:has-text("Select time")');

    // Organization
    this.categorySelect = page.locator('button[role="combobox"]:has-text("Select category"), button[role="combobox"]:has-text("No category")');
    this.tagsContainer = page.locator('div:has-text("Click to toggle tags") >> ..');

    // Featured image
    this.imageUploadArea = page.locator('text="Click to upload featured image"').first();
    this.imagePreview = page.locator('img[alt*="preview"], img[alt*="Featured"]');
    this.removeImageButton = page.locator('button[aria-label*="Remove"], button[title*="Remove"], button[class*="destructive"]').first();
    this.replaceImageButton = page.locator('button:has-text("Replace Image")');
    this.imageAltInput = page.locator('input[placeholder*="Describe the image"]');

    // SEO fields
    this.metaTitleInput = page.locator('input[placeholder*="SEO title"]');
    this.metaDescriptionInput = page.locator('textarea[placeholder*="SEO description"]');
    this.canonicalUrlInput = page.locator('input[placeholder*="https://"]');
    this.allowIndexingSwitch = page.locator('button[role="switch"]');
  }

  /**
   * Navigate to new post editor
   */
  async navigateToNew(): Promise<void> {
    await this.goto('/portal/blog/posts/new');
  }

  /**
   * Navigate to edit post editor
   */
  async navigateToEdit(postId: string): Promise<void> {
    await this.goto(`/portal/blog/posts/${postId}/edit`);
  }

  /**
   * Verify page loaded for new post
   */
  async expectNewPostPageLoaded(): Promise<void> {
    const header = this.page.locator('h1:has-text("New Post")');
    await expect(header).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.saveButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify page loaded for editing post
   */
  async expectEditPostPageLoaded(): Promise<void> {
    const header = this.page.locator('h1:has-text("Edit Post")');
    await expect(header).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.saveButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Go back to posts list
   */
  async goBack(): Promise<void> {
    await this.backButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Fill in the post title
   */
  async fillTitle(title: string): Promise<void> {
    await this.titleInput.fill(title);
  }

  /**
   * Fill in the post slug
   */
  async fillSlug(slug: string): Promise<void> {
    await this.slugInput.clear();
    await this.slugInput.fill(slug);
  }

  /**
   * Fill in the post excerpt
   */
  async fillExcerpt(excerpt: string): Promise<void> {
    await this.excerptInput.fill(excerpt);
  }

  /**
   * Fill content in TinyMCE editor
   * Note: TinyMCE uses an iframe, so we need to access it directly
   */
  async fillContent(content: string): Promise<void> {
    // Wait for TinyMCE to be ready
    await expect(this.contentEditor).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

    // TinyMCE creates an iframe - we need to interact with it
    const frame = this.page.frameLocator('iframe[id*="tinymce"]').first();
    const body = frame.locator('body');
    await body.click();
    await body.fill(content);
  }

  /**
   * Select a publishing option
   */
  async selectPublishOption(option: 'draft' | 'publish' | 'schedule'): Promise<void> {
    const label = this.page.locator(`label[for="${option}"]`);
    await label.click();
  }

  /**
   * Set scheduled date and time (for schedule option)
   */
  async setScheduleDateTime(date: Date): Promise<void> {
    // Click on schedule option first
    await this.selectPublishOption('schedule');

    // Wait for date/time pickers to appear
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);

    // Select date using DatePicker
    const dateButton = this.page.locator('button[aria-label*="date"], button:has-text("Select date")').first();
    await dateButton.click();

    // The DatePicker should open a calendar - select the date
    // This is a simplified approach; actual implementation may vary
    const dayButton = this.page.locator(`button:has-text("${date.getDate()}")`).first();
    await dayButton.click();

    // Select time using TimePicker
    const timeValue = `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
    const timeButton = this.page.locator('button:has-text("Select time")').first();
    await timeButton.click();

    const timeOption = this.page.locator(`[role="option"]:has-text("${timeValue}")`);
    if (await timeOption.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false)) {
      await timeOption.click();
    }
  }

  /**
   * Select a category
   */
  async selectCategory(categoryName: string): Promise<void> {
    await this.categorySelect.click();
    const selectContent = this.page.locator('[role="listbox"]');
    await expect(selectContent).toBeVisible({ timeout: Timeouts.ELEMENT_ENABLED });

    const option = selectContent.locator(`[role="option"]:has-text("${categoryName}")`);
    await expect(option).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await option.click();

    await expect(selectContent).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Clear category selection
   */
  async clearCategory(): Promise<void> {
    await this.selectCategory('No category');
  }

  /**
   * Toggle a tag selection
   */
  async toggleTag(tagName: string): Promise<void> {
    const tag = this.page.locator(`[class*="badge"]:has-text("${tagName}")`).first();
    await tag.click();
  }

  /**
   * Select multiple tags
   */
  async selectTags(tagNames: string[]): Promise<void> {
    for (const tagName of tagNames) {
      await this.toggleTag(tagName);
    }
  }

  /**
   * Upload featured image
   */
  async uploadFeaturedImage(filePath: string): Promise<void> {
    const fileInput = this.page.locator('input[type="file"][accept*="image"]');
    await fileInput.setInputFiles(filePath);

    // Wait for upload to complete
    await expect(this.imagePreview).toBeVisible({ timeout: Timeouts.API_RESPONSE });
  }

  /**
   * Remove featured image
   */
  async removeFeaturedImage(): Promise<void> {
    await this.removeImageButton.click();
    await expect(this.imageUploadArea).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Fill image alt text
   */
  async fillImageAlt(altText: string): Promise<void> {
    await this.imageAltInput.fill(altText);
  }

  /**
   * Fill SEO meta title
   */
  async fillMetaTitle(title: string): Promise<void> {
    await this.metaTitleInput.fill(title);
  }

  /**
   * Fill SEO meta description
   */
  async fillMetaDescription(description: string): Promise<void> {
    await this.metaDescriptionInput.fill(description);
  }

  /**
   * Fill canonical URL
   */
  async fillCanonicalUrl(url: string): Promise<void> {
    await this.canonicalUrlInput.fill(url);
  }

  /**
   * Toggle allow indexing switch
   */
  async toggleAllowIndexing(): Promise<void> {
    await this.allowIndexingSwitch.click();
  }

  /**
   * Save the post (as draft, published, or scheduled based on option)
   */
  async save(): Promise<void> {
    await this.saveButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Save and expect success toast
   */
  async saveAndExpectSuccess(messagePattern?: string | RegExp): Promise<void> {
    await this.saveButton.click();
    await this.expectSuccessToast(messagePattern);
    await this.waitForPageLoad();
  }

  /**
   * Create a complete post with all common fields
   */
  async createPost(data: {
    title: string;
    slug?: string;
    excerpt?: string;
    content?: string;
    publishOption?: 'draft' | 'publish' | 'schedule';
    category?: string;
    tags?: string[];
    metaTitle?: string;
    metaDescription?: string;
  }): Promise<void> {
    await this.fillTitle(data.title);

    if (data.slug) {
      await this.fillSlug(data.slug);
    }

    if (data.excerpt) {
      await this.fillExcerpt(data.excerpt);
    }

    if (data.content) {
      await this.fillContent(data.content);
    }

    if (data.publishOption) {
      await this.selectPublishOption(data.publishOption);
    }

    if (data.category) {
      await this.selectCategory(data.category);
    }

    if (data.tags && data.tags.length > 0) {
      await this.selectTags(data.tags);
    }

    if (data.metaTitle) {
      await this.fillMetaTitle(data.metaTitle);
    }

    if (data.metaDescription) {
      await this.fillMetaDescription(data.metaDescription);
    }

    await this.save();
  }

  /**
   * Get the current post title
   */
  async getTitle(): Promise<string> {
    return await this.titleInput.inputValue();
  }

  /**
   * Get the current post slug
   */
  async getSlug(): Promise<string> {
    return await this.slugInput.inputValue();
  }

  /**
   * Verify form validation error is displayed
   */
  async expectValidationError(fieldName: string): Promise<void> {
    const errorMessage = this.page.locator(`[data-field="${fieldName}"] + p, input[name="${fieldName}"] ~ p[class*="text-destructive"]`);
    await expect(errorMessage).toBeVisible({ timeout: Timeouts.QUICK_CHECK });
  }

  /**
   * Verify the title field has auto-generated slug
   */
  async expectAutoGeneratedSlug(expectedSlug: string): Promise<void> {
    await expect(this.slugInput).toHaveValue(expectedSlug, { timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Check if currently in edit mode
   */
  async isEditMode(): Promise<boolean> {
    const editHeader = this.page.locator('h1:has-text("Edit Post")');
    return await editHeader.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false);
  }

  /**
   * Check if currently in new post mode
   */
  async isNewMode(): Promise<boolean> {
    const newHeader = this.page.locator('h1:has-text("New Post")');
    return await newHeader.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false);
  }

  /**
   * Wait for TinyMCE editor to be ready
   */
  async waitForEditorReady(): Promise<void> {
    await expect(this.contentEditor).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    // Wait for iframe to load
    const iframe = this.page.locator('iframe[id*="tinymce"]');
    await expect(iframe).toBeAttached({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify the selected category
   */
  async expectSelectedCategory(categoryName: string): Promise<void> {
    const trigger = this.page.locator(`button[role="combobox"]:has-text("${categoryName}")`);
    await expect(trigger).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify a tag is selected
   */
  async expectTagSelected(tagName: string): Promise<void> {
    const selectedTag = this.page.locator(`[class*="badge"]:has-text("${tagName}")[class*="default"]`);
    await expect(selectedTag).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Get current publish option
   */
  async getCurrentPublishOption(): Promise<'draft' | 'publish' | 'schedule' | null> {
    if (await this.draftOption.isChecked()) return 'draft';
    if (await this.publishOption.isChecked()) return 'publish';
    if (await this.scheduleOption.isChecked()) return 'schedule';
    return null;
  }
}
