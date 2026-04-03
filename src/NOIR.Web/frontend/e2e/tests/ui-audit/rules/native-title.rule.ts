import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

export const nativeTitleRule: AuditRule = {
  id: 'native-title',
  name: 'No native title= tooltips on interactive elements',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    const violations = await page.$$eval(
      'button[title], a[title], [role="tab"][title], select[title], [role="combobox"][title], [role="switch"][title]',
      (elements) => {
        return elements
          .filter(el => !el.closest('.rich-text-editor, .tiptap-content'))
          .slice(0, 10)
          .map(el => ({
            title: el.getAttribute('title') ?? '',
            html: el.outerHTML.substring(0, 200),
          }));
      },
    );

    if (violations.length === 0) return [];

    return [{
      ruleId: 'native-title',
      severity: 'MEDIUM',
      message: `${violations.length} interactive element(s) using native title= instead of Radix Tooltip`,
      detail: violations.map(v => `title="${v.title}": ${v.html}`).join('\n---\n'),
      fix: 'Replace title= with Radix <Tooltip> from @uikit + add aria-label for accessibility',
      reference: '.claude/rules/dialog-header-spacing.md — "No native title= tooltips"',
      nodes: violations.map(v => v.html),
    }];
  },
};
