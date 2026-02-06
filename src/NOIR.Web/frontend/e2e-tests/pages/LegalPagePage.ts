import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * LegalPagePage - Page Object for Legal Page Edit Page
 *
 * Based on: src/pages/portal/legal-pages/LegalPageEditPage.tsx
 * Route: /portal/legal-pages/:id
 *
 * Features:
 * - TinyMCE HTML editor for page content
 * - Title editing
 * - SEO metadata (meta title, meta description, canonical URL)
 * - Search indexing toggle
 * - Revert to platform default (for tenant-owned pages)
 * - Copy-on-write notice for platform templates
 */
export class LegalPagePage extends BasePage {
  // Page Header
  readonly pageHeader: Locator;
  readonly backButton: Locator;
  readonly slugDisplay: Locator;
  readonly platformDefaultBadge: Locator;
  readonly customizedBadge: Locator;

  // Action Buttons
  readonly revertToDefaultButton: Locator;
  readonly saveButton: Locator;

  // Content Section
  readonly contentCard: Locator;
  readonly titleInput: Locator;
  readonly titleLabel: Locator;
  readonly tinymceEditor: Locator;
  readonly tinymceIframe: Locator;

  // SEO Section
  readonly seoCard: Locator;
  readonly metaTitleInput: Locator;
  readonly metaTitleCharCount: Locator;
  readonly metaDescriptionTextarea: Locator;
  readonly metaDescriptionCharCount: Locator;
  readonly canonicalUrlInput: Locator;
  readonly allowIndexingSwitch: Locator;
  readonly allowIndexingLabel: Locator;

  // Info Section
  readonly infoCard: Locator;
  readonly infoSlug: Locator;
  readonly infoStatus: Locator;
  readonly infoVersion: Locator;
  readonly infoLastModified: Locator;

  // Notices
  readonly copyOnWriteNotice: Locator;

  // Revert Dialog (AlertDialog)
  readonly revertDialog: Locator;
  readonly revertDialogTitle: Locator;
  readonly revertDialogDescription: Locator;
  readonly revertDialogConfirmButton: Locator;
  readonly revertDialogCancelButton: Locator;

  constructor(page: Page) {
    super(page);

    // Page Header
    this.pageHeader = page.locator('h1.text-2xl');
    this.backButton = page.locator('button:has([class*="lucide-arrow-left"])').first();
    this.slugDisplay = page.locator('span.text-sm.text-muted-foreground');
    // shadcn Badge uses data-slot="badge"
    this.platformDefaultBadge = page.locator('[data-slot="badge"]:has-text("Platform Default")');
    this.customizedBadge = page.locator('[data-slot="badge"]:has-text("Customized")');

    // Action Buttons
    this.revertToDefaultButton = page.locator('button:has-text("Revert to Default")');
    this.saveButton = page.locator('button:has-text("Save")');

    // Content Section
    this.contentCard = page.locator('div:has(> div:has-text("Content"))').first();
    this.titleInput = page.locator('#title');
    this.titleLabel = page.locator('label[for="title"]');
    this.tinymceEditor = page.locator('.tox-tinymce');
    this.tinymceIframe = page.locator('.tox-edit-area__iframe');

    // SEO Section
    this.seoCard = page.locator('div:has(> div:has-text("SEO"))').first();
    this.metaTitleInput = page.locator('#metaTitle');
    this.metaTitleCharCount = page.locator('text=/\\d+\\/60 characters/');
    this.metaDescriptionTextarea = page.locator('#metaDescription');
    this.metaDescriptionCharCount = page.locator('text=/\\d+\\/160 characters/');
    this.canonicalUrlInput = page.locator('#canonicalUrl');
    this.allowIndexingSwitch = page.locator('#allowIndexing');
    this.allowIndexingLabel = page.locator('label[for="allowIndexing"]');

    // Info Section
    this.infoCard = page.locator('div:has(> div:has-text("Info"))').first();
    this.infoSlug = page.locator('text=Slug:').locator('..').locator('span.font-mono');
    this.infoStatus = page.locator('text=Status:').locator('..').locator('span').last();
    this.infoVersion = page.locator('text=Version:').locator('..').locator('span').last();
    this.infoLastModified = page.locator('text=Last Modified:').locator('..').locator('span').last();

    // Notices
    this.copyOnWriteNotice = page.locator('div:has-text("Customizing Platform")').first();

    // Revert Dialog (AlertDialog)
    this.revertDialog = page.locator('[role="alertdialog"], [role="dialog"]:has-text("Revert to Platform Default")');
    this.revertDialogTitle = this.revertDialog.locator('h2, [class*="AlertDialogTitle"]');
    this.revertDialogDescription = this.revertDialog.locator('[class*="AlertDialogDescription"], p');
    this.revertDialogConfirmButton = this.revertDialog.locator('button:has-text("Revert")');
    this.revertDialogCancelButton = this.revertDialog.locator('button:has-text("Cancel")');
  }

  /**
   * Navigate to legal page edit page by ID
   */
  async navigateToEdit(pageId: string): Promise<void> {
    await this.goto(`/portal/legal-pages/${pageId}`);
  }

  /**
   * Verify page loaded using sequential wait pattern
   */
  async expectPageLoaded(): Promise<void> {
    // Wait for the page header (proves initial render)
    await expect(this.pageHeader).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    // Wait for the TinyMCE editor to initialize (proves form loaded)
    await expect(this.tinymceEditor).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Get page title from header
   */
  async getPageTitle(): Promise<string> {
    return await this.pageHeader.textContent() || '';
  }

  /**
   * Get page slug from display
   */
  async getPageSlug(): Promise<string> {
    const text = await this.slugDisplay.textContent();
    return text?.replace('/', '') || '';
  }

  /**
   * Fill the title field
   */
  async fillTitle(title: string): Promise<void> {
    await this.titleInput.clear();
    await this.titleInput.fill(title);
  }

  /**
   * Get current title value
   */
  async getTitle(): Promise<string> {
    return await this.titleInput.inputValue();
  }

  /**
   * Fill HTML content using TinyMCE editor
   * Note: TinyMCE uses an iframe, so we need to access the contenteditable body
   */
  async fillHtmlContent(content: string): Promise<void> {
    // Click into the editor first
    await this.tinymceIframe.click();
    // Access the iframe content
    const frame = this.tinymceIframe.contentFrame();
    const body = frame.locator('body');
    await body.fill(content);
  }

  /**
   * Get HTML content from TinyMCE editor
   */
  async getHtmlContent(): Promise<string> {
    const frame = this.tinymceIframe.contentFrame();
    const body = frame.locator('body');
    return await body.innerHTML();
  }

  /**
   * Fill meta title
   */
  async fillMetaTitle(metaTitle: string): Promise<void> {
    await this.metaTitleInput.clear();
    await this.metaTitleInput.fill(metaTitle);
  }

  /**
   * Get meta title value
   */
  async getMetaTitle(): Promise<string> {
    return await this.metaTitleInput.inputValue();
  }

  /**
   * Fill meta description
   */
  async fillMetaDescription(metaDescription: string): Promise<void> {
    await this.metaDescriptionTextarea.clear();
    await this.metaDescriptionTextarea.fill(metaDescription);
  }

  /**
   * Get meta description value
   */
  async getMetaDescription(): Promise<string> {
    return await this.metaDescriptionTextarea.inputValue();
  }

  /**
   * Fill canonical URL
   */
  async fillCanonicalUrl(url: string): Promise<void> {
    await this.canonicalUrlInput.clear();
    await this.canonicalUrlInput.fill(url);
  }

  /**
   * Get canonical URL value
   */
  async getCanonicalUrl(): Promise<string> {
    return await this.canonicalUrlInput.inputValue();
  }

  /**
   * Toggle allow indexing switch
   */
  async toggleAllowIndexing(): Promise<void> {
    await this.allowIndexingSwitch.click();
  }

  /**
   * Check if allow indexing is enabled
   */
  async isAllowIndexingEnabled(): Promise<boolean> {
    return await this.allowIndexingSwitch.isChecked();
  }

  /**
   * Set allow indexing to a specific value
   */
  async setAllowIndexing(enabled: boolean): Promise<void> {
    const currentState = await this.isAllowIndexingEnabled();
    if (currentState !== enabled) {
      await this.toggleAllowIndexing();
    }
  }

  /**
   * Save page changes
   */
  async save(): Promise<void> {
    await this.saveButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Save page and expect success
   */
  async saveAndExpectSuccess(): Promise<void> {
    await this.save();
    await this.expectSuccessToast();
  }

  /**
   * Open revert to default confirmation dialog
   */
  async openRevertDialog(): Promise<void> {
    await this.revertToDefaultButton.click();
    await expect(this.revertDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Confirm revert to default
   */
  async confirmRevert(): Promise<void> {
    await this.revertDialogConfirmButton.click();
    await expect(this.revertDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
    await this.waitForPageLoad();
  }

  /**
   * Cancel revert to default
   */
  async cancelRevert(): Promise<void> {
    await this.revertDialogCancelButton.click();
    await expect(this.revertDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Revert to platform default (full flow)
   */
  async revertToDefault(): Promise<void> {
    await this.openRevertDialog();
    await this.confirmRevert();
  }

  /**
   * Navigate back to tenant settings
   */
  async navigateBack(): Promise<void> {
    await this.backButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Check if page is inherited (platform default)
   */
  async isInheritedPage(): Promise<boolean> {
    return await this.platformDefaultBadge.isVisible();
  }

  /**
   * Check if page is customized (tenant-owned)
   */
  async isCustomizedPage(): Promise<boolean> {
    return await this.customizedBadge.isVisible();
  }

  /**
   * Check if copy-on-write notice is visible
   */
  async isCopyOnWriteNoticeVisible(): Promise<boolean> {
    return await this.copyOnWriteNotice.isVisible();
  }

  /**
   * Verify save button is disabled (no changes or no permission)
   */
  async expectSaveButtonDisabled(): Promise<void> {
    await expect(this.saveButton).toBeDisabled();
  }

  /**
   * Verify save button is enabled (has changes)
   */
  async expectSaveButtonEnabled(): Promise<void> {
    await expect(this.saveButton).toBeEnabled();
  }

  /**
   * Verify revert button is visible (only for tenant-owned pages)
   */
  async expectRevertButtonVisible(): Promise<void> {
    await expect(this.revertToDefaultButton).toBeVisible();
  }

  /**
   * Verify revert button is not visible (for platform default pages)
   */
  async expectRevertButtonHidden(): Promise<void> {
    await expect(this.revertToDefaultButton).toBeHidden();
  }

  /**
   * Get page info from sidebar
   */
  async getPageInfo(): Promise<{
    slug: string;
    status: string;
    version: string;
    lastModified: string;
  }> {
    return {
      slug: await this.infoSlug.textContent() || '',
      status: await this.infoStatus.textContent() || '',
      version: await this.infoVersion.textContent() || '',
      lastModified: await this.infoLastModified.textContent() || '',
    };
  }

  /**
   * Fill all SEO fields at once
   */
  async fillSeoFields(data: {
    metaTitle?: string;
    metaDescription?: string;
    canonicalUrl?: string;
    allowIndexing?: boolean;
  }): Promise<void> {
    if (data.metaTitle !== undefined) {
      await this.fillMetaTitle(data.metaTitle);
    }
    if (data.metaDescription !== undefined) {
      await this.fillMetaDescription(data.metaDescription);
    }
    if (data.canonicalUrl !== undefined) {
      await this.fillCanonicalUrl(data.canonicalUrl);
    }
    if (data.allowIndexing !== undefined) {
      await this.setAllowIndexing(data.allowIndexing);
    }
  }

  /**
   * Fill all content fields at once
   */
  async fillContent(data: {
    title: string;
    htmlContent: string;
  }): Promise<void> {
    await this.fillTitle(data.title);
    await this.fillHtmlContent(data.htmlContent);
  }

  /**
   * Edit complete legal page (title, content, and SEO)
   */
  async editPage(data: {
    title?: string;
    htmlContent?: string;
    metaTitle?: string;
    metaDescription?: string;
    canonicalUrl?: string;
    allowIndexing?: boolean;
  }): Promise<void> {
    if (data.title !== undefined) {
      await this.fillTitle(data.title);
    }
    if (data.htmlContent !== undefined) {
      await this.fillHtmlContent(data.htmlContent);
    }
    await this.fillSeoFields({
      metaTitle: data.metaTitle,
      metaDescription: data.metaDescription,
      canonicalUrl: data.canonicalUrl,
      allowIndexing: data.allowIndexing,
    });
  }

  /**
   * Verify meta title character count is displayed correctly
   */
  async expectMetaTitleCharCount(count: number): Promise<void> {
    await expect(this.metaTitleCharCount).toContainText(`${count}/60`);
  }

  /**
   * Verify meta description character count is displayed correctly
   */
  async expectMetaDescriptionCharCount(count: number): Promise<void> {
    await expect(this.metaDescriptionCharCount).toContainText(`${count}/160`);
  }
}
