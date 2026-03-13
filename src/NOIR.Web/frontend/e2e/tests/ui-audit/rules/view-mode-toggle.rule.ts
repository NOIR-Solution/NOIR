import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

/**
 * Rule: view-mode-toggle
 *
 * Why this exists:
 *   The Projects page was found using raw <button> elements with aria-labels like
 *   "Grid view" / "List view" instead of the canonical <ViewModeToggle> from @uikit.
 *   ViewModeToggle always renders Button[aria-pressed], so we detect the legacy pattern
 *   by finding view-toggle buttons that lack aria-pressed.
 *
 *   This rule catches: raw <button aria-label="Grid view"> without aria-pressed.
 *   Standard pattern:  <button aria-pressed="true/false"> (ViewModeToggle from @uikit).
 */
export const viewModeToggleRule: AuditRule = {
  id: 'view-mode-toggle',
  name: 'View mode toggles must use <ViewModeToggle> from @uikit',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    const violations = await page.$$eval('button[aria-label]', (buttons) => {
      const VIEW_TOGGLE_RE = /^(grid view|list view|table view|card view)$/i;
      const wrong: string[] = [];

      for (const btn of buttons) {
        const label = (btn.getAttribute('aria-label') ?? '').trim();
        if (!VIEW_TOGGLE_RE.test(label)) continue;
        // Standard ViewModeToggle always has aria-pressed
        if (!btn.hasAttribute('aria-pressed')) {
          wrong.push(`"${label}": ${btn.outerHTML.substring(0, 200)}`);
        }
      }
      return wrong.slice(0, 10);
    });

    if (violations.length === 0) return [];

    return [{
      ruleId: 'view-mode-toggle',
      severity: 'HIGH',
      message: `${violations.length} view toggle button(s) using legacy raw <button> instead of <ViewModeToggle>`,
      detail: violations.join('\n---\n'),
      fix: 'Replace custom toggle buttons with <ViewModeToggle options={...} value={viewMode} onChange={setViewMode} /> from @uikit. Buttons must have aria-pressed.',
      reference: 'CLAUDE.md — ViewModeToggle from @uikit (Products page = reference)',
      nodes: violations,
    }];
  },
};
