import type { Page } from '@playwright/test';

export interface PageAuditConfig {
  id: string;
  domain: string;
  url: string;
  authProfile?: 'admin' | 'platform';
  requiresData: boolean;
  seedFn?: (api: any) => Promise<SeedResult>;
  waitFor?: string;
  tabs?: Array<{ id: string; param: string }>;
  skipAxe?: boolean;
  skipRules?: string[];
  dialogTriggers?: DialogTrigger[];
  sourceFile?: string;
}

export interface DialogTrigger {
  id: string;
  label: string | RegExp;
  triggerSelector?: string;
  waitForSelector?: string;
}

export interface SeedResult {
  cleanup: () => Promise<void>;
  routeParam?: string;
}

export interface AuditRule {
  id: string;
  name: string;
  check(page: Page, config: PageAuditConfig): Promise<AuditIssue[]>;
}

export interface AuditIssue {
  ruleId: string;
  severity: 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW' | 'INFO';
  message: string;
  detail?: string;
  fix: string;
  reference: string;
  sourceFile?: string;
  nodes?: string[];
}

export interface CollectedIssue extends AuditIssue {
  pageId: string;
  screenshotPath?: string;
}

export const SEVERITY_ORDER: Record<string, number> = {
  CRITICAL: 0,
  HIGH: 1,
  MEDIUM: 2,
  LOW: 3,
  INFO: 4,
};
