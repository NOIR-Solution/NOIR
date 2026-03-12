import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import type { AuditIssue } from './rules/types';

const IMPACT_TO_SEVERITY: Record<string, AuditIssue['severity']> = {
  critical: 'CRITICAL',
  serious: 'CRITICAL',
  moderate: 'HIGH',
  minor: 'MEDIUM',
};

// Known false positives for Radix UI / NOIR app
const SKIP_VIOLATIONS = new Set([
  'aria-hidden-body',
]);

export async function runAxeScan(page: Page, pageId: string): Promise<AuditIssue[]> {
  try {
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .exclude('[data-radix-popper-content-wrapper]')
      .analyze();

    return results.violations
      .filter(v => !SKIP_VIOLATIONS.has(v.id))
      .map(v => ({
        ruleId: `axe:${v.id}`,
        severity: IMPACT_TO_SEVERITY[v.impact ?? 'minor'] ?? 'MEDIUM',
        message: `[a11y] ${v.description}`,
        detail: v.nodes
          .slice(0, 5)
          .map(n => `${n.target.join(' > ')}\n  ${n.html.substring(0, 200)}`)
          .join('\n---\n'),
        fix: v.nodes[0]?.failureSummary ?? 'See help URL for guidance',
        reference: `${v.helpUrl}`,
        nodes: v.nodes.slice(0, 5).map(n => n.html.substring(0, 200)),
      }));
  } catch (error) {
    // axe-core can fail on some pages (e.g., empty iframe)
    return [{
      ruleId: 'axe:scan-error',
      severity: 'INFO',
      message: `axe-core scan failed on ${pageId}`,
      detail: String(error),
      fix: 'Investigate why axe-core cannot scan this page',
      reference: 'https://github.com/dequelabs/axe-core',
    }];
  }
}
