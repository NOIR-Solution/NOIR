import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

// Console errors are collected by environment-setup.ts and injected into the config
// This rule is a placeholder that returns empty — actual console error issues are
// added directly by audit-runner.spec.ts from the attachListeners() output
export const consoleErrorsRule: AuditRule = {
  id: 'console-errors',
  name: 'No JavaScript console errors during page load',

  async check(_page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    // Console errors are handled directly by audit-runner.spec.ts
    // This rule exists for the barrel export and documentation
    return [];
  },
};
