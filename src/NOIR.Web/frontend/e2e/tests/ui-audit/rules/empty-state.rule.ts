import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

export const emptyStateRule: AuditRule = {
  id: 'empty-state',
  name: 'Empty states must use <EmptyState> component',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    // Find div-based empty states that should use the EmptyState component
    // EmptyState component always has: border-2 border-dashed border-border + group class
    // Raw empty divs typically: text-center py-8 text-muted-foreground without dashed border
    const violations = await page.$$eval('div', (divs) => {
      const wrong: string[] = [];
      for (const div of divs) {
        const classes = div.className;
        if (typeof classes !== 'string') continue;
        // Match common raw empty state patterns
        const isRawEmpty =
          classes.includes('text-center') &&
          classes.includes('text-muted-foreground') &&
          !classes.includes('border-dashed') && // Not using EmptyState component
          !div.closest('[role="dialog"]') && // Skip dialogs (may have different patterns)
          !div.closest('table'); // Skip table cells
        if (!isRawEmpty) continue;
        // Verify this looks like an empty state message (short text, no complex children)
        const text = div.textContent?.trim() ?? '';
        if (text.length > 10 && text.length < 200 && div.children.length <= 3) {
          wrong.push(`"${text.substring(0, 80)}": ${div.outerHTML.substring(0, 200)}`);
        }
      }
      return wrong.slice(0, 5);
    });

    if (violations.length === 0) return [];

    return [{
      ruleId: 'empty-state',
      severity: 'MEDIUM',
      message: `${violations.length} raw empty state div(s) — should use <EmptyState> component`,
      detail: violations.join('\n---\n'),
      fix: 'Replace raw div with <EmptyState icon={X} title={t("...")} description={t("...")} /> from @uikit',
      reference: 'CLAUDE.md — "Empty states: Use <EmptyState> from @uikit, never plain div"',
      nodes: violations,
    }];
  },
};
