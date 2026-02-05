import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * EmailTemplatePage - Page Object for Email Template Edit Page
 *
 * Based on: src/pages/portal/email-templates/EmailTemplateEditPage.tsx
 * Route: /portal/email-templates/:id
 *
 * Features:
 * - TinyMCE HTML editor for email body
 * - Subject line editing with variable insertion
 * - Plain text body (collapsible)
 * - Template preview with sample data
 * - Test email sending
 * - Active status toggle
 * - Revert to platform default (for tenant-owned templates)
 * - Variable drag & drop / autocomplete
 */
export class EmailTemplatePage extends BasePage {
  // Page Header
  readonly pageHeader: Locator;
  readonly backButton: Locator;
  readonly templateNameBadge: Locator;

  // Action Buttons
  readonly previewButton: Locator;
  readonly sendTestEmailButton: Locator;
  readonly revertToDefaultButton: Locator;
  readonly saveButton: Locator;

  // Subject Section
  readonly subjectInput: Locator;
  readonly subjectVariableButton: Locator;

  // HTML Body Editor
  readonly htmlBodySection: Locator;
  readonly insertVariableButton: Locator;
  readonly tinymceEditor: Locator;
  readonly tinymceIframe: Locator;

  // Plain Text Section
  readonly plainTextSection: Locator;
  readonly plainTextToggle: Locator;
  readonly plainTextTextarea: Locator;

  // Sidebar - Template Info
  readonly templateInfoCard: Locator;
  readonly templateVersionDisplay: Locator;
  readonly templateStatusSwitch: Locator;
  readonly templateSourceBadge: Locator;

  // Sidebar - Variables
  readonly variablesCard: Locator;
  readonly variableButtons: Locator;

  // Sidebar - Description
  readonly descriptionCard: Locator;
  readonly descriptionTextarea: Locator;

  // Preview Dialog
  readonly previewDialog: Locator;
  readonly previewDialogTitle: Locator;
  readonly previewSubject: Locator;
  readonly previewIframe: Locator;
  readonly previewPlainText: Locator;
  readonly previewCloseButton: Locator;
  readonly previewLoading: Locator;

  // Test Email Dialog
  readonly testEmailDialog: Locator;
  readonly testEmailInput: Locator;
  readonly testEmailSendButton: Locator;
  readonly testEmailCancelButton: Locator;

  // Notices
  readonly inheritedNotice: Locator;
  readonly unsavedChangesNotice: Locator;

  constructor(page: Page) {
    super(page);

    // Page Header
    this.pageHeader = page.locator('h1.text-2xl');
    this.backButton = page.locator('button:has([class*="lucide-arrow-left"])').first();
    this.templateNameBadge = page.locator('h1 .badge, h1 ~ .badge').first();

    // Action Buttons
    this.previewButton = page.locator('button:has-text("Preview")');
    this.sendTestEmailButton = page.locator('button:has-text("Send Test"), button:has-text("Test Email")');
    this.revertToDefaultButton = page.locator('button:has-text("Revert to Default")');
    this.saveButton = page.locator('button:has-text("Save")');

    // Subject Section
    this.subjectInput = page.locator('#subject-input, input[placeholder*="Enter email subject"]');
    this.subjectVariableButton = page.locator('button:has([class*="lucide-variable"])').first();

    // HTML Body Editor
    this.htmlBodySection = page.locator('div:has(> div:has-text("HTML Body"))').first();
    this.insertVariableButton = page.locator('button:has-text("Insert Variable")');
    this.tinymceEditor = page.locator('.tox-tinymce');
    this.tinymceIframe = page.locator('.tox-edit-area__iframe');

    // Plain Text Section
    this.plainTextSection = page.locator('div:has(> div:has-text("Plain Text"))').first();
    this.plainTextToggle = page.locator('div:has-text("Plain Text Body")').first();
    this.plainTextTextarea = page.locator('textarea[placeholder*="plain text"]');

    // Sidebar - Template Info
    this.templateInfoCard = page.locator('div:has(> div:has-text("Template Info"))').first();
    this.templateVersionDisplay = page.locator('text=Version').locator('..').locator('span.font-medium').last();
    this.templateStatusSwitch = page.locator('#template-active');
    this.templateSourceBadge = page.locator('text=Source').locator('..').locator('.badge').last();

    // Sidebar - Variables
    this.variablesCard = page.locator('div:has(> div:has-text("Variables"))').first();
    this.variableButtons = page.locator('button:has-text("{{")');

    // Sidebar - Description
    this.descriptionCard = page.locator('div:has(> div:has-text("Description"))').last();
    this.descriptionTextarea = page.locator('textarea[placeholder*="Template description"]');

    // Preview Dialog
    this.previewDialog = page.locator('[role="dialog"]:has-text("Email Preview")');
    this.previewDialogTitle = this.previewDialog.locator('h2, [class*="DialogTitle"]');
    this.previewSubject = this.previewDialog.locator('p.text-sm.font-medium');
    this.previewIframe = this.previewDialog.locator('iframe[title="Email Preview"]');
    this.previewPlainText = this.previewDialog.locator('pre');
    this.previewCloseButton = this.previewDialog.locator('button:has-text("Close")');
    this.previewLoading = this.previewDialog.locator('[class*="animate-spin"]');

    // Test Email Dialog
    this.testEmailDialog = page.locator('[role="dialog"]:has-text("Test Email"), [role="dialog"]:has-text("Send Test")');
    this.testEmailInput = this.testEmailDialog.locator('input[type="email"], input[placeholder*="email"]');
    this.testEmailSendButton = this.testEmailDialog.locator('button:has-text("Send")');
    this.testEmailCancelButton = this.testEmailDialog.locator('button:has-text("Cancel")');

    // Notices
    this.inheritedNotice = page.locator('div:has-text("Customizing Platform Template")').first();
    this.unsavedChangesNotice = page.locator('div:has-text("unsaved changes"), div:has-text("Unsaved changes")').first();
  }

  /**
   * Navigate to email template edit page by ID
   */
  async navigateToEdit(templateId: string): Promise<void> {
    await this.goto(`/portal/email-templates/${templateId}`);
  }

  /**
   * Navigate to email template edit page with preview mode
   */
  async navigateToEditWithPreview(templateId: string): Promise<void> {
    await this.goto(`/portal/email-templates/${templateId}?mode=preview`);
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
   * Get template name from page header
   */
  async getTemplateName(): Promise<string> {
    const header = await this.pageHeader.textContent();
    return header?.replace('HTML', '').trim() || '';
  }

  /**
   * Fill the subject field
   */
  async fillSubject(subject: string): Promise<void> {
    await this.subjectInput.clear();
    await this.subjectInput.fill(subject);
  }

  /**
   * Get current subject value
   */
  async getSubject(): Promise<string> {
    return await this.subjectInput.inputValue();
  }

  /**
   * Insert variable into subject field
   */
  async insertVariableIntoSubject(variableName: string): Promise<void> {
    await this.subjectVariableButton.click();
    const variableOption = this.page.locator(`[role="menuitem"]:has-text("{{${variableName}}}")`);
    await variableOption.click();
  }

  /**
   * Fill HTML body using TinyMCE editor
   * Note: TinyMCE uses an iframe, so we need to access the contenteditable body
   */
  async fillHtmlBody(content: string): Promise<void> {
    // Click into the editor first
    await this.tinymceIframe.click();
    // Access the iframe content
    const frame = this.tinymceIframe.contentFrame();
    const body = frame.locator('body');
    await body.fill(content);
  }

  /**
   * Get HTML body content from TinyMCE editor
   */
  async getHtmlBody(): Promise<string> {
    const frame = this.tinymceIframe.contentFrame();
    const body = frame.locator('body');
    return await body.innerHTML();
  }

  /**
   * Insert variable into HTML body via Insert Variable dropdown
   */
  async insertVariableIntoHtmlBody(variableName: string): Promise<void> {
    await this.insertVariableButton.click();
    const variableOption = this.page.locator(`[role="menuitem"]:has-text("{{${variableName}}}")`);
    await variableOption.click();
  }

  /**
   * Toggle plain text section visibility
   */
  async togglePlainTextSection(): Promise<void> {
    await this.plainTextToggle.click();
  }

  /**
   * Fill plain text body
   */
  async fillPlainTextBody(content: string): Promise<void> {
    // Expand plain text section if collapsed
    const isVisible = await this.plainTextTextarea.isVisible();
    if (!isVisible) {
      await this.togglePlainTextSection();
    }
    await this.plainTextTextarea.clear();
    await this.plainTextTextarea.fill(content);
  }

  /**
   * Fill description
   */
  async fillDescription(description: string): Promise<void> {
    await this.descriptionTextarea.clear();
    await this.descriptionTextarea.fill(description);
  }

  /**
   * Toggle template active status
   */
  async toggleActiveStatus(): Promise<void> {
    await this.templateStatusSwitch.click();
  }

  /**
   * Check if template is active
   */
  async isTemplateActive(): Promise<boolean> {
    return await this.templateStatusSwitch.isChecked();
  }

  /**
   * Save template changes
   */
  async save(): Promise<void> {
    await this.saveButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Save template and expect success
   */
  async saveAndExpectSuccess(): Promise<void> {
    await this.save();
    await this.expectSuccessToast();
  }

  /**
   * Open preview dialog
   */
  async openPreview(): Promise<void> {
    await this.previewButton.click();
    await expect(this.previewDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Wait for preview to load
   */
  async waitForPreviewLoaded(): Promise<void> {
    // Wait for loading spinner to disappear
    await this.previewLoading.waitFor({ state: 'hidden', timeout: Timeouts.API_RESPONSE }).catch(() => {});
    // Verify preview content is visible
    await expect(this.previewSubject).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Get preview subject
   */
  async getPreviewSubject(): Promise<string> {
    return await this.previewSubject.textContent() || '';
  }

  /**
   * Close preview dialog
   */
  async closePreview(): Promise<void> {
    await this.previewCloseButton.click();
    await expect(this.previewDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Open test email dialog
   */
  async openTestEmailDialog(): Promise<void> {
    await this.sendTestEmailButton.click();
    await expect(this.testEmailDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Send test email
   */
  async sendTestEmail(email: string): Promise<void> {
    await this.testEmailInput.fill(email);
    await this.testEmailSendButton.click();
  }

  /**
   * Close test email dialog
   */
  async closeTestEmailDialog(): Promise<void> {
    await this.testEmailCancelButton.click();
    await expect(this.testEmailDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Revert to platform default
   */
  async revertToDefault(): Promise<void> {
    await this.revertToDefaultButton.click();
    // Handle browser confirm dialog
    this.page.once('dialog', (dialog) => dialog.accept());
    await this.waitForPageLoad();
  }

  /**
   * Click a variable button to copy to clipboard
   */
  async clickVariableButton(variableName: string): Promise<void> {
    const button = this.page.locator(`button:has-text("{{${variableName}}}")`);
    await button.click();
  }

  /**
   * Navigate back to tenant settings
   */
  async navigateBack(): Promise<void> {
    await this.backButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Check if template is inherited (platform default)
   */
  async isInheritedTemplate(): Promise<boolean> {
    return await this.inheritedNotice.isVisible();
  }

  /**
   * Check if there are unsaved changes
   */
  async hasUnsavedChanges(): Promise<boolean> {
    return await this.unsavedChangesNotice.isVisible();
  }

  /**
   * Verify save button is disabled (no changes)
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
   * Get available variables from the Variables card
   */
  async getAvailableVariables(): Promise<string[]> {
    const buttons = await this.variableButtons.all();
    const variables: string[] = [];
    for (const button of buttons) {
      const text = await button.textContent();
      if (text) {
        // Extract variable name from {{variableName}}
        const match = text.match(/\{\{(.+?)\}\}/);
        if (match) {
          variables.push(match[1]);
        }
      }
    }
    return variables;
  }
}
