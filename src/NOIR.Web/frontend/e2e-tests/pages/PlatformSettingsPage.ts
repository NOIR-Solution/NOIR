import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * PlatformSettingsPage - Page Object for Platform Settings Management
 *
 * Based on: src/pages/portal/admin/platform-settings/PlatformSettingsPage.tsx
 * - Uses Tabs component with three tabs
 * - Tabs: SMTP, Email Templates, Legal Pages
 * - Platform admin only features (requires platform admin role)
 */
export class PlatformSettingsPage extends BasePage {
  readonly pageHeader: Locator;
  readonly tabsList: Locator;

  // Tab triggers
  readonly smtpTab: Locator;
  readonly emailTemplatesTab: Locator;
  readonly legalPagesTab: Locator;

  // Common form elements
  readonly saveButton: Locator;
  readonly cancelButton: Locator;
  readonly loadingSpinner: Locator;

  // SMTP Settings Card
  readonly smtpCard: Locator;
  readonly smtpConfiguredBadge: Locator;
  readonly smtpDefaultBadge: Locator;
  readonly smtpNotConfiguredAlert: Locator;

  // SMTP Settings Form Fields
  readonly smtpHostInput: Locator;
  readonly smtpPortInput: Locator;
  readonly smtpUsernameInput: Locator;
  readonly smtpPasswordInput: Locator;
  readonly smtpFromEmailInput: Locator;
  readonly smtpFromNameInput: Locator;
  readonly smtpUseSslSwitch: Locator;
  readonly smtpTestConnectionButton: Locator;
  readonly smtpSaveButton: Locator;

  // Test Connection Dialog
  readonly testConnectionDialog: Locator;
  readonly testRecipientEmailInput: Locator;
  readonly testSendButton: Locator;
  readonly testCancelButton: Locator;

  // Email Templates Tab
  readonly emailTemplatesCard: Locator;
  readonly emailTemplateCards: Locator;
  readonly noEmailTemplatesMessage: Locator;

  // Legal Pages Tab
  readonly legalPagesCard: Locator;
  readonly legalPageCards: Locator;
  readonly noLegalPagesMessage: Locator;

  constructor(page: Page) {
    super(page);

    this.pageHeader = page.locator('h1:has-text("Platform Settings"), [data-testid="page-header"]');
    this.tabsList = page.locator('[role="tablist"]');

    // Tab triggers - using data-value or text content
    this.smtpTab = page.locator('[role="tab"]:has-text("SMTP"), [data-value="smtp"]');
    this.emailTemplatesTab = page.locator('[role="tab"]:has-text("Email Templates"), [data-value="emailTemplates"]');
    this.legalPagesTab = page.locator('[role="tab"]:has-text("Legal Pages"), [data-value="legalPages"]');

    // Common form elements
    this.saveButton = page.locator('button[type="submit"], button:has-text("Save")');
    this.cancelButton = page.locator('button:has-text("Cancel")');
    this.loadingSpinner = page.locator('[role="progressbar"], .animate-spin');

    // SMTP Settings Card
    this.smtpCard = page.locator('[data-testid="smtp-card"], .card:has-text("SMTP")').first();
    this.smtpConfiguredBadge = page.locator('.badge:has-text("Configured")');
    this.smtpDefaultBadge = page.locator('.badge:has-text("Using defaults")');
    this.smtpNotConfiguredAlert = page.locator('[role="alert"]:has-text("Using Default Configuration")');

    // SMTP Settings Form Fields
    this.smtpHostInput = page.locator('input[name="host"]');
    this.smtpPortInput = page.locator('input[name="port"]');
    this.smtpUsernameInput = page.locator('input[name="username"]');
    this.smtpPasswordInput = page.locator('input[name="password"]');
    this.smtpFromEmailInput = page.locator('input[name="fromEmail"]');
    this.smtpFromNameInput = page.locator('input[name="fromName"]');
    this.smtpUseSslSwitch = page.locator('[role="switch"]');
    this.smtpTestConnectionButton = page.locator('button:has-text("Test Connection"), button:has-text("Test")');
    this.smtpSaveButton = page.locator('button[type="submit"]:has-text("Save")');

    // Test Connection Dialog
    this.testConnectionDialog = page.locator('[role="dialog"]:has-text("Test")');
    this.testRecipientEmailInput = page.locator('[role="dialog"] input[name="recipientEmail"], [role="dialog"] input[type="email"]');
    this.testSendButton = page.locator('[role="dialog"] button:has-text("Send Test")');
    this.testCancelButton = page.locator('[role="dialog"] button:has-text("Cancel")');

    // Email Templates Tab
    this.emailTemplatesCard = page.locator('[data-testid="email-templates-card"], .card:has-text("Email Templates")').first();
    this.emailTemplateCards = page.locator('.card:has-text("Platform"):has(.badge)');
    this.noEmailTemplatesMessage = page.locator('text="No platform email templates found."');

    // Legal Pages Tab
    this.legalPagesCard = page.locator('[data-testid="legal-pages-card"], .card:has-text("Legal Pages")').first();
    this.legalPageCards = page.locator('.card:has-text("Platform Default"):has(.badge)');
    this.noLegalPagesMessage = page.locator('text="No platform legal pages found."');
  }

  /**
   * Navigate to platform settings page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/admin/platform-settings');
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify page loaded using sequential wait pattern
   * This page uses tabs instead of create buttons, so we wait for:
   * 1. Header (proves initial render)
   * 2. Tablist (proves tabs are rendered and page is interactive)
   */
  async expectPageLoaded(): Promise<void> {
    await this.expectStandardPageLoaded(this.pageHeader, this.tabsList);
  }

  /**
   * Wait for SMTP settings to load (loading spinner disappears and form is visible)
   */
  async waitForSmtpSettingsLoaded(): Promise<void> {
    // Wait for loading spinner to disappear
    const spinners = await this.page.locator('.animate-spin').count();
    if (spinners > 0) {
      await this.page.locator('.animate-spin').first().waitFor({ state: 'hidden', timeout: Timeouts.API_RESPONSE });
    }
    // Verify form fields are visible
    await expect(this.smtpHostInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Click on a specific tab
   */
  async selectTab(tabName: 'smtp' | 'emailTemplates' | 'legalPages'): Promise<void> {
    const tabMap = {
      'smtp': this.smtpTab,
      'emailTemplates': this.emailTemplatesTab,
      'legalPages': this.legalPagesTab,
    };

    const tab = tabMap[tabName];
    if (tab) {
      await tab.first().click();
      await this.page.waitForTimeout(Timeouts.SEARCH_WAIT);
    }
  }

  /**
   * Verify a tab is active
   */
  async expectTabActive(tabName: string): Promise<void> {
    const tab = this.page.locator(`[role="tab"]:has-text("${tabName}")`);
    await expect(tab.first()).toHaveAttribute('aria-selected', 'true', { timeout: Timeouts.ELEMENT_ENABLED });
  }

  /**
   * Check if SMTP is configured (not using defaults)
   */
  async isSmtpConfigured(): Promise<boolean> {
    return await this.smtpConfiguredBadge.isVisible({ timeout: Timeouts.QUICK_CHECK }).catch(() => false);
  }

  /**
   * Get current SMTP settings from form
   */
  async getSmtpSettings(): Promise<{
    host: string;
    port: string;
    username: string;
    fromEmail: string;
    fromName: string;
    useSsl: boolean;
  }> {
    await this.waitForSmtpSettingsLoaded();

    return {
      host: await this.smtpHostInput.inputValue(),
      port: await this.smtpPortInput.inputValue(),
      username: await this.smtpUsernameInput.inputValue(),
      fromEmail: await this.smtpFromEmailInput.inputValue(),
      fromName: await this.smtpFromNameInput.inputValue(),
      useSsl: await this.smtpUseSslSwitch.isChecked(),
    };
  }

  /**
   * Update SMTP settings
   */
  async updateSmtpSettings(data: {
    host?: string;
    port?: number;
    username?: string;
    password?: string;
    fromEmail?: string;
    fromName?: string;
    useSsl?: boolean;
  }): Promise<void> {
    await this.selectTab('smtp');
    await this.waitForSmtpSettingsLoaded();

    if (data.host !== undefined) {
      await this.smtpHostInput.clear();
      await this.smtpHostInput.fill(data.host);
    }

    if (data.port !== undefined) {
      await this.smtpPortInput.clear();
      await this.smtpPortInput.fill(data.port.toString());
    }

    if (data.username !== undefined) {
      await this.smtpUsernameInput.clear();
      await this.smtpUsernameInput.fill(data.username);
    }

    if (data.password !== undefined) {
      await this.smtpPasswordInput.clear();
      await this.smtpPasswordInput.fill(data.password);
    }

    if (data.fromEmail !== undefined) {
      await this.smtpFromEmailInput.clear();
      await this.smtpFromEmailInput.fill(data.fromEmail);
    }

    if (data.fromName !== undefined) {
      await this.smtpFromNameInput.clear();
      await this.smtpFromNameInput.fill(data.fromName);
    }

    if (data.useSsl !== undefined) {
      const currentState = await this.smtpUseSslSwitch.isChecked();
      if (currentState !== data.useSsl) {
        await this.smtpUseSslSwitch.click();
      }
    }
  }

  /**
   * Save SMTP settings
   */
  async saveSmtpSettings(): Promise<void> {
    await this.smtpSaveButton.click();
    await this.expectSuccessToast();
  }

  /**
   * Open test connection dialog
   */
  async openTestConnectionDialog(): Promise<void> {
    await expect(this.smtpTestConnectionButton).toBeEnabled({ timeout: Timeouts.ELEMENT_ENABLED });
    await this.smtpTestConnectionButton.click();
    await expect(this.testConnectionDialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Send test email
   */
  async sendTestEmail(recipientEmail: string): Promise<void> {
    await this.testRecipientEmailInput.clear();
    await this.testRecipientEmailInput.fill(recipientEmail);
    await this.testSendButton.click();
    await this.expectSuccessToast();
    // Dialog should close after successful test
    await expect(this.testConnectionDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Cancel test connection dialog
   */
  async cancelTestConnection(): Promise<void> {
    await this.testCancelButton.click();
    await expect(this.testConnectionDialog).toBeHidden({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Get count of email templates displayed
   */
  async getEmailTemplateCount(): Promise<number> {
    await this.selectTab('emailTemplates');
    // Wait for loading to complete
    await this.page.waitForTimeout(Timeouts.SEARCH_WAIT);
    const spinners = await this.page.locator('.animate-spin').count();
    if (spinners > 0) {
      await this.page.locator('.animate-spin').first().waitFor({ state: 'hidden', timeout: Timeouts.API_RESPONSE });
    }

    // Count cards with "Platform" badge in email templates section
    const cards = this.page.locator('.card:has(.badge:has-text("Platform"))');
    return await cards.count();
  }

  /**
   * Click edit button on an email template by name
   */
  async editEmailTemplate(templateName: string): Promise<void> {
    await this.selectTab('emailTemplates');
    const templateCard = this.page.locator(`.card:has-text("${templateName}")`).first();
    await expect(templateCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

    const editButton = templateCard.locator('button').filter({ has: this.page.locator('svg.lucide-pencil, .lucide-pencil') });
    await editButton.click();
  }

  /**
   * Get count of legal pages displayed
   */
  async getLegalPageCount(): Promise<number> {
    await this.selectTab('legalPages');
    // Wait for loading to complete
    await this.page.waitForTimeout(Timeouts.SEARCH_WAIT);
    const spinners = await this.page.locator('.animate-spin').count();
    if (spinners > 0) {
      await this.page.locator('.animate-spin').first().waitFor({ state: 'hidden', timeout: Timeouts.API_RESPONSE });
    }

    // Count cards with "Platform Default" badge in legal pages section
    const cards = this.page.locator('.card:has(.badge:has-text("Platform Default"))');
    return await cards.count();
  }

  /**
   * Click edit button on a legal page by title
   */
  async editLegalPage(pageTitle: string): Promise<void> {
    await this.selectTab('legalPages');
    const legalPageCard = this.page.locator(`.card:has-text("${pageTitle}")`).first();
    await expect(legalPageCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

    const editButton = legalPageCard.locator('button').filter({ has: this.page.locator('svg.lucide-pencil, .lucide-pencil') }).first();
    await editButton.click();
  }

  /**
   * Click view button on a legal page by title (opens in new tab)
   */
  async viewLegalPage(pageTitle: string): Promise<Page> {
    await this.selectTab('legalPages');
    const legalPageCard = this.page.locator(`.card:has-text("${pageTitle}")`).first();
    await expect(legalPageCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

    const viewButton = legalPageCard.locator('button').filter({ has: this.page.locator('svg.lucide-eye, .lucide-eye') }).first();

    // Wait for new page popup
    const [newPage] = await Promise.all([
      this.page.context().waitForEvent('page'),
      viewButton.click(),
    ]);

    await newPage.waitForLoadState('domcontentloaded');
    return newPage;
  }

  /**
   * Verify email template exists in the list
   */
  async expectEmailTemplateExists(templateName: string): Promise<void> {
    await this.selectTab('emailTemplates');
    const templateCard = this.page.locator(`.card:has-text("${templateName}")`).first();
    await expect(templateCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify legal page exists in the list
   */
  async expectLegalPageExists(pageTitle: string): Promise<void> {
    await this.selectTab('legalPages');
    const legalPageCard = this.page.locator(`.card:has-text("${pageTitle}")`).first();
    await expect(legalPageCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Get email template details by name
   */
  async getEmailTemplateDetails(templateName: string): Promise<{
    name: string;
    description: string;
    language: string;
    isActive: boolean;
  }> {
    await this.selectTab('emailTemplates');
    const templateCard = this.page.locator(`.card:has-text("${templateName}")`).first();
    await expect(templateCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

    const name = await templateCard.locator('h4').textContent() ?? '';
    const description = await templateCard.locator('p.text-muted-foreground').first().textContent() ?? '';

    // Get language badge
    const languageBadge = templateCard.locator('.badge:has-text("en"), .badge:has-text("vi")').first();
    const language = await languageBadge.textContent() ?? '';

    // Check if active
    const activeBadge = templateCard.locator('.badge:has-text("Active")');
    const isActive = await activeBadge.isVisible().catch(() => false);

    return {
      name: name.trim(),
      description: description.trim(),
      language: language.trim(),
      isActive,
    };
  }

  /**
   * Get legal page details by title
   */
  async getLegalPageDetails(pageTitle: string): Promise<{
    title: string;
    slug: string;
    lastModified: string;
  }> {
    await this.selectTab('legalPages');
    const legalPageCard = this.page.locator(`.card:has-text("${pageTitle}")`).first();
    await expect(legalPageCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

    const title = await legalPageCard.locator('h4').textContent() ?? '';
    const slugText = await legalPageCard.locator('p.text-muted-foreground').first().textContent() ?? '';
    const lastModifiedText = await legalPageCard.locator('p.text-xs').textContent() ?? '';

    return {
      title: title.trim(),
      slug: slugText.trim(),
      lastModified: lastModifiedText.replace('Last modified:', '').trim(),
    };
  }

  /**
   * Verify no email templates message is displayed
   */
  async expectNoEmailTemplates(): Promise<void> {
    await this.selectTab('emailTemplates');
    await expect(this.noEmailTemplatesMessage).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify no legal pages message is displayed
   */
  async expectNoLegalPages(): Promise<void> {
    await this.selectTab('legalPages');
    await expect(this.noLegalPagesMessage).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Fill complete SMTP configuration and save
   * Helper method for common test scenario
   */
  async configureSmtp(config: {
    host: string;
    port: number;
    username?: string;
    password?: string;
    fromEmail: string;
    fromName: string;
    useSsl?: boolean;
  }): Promise<void> {
    await this.updateSmtpSettings(config);
    await this.saveSmtpSettings();
  }

  /**
   * Verify SMTP form validation error appears
   */
  async expectSmtpValidationError(fieldName: string): Promise<void> {
    const errorMessage = this.page.locator(`[data-slot="form-message"]:near(input[name="${fieldName}"]), p.text-destructive:near(input[name="${fieldName}"])`);
    await expect(errorMessage.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Toggle SSL/TLS setting
   */
  async toggleUseSsl(): Promise<void> {
    await this.smtpUseSslSwitch.click();
  }

  /**
   * Verify SSL is enabled
   */
  async expectSslEnabled(): Promise<void> {
    await expect(this.smtpUseSslSwitch).toBeChecked();
  }

  /**
   * Verify SSL is disabled
   */
  async expectSslDisabled(): Promise<void> {
    await expect(this.smtpUseSslSwitch).not.toBeChecked();
  }
}
