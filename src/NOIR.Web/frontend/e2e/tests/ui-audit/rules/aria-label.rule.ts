import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

export const ariaLabelRule: AuditRule = {
  id: 'aria-label',
  name: 'Icon-only buttons must have aria-label',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    const violations = await page.$$eval('button', (buttons) => {
      const missing: string[] = [];
      for (const btn of buttons) {
        const hasSvg = btn.querySelector('svg');
        if (!hasSvg) continue;
        // Check if button has text content (besides SVG)
        const textContent = Array.from(btn.childNodes)
          .filter(n => n.nodeType === Node.TEXT_NODE || (n.nodeType === Node.ELEMENT_NODE && (n as Element).tagName !== 'svg' && (n as Element).tagName !== 'SVG'))
          .map(n => n.textContent?.trim())
          .filter(Boolean)
          .join('');
        if (textContent) continue;
        // Icon-only button — check for aria-label
        const ariaLabel = btn.getAttribute('aria-label');
        if (!ariaLabel) {
          missing.push(btn.outerHTML.substring(0, 200));
        }
      }
      return missing.slice(0, 10);
    });

    if (violations.length === 0) return [];

    return [{
      ruleId: 'aria-label',
      severity: 'CRITICAL',
      message: `${violations.length} icon-only button(s) missing aria-label`,
      detail: violations.join('\n---\n'),
      fix: 'Add aria-label={`Descriptive text`} to each icon-only button',
      reference: 'CLAUDE.md — "ALL icon-only buttons must have contextual aria-label"',
      nodes: violations,
    }];
  },
};
