import { test, expect } from '@playwright/test';
import { EmailTemplatePage, TenantSettingsPage } from '../pages';

/**
 * Email Template Tests
 *
 * Comprehensive E2E tests for email template editing functionality.
 * Tests are organized by priority (P0 = Critical, P1 = High).
 *
 * Flow:
 * 1. Template list is in Tenant Settings > Email Templates tab
 * 2. Clicking edit navigates to separate edit page at /portal/email-templates/:id
 *
 * Tags: @email-templates @P0 @P1
 */

test.describe('Email Template Management @email-templates', () => {
  /**
   * Helper to get first template ID from the settings page.
   * Navigates to tenant settings, selects email templates tab,
   * and extracts the template ID from the first edit button's navigation.
   */
  async function getFirstTemplateId(page: import('@playwright/test').Page): Promise<string | null> {
    const settingsPage = new TenantSettingsPage(page);
    await settingsPage.navigate();
    await settingsPage.expectPageLoaded();

    // Navigate to email templates tab
    await settingsPage.selectTab('email-templates');
    await page.waitForTimeout(1000);

    // Find the first edit button and extract template ID from its click behavior
    const editButton = page.locator('button:has([class*="lucide-pencil"])').first();

    if (await editButton.isVisible({ timeout: 10000 })) {
      // Set up listener for navigation to capture the template ID
      const navigationPromise = page.waitForURL(/\/portal\/email-templates\/[a-f0-9-]+/, {
        timeout: 10000,
      });

      await editButton.click();
      await navigationPromise;

      // Extract ID from URL
      const url = page.url();
      const match = url.match(/\/portal\/email-templates\/([a-f0-9-]+)/);
      return match ? match[1] : null;
    }

    return null;
  }

  test.describe('Email Template Edit Page @P0', () => {
    test('EMAIL-001: Email template edit page loads with valid ID', async ({ page }) => {
      // Navigate via settings to get a valid template ID
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Verify key elements are present
      await expect(templatePage.pageHeader).toBeVisible();
      await expect(templatePage.subjectInput).toBeVisible();
      await expect(templatePage.tinymceEditor).toBeVisible();
    });

    test('EMAIL-002: Subject field is editable', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Get original subject
      const originalSubject = await templatePage.getSubject();

      // Edit subject
      const testSubject = `Test Subject ${Date.now()}`;
      await templatePage.fillSubject(testSubject);

      // Verify the change
      const newSubject = await templatePage.getSubject();
      expect(newSubject).toBe(testSubject);

      // Restore original (without saving, just for this test)
      await templatePage.fillSubject(originalSubject);
    });

    test('EMAIL-003: HTML body editor is visible', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // TinyMCE editor should be visible
      await expect(templatePage.tinymceEditor).toBeVisible();

      // TinyMCE iframe should be present (indicates editor is initialized)
      await expect(templatePage.tinymceIframe).toBeVisible({ timeout: 15000 });
    });
  });

  test.describe('Email Template Actions @P1', () => {
    test('EMAIL-010: Save button works', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Save button should be initially disabled (no changes)
      await templatePage.expectSaveButtonDisabled();

      // Make a change to enable save button
      const originalSubject = await templatePage.getSubject();
      await templatePage.fillSubject(originalSubject + ' (test)');

      // Save button should now be enabled
      await templatePage.expectSaveButtonEnabled();

      // Click save
      await templatePage.saveAndExpectSuccess();

      // Save button should be disabled again after successful save
      await templatePage.expectSaveButtonDisabled();

      // Restore original subject
      await templatePage.fillSubject(originalSubject);
      await templatePage.saveAndExpectSuccess();
    });

    test('EMAIL-011: Preview dialog opens', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Click preview button
      await templatePage.openPreview();

      // Preview dialog should be visible
      await expect(templatePage.previewDialog).toBeVisible();
    });

    test('EMAIL-012: Preview shows rendered content', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Open preview
      await templatePage.openPreview();
      await templatePage.waitForPreviewLoaded();

      // Preview should show subject
      const previewSubject = await templatePage.getPreviewSubject();
      expect(previewSubject.length).toBeGreaterThan(0);

      // Preview iframe should be visible (contains rendered HTML)
      await expect(templatePage.previewIframe).toBeVisible();
    });

    test('EMAIL-013: Close preview dialog', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Open preview
      await templatePage.openPreview();
      await expect(templatePage.previewDialog).toBeVisible();

      // Close preview
      await templatePage.closePreview();
      await expect(templatePage.previewDialog).toBeHidden();
    });

    test('EMAIL-014: Test email dialog opens', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Open test email dialog
      await templatePage.openTestEmailDialog();

      // Test email dialog should be visible
      await expect(templatePage.testEmailDialog).toBeVisible();

      // Email input should be visible
      await expect(templatePage.testEmailInput).toBeVisible();

      // Close dialog
      await templatePage.closeTestEmailDialog();
      await expect(templatePage.testEmailDialog).toBeHidden();
    });

    test('EMAIL-015: Insert variable into subject', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Get original subject
      const originalSubject = await templatePage.getSubject();

      // Clear subject and position cursor at the start
      await templatePage.subjectInput.clear();

      // Click the variable button for subject
      await templatePage.subjectVariableButton.click();

      // Wait for dropdown menu to appear
      const variableMenu = page.locator('[role="menu"]');
      await expect(variableMenu).toBeVisible({ timeout: 5000 });

      // Click first available variable
      const firstVariable = variableMenu.locator('[role="menuitem"]').first();
      await firstVariable.click();

      // Verify variable was inserted (should contain {{ and }})
      const newSubject = await templatePage.getSubject();
      expect(newSubject).toContain('{{');
      expect(newSubject).toContain('}}');

      // Restore original subject
      await templatePage.fillSubject(originalSubject);
    });

    test('EMAIL-016: Active/inactive toggle works', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Get current active status
      const originalStatus = await templatePage.isTemplateActive();

      // Toggle status
      await templatePage.toggleActiveStatus();

      // Wait for API response
      await page.waitForTimeout(1000);

      // Verify status changed
      const newStatus = await templatePage.isTemplateActive();
      expect(newStatus).toBe(!originalStatus);

      // Restore original status
      await templatePage.toggleActiveStatus();
      await page.waitForTimeout(1000);

      // Verify restored
      const restoredStatus = await templatePage.isTemplateActive();
      expect(restoredStatus).toBe(originalStatus);
    });

    test('EMAIL-017: Navigate back to settings', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Click back button
      await templatePage.navigateBack();

      // Should navigate to tenant settings
      await page.waitForURL(/\/portal\/admin\/tenant-settings/, { timeout: 10000 });

      // Verify we're back on settings page
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.expectPageLoaded();
    });
  });

  test.describe('Email Templates Tab in Settings @P1', () => {
    test('EMAIL-020: Email templates tab displays template list', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Navigate to email templates tab
      await settingsPage.selectTab('email-templates');
      await page.waitForTimeout(500);

      // Should show template cards
      const templateCards = page.locator('div:has(> h4.font-medium)');
      const count = await templateCards.count();
      expect(count).toBeGreaterThan(0);
    });

    test('EMAIL-021: Preview button works from template list', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Navigate to email templates tab
      await settingsPage.selectTab('email-templates');
      await page.waitForTimeout(500);

      // Find first preview button (Eye icon)
      const previewButton = page.locator('button:has([class*="lucide-eye"])').first();

      if (await previewButton.isVisible({ timeout: 5000 })) {
        await previewButton.click();

        // Preview dialog should open
        const previewDialog = page.locator('[role="dialog"]:has-text("Email Preview")');
        await expect(previewDialog).toBeVisible({ timeout: 10000 });

        // Close dialog
        const closeButton = previewDialog.locator('button:has-text("Close")');
        await closeButton.click();
        await expect(previewDialog).toBeHidden({ timeout: 5000 });
      }
    });

    test('EMAIL-022: Edit button navigates to edit page', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Navigate to email templates tab
      await settingsPage.selectTab('email-templates');
      await page.waitForTimeout(500);

      // Find first edit button (Pencil icon)
      const editButton = page.locator('button:has([class*="lucide-pencil"])').first();

      if (await editButton.isVisible({ timeout: 5000 })) {
        await editButton.click();

        // Should navigate to edit page
        await page.waitForURL(/\/portal\/email-templates\/[a-f0-9-]+/, { timeout: 10000 });

        // Verify edit page loaded
        const templatePage = new EmailTemplatePage(page);
        await templatePage.expectPageLoaded();
      }
    });
  });

  test.describe('Email Template Variables @P1', () => {
    test('EMAIL-030: Variables card displays available variables', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Variables card should be visible in sidebar
      await expect(templatePage.variablesCard).toBeVisible();

      // Should have at least one variable button
      const variables = await templatePage.getAvailableVariables();
      expect(variables.length).toBeGreaterThan(0);
    });

    test('EMAIL-031: Click variable copies to clipboard', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Get available variables
      const variables = await templatePage.getAvailableVariables();

      if (variables.length > 0) {
        // Click first variable button
        await templatePage.clickVariableButton(variables[0]);

        // Should show success toast for copy
        await templatePage.expectSuccessToast();
      }
    });
  });

  test.describe('Email Template Info @P1', () => {
    test('EMAIL-040: Template info card displays metadata', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Template info card should be visible
      await expect(templatePage.templateInfoCard).toBeVisible();

      // Should show version
      const versionText = page.locator('text=Version').first();
      await expect(versionText).toBeVisible();

      // Should show status
      const statusText = page.locator('text=Status').first();
      await expect(statusText).toBeVisible();

      // Should show source badge (Platform or Custom)
      const sourceText = page.locator('text=Source').first();
      await expect(sourceText).toBeVisible();
    });
  });

  test.describe('Email Template Description @P1', () => {
    test('EMAIL-050: Description textarea is editable', async ({ page }) => {
      const templateId = await getFirstTemplateId(page);

      if (!templateId) {
        test.skip(true, 'No email templates available to test');
        return;
      }

      const templatePage = new EmailTemplatePage(page);
      await templatePage.expectPageLoaded();

      // Description card should be visible
      await expect(templatePage.descriptionCard).toBeVisible();

      // Description textarea should be visible
      await expect(templatePage.descriptionTextarea).toBeVisible();

      // Fill description
      const testDescription = `Test description ${Date.now()}`;
      await templatePage.fillDescription(testDescription);

      // Verify the change triggered unsaved changes state
      await expect(templatePage.saveButton).toBeEnabled();
    });
  });
});
