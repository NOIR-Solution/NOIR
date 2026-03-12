import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

export const gradientTextRule: AuditRule = {
  id: 'gradient-text',
  name: 'Gradient text must include text-transparent with bg-clip-text',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    const violations = await page.$$eval('[class*="bg-clip-text"]', (elements) => {
      return elements
        .filter(el => !el.className.includes('text-transparent'))
        .slice(0, 5)
        .map(el => el.outerHTML.substring(0, 200));
    });

    if (violations.length === 0) return [];

    return [{
      ruleId: 'gradient-text',
      severity: 'LOW',
      message: `${violations.length} gradient text element(s) missing text-transparent`,
      detail: violations.join('\n---\n'),
      fix: 'Add text-transparent class alongside bg-clip-text',
      reference: 'CLAUDE.md — "Gradient text: MUST include text-transparent with bg-clip-text"',
      nodes: violations,
    }];
  },
};
