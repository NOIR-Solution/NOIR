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
        if (!classes.includes('text-center')) continue;
        if (classes.includes('border-dashed')) continue; // Already using EmptyState
        if (div.closest('[role="dialog"]')) continue;
        if (div.closest('table')) continue;
        if (div.querySelector('input[type="file"]')) continue; // Upload zones are not empty states
        if (div.closest('[class*="border-dashed"]')) continue; // Inside a dropzone or EmptyState wrapper

        // Pattern A: text-muted-foreground on the div itself
        const selfMuted = classes.includes('text-muted-foreground');
        // Pattern B: text-muted-foreground on a direct <p> child (missed by original rule)
        const childMuted = Array.from(div.children).some((child) => {
          const cc = (child as HTMLElement).className ?? '';
          return child.tagName === 'P' && typeof cc === 'string' && cc.includes('text-muted-foreground');
        });

        if (!selfMuted && !childMuted) continue;

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
