import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * PrivacyPage - Page Object for the public Privacy Policy page (/privacy)
 *
 * This page displays the Privacy Policy content fetched from the API.
 * It includes navigation back to the landing page and links to the Terms page.
 */
export class PrivacyPage extends BasePage {
  // Navigation elements
  readonly logo: Locator;
  readonly backButton: Locator;

  // Content elements
  readonly pageTitle: Locator;
  readonly lastUpdatedText: Locator;
  readonly contentArea: Locator;

  // Loading state
  readonly loadingIndicator: Locator;

  // Error state
  readonly errorMessage: Locator;
  readonly backToHomeButton: Locator;

  // Footer elements
  readonly footer: Locator;
  readonly termsLink: Locator;

  constructor(page: Page) {
    super(page);

    // Navigation
    this.logo = page.locator('nav a:has-text("NOIR")').first();
    this.backButton = page.locator('nav button:has-text("Back"), nav a:has-text("Back")').first();

    // Content
    this.pageTitle = page.locator('main h1');
    this.lastUpdatedText = page.locator('main p:has-text("Last updated")');
    this.contentArea = page.locator('main .prose, main div[class*="prose"]');

    // Loading state
    this.loadingIndicator = page.locator('.animate-pulse, [data-testid="loading"]').first();

    // Error state
    this.errorMessage = page.locator('p:has-text("Unable to load"), p:has-text("Page not found")');
    this.backToHomeButton = page.locator('button:has-text("Back to Home"), a:has-text("Back to Home")');

    // Footer
    this.footer = page.locator('footer');
    this.termsLink = page.locator('footer a[href="/terms"]');
  }

  /**
   * Navigate to the Privacy Policy page
   */
  async navigate(): Promise<void> {
    await this.goto('/privacy');
  }

  /**
   * Verify the Privacy page has loaded correctly with content
   */
  async expectPageLoaded(): Promise<void> {
    await expect(this.pageTitle).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.contentArea).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify the page title contains expected text
   */
  async expectTitleContains(text: string): Promise<void> {
    await expect(this.pageTitle).toContainText(text, { timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify the page shows the last updated date
   */
  async expectLastUpdatedVisible(): Promise<void> {
    await expect(this.lastUpdatedText).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify the page is in loading state
   */
  async expectLoadingState(): Promise<void> {
    await expect(this.loadingIndicator).toBeVisible({ timeout: Timeouts.QUICK_CHECK });
  }

  /**
   * Verify the page shows an error state
   */
  async expectErrorState(): Promise<void> {
    await expect(this.errorMessage).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.backToHomeButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Click the back button to return to landing page
   */
  async clickBack(): Promise<void> {
    await this.backButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Click the logo to return to landing page
   */
  async clickLogo(): Promise<void> {
    await this.logo.click();
    await this.waitForPageLoad();
  }

  /**
   * Navigate to Terms page via footer link
   */
  async clickTermsLink(): Promise<void> {
    await this.termsLink.click();
    await this.waitForPageLoad();
  }

  /**
   * Verify navigation elements are present
   */
  async expectNavigationVisible(): Promise<void> {
    await expect(this.logo).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.backButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify footer elements are visible
   */
  async expectFooterVisible(): Promise<void> {
    await expect(this.footer).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.termsLink).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Get the page title text
   */
  async getPageTitle(): Promise<string> {
    return await this.pageTitle.textContent() || '';
  }

  /**
   * Verify content area has text (not empty)
   */
  async expectContentNotEmpty(): Promise<void> {
    const content = await this.contentArea.textContent();
    expect(content?.trim().length).toBeGreaterThan(0);
  }
}
