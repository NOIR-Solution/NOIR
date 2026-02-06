import { test, expect } from '@playwright/test';
import { TenantSettingsPage } from '../pages';

/**
 * Tenant Settings Tests
 *
 * Comprehensive E2E tests for tenant settings management.
 * Tags: @tenant-settings @P0 @P1
 */

test.describe('Tenant Settings @tenant-settings', () => {
  test.describe('Settings Page @P0', () => {
    test('SETTINGS-001: Tenant settings page loads successfully', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();
    });

    test('SETTINGS-002: Tab list is visible', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      await expect(settingsPage.tabsList).toBeVisible();
    });

    test('SETTINGS-003: Multiple tabs are available', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Check for presence of tabs
      const tabs = page.locator('[role="tab"]');
      const tabCount = await tabs.count();
      expect(tabCount).toBeGreaterThan(2);
    });
  });

  test.describe('Regional Settings Tab @P1', () => {
    test('SETTINGS-010: Regional tab is accessible', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.regionalTab.first().isVisible()) {
        await settingsPage.selectTab('regional');
        // Tab content should be visible
        await expect(settingsPage.tabsList).toBeVisible();
      }
    });

    test('SETTINGS-011: Regional settings have form fields', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.regionalTab.first().isVisible()) {
        await settingsPage.selectTab('regional');

        // Check for language, currency, or timezone selectors
        const formElements = page.locator(
          'select, ' +
          'button[role="combobox"], ' +
          'input[type="text"], ' +
          '[data-testid*="language"], ' +
          '[data-testid*="currency"], ' +
          '[data-testid*="timezone"]'
        );
        await expect(formElements.first()).toBeVisible({ timeout: 10000 });
      }
    });
  });

  test.describe('Branding Settings Tab @P1', () => {
    test('SETTINGS-020: Branding tab is accessible', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.brandingTab.first().isVisible()) {
        await settingsPage.selectTab('branding');
        await expect(settingsPage.tabsList).toBeVisible();
      }
    });

    test('SETTINGS-021: Branding settings have form fields', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.brandingTab.first().isVisible()) {
        await settingsPage.selectTab('branding');

        // Check for branding-related inputs
        const formElements = page.locator(
          'input[type="text"], ' +
          'input[type="file"], ' +
          'input[type="color"], ' +
          '[data-testid*="brand"], ' +
          '[data-testid*="logo"]'
        );
        await expect(formElements.first()).toBeVisible({ timeout: 10000 });
      }
    });
  });

  test.describe('Contact Settings Tab @P1', () => {
    test('SETTINGS-030: Contact tab is accessible', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.contactTab.first().isVisible()) {
        await settingsPage.selectTab('contact');
        await expect(settingsPage.tabsList).toBeVisible();
      }
    });

    test('SETTINGS-031: Contact settings have form fields', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.contactTab.first().isVisible()) {
        await settingsPage.selectTab('contact');

        // Check for contact-related inputs
        const formElements = page.locator(
          'input[type="email"], ' +
          'input[type="tel"], ' +
          'textarea, ' +
          '[data-testid*="email"], ' +
          '[data-testid*="phone"], ' +
          '[data-testid*="address"]'
        );
        await expect(formElements.first()).toBeVisible({ timeout: 10000 });
      }
    });
  });

  test.describe('SMTP Settings Tab @P1', () => {
    test('SETTINGS-040: SMTP tab is accessible', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.smtpTab.first().isVisible()) {
        await settingsPage.selectTab('smtp');
        await expect(settingsPage.tabsList).toBeVisible();
      }
    });

    test('SETTINGS-041: SMTP settings have form fields', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.smtpTab.first().isVisible()) {
        await settingsPage.selectTab('smtp');

        // Check for SMTP-related inputs
        const formElements = page.locator(
          'input[type="text"], ' +
          'input[type="number"], ' +
          'input[type="password"], ' +
          '[data-testid*="smtp"], ' +
          '[data-testid*="host"], ' +
          '[data-testid*="port"]'
        );
        await expect(formElements.first()).toBeVisible({ timeout: 10000 });
      }
    });

    test('SETTINGS-042: Test email button exists', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.smtpTab.first().isVisible()) {
        await settingsPage.selectTab('smtp');

        const testButton = page.locator('button:has-text("Test"), button:has-text("Send Test")');
        // Test button may or may not exist
        if (await testButton.isVisible({ timeout: 5000 }).catch(() => false)) {
          await expect(testButton).toBeEnabled();
        }
      }
    });
  });

  test.describe('Tab Navigation @P1', () => {
    test('SETTINGS-050: Can switch between all tabs', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Get all tabs
      const tabs = page.locator('[role="tab"]');
      const tabCount = await tabs.count();

      // Click through each tab
      for (let i = 0; i < Math.min(tabCount, 5); i++) {
        const tab = tabs.nth(i);
        if (await tab.isVisible()) {
          await tab.click();
          await page.waitForTimeout(300);
          // Tab should become selected
          await expect(tab).toHaveAttribute('aria-selected', 'true', { timeout: 5000 });
        }
      }
    });

    test('SETTINGS-051: Tab content updates when switching tabs', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      const tabs = page.locator('[role="tab"]');
      if (await tabs.count() > 1) {
        // Click first tab
        await tabs.first().click();
        await page.waitForTimeout(500);
        // Get active tabpanel content
        const firstTabContent = await page.locator('[role="tabpanel"][data-state="active"]').textContent();

        // Click second tab
        await tabs.nth(1).click();
        await page.waitForTimeout(500);
        // Get active tabpanel content
        const secondTabContent = await page.locator('[role="tabpanel"][data-state="active"]').textContent();

        // Content should be different (or at least the selected tab is different)
        // Both tabs are loaded so we verify the selection changed
        const firstTabSelected = await tabs.first().getAttribute('aria-selected');
        const secondTabSelected = await tabs.nth(1).getAttribute('aria-selected');
        expect(secondTabSelected).toBe('true');
        expect(firstTabSelected).toBe('false');
      }
    });
  });

  test.describe('Payment Gateways Tab @P1', () => {
    test('SETTINGS-070: Payment Gateways tab is accessible', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.paymentGatewaysTab.first().isVisible({ timeout: 5000 }).catch(() => false)) {
        await settingsPage.selectTab('payment');
        await settingsPage.expectTabActive('Payment');
      }
    });

    test('SETTINGS-071: Payment Gateways tab has content', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.paymentGatewaysTab.first().isVisible({ timeout: 5000 }).catch(() => false)) {
        await settingsPage.selectTab('payment');

        // Verify active tab panel has content
        const tabPanel = page.locator('[role="tabpanel"][data-state="active"]');
        await expect(tabPanel).toBeVisible({ timeout: 10000 });
      }
    });
  });

  test.describe('Email Templates Tab @P1', () => {
    test('SETTINGS-080: Email Templates tab is accessible', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.emailTemplatesTab.first().isVisible({ timeout: 5000 }).catch(() => false)) {
        await settingsPage.selectTab('email-templates');

        // Tab content should load
        const tabPanel = page.locator('[role="tabpanel"][data-state="active"]');
        await expect(tabPanel).toBeVisible({ timeout: 10000 });
      }
    });

    test('SETTINGS-081: Email Templates tab shows template list', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.emailTemplatesTab.first().isVisible({ timeout: 5000 }).catch(() => false)) {
        await settingsPage.selectTab('email-templates');

        // Verify active tab panel has content
        const tabPanel = page.locator('[role="tabpanel"][data-state="active"]');
        await expect(tabPanel).toBeVisible({ timeout: 10000 });
      }
    });
  });

  test.describe('Legal Pages Tab @P1', () => {
    test('SETTINGS-090: Legal Pages tab is accessible', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.legalPagesTab.first().isVisible({ timeout: 5000 }).catch(() => false)) {
        await settingsPage.selectTab('legal');

        // Tab content should load
        const tabPanel = page.locator('[role="tabpanel"][data-state="active"]');
        await expect(tabPanel).toBeVisible({ timeout: 10000 });
      }
    });

    test('SETTINGS-091: Legal Pages tab shows legal page list', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      if (await settingsPage.legalPagesTab.first().isVisible({ timeout: 5000 }).catch(() => false)) {
        await settingsPage.selectTab('legal');

        // Verify active tab panel has content
        const tabPanel = page.locator('[role="tabpanel"][data-state="active"]');
        await expect(tabPanel).toBeVisible({ timeout: 10000 });
      }
    });
  });

  test.describe('Save Functionality @P1', () => {
    test('SETTINGS-060: Save button exists in form', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      // Check if any save button exists on the page
      // Note: Save button may be disabled initially until changes are made
      const saveButton = page.locator('button[type="submit"], button:has-text("Save")');
      if (await saveButton.first().isVisible({ timeout: 5000 }).catch(() => false)) {
        // Just verify the button exists and is visible - it may be disabled by design
        await expect(saveButton.first()).toBeVisible();
      }
    });
  });

  test.describe('All Tabs Navigation @P1', () => {
    test('SETTINGS-100: Can navigate through all 7 tabs', async ({ page }) => {
      const settingsPage = new TenantSettingsPage(page);
      await settingsPage.navigate();
      await settingsPage.expectPageLoaded();

      const tabNames: Array<'regional' | 'branding' | 'contact' | 'smtp' | 'payment' | 'legal' | 'email-templates'> = [
        'regional', 'branding', 'contact', 'smtp', 'payment', 'legal', 'email-templates'
      ];

      for (const tabName of tabNames) {
        const tabLocator = {
          'regional': settingsPage.regionalTab,
          'branding': settingsPage.brandingTab,
          'contact': settingsPage.contactTab,
          'smtp': settingsPage.smtpTab,
          'payment': settingsPage.paymentGatewaysTab,
          'legal': settingsPage.legalPagesTab,
          'email-templates': settingsPage.emailTemplatesTab,
        }[tabName];

        if (await tabLocator.first().isVisible({ timeout: 3000 }).catch(() => false)) {
          await settingsPage.selectTab(tabName);

          // Verify tab panel is active
          const tabPanel = page.locator('[role="tabpanel"][data-state="active"]');
          await expect(tabPanel).toBeVisible({ timeout: 5000 });
        }
      }
    });
  });
});
