import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

export const dialogFooterRule: AuditRule = {
  id: 'dialog-footer',
  name: 'Dialogs must have footer with Close/Cancel button',

  async check(page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    const issues: AuditIssue[] = [];

    // Only check open dialogs
    const dialogs = page.locator('[role="dialog"]:visible');
    const count = await dialogs.count();

    for (let i = 0; i < count; i++) {
      const dialog = dialogs.nth(i);

      // Look for footer patterns: div with mt-4 (desktop) or pb-6 (mobile) containing buttons
      const hasFooterButtons = await dialog.evaluate((el) => {
        // Check for buttons with close/cancel text
        const buttons = el.querySelectorAll('button');
        const hasCloseOrCancel = Array.from(buttons).some(btn => {
          const text = btn.textContent?.toLowerCase().trim() ?? '';
          return text.includes('close') || text.includes('cancel') || text.includes('đóng') || text.includes('hủy');
        });
        // Also check for footer-like containers
        const hasFooterDiv = el.querySelector('.mt-4:has(button), .pb-6:has(button), [class*="footer"]:has(button)');
        return hasCloseOrCancel || !!hasFooterDiv;
      });

      if (!hasFooterButtons) {
        const dialogHtml = await dialog.evaluate(el => el.outerHTML.substring(0, 300));
        issues.push({
          ruleId: 'dialog-footer',
          severity: 'HIGH',
          message: 'Dialog missing footer with Close/Cancel button',
          detail: dialogHtml,
          fix: 'Add <CredenzaFooter> with Close/Cancel button as last child of dialog',
          reference: '.claude/rules/dialog-header-spacing.md — "Every Dialog MUST Have a Footer"',
        });
      }
    }

    return issues;
  },
};
