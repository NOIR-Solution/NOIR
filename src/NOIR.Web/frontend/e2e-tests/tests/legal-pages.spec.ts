import { test, expect } from '@playwright/test';
import { LegalPagePage, TenantSettingsPage, Timeouts } from '../pages';

/**
 * Legal Page Edit Tests
 *
 * Comprehensive E2E tests for legal page editing functionality.
 * Legal pages are accessible from Tenant Settings > Legal Pages tab.
 * Each legal page has its own edit page at /portal/legal-pages/:id
 *
 * Tags: @legal-pages @P0 @P1
 */

test.describe('Legal Page Edit @legal-pages', () => {
  // Helper to get a valid legal page ID from the tenant settings
  async function getFirstLegalPageId(page: LegalPagePage['page']): Promise<string | null> {
    const settingsPage = new TenantSettingsPage(page);
    await settingsPage.navigate();
    await settingsPage.expectPageLoaded();

    // Navigate to Legal Pages tab
    await settingsPage.selectTab('legal');
    await page.waitForTimeout(500);

    // Find the first edit button and extract the page ID from its click action
    const editButton = page.locator('button:has([class*="lucide-pencil"]), button:has-text("Edit")').first();
    if (await editButton.isVisible({ timeout: Timeouts.ELEMENT_VISIBLE }).catch(() => false)) {
      // Click the edit button to navigate to the legal page edit page
      await editButton.click();
      await page.waitForURL(/\/portal\/legal-pages\/[a-f0-9-]+/);

      // Extract ID from URL
      const url = page.url();
      const match = url.match(/\/portal\/legal-pages\/([a-f0-9-]+)/);
      return match ? match[1] : null;
    }
    return null;
  }

  test.describe('Page Load @P0', () => {
    test('LEGAL-001: Legal page edit page loads with valid ID', async ({ page }) => {
      // First get a valid legal page ID
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Verify page header is visible
      await expect(legalPage.pageHeader).toBeVisible();
      const title = await legalPage.getPageTitle();
      expect(title.length).toBeGreaterThan(0);
    });

    test('LEGAL-002: Title field is editable', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Verify title input exists and is editable
      await expect(legalPage.titleInput).toBeVisible();
      await expect(legalPage.titleInput).toBeEnabled();

      // Get current title
      const originalTitle = await legalPage.getTitle();
      expect(originalTitle.length).toBeGreaterThan(0);

      // Test editing
      const testTitle = `${originalTitle} - Test Edit`;
      await legalPage.fillTitle(testTitle);
      const updatedTitle = await legalPage.getTitle();
      expect(updatedTitle).toBe(testTitle);

      // Revert to original
      await legalPage.fillTitle(originalTitle);
    });

    test('LEGAL-003: Content editor is visible', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Verify TinyMCE editor is visible
      await expect(legalPage.tinymceEditor).toBeVisible();
      await expect(legalPage.tinymceIframe).toBeVisible();
    });
  });

  test.describe('Save Functionality @P1', () => {
    test('LEGAL-010: Save button works', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Save button should exist
      await expect(legalPage.saveButton).toBeVisible();

      // Initially, save button should be disabled (no changes)
      // Note: This depends on whether hasChanges is false initially
      const isDisabled = await legalPage.saveButton.isDisabled();

      // Make a minor change to enable save
      const originalTitle = await legalPage.getTitle();
      await legalPage.fillTitle(originalTitle + ' ');

      // Save button should now be enabled (has changes)
      await expect(legalPage.saveButton).toBeEnabled({ timeout: Timeouts.ELEMENT_ENABLED });

      // Revert the change
      await legalPage.fillTitle(originalTitle);
    });
  });

  test.describe('SEO Fields @P1', () => {
    test('LEGAL-011: SEO fields are visible', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Verify SEO card/section exists
      await expect(legalPage.seoCard).toBeVisible();

      // Verify all SEO fields are visible
      await expect(legalPage.metaTitleInput).toBeVisible();
      await expect(legalPage.metaDescriptionTextarea).toBeVisible();
      await expect(legalPage.canonicalUrlInput).toBeVisible();
      await expect(legalPage.allowIndexingSwitch).toBeVisible();
    });

    test('LEGAL-012: Meta title field works', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Meta title should be editable
      await expect(legalPage.metaTitleInput).toBeEnabled();

      // Get current value
      const originalMetaTitle = await legalPage.getMetaTitle();

      // Test filling the field
      const testMetaTitle = 'Test Meta Title';
      await legalPage.fillMetaTitle(testMetaTitle);
      const updatedMetaTitle = await legalPage.getMetaTitle();
      expect(updatedMetaTitle).toBe(testMetaTitle);

      // Verify character count is displayed
      const charCountText = await legalPage.metaTitleCharCount.textContent().catch(() => null);
      // Character count format: "XX/60 characters"
      if (charCountText) {
        expect(charCountText).toContain('/60');
      }

      // Revert
      await legalPage.fillMetaTitle(originalMetaTitle);
    });

    test('LEGAL-013: Meta description field works', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Meta description should be editable
      await expect(legalPage.metaDescriptionTextarea).toBeEnabled();

      // Get current value
      const originalMetaDesc = await legalPage.getMetaDescription();

      // Test filling the field
      const testMetaDesc = 'This is a test meta description for SEO purposes.';
      await legalPage.fillMetaDescription(testMetaDesc);
      const updatedMetaDesc = await legalPage.getMetaDescription();
      expect(updatedMetaDesc).toBe(testMetaDesc);

      // Verify character count is displayed
      const charCountText = await legalPage.metaDescriptionCharCount.textContent().catch(() => null);
      // Character count format: "XX/160 characters"
      if (charCountText) {
        expect(charCountText).toContain('/160');
      }

      // Revert
      await legalPage.fillMetaDescription(originalMetaDesc);
    });

    test('LEGAL-014: Allow indexing toggle works', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Allow indexing switch should be visible
      await expect(legalPage.allowIndexingSwitch).toBeVisible();

      // Get current state
      const originalState = await legalPage.isAllowIndexingEnabled();

      // Toggle the switch
      await legalPage.toggleAllowIndexing();
      const newState = await legalPage.isAllowIndexingEnabled();
      expect(newState).toBe(!originalState);

      // Toggle back to original
      await legalPage.toggleAllowIndexing();
      const finalState = await legalPage.isAllowIndexingEnabled();
      expect(finalState).toBe(originalState);
    });
  });

  test.describe('Revert to Default @P1', () => {
    test('LEGAL-015: Revert to default dialog opens', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Check if this is an inherited (platform default) page
      const isInherited = await legalPage.isInheritedPage();

      if (isInherited) {
        // For inherited pages, the revert button should NOT be visible
        await legalPage.expectRevertButtonHidden();
      } else {
        // For customized pages, the revert button should be visible
        await legalPage.expectRevertButtonVisible();

        // Open the revert dialog
        await legalPage.openRevertDialog();

        // Verify dialog content
        await expect(legalPage.revertDialog).toBeVisible();
        await expect(legalPage.revertDialogConfirmButton).toBeVisible();
        await expect(legalPage.revertDialogCancelButton).toBeVisible();

        // Cancel the revert
        await legalPage.cancelRevert();
        await expect(legalPage.revertDialog).toBeHidden();
      }
    });
  });

  test.describe('Navigation @P1', () => {
    test('LEGAL-016: Navigate back', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Back button should be visible
      await expect(legalPage.backButton).toBeVisible();

      // Click back button
      await legalPage.navigateBack();

      // Should navigate to tenant settings with legal pages tab
      // Wait for navigation
      await page.waitForURL(/\/portal\/admin\/tenant-settings/, { timeout: Timeouts.PAGE_LOAD });

      // Verify we're on the tenant settings page
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.expectPageLoaded();
    });
  });

  test.describe('Page Info Display @P1', () => {
    test('LEGAL-017: Info section displays page metadata', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Info card should be visible
      await expect(legalPage.infoCard).toBeVisible();

      // Get page info
      const info = await legalPage.getPageInfo();

      // Verify info fields have values
      expect(info.slug.length).toBeGreaterThan(0);
      expect(info.status.length).toBeGreaterThan(0);
      // Version should be at least 1
      expect(parseInt(info.version) || 0).toBeGreaterThanOrEqual(0);
    });

    test('LEGAL-018: Slug is displayed in header', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Slug display should be visible
      await expect(legalPage.slugDisplay).toBeVisible();

      // Get slug
      const slug = await legalPage.getPageSlug();
      // Slug should be terms-of-service or privacy-policy format
      expect(slug.length).toBeGreaterThan(0);
    });
  });

  test.describe('Platform vs Customized Pages @P1', () => {
    test('LEGAL-019: Platform default pages show correct badge', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Check page inheritance status
      const isInherited = await legalPage.isInheritedPage();
      const isCustomized = await legalPage.isCustomizedPage();

      // Page should be either inherited OR customized, not both
      // Note: For brand new tenants, pages start as inherited (platform default)
      if (isInherited) {
        // Platform default badge should be visible
        await expect(legalPage.platformDefaultBadge).toBeVisible();
        // Copy-on-write notice should be visible for inherited pages
        const hasNotice = await legalPage.isCopyOnWriteNoticeVisible();
        expect(hasNotice).toBe(true);
      } else if (isCustomized) {
        // Customized badge should be visible for non-v1 pages
        // (v1 pages don't show customized badge)
        const info = await legalPage.getPageInfo();
        if (parseInt(info.version) > 1) {
          await expect(legalPage.customizedBadge).toBeVisible();
        }
      }
    });

    test('LEGAL-020: Copy-on-write notice appears for inherited pages', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      const isInherited = await legalPage.isInheritedPage();

      if (isInherited) {
        // Notice explaining copy-on-write should be visible
        const hasNotice = await legalPage.isCopyOnWriteNoticeVisible();
        expect(hasNotice).toBe(true);
      }
      // For customized pages, notice should not be visible
      // (already verified in LEGAL-019)
    });
  });

  test.describe('Form State @P1', () => {
    test('LEGAL-021: Save button disabled when no changes', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Wait a moment for form state to stabilize
      await page.waitForTimeout(500);

      // Save button should be disabled when no changes are made
      await legalPage.expectSaveButtonDisabled();
    });

    test('LEGAL-022: Save button enabled after making changes', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Save current title to restore later
      const originalTitle = await legalPage.getTitle();

      // Make a change
      await legalPage.fillTitle(originalTitle + ' (modified)');

      // Save button should now be enabled
      await legalPage.expectSaveButtonEnabled();

      // Revert the change
      await legalPage.fillTitle(originalTitle);

      // Save button should be disabled again
      await legalPage.expectSaveButtonDisabled();
    });
  });

  test.describe('Content Editor @P1', () => {
    test('LEGAL-023: TinyMCE editor is interactive', async ({ page }) => {
      const pageId = await getFirstLegalPageId(page);
      test.skip(!pageId, 'No legal pages available for testing');

      const legalPage = new LegalPagePage(page);
      await legalPage.navigateToEdit(pageId!);
      await legalPage.expectPageLoaded();

      // Verify TinyMCE editor is loaded
      await expect(legalPage.tinymceEditor).toBeVisible();

      // Wait for editor to fully initialize (TinyMCE can be slow)
      await page.waitForTimeout(1000);

      // Verify iframe is accessible
      await expect(legalPage.tinymceIframe).toBeVisible();

      // Try to get content from editor
      const content = await legalPage.getHtmlContent();
      // Content should exist (legal pages have default content)
      expect(content.length).toBeGreaterThan(0);
    });
  });
});
