import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

export const datatableActionsRule: AuditRule = {
  id: 'datatable-actions',
  name: 'DataTable actions column must be first with EllipsisVertical',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    const issues: AuditIssue[] = [];

    // Check all tables on the page
    const tableCount = await page.locator('table').count();
    if (tableCount === 0) return [];

    for (let t = 0; t < tableCount; t++) {
      const table = page.locator('table').nth(t);

      // Check 1: First column should not be a select checkbox (actions must be first)
      const firstThHasCheckbox = await table
        .locator('thead tr th:first-child [role="checkbox"]')
        .count();
      if (firstThHasCheckbox > 0) {
        issues.push({
          ruleId: 'datatable-actions-order',
          severity: 'HIGH',
          message: 'DataTable select column is first — actions column must be first (leftmost)',
          fix: 'In columns array, move createActionsColumn() before createSelectColumn()',
          reference: '.claude/rules/datatable-standard.md — "Actions column FIRST (leftmost)"',
        });
      }

      // Check 2: Actions buttons should use EllipsisVertical (vertical dots), not MoreHorizontal
      // EllipsisVertical: 3 SVG circles stacked vertically (same cx, different cy)
      // MoreHorizontal: 3 SVG circles in a row (same cy, different cx)
      const wrongIcons = await table.evaluate((tableEl) => {
        const actionBtns = tableEl.querySelectorAll('button[aria-label="Open row actions"], td:first-child button:has(svg)');
        const wrong: string[] = [];
        for (const btn of actionBtns) {
          const circles = btn.querySelectorAll('svg circle');
          if (circles.length === 3) {
            const cxValues = new Set(Array.from(circles).map(c => c.getAttribute('cx')));
            // MoreHorizontal has 3 different cx values (horizontal layout)
            if (cxValues.size > 1) {
              wrong.push(btn.outerHTML.substring(0, 200));
            }
          }
        }
        return wrong.slice(0, 3);
      });

      if (wrongIcons.length > 0) {
        issues.push({
          ruleId: 'datatable-actions-icon',
          severity: 'HIGH',
          message: `${wrongIcons.length} action button(s) use MoreHorizontal icon — must use EllipsisVertical`,
          detail: wrongIcons.join('\n---\n'),
          fix: 'Replace MoreHorizontal import with EllipsisVertical from lucide-react',
          reference: '.claude/rules/datatable-standard.md — "Icon: EllipsisVertical (vertical ⋮), NOT MoreHorizontal"',
          nodes: wrongIcons,
        });
      }
    }

    return issues;
  },
};
