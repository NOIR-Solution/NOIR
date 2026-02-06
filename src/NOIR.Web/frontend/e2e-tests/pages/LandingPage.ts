import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * LandingPage - Page Object for the public landing page (/)
 *
 * The landing page is the main entry point for the application,
 * featuring hero content, trust indicators, and navigation to the portal.
 */
export class LandingPage extends BasePage {
  // Navigation elements
  readonly logo: Locator;
  readonly portalButton: Locator;
  readonly languageSwitcher: Locator;
  readonly themeToggle: Locator;

  // Hero section elements
  readonly heroBadge: Locator;
  readonly heroHeadline: Locator;
  readonly heroDescription: Locator;
  readonly accessPortalButton: Locator;

  // Trust indicators
  readonly trustIndicators: Locator;
  readonly enterpriseSecurityCard: Locator;
  readonly realTimeSyncCard: Locator;
  readonly multiTenantCard: Locator;

  // Footer elements
  readonly footer: Locator;
  readonly termsLink: Locator;
  readonly privacyLink: Locator;
  readonly copyrightText: Locator;

  constructor(page: Page) {
    super(page);

    // Navigation
    this.logo = page.locator('nav a:has-text("NOIR")').first();
    this.portalButton = page.locator('nav a[href="/portal"] button, nav button:has-text("Portal")').first();
    this.languageSwitcher = page.locator('[data-testid="language-switcher"], button:has-text("EN"), button:has-text("VI")').first();
    this.themeToggle = page.locator('[data-testid="theme-toggle"], button:has([class*="sun"]), button:has([class*="moon"])').first();

    // Hero section
    this.heroBadge = page.locator('span:has-text("AI-Powered"), span:has-text("Enterprise")').first();
    this.heroHeadline = page.locator('h1:has-text("NOIR")');
    this.heroDescription = page.locator('p.text-xl, p.text-2xl').first();
    this.accessPortalButton = page.locator('a[href="/portal"] button:has-text("Access"), button:has-text("Access Portal")').first();

    // Trust indicators
    this.trustIndicators = page.locator('p:has-text("Trusted by")').first();
    this.enterpriseSecurityCard = page.getByText('Enterprise Security').first();
    this.realTimeSyncCard = page.getByText('Real-time Sync').first();
    this.multiTenantCard = page.getByText('Multi-tenant').first();

    // Footer
    this.footer = page.locator('footer');
    this.termsLink = page.locator('footer a[href="/terms"]');
    this.privacyLink = page.locator('footer a[href="/privacy"]');
    this.copyrightText = page.locator('footer p:has-text("NOIR")');
  }

  /**
   * Navigate to the landing page
   */
  async navigate(): Promise<void> {
    await this.goto('/');
  }

  /**
   * Verify the landing page has loaded correctly
   */
  async expectPageLoaded(): Promise<void> {
    await expect(this.heroHeadline).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.accessPortalButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify all hero section elements are visible
   */
  async expectHeroSectionVisible(): Promise<void> {
    await expect(this.heroHeadline).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.heroDescription).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.accessPortalButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify all trust indicator cards are visible
   */
  async expectTrustIndicatorsVisible(): Promise<void> {
    await expect(this.enterpriseSecurityCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.realTimeSyncCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.multiTenantCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify footer elements are visible
   */
  async expectFooterVisible(): Promise<void> {
    await expect(this.footer).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.termsLink).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.privacyLink).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Click the Access Portal button in the hero section
   */
  async clickAccessPortal(): Promise<void> {
    await this.accessPortalButton.click();
    await this.waitForPageLoad();
  }

  /**
   * Click the Portal button in the navigation
   */
  async clickPortalNav(): Promise<void> {
    await this.portalButton.click();
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
   * Navigate to Privacy page via footer link
   */
  async clickPrivacyLink(): Promise<void> {
    await this.privacyLink.click();
    await this.waitForPageLoad();
  }

  /**
   * Verify navigation elements are present
   */
  async expectNavigationVisible(): Promise<void> {
    await expect(this.logo).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.portalButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }
}
