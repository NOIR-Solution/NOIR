import AxeBuilder from '@axe-core/playwright';
import { Page } from '@playwright/test';

/**
 * Accessibility Testing Utilities
 *
 * Provides standardized WCAG 2.1 Level AA compliance testing using axe-core.
 * Used across all accessibility E2E tests for consistent scanning and reporting.
 */

/**
 * Accessibility configuration constants
 */
export const AccessibilityConfig = {
  /** Maximum tabs to reach primary action (WCAG 2.4.3 - Focus Order) */
  MAX_TAB_COUNT: 25,
  /** Delay between keyboard actions for screen reader compatibility */
  KEYBOARD_DELAY: 100,
} as const;

/**
 * Standard WCAG 2.1 AA accessibility scan configuration
 * Tests against: WCAG 2.0 Level A, WCAG 2.0 Level AA, WCAG 2.1 Level A, WCAG 2.1 Level AA
 *
 * @param page - Playwright page object
 * @returns AxeBuilder scan results with violations, passes, incomplete, and inapplicable rules
 *
 * @example
 * const results = await scanPageAccessibility(page);
 * expect(results.violations).toEqual([]);
 */
export async function scanPageAccessibility(page: Page) {
  return await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
}

/**
 * Scan specific region of the page (e.g., dialog, form, navigation)
 * Useful for testing isolated components without full page scan
 *
 * @param page - Playwright page object
 * @param selector - CSS selector for the region to scan
 * @returns AxeBuilder scan results for the specified region
 *
 * @example
 * await rolesPage.openCreateDialog();
 * const results = await scanRegionAccessibility(page, '[role="dialog"]');
 * expect(results.violations).toEqual([]);
 */
export async function scanRegionAccessibility(page: Page, selector: string) {
  return await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .include(selector)
    .analyze();
}

/**
 * Filter violations by severity level
 * Allows testing for specific severity thresholds (e.g., no critical/serious violations)
 *
 * @param violations - Array of axe violation objects
 * @param severities - Array of severity levels to include
 * @returns Filtered array of violations matching specified severities
 *
 * @example
 * const results = await scanPageAccessibility(page);
 * const critical = filterViolationsBySeverity(results.violations, ['critical', 'serious']);
 * expect(critical).toEqual([]);
 */
export function filterViolationsBySeverity(
  violations: any[],
  severities: Array<'critical' | 'serious' | 'moderate' | 'minor'>
): any[] {
  return violations.filter(v => severities.includes(v.impact));
}

/**
 * Filter violations by axe rule ID
 * Useful for testing specific WCAG success criteria
 *
 * @param violations - Array of axe violation objects
 * @param ruleIds - Array of axe rule IDs to include (e.g., 'color-contrast', 'label')
 * @returns Filtered array of violations matching specified rule IDs
 *
 * @example
 * const results = await scanPageAccessibility(page);
 * const labelViolations = filterViolationsByRule(results.violations, ['label', 'label-title-only']);
 * expect(labelViolations).toEqual([]);
 */
export function filterViolationsByRule(violations: any[], ruleIds: string[]): any[] {
  return violations.filter(v => ruleIds.includes(v.id));
}

/**
 * Log violations in a readable format for debugging
 * Automatically called by Playwright on test failure, but can be used for debugging
 *
 * @param violations - Array of axe violation objects
 *
 * @example
 * const results = await scanPageAccessibility(page);
 * if (results.violations.length > 0) {
 *   logViolations(results.violations);
 * }
 */
export function logViolations(violations: any[]): void {
  if (violations.length === 0) {
    console.log('✅ No accessibility violations found');
    return;
  }

  console.log(`\n⚠️  Found ${violations.length} accessibility violation(s):\n`);

  violations.forEach((violation, index) => {
    console.log(`${index + 1}. ${violation.id}: ${violation.description}`);
    console.log(`   Impact: ${violation.impact}`);
    console.log(`   Affected nodes: ${violation.nodes.length}`);
    console.log(`   Help: ${violation.helpUrl}\n`);
  });
}

/**
 * Get summary statistics from scan results
 * Useful for reporting and dashboards
 *
 * @param results - AxeBuilder scan results
 * @returns Object with violation counts by severity
 *
 * @example
 * const results = await scanPageAccessibility(page);
 * const summary = getScanSummary(results);
 * console.log(`Critical: ${summary.critical}, Serious: ${summary.serious}`);
 */
export function getScanSummary(results: any) {
  const violations = results.violations || [];

  return {
    critical: violations.filter((v: any) => v.impact === 'critical').length,
    serious: violations.filter((v: any) => v.impact === 'serious').length,
    moderate: violations.filter((v: any) => v.impact === 'moderate').length,
    minor: violations.filter((v: any) => v.impact === 'minor').length,
    total: violations.length,
    passed: results.passes?.length || 0,
    incomplete: results.incomplete?.length || 0,
  };
}
