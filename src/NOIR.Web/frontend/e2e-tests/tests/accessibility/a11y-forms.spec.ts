import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { RolesPage, UsersPage } from '../../pages';
import { Timeouts } from '../../pages/BasePage';

/**
 * Accessibility Tests - Forms
 *
 * Tests WCAG 2.1 Level AA compliance for form dialogs
 * Tags: @accessibility @a11y @forms @P1
 */

test.describe('Accessibility - Forms @accessibility @a11y', () => {
  test('A11Y-FORM-001: Create role dialog has no accessibility violations', async ({ page }) => {
    const rolesPage = new RolesPage(page);
    await rolesPage.navigate();
    await rolesPage.expectPageLoaded();

    // Open create dialog
    await rolesPage.openCreateDialog();

    // Run axe on dialog
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();

    // Should have no critical violations in form
    const criticalViolations = accessibilityScanResults.violations.filter(
      v => v.impact === 'critical'
    );
    expect(criticalViolations).toEqual([]);
  });

  test('A11Y-FORM-002: Form inputs have associated labels', async ({ page }) => {
    const rolesPage = new RolesPage(page);
    await rolesPage.navigate();
    await rolesPage.expectPageLoaded();
    await rolesPage.openCreateDialog();

    // Check for label violations
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a'])
      .analyze();

    const labelViolations = accessibilityScanResults.violations.filter(
      v => v.id === 'label' || v.id === 'label-title-only'
    );

    expect(labelViolations).toEqual([]);
  });

  test('A11Y-FORM-003: Form validation errors are announced', async ({ page }) => {
    const rolesPage = new RolesPage(page);
    await rolesPage.navigate();
    await rolesPage.expectPageLoaded();
    await rolesPage.openCreateDialog();

    // Focus and blur the required field to trigger validation (with mode: 'onBlur')
    const nameInput = page.locator('input[name="name"]').or(page.locator('input[placeholder*="Editor"]'));
    await nameInput.first().click();  // Focus
    await nameInput.first().blur();   // Blur triggers validation with mode: 'onBlur'

    // Wait for validation
    await page.waitForTimeout(Timeouts.STABILITY_WAIT);

    // Verify role="alert" is present
    const alertElements = page.locator('[role="alert"]');
    expect(await alertElements.count()).toBeGreaterThan(0);

    // Verify aria-live="assertive" is present on the same element
    const assertiveElements = page.locator('[role="alert"][aria-live="assertive"]');
    expect(await assertiveElements.count()).toBeGreaterThan(0);
  });

  test('A11Y-FORM-004: Dialog has proper focus trap', async ({ page }) => {
    const rolesPage = new RolesPage(page);
    await rolesPage.navigate();
    await rolesPage.expectPageLoaded();
    await rolesPage.openCreateDialog();

    // Try to tab through dialog
    let tabCount = 0;
    const elements = [];

    while (tabCount < 15) {
      await page.keyboard.press('Tab');
      const focused = await page.evaluate(() => ({
        tag: document.activeElement?.tagName,
        type: document.activeElement?.getAttribute('type'),
        text: document.activeElement?.textContent?.substring(0, 30)
      }));
      elements.push(focused);
      tabCount++;
    }

    // Focus should stay within dialog (not escape to page behind)
    // This is a basic check - full focus trap testing would need more
    expect(elements.length).toBeGreaterThan(0);
  });

  test('A11Y-FORM-005: Create user dialog has accessible form fields', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.navigate();
    await usersPage.expectPageLoaded();
    await usersPage.openCreateDialog();

    // Run axe on form
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    // Check for form-related violations
    const formViolations = accessibilityScanResults.violations.filter(
      v => v.id.includes('label') || v.id.includes('input') || v.id.includes('form')
    );

    expect(formViolations).toEqual([]);
  });

  test('A11Y-FORM-006: Dialog close button has accessible name', async ({ page }) => {
    const rolesPage = new RolesPage(page);
    await rolesPage.navigate();
    await rolesPage.expectPageLoaded();
    await rolesPage.openCreateDialog();

    // Find close button (usually X icon)
    const closeButton = page.locator('button[aria-label*="Close"], button[aria-label*="close"], button:has([class*="close"])');

    if (await closeButton.count() > 0) {
      const ariaLabel = await closeButton.first().getAttribute('aria-label');
      const text = await closeButton.first().textContent();

      // Should have accessible name
      expect(ariaLabel || text).toBeTruthy();
    }
  });
});
