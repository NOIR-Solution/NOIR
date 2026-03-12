import type { Page } from '@playwright/test';
import type { AuditRule, AuditIssue, PageAuditConfig } from './types';

// Network errors are collected by environment-setup.ts and injected into the config
// This rule is a placeholder — actual network error issues are added directly by audit-runner.spec.ts
export const networkErrorsRule: AuditRule = {
  id: 'network-errors',
  name: 'No failed API requests during page load',

  async check(_page: Page, _config: PageAuditConfig): Promise<AuditIssue[]> {
    // Network errors are handled directly by audit-runner.spec.ts
    return [];
  },
};
