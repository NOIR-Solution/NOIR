import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

export const cursorPointerRule: AuditRule = {
  id: 'cursor-pointer',
  name: 'Interactive elements must have cursor-pointer',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    const missing = await page.$$eval(
      [
        '[role="tab"]',
        '[role="checkbox"]',
        '[role="switch"]',
        '[role="radio"]',
        '[role="combobox"]',
        '[data-radix-collection-item]',
      ].join(', '),
      (elements) => {
        const results: Array<{ html: string; tag: string }> = [];
        for (const el of elements) {
          // Skip hidden, disabled, or inside closed containers
          if (el.closest('[disabled], [aria-disabled="true"], [data-state="closed"]')) continue;
          const rect = el.getBoundingClientRect();
          if (rect.width === 0 || rect.height === 0) continue;

          const style = window.getComputedStyle(el);
          if (style.cursor !== 'pointer') {
            results.push({
              html: el.outerHTML.substring(0, 200),
              tag: `${el.tagName.toLowerCase()}[role="${el.getAttribute('role')}"]`,
            });
          }
        }
        return results.slice(0, 10);
      },
    );

    if (missing.length === 0) return [];

    return [{
      ruleId: 'cursor-pointer',
      severity: 'LOW',
      message: `${missing.length} interactive element(s) missing cursor-pointer`,
      detail: missing.map(m => `${m.tag}: ${m.html}`).join('\n---\n'),
      fix: 'Add className="cursor-pointer" to each interactive element',
      reference: 'CLAUDE.md — "cursor-pointer: ALL interactive elements MUST have cursor-pointer"',
      nodes: missing.map(m => m.html),
    }];
  },
};
