import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

export const badgeVariantRule: AuditRule = {
  id: 'badge-variant',
  name: 'Status badges must use variant="outline" + getStatusBadgeClasses',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    const violations = await page.$$eval('[data-slot="badge"]', (badges) => {
      const STATUS_RE = /^(active|inactive|draft|pending|confirmed|processing|shipped|delivered|completed|cancelled|refunded|approved|rejected|published|archived|expired|enabled|disabled|open|closed|won|lost|new|qualified|paused)$/i;

      const wrong: string[] = [];
      for (const badge of badges) {
        const text = badge.textContent?.trim() ?? '';
        if (!STATUS_RE.test(text)) continue;
        // Status badge detected — should use outline variant + status colors
        const classes = badge.className;
        const usesDefaultVariant = classes.includes('bg-primary') && classes.includes('text-primary-foreground');
        const usesSecondaryVariant = classes.includes('bg-secondary') && classes.includes('text-secondary-foreground');
        if (usesDefaultVariant || usesSecondaryVariant) {
          wrong.push(`"${text}": ${badge.outerHTML.substring(0, 200)}`);
        }
      }
      return wrong.slice(0, 10);
    });

    if (violations.length === 0) return [];

    return [{
      ruleId: 'badge-variant',
      severity: 'MEDIUM',
      message: `${violations.length} status badge(s) using wrong variant (default/secondary instead of outline)`,
      detail: violations.join('\n---\n'),
      fix: 'Use variant="outline" + getStatusBadgeClasses() from @/utils/statusBadge',
      reference: 'CLAUDE.md — "Status badges: variant=outline + getStatusBadgeClasses"',
      nodes: violations,
    }];
  },
};
