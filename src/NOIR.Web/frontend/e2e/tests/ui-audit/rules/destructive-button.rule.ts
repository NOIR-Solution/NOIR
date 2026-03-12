import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

export const destructiveButtonRule: AuditRule = {
  id: 'destructive-button',
  name: 'Destructive buttons must follow CLAUDE.md pattern',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    // Find buttons that appear to be destructive but don't follow the full pattern
    // The correct pattern: variant="destructive" + bg-destructive/10 text-destructive border border-destructive/30
    const violations = await page.$$eval('button', (buttons) => {
      const wrong: string[] = [];
      for (const btn of buttons) {
        const classes = btn.className;
        if (typeof classes !== 'string') continue;
        // Find buttons with destructive styling
        const hasDestructiveText = classes.includes('text-destructive');
        const hasDestructiveBg = classes.includes('bg-destructive');
        if (!hasDestructiveText && !hasDestructiveBg) continue;
        // Skip buttons that are just text-destructive links (no bg needed)
        if (hasDestructiveText && !hasDestructiveBg) continue;
        // Check if it follows the full pattern
        const hasCorrectPattern =
          classes.includes('bg-destructive/10') &&
          classes.includes('text-destructive') &&
          classes.includes('border-destructive/30');
        // Also allow the default CVA destructive variant (bg-destructive text-white)
        const isDefaultCVA = classes.includes('bg-destructive') && classes.includes('text-white');
        // The design standard says use the custom pattern, not default CVA
        if (isDefaultCVA && !classes.includes('bg-destructive/10')) {
          wrong.push(btn.outerHTML.substring(0, 250));
        }
      }
      return wrong.slice(0, 5);
    });

    if (violations.length === 0) return [];

    return [{
      ruleId: 'destructive-button',
      severity: 'MEDIUM',
      message: `${violations.length} destructive button(s) using default CVA variant instead of CLAUDE.md pattern`,
      detail: violations.join('\n---\n'),
      fix: 'Use: className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"',
      reference: 'CLAUDE.md — Destructive buttons pattern',
      nodes: violations,
    }];
  },
};
