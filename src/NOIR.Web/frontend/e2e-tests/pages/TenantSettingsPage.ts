import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * TenantSettingsPage - Page Object for Tenant Settings Management
 *
 * Based on: src/pages/portal/admin/tenant-settings/TenantSettingsPage.tsx
 * - Uses Tabs component with multiple tabs
 * - Tabs: Regional, Branding, Contact, SMTP, Payment Gateways, Legal Pages, Email Templates
 */
export class TenantSettingsPage extends BasePage {
  readonly pageHeader: Locator;
  readonly tabsList: Locator;

  // Tab triggers
  readonly regionalTab: Locator;
  readonly brandingTab: Locator;
  readonly contactTab: Locator;
  readonly smtpTab: Locator;
  readonly paymentGatewaysTab: Locator;
  readonly legalPagesTab: Locator;
  readonly emailTemplatesTab: Locator;

  // Common form elements
  readonly saveButton: Locator;
  readonly cancelButton: Locator;
  readonly successToast: Locator;

  // Regional Settings
  readonly defaultLanguageSelect: Locator;
  readonly currencySelect: Locator;
  readonly timezoneSelect: Locator;

  // Branding Settings
  readonly brandNameInput: Locator;
  readonly logoUpload: Locator;
  readonly primaryColorInput: Locator;

  // Contact Settings
  readonly supportEmailInput: Locator;
  readonly supportPhoneInput: Locator;
  readonly addressInput: Locator;

  // SMTP Settings
  readonly smtpHostInput: Locator;
  readonly smtpPortInput: Locator;
  readonly smtpUsernameInput: Locator;
  readonly smtpPasswordInput: Locator;
  readonly fromEmailInput: Locator;
  readonly fromNameInput: Locator;
  readonly testEmailButton: Locator;

  constructor(page: Page) {
    super(page);

    this.pageHeader = page.locator('h1:has-text("Tenant Settings"), h1:has-text("Settings")');
    this.tabsList = page.locator('[role="tablist"]');

    // Tab triggers - using data-value or text content
    this.regionalTab = page.locator('[role="tab"]:has-text("Regional"), [data-value="regional"]');
    this.brandingTab = page.locator('[role="tab"]:has-text("Branding"), [data-value="branding"]');
    this.contactTab = page.locator('[role="tab"]:has-text("Contact"), [data-value="contact"]');
    this.smtpTab = page.locator('[role="tab"]:has-text("SMTP"), [role="tab"]:has-text("Email"), [data-value="smtp"]');
    this.paymentGatewaysTab = page.locator('[role="tab"]:has-text("Payment"), [data-value="payment"]');
    this.legalPagesTab = page.locator('[role="tab"]:has-text("Legal"), [data-value="legal"]');
    this.emailTemplatesTab = page.locator('[role="tab"]:has-text("Email Templates"), [data-value="email-templates"]');

    // Common form elements
    this.saveButton = page.locator('button[type="submit"], button:has-text("Save")');
    this.cancelButton = page.locator('button:has-text("Cancel")');
    this.successToast = page.locator('[data-testid="success-toast"], .toast-success');

    // Regional Settings inputs
    this.defaultLanguageSelect = page.locator('[name="defaultLanguage"], #defaultLanguage, button[role="combobox"]:near(:text("Language"))');
    this.currencySelect = page.locator('[name="currency"], #currency, button[role="combobox"]:near(:text("Currency"))');
    this.timezoneSelect = page.locator('[name="timezone"], #timezone, button[role="combobox"]:near(:text("Timezone"))');

    // Branding Settings inputs
    this.brandNameInput = page.locator('input[name="brandName"], input#brandName, input:near(:text("Brand Name"))');
    this.logoUpload = page.locator('input[type="file"]');
    this.primaryColorInput = page.locator('input[name="primaryColor"], input#primaryColor, input[type="color"]');

    // Contact Settings inputs
    this.supportEmailInput = page.locator('input[name="supportEmail"], input#supportEmail');
    this.supportPhoneInput = page.locator('input[name="supportPhone"], input#supportPhone');
    this.addressInput = page.locator('textarea[name="address"], textarea#address');

    // SMTP Settings inputs
    this.smtpHostInput = page.locator('input[name="smtpHost"], input#smtpHost');
    this.smtpPortInput = page.locator('input[name="smtpPort"], input#smtpPort');
    this.smtpUsernameInput = page.locator('input[name="smtpUsername"], input#smtpUsername');
    this.smtpPasswordInput = page.locator('input[name="smtpPassword"], input#smtpPassword');
    this.fromEmailInput = page.locator('input[name="fromEmail"], input#fromEmail');
    this.fromNameInput = page.locator('input[name="fromName"], input#fromName');
    this.testEmailButton = page.locator('button:has-text("Test"), button:has-text("Send Test")');
  }

  /**
   * Navigate to tenant settings page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/admin/tenant-settings');
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
   * Click on a specific tab
   */
  async selectTab(tabName: 'regional' | 'branding' | 'contact' | 'smtp' | 'payment' | 'legal' | 'email-templates'): Promise<void> {
    const tabMap = {
      'regional': this.regionalTab,
      'branding': this.brandingTab,
      'contact': this.contactTab,
      'smtp': this.smtpTab,
      'payment': this.paymentGatewaysTab,
      'legal': this.legalPagesTab,
      'email-templates': this.emailTemplatesTab,
    };

    const tab = tabMap[tabName];
    if (tab) {
      await tab.first().click();
      await this.page.waitForTimeout(500);
    }
  }

  /**
   * Verify a tab is active
   */
  async expectTabActive(tabName: string): Promise<void> {
    const tab = this.page.locator(`[role="tab"]:has-text("${tabName}")`);
    await expect(tab.first()).toHaveAttribute('aria-selected', 'true', { timeout: 5000 });
  }

  /**
   * Save settings
   */
  async saveSettings(): Promise<void> {
    await this.saveButton.first().click();
    await this.expectSuccessToast();
  }

  /**
   * Update regional settings
   */
  async updateRegionalSettings(data: {
    language?: string;
    currency?: string;
    timezone?: string;
  }): Promise<void> {
    await this.selectTab('regional');

    if (data.language && await this.defaultLanguageSelect.first().isVisible()) {
      await this.defaultLanguageSelect.first().click();
      const option = this.page.locator(`[role="option"]:has-text("${data.language}")`);
      await option.click();
    }

    if (data.currency && await this.currencySelect.first().isVisible()) {
      await this.currencySelect.first().click();
      const option = this.page.locator(`[role="option"]:has-text("${data.currency}")`);
      await option.click();
    }

    await this.saveSettings();
  }

  /**
   * Update branding settings
   */
  async updateBrandingSettings(data: {
    brandName?: string;
    primaryColor?: string;
  }): Promise<void> {
    await this.selectTab('branding');

    if (data.brandName && await this.brandNameInput.first().isVisible()) {
      await this.brandNameInput.first().clear();
      await this.brandNameInput.first().fill(data.brandName);
    }

    await this.saveSettings();
  }

  /**
   * Update contact settings
   */
  async updateContactSettings(data: {
    supportEmail?: string;
    supportPhone?: string;
    address?: string;
  }): Promise<void> {
    await this.selectTab('contact');

    if (data.supportEmail && await this.supportEmailInput.isVisible()) {
      await this.supportEmailInput.clear();
      await this.supportEmailInput.fill(data.supportEmail);
    }

    if (data.supportPhone && await this.supportPhoneInput.isVisible()) {
      await this.supportPhoneInput.clear();
      await this.supportPhoneInput.fill(data.supportPhone);
    }

    if (data.address && await this.addressInput.isVisible()) {
      await this.addressInput.clear();
      await this.addressInput.fill(data.address);
    }

    await this.saveSettings();
  }

  /**
   * Test SMTP connection
   */
  async testSmtpConnection(): Promise<void> {
    await this.selectTab('smtp');

    if (await this.testEmailButton.isVisible()) {
      await this.testEmailButton.click();
      // Wait for response
      await this.page.waitForTimeout(2000);
    }
  }
}
