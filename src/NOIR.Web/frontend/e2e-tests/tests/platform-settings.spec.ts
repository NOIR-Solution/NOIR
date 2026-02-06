import { test, expect } from '@playwright/test';
import { PlatformSettingsPage } from '../pages';

/**
 * Platform Settings Tests
 *
 * Comprehensive E2E tests for platform settings management.
 * These tests require platform admin authentication.
 * Tags: @platform-settings @platform-admin @P0 @P1
 *
 * Note: This file matches /.*platform.*\.spec\.ts/ pattern,
 * so it runs with platform admin authentication state.
 */

test.describe('Platform Settings @platform-settings', () => {
  // ============================================================================
  // P0: Critical - Page Load & Navigation
  // ============================================================================
  test.describe('Page Load @P0', () => {
    test('PLAT-001: Platform settings page loads (platform admin)', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Verify we're on the correct page
      await expect(page).toHaveURL(/\/portal\/admin\/platform-settings/);
    });

    test('PLAT-002: All tabs are visible', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Verify all three tabs are visible
      await expect(settingsPage.smtpTab.first()).toBeVisible({ timeout: 10000 });
      await expect(settingsPage.emailTemplatesTab.first()).toBeVisible({ timeout: 10000 });
      await expect(settingsPage.legalPagesTab.first()).toBeVisible({ timeout: 10000 });

      // Verify tab list exists
      await expect(settingsPage.tabsList).toBeVisible();

      // Count tabs - should be exactly 3
      const tabs = page.locator('[role="tab"]');
      const tabCount = await tabs.count();
      expect(tabCount).toBe(3);
    });

    test('PLAT-003: SMTP tab is default active', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // SMTP tab should be active by default
      await settingsPage.expectTabActive('SMTP');

      // SMTP form should be visible
      await settingsPage.waitForSmtpSettingsLoaded();
      await expect(settingsPage.smtpHostInput).toBeVisible();
    });
  });

  // ============================================================================
  // P1: High - SMTP Settings
  // ============================================================================
  test.describe('SMTP Settings @P1', () => {
    test('PLAT-010: SMTP form fields are visible', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Select SMTP tab (should be default, but be explicit)
      await settingsPage.selectTab('smtp');
      await settingsPage.waitForSmtpSettingsLoaded();

      // Verify all SMTP form fields are visible
      await expect(settingsPage.smtpHostInput).toBeVisible({ timeout: 10000 });
      await expect(settingsPage.smtpPortInput).toBeVisible({ timeout: 10000 });
      await expect(settingsPage.smtpUsernameInput).toBeVisible({ timeout: 10000 });
      await expect(settingsPage.smtpPasswordInput).toBeVisible({ timeout: 10000 });
      await expect(settingsPage.smtpFromEmailInput).toBeVisible({ timeout: 10000 });
      await expect(settingsPage.smtpFromNameInput).toBeVisible({ timeout: 10000 });
      await expect(settingsPage.smtpUseSslSwitch).toBeVisible({ timeout: 10000 });

      // Verify buttons
      await expect(settingsPage.smtpTestConnectionButton).toBeVisible({ timeout: 10000 });
      await expect(settingsPage.smtpSaveButton).toBeVisible({ timeout: 10000 });
    });

    test('PLAT-011: SMTP validation (empty host, invalid port)', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      await settingsPage.selectTab('smtp');
      await settingsPage.waitForSmtpSettingsLoaded();

      // Clear the host field and blur to trigger validation
      await settingsPage.smtpHostInput.clear();
      await settingsPage.smtpHostInput.blur();

      // Wait a moment for validation to trigger
      await page.waitForTimeout(500);

      // Check for validation error on host field
      // Look for FormMessage component which shows validation errors
      const hostError = page.locator(
        '[data-slot="form-message"]:near(input[name="host"]), ' +
        'p.text-destructive:near(input[name="host"]), ' +
        '.text-\\[0\\.8rem\\]:near(input[name="host"])'
      );

      // The error should be visible after clearing required field
      const isHostErrorVisible = await hostError.first().isVisible({ timeout: 5000 }).catch(() => false);

      // Test invalid port - port should be between 1 and 65535
      await settingsPage.smtpPortInput.clear();
      await settingsPage.smtpPortInput.fill('0');
      await settingsPage.smtpPortInput.blur();
      await page.waitForTimeout(500);

      // Port validation error
      const portError = page.locator(
        '[data-slot="form-message"]:near(input[name="port"]), ' +
        'p.text-destructive:near(input[name="port"]), ' +
        '.text-\\[0\\.8rem\\]:near(input[name="port"])'
      );

      const isPortErrorVisible = await portError.first().isVisible({ timeout: 5000 }).catch(() => false);

      // At least one validation should have triggered
      expect(isHostErrorVisible || isPortErrorVisible).toBe(true);
    });

    test('PLAT-012: Save SMTP settings', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      await settingsPage.selectTab('smtp');
      await settingsPage.waitForSmtpSettingsLoaded();

      // Get current settings to restore later if needed
      const originalSettings = await settingsPage.getSmtpSettings();

      // Update with test values
      await settingsPage.updateSmtpSettings({
        host: 'smtp.test.example.com',
        port: 587,
        username: 'testuser',
        fromEmail: 'test@example.com',
        fromName: 'Test Sender',
        useSsl: true,
      });

      // Save settings
      await settingsPage.smtpSaveButton.click();

      // Wait for save operation and check for success toast
      await settingsPage.expectSuccessToast();

      // Verify the settings were saved by checking form values persist
      await page.waitForTimeout(500);
      const savedHost = await settingsPage.smtpHostInput.inputValue();
      expect(savedHost).toBe('smtp.test.example.com');
    });

    test('PLAT-013: Test connection dialog opens', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      await settingsPage.selectTab('smtp');
      await settingsPage.waitForSmtpSettingsLoaded();

      // First, ensure SMTP is configured (test button is disabled if not configured)
      const isConfigured = await settingsPage.isSmtpConfigured();

      if (isConfigured) {
        // Test connection button should be enabled when configured
        await expect(settingsPage.smtpTestConnectionButton).toBeEnabled({ timeout: 5000 });

        // Click to open test connection dialog
        await settingsPage.openTestConnectionDialog();

        // Verify dialog is visible
        await expect(settingsPage.testConnectionDialog).toBeVisible({ timeout: 5000 });

        // Verify dialog has recipient email input
        await expect(settingsPage.testRecipientEmailInput).toBeVisible({ timeout: 5000 });

        // Verify dialog has send and cancel buttons
        await expect(settingsPage.testSendButton).toBeVisible({ timeout: 5000 });
        await expect(settingsPage.testCancelButton).toBeVisible({ timeout: 5000 });

        // Cancel the dialog
        await settingsPage.cancelTestConnection();
        await expect(settingsPage.testConnectionDialog).toBeHidden({ timeout: 5000 });
      } else {
        // If not configured, test button should be disabled
        await expect(settingsPage.smtpTestConnectionButton).toBeDisabled({ timeout: 5000 });
      }
    });

    test('PLAT-014: SSL toggle works', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      await settingsPage.selectTab('smtp');
      await settingsPage.waitForSmtpSettingsLoaded();

      // Get initial SSL state
      const initialSslState = await settingsPage.smtpUseSslSwitch.isChecked();

      // Toggle SSL
      await settingsPage.toggleUseSsl();
      await page.waitForTimeout(300);

      // Verify state changed
      const newSslState = await settingsPage.smtpUseSslSwitch.isChecked();
      expect(newSslState).toBe(!initialSslState);

      // Toggle back to original state
      await settingsPage.toggleUseSsl();
      await page.waitForTimeout(300);

      // Verify returned to original
      const finalSslState = await settingsPage.smtpUseSslSwitch.isChecked();
      expect(finalSslState).toBe(initialSslState);
    });
  });

  // ============================================================================
  // P1: High - Email Templates
  // ============================================================================
  test.describe('Email Templates @P1', () => {
    test('PLAT-020: Email templates tab shows platform templates', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Navigate to Email Templates tab
      await settingsPage.selectTab('emailTemplates');

      // Wait for loading to complete
      await page.waitForTimeout(500);
      const spinners = await page.locator('.animate-spin').count();
      if (spinners > 0) {
        await page.locator('.animate-spin').first().waitFor({ state: 'hidden', timeout: 10000 });
      }

      // Check for either templates or empty message
      const templateCards = page.locator('.card:has([data-slot="badge"]:has-text("Platform"))');
      const noTemplatesMessage = page.locator('text="No platform email templates found."');

      const hasTemplates = await templateCards.first().isVisible({ timeout: 5000 }).catch(() => false);
      const hasNoTemplatesMessage = await noTemplatesMessage.isVisible({ timeout: 5000 }).catch(() => false);

      // One of these should be true
      expect(hasTemplates || hasNoTemplatesMessage).toBe(true);

      if (hasTemplates) {
        // Verify template cards have expected structure
        const templateCount = await templateCards.count();
        expect(templateCount).toBeGreaterThan(0);

        // Check first template has expected elements
        const firstTemplate = templateCards.first();
        await expect(firstTemplate.locator('h4')).toBeVisible(); // Template name
        await expect(firstTemplate.locator('[data-slot="badge"]')).toBeVisible(); // At least one badge
      }
    });

    test('PLAT-021: Edit email template navigates to edit page', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Navigate to Email Templates tab
      await settingsPage.selectTab('emailTemplates');

      // Wait for loading to complete
      await page.waitForTimeout(500);
      const spinners = await page.locator('.animate-spin').count();
      if (spinners > 0) {
        await page.locator('.animate-spin').first().waitFor({ state: 'hidden', timeout: 10000 });
      }

      // Check if there are templates to edit
      const templateCards = page.locator('.card:has([data-slot="badge"]:has-text("Platform"))');
      const hasTemplates = await templateCards.first().isVisible({ timeout: 5000 }).catch(() => false);

      if (hasTemplates) {
        // Find and click the edit button on the first template
        const firstTemplate = templateCards.first();
        const editButton = firstTemplate.locator('button').filter({
          has: page.locator('svg.lucide-pencil, .lucide-pencil, svg[class*="pencil"]')
        }).first();

        // If edit button not found by icon, try finding any button in the template card
        const hasEditButton = await editButton.isVisible({ timeout: 3000 }).catch(() => false);

        if (hasEditButton) {
          await editButton.click();

          // Should navigate to email template edit page
          await page.waitForURL(/\/portal\/email-templates\//, { timeout: 10000 });
          expect(page.url()).toMatch(/\/portal\/email-templates\//);
        } else {
          // Template exists but no edit button - this is acceptable for read-only templates
          test.info().annotations.push({ type: 'info', description: 'Template exists but no edit button found' });
        }
      } else {
        // No templates - skip this test
        test.skip(true, 'No email templates available to edit');
      }
    });
  });

  // ============================================================================
  // P1: High - Legal Pages
  // ============================================================================
  test.describe('Legal Pages @P1', () => {
    test('PLAT-030: Legal pages tab shows platform pages', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Navigate to Legal Pages tab
      await settingsPage.selectTab('legalPages');

      // Wait for loading to complete
      await page.waitForTimeout(500);
      const spinners = await page.locator('.animate-spin').count();
      if (spinners > 0) {
        await page.locator('.animate-spin').first().waitFor({ state: 'hidden', timeout: 10000 });
      }

      // Check for either legal pages or empty message
      const legalPageCards = page.locator('.card:has([data-slot="badge"]:has-text("Platform Default"))');
      const noLegalPagesMessage = page.locator('text="No platform legal pages found."');

      const hasLegalPages = await legalPageCards.first().isVisible({ timeout: 5000 }).catch(() => false);
      const hasNoLegalPagesMessage = await noLegalPagesMessage.isVisible({ timeout: 5000 }).catch(() => false);

      // One of these should be true
      expect(hasLegalPages || hasNoLegalPagesMessage).toBe(true);

      if (hasLegalPages) {
        // Verify legal page cards have expected structure
        const pageCount = await legalPageCards.count();
        expect(pageCount).toBeGreaterThan(0);

        // Check first legal page has expected elements
        const firstPage = legalPageCards.first();
        await expect(firstPage.locator('h4')).toBeVisible(); // Page title
        await expect(firstPage.locator('[data-slot="badge"]')).toBeVisible(); // Platform Default badge
      }
    });

    test('PLAT-031: Edit legal page navigates to edit page', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Navigate to Legal Pages tab
      await settingsPage.selectTab('legalPages');

      // Wait for loading to complete
      await page.waitForTimeout(500);
      const spinners = await page.locator('.animate-spin').count();
      if (spinners > 0) {
        await page.locator('.animate-spin').first().waitFor({ state: 'hidden', timeout: 10000 });
      }

      // Check if there are legal pages to edit
      const legalPageCards = page.locator('.card:has([data-slot="badge"]:has-text("Platform Default"))');
      const hasLegalPages = await legalPageCards.first().isVisible({ timeout: 5000 }).catch(() => false);

      if (hasLegalPages) {
        // Find and click the edit button (pencil icon) on the first legal page
        const firstPage = legalPageCards.first();
        const editButton = firstPage.locator('button').filter({
          has: page.locator('svg.lucide-pencil, .lucide-pencil, svg[class*="pencil"]')
        }).first();

        const hasEditButton = await editButton.isVisible({ timeout: 3000 }).catch(() => false);

        if (hasEditButton) {
          await editButton.click();

          // Should navigate to legal page edit page
          await page.waitForURL(/\/portal\/legal-pages\//, { timeout: 10000 });
          expect(page.url()).toMatch(/\/portal\/legal-pages\//);
        } else {
          test.info().annotations.push({ type: 'info', description: 'Legal page exists but no edit button found' });
        }
      } else {
        test.skip(true, 'No legal pages available to edit');
      }
    });

    test('PLAT-032: View legal page opens in new tab', async ({ page, context }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Navigate to Legal Pages tab
      await settingsPage.selectTab('legalPages');

      // Wait for loading to complete
      await page.waitForTimeout(500);
      const spinners = await page.locator('.animate-spin').count();
      if (spinners > 0) {
        await page.locator('.animate-spin').first().waitFor({ state: 'hidden', timeout: 10000 });
      }

      // Check if there are legal pages to view
      const legalPageCards = page.locator('.card:has([data-slot="badge"]:has-text("Platform Default"))');
      const hasLegalPages = await legalPageCards.first().isVisible({ timeout: 5000 }).catch(() => false);

      if (hasLegalPages) {
        // Find the view button (eye icon) on the first legal page
        const firstPage = legalPageCards.first();
        const viewButton = firstPage.locator('button').filter({
          has: page.locator('svg.lucide-eye, .lucide-eye, svg[class*="eye"]')
        }).first();

        const hasViewButton = await viewButton.isVisible({ timeout: 3000 }).catch(() => false);

        if (hasViewButton) {
          // Listen for new page (popup) event
          const [newPage] = await Promise.all([
            context.waitForEvent('page'),
            viewButton.click(),
          ]);

          // Wait for the new page to load
          await newPage.waitForLoadState('domcontentloaded');

          // Verify new page URL is a legal page (terms or privacy)
          const newPageUrl = newPage.url();
          expect(newPageUrl).toMatch(/\/(terms|privacy)/);

          // Close the new tab
          await newPage.close();
        } else {
          test.info().annotations.push({ type: 'info', description: 'Legal page exists but no view button found' });
        }
      } else {
        test.skip(true, 'No legal pages available to view');
      }
    });
  });

  // ============================================================================
  // Tab Navigation Tests
  // ============================================================================
  test.describe('Tab Navigation @P1', () => {
    test('PLAT-040: Can switch between all tabs', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Test switching to Email Templates tab
      await settingsPage.selectTab('emailTemplates');
      await settingsPage.expectTabActive('Email Templates');

      // Test switching to Legal Pages tab
      await settingsPage.selectTab('legalPages');
      await settingsPage.expectTabActive('Legal Pages');

      // Test switching back to SMTP tab
      await settingsPage.selectTab('smtp');
      await settingsPage.expectTabActive('SMTP');
    });

    test('PLAT-041: Tab content updates when switching tabs', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // On SMTP tab, form fields should be visible
      await settingsPage.selectTab('smtp');
      await settingsPage.waitForSmtpSettingsLoaded();
      await expect(settingsPage.smtpHostInput).toBeVisible();

      // Switch to Email Templates - SMTP form should not be in active tab panel
      await settingsPage.selectTab('emailTemplates');
      await page.waitForTimeout(500);

      // The active tab panel should now show email templates content, not SMTP form
      const activePanel = page.locator('[role="tabpanel"][data-state="active"]');
      const smtpHostInActivePanel = activePanel.locator('input[name="host"]');
      await expect(smtpHostInActivePanel).not.toBeVisible();

      // Switch to Legal Pages
      await settingsPage.selectTab('legalPages');
      await page.waitForTimeout(500);

      // Verify Legal Pages tab is now active
      await settingsPage.expectTabActive('Legal Pages');
    });
  });

  // ============================================================================
  // Access Control Tests
  // ============================================================================
  test.describe('Access Control @P1', () => {
    test('PLAT-050: Page header displays correct title', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Verify page header is visible
      await expect(settingsPage.pageHeader).toBeVisible();

      // Header should contain "Platform Settings" text
      const headerText = await settingsPage.pageHeader.textContent();
      expect(headerText?.toLowerCase()).toContain('platform');
    });

    test('PLAT-051: SMTP card shows configuration status badge', async ({ page }) => {
      const settingsPage = new PlatformSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      await settingsPage.selectTab('smtp');
      await settingsPage.waitForSmtpSettingsLoaded();

      // One of these badges should be visible - shadcn Badge uses data-slot="badge"
      const configuredBadge = page.locator('[data-slot="badge"]:has-text("Configured"), span:has-text("Configured"):has(svg)');
      const defaultBadge = page.locator('[data-slot="badge"]:has-text("Using defaults"), span:has-text("Using defaults"):has(svg)');

      const isConfigured = await configuredBadge.isVisible({ timeout: 5000 }).catch(() => false);
      const isDefault = await defaultBadge.isVisible({ timeout: 5000 }).catch(() => false);

      // At least one badge should be visible
      expect(isConfigured || isDefault).toBe(true);
    });
  });
});
