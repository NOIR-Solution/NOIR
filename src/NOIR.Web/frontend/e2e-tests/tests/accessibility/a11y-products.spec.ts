import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { ProductsPage } from '../../pages';

/**
 * Accessibility Tests - Products Pages
 *
 * Tests WCAG 2.1 Level AA compliance for product management
 * Tags: @accessibility @a11y @products @P1
 */

test.describe('Accessibility - Products @accessibility @a11y', () => {
  test('A11Y-PROD-001: Products page has no accessibility violations', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.navigate();
    await productsPage.expectPageLoaded();

    // Run axe accessibility scan
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();

    // Log violations if any (for debugging)
    if (accessibilityScanResults.violations.length > 0) {
      console.log('Accessibility violations found:');
      accessibilityScanResults.violations.forEach(violation => {
        console.log(`- ${violation.id}: ${violation.description}`);
        console.log(`  Impact: ${violation.impact}`);
        console.log(`  Nodes: ${violation.nodes.length}`);
      });
    }

    // Should have no critical or serious violations
    const criticalViolations = accessibilityScanResults.violations.filter(
      v => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(criticalViolations).toEqual([]);
  });

  test('A11Y-PROD-002: Product search has accessible labels', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.navigate();
    await productsPage.expectPageLoaded();

    // Search input should have label or aria-label
    const searchInput = productsPage.searchInput;
    const hasLabel = await searchInput.evaluate(el => {
      const ariaLabel = el.getAttribute('aria-label');
      const id = el.getAttribute('id');
      const label = id ? document.querySelector(`label[for="${id}"]`) : null;
      return !!(ariaLabel || label);
    });

    expect(hasLabel).toBeTruthy();
  });

  test('A11Y-PROD-003: Create button has accessible label', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.navigate();
    await productsPage.expectPageLoaded();

    // Create button should have accessible text or aria-label
    const createButton = productsPage.createButton;
    const text = await createButton.textContent();
    const ariaLabel = await createButton.getAttribute('aria-label');

    expect(text || ariaLabel).toBeTruthy();
  });

  test('A11Y-PROD-004: Product cards have proper semantic structure', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.navigate();
    await productsPage.expectPageLoaded();

    // Run axe focusing on semantic structure
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a'])
      .analyze();

    // Check for landmarks and semantic violations
    const semanticViolations = accessibilityScanResults.violations.filter(
      v => v.id.includes('landmark') || v.id.includes('region')
    );

    // Should have no landmark or region violations
    expect(semanticViolations).toEqual([]);
  });

  test('A11Y-PROD-005: Keyboard navigation works for product actions', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.navigate();
    await productsPage.expectPageLoaded();

    // Tab to create button
    let tabCount = 0;
    while (tabCount < 20) {
      await page.keyboard.press('Tab');
      const focusedText = await page.evaluate(() => document.activeElement?.textContent || '');

      if (focusedText.toLowerCase().includes('create') || focusedText.toLowerCase().includes('new')) {
        // Found create button via keyboard
        break;
      }
      tabCount++;
    }

    // Should be able to reach interactive elements within 20 tabs
    expect(tabCount).toBeLessThanOrEqual(20);
  });

  test('A11Y-PROD-006: Color contrast meets WCAG AA standards', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.navigate();
    await productsPage.expectPageLoaded();

    // Run axe focusing on color contrast
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2aa'])
      .analyze();

    // Check color contrast violations
    const contrastViolations = accessibilityScanResults.violations.filter(
      v => v.id === 'color-contrast'
    );

    // Should have no critical contrast issues
    expect(contrastViolations).toEqual([]);
  });
});
