import { test, expect } from '@playwright/test';
import { LandingPage, TermsPage, PrivacyPage, LoginPage, Timeouts } from '../pages';

/**
 * Public Pages Tests
 *
 * Comprehensive tests for public-facing pages that don't require authentication:
 * - Landing Page (/)
 * - Terms of Service (/terms)
 * - Privacy Policy (/privacy)
 *
 * Tags: @public @P0 @P1
 */

// All public page tests use unauthenticated state
test.use({ storageState: { cookies: [], origins: [] } });

test.describe('Public Pages @public', () => {
  test.describe('Landing Page @P0', () => {
    test('PUBLIC-001: Landing page loads successfully', async ({ page }) => {
      const landingPage = new LandingPage(page);
      await landingPage.navigate();
      await landingPage.expectPageLoaded();

      // Verify URL
      await expect(page).toHaveURL('/');
    });

    test('PUBLIC-002: Hero section is visible with all elements', async ({ page }) => {
      const landingPage = new LandingPage(page);
      await landingPage.navigate();
      await landingPage.expectPageLoaded();

      // Verify hero section elements
      await landingPage.expectHeroSectionVisible();
      await expect(landingPage.heroHeadline).toContainText('NOIR');
    });

    test('PUBLIC-003: Access portal button navigates to login', async ({ page }) => {
      const landingPage = new LandingPage(page);
      await landingPage.navigate();
      await landingPage.expectPageLoaded();

      // Click the Access Portal button
      await landingPage.clickAccessPortal();

      // Should redirect to login (unauthenticated users)
      await expect(page).toHaveURL(/\/login/, { timeout: Timeouts.PAGE_LOAD });

      // Verify login page elements are visible
      const loginPage = new LoginPage(page);
      await loginPage.expectOnLoginPage();
    });

    test('PUBLIC-004: Trust indicators are visible', async ({ page }) => {
      const landingPage = new LandingPage(page);
      await landingPage.navigate();
      await landingPage.expectPageLoaded();

      // Verify trust indicator cards
      await landingPage.expectTrustIndicatorsVisible();
    });
  });

  test.describe('Terms of Service Page @P1', () => {
    test('PUBLIC-010: Terms page loads successfully', async ({ page }) => {
      const termsPage = new TermsPage(page);
      await termsPage.navigate();
      await termsPage.expectPageLoaded();

      // Verify URL
      await expect(page).toHaveURL('/terms');
    });

    test('PUBLIC-011: Page title is visible', async ({ page }) => {
      const termsPage = new TermsPage(page);
      await termsPage.navigate();
      await termsPage.expectPageLoaded();

      // Verify title contains expected text
      await expect(termsPage.pageTitle).toBeVisible();
      const title = await termsPage.getPageTitle();
      expect(title.length).toBeGreaterThan(0);
    });

    test('PUBLIC-012: Content area is visible and not empty', async ({ page }) => {
      const termsPage = new TermsPage(page);
      await termsPage.navigate();
      await termsPage.expectPageLoaded();

      // Verify content area is visible
      await expect(termsPage.contentArea).toBeVisible();

      // Verify content is not empty
      await termsPage.expectContentNotEmpty();
    });

    test('PUBLIC-013: Back navigation works', async ({ page }) => {
      const termsPage = new TermsPage(page);
      await termsPage.navigate();
      await termsPage.expectPageLoaded();

      // Verify navigation elements are present
      await termsPage.expectNavigationVisible();

      // Click the logo to go back to landing page
      await termsPage.clickLogo();

      // Should be on landing page
      await expect(page).toHaveURL('/');

      // Verify landing page loaded
      const landingPage = new LandingPage(page);
      await landingPage.expectPageLoaded();
    });

    test('PUBLIC-014: Privacy link navigates to privacy page', async ({ page }) => {
      const termsPage = new TermsPage(page);
      await termsPage.navigate();
      await termsPage.expectPageLoaded();

      // Verify footer has privacy link
      await termsPage.expectFooterVisible();

      // Click privacy link
      await termsPage.clickPrivacyLink();

      // Should be on privacy page
      await expect(page).toHaveURL('/privacy');

      // Verify privacy page loaded
      const privacyPage = new PrivacyPage(page);
      await privacyPage.expectPageLoaded();
    });
  });

  test.describe('Privacy Policy Page @P1', () => {
    test('PUBLIC-020: Privacy page loads successfully', async ({ page }) => {
      const privacyPage = new PrivacyPage(page);
      await privacyPage.navigate();
      await privacyPage.expectPageLoaded();

      // Verify URL
      await expect(page).toHaveURL('/privacy');
    });

    test('PUBLIC-021: Page title is visible', async ({ page }) => {
      const privacyPage = new PrivacyPage(page);
      await privacyPage.navigate();
      await privacyPage.expectPageLoaded();

      // Verify title is visible
      await expect(privacyPage.pageTitle).toBeVisible();
      const title = await privacyPage.getPageTitle();
      expect(title.length).toBeGreaterThan(0);
    });

    test('PUBLIC-022: Content area is visible and not empty', async ({ page }) => {
      const privacyPage = new PrivacyPage(page);
      await privacyPage.navigate();
      await privacyPage.expectPageLoaded();

      // Verify content area is visible
      await expect(privacyPage.contentArea).toBeVisible();

      // Verify content is not empty
      await privacyPage.expectContentNotEmpty();
    });

    test('PUBLIC-023: Terms link navigates to terms page', async ({ page }) => {
      const privacyPage = new PrivacyPage(page);
      await privacyPage.navigate();
      await privacyPage.expectPageLoaded();

      // Verify footer has terms link
      await privacyPage.expectFooterVisible();

      // Click terms link
      await privacyPage.clickTermsLink();

      // Should be on terms page
      await expect(page).toHaveURL('/terms');

      // Verify terms page loaded
      const termsPage = new TermsPage(page);
      await termsPage.expectPageLoaded();
    });
  });

  test.describe('Cross-Page Navigation @P1', () => {
    test('PUBLIC-030: Can navigate from landing to terms via footer', async ({ page }) => {
      const landingPage = new LandingPage(page);
      await landingPage.navigate();
      await landingPage.expectPageLoaded();

      // Verify footer is visible
      await landingPage.expectFooterVisible();

      // Click terms link in footer
      await landingPage.clickTermsLink();

      // Should be on terms page
      await expect(page).toHaveURL('/terms');

      const termsPage = new TermsPage(page);
      await termsPage.expectPageLoaded();
    });

    test('PUBLIC-031: Can navigate from landing to privacy via footer', async ({ page }) => {
      const landingPage = new LandingPage(page);
      await landingPage.navigate();
      await landingPage.expectPageLoaded();

      // Verify footer is visible
      await landingPage.expectFooterVisible();

      // Click privacy link in footer
      await landingPage.clickPrivacyLink();

      // Should be on privacy page
      await expect(page).toHaveURL('/privacy');

      const privacyPage = new PrivacyPage(page);
      await privacyPage.expectPageLoaded();
    });

    test('PUBLIC-032: Can navigate between terms and privacy pages', async ({ page }) => {
      // Start at terms page
      const termsPage = new TermsPage(page);
      await termsPage.navigate();
      await termsPage.expectPageLoaded();

      // Navigate to privacy
      await termsPage.clickPrivacyLink();
      await expect(page).toHaveURL('/privacy');

      // Navigate back to terms
      const privacyPage = new PrivacyPage(page);
      await privacyPage.expectPageLoaded();
      await privacyPage.clickTermsLink();
      await expect(page).toHaveURL('/terms');

      // Verify terms page loaded again
      await termsPage.expectPageLoaded();
    });
  });
});
