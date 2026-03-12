import { test } from '@playwright/test';
import { PLATFORM_PAGE_REGISTRY } from './page-registry';
import { AUDIT_RULES } from './rules';
import { IssueCollector } from './issue-collector';
import { ScreenshotManager } from './screenshot-manager';
import { runAxeScan } from './axe-scanner';
import { lockEnvironment, attachListeners } from './environment-setup';
import { saveRunnerIssues, generateMergedReport, cleanStaleIssues } from './report-generator';

const collector = new IssueCollector();
const screenshots = new ScreenshotManager();

// Clean stale issues from previous runs before starting
test.beforeAll(async () => {
  await cleanStaleIssues('platform');
});

for (const pageConfig of PLATFORM_PAGE_REGISTRY) {
  test.describe(`audit-platform: ${pageConfig.domain}/${pageConfig.id}`, () => {
    test(`page: ${pageConfig.id}`, async ({ page, context }) => {
      await lockEnvironment(context);
      const listeners = attachListeners(page);

      try {
        // Navigate
        await page.goto(pageConfig.url);
        await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {});
        if (pageConfig.waitFor) {
          await page.waitForSelector(pageConfig.waitFor, { timeout: 10_000 }).catch(() => {});
        }

        // Screenshot
        const screenshotPath = await screenshots.takePage(page, pageConfig.id);

        // Axe scan
        if (!pageConfig.skipAxe) {
          const axeIssues = await runAxeScan(page, pageConfig.id);
          for (const issue of axeIssues) {
            collector.add({
              pageId: pageConfig.id,
              ...issue,
              screenshotPath,
              sourceFile: issue.sourceFile ?? pageConfig.sourceFile,
            });
          }
        }

        // Custom rules
        const skipRules = new Set(pageConfig.skipRules ?? []);
        for (const rule of AUDIT_RULES) {
          if (skipRules.has(rule.id)) continue;
          try {
            const issues = await rule.check(page, pageConfig);
            for (const issue of issues) {
              collector.add({
                pageId: pageConfig.id,
                ...issue,
                screenshotPath,
                sourceFile: issue.sourceFile ?? pageConfig.sourceFile,
              });
            }
          } catch {
            // Non-fatal
          }
        }

        // Console/network errors
        if (listeners.consoleErrors.length > 0) {
          collector.add({
            pageId: pageConfig.id,
            ruleId: 'console-errors',
            severity: 'MEDIUM',
            message: `${listeners.consoleErrors.length} console error(s)`,
            detail: listeners.consoleErrors.map(e => e.text).join('\n---\n'),
            fix: 'Investigate console errors',
            reference: 'React error boundaries',
            screenshotPath,
            sourceFile: pageConfig.sourceFile,
          });
        }
        if (listeners.networkErrors.length > 0) {
          collector.add({
            pageId: pageConfig.id,
            ruleId: 'network-errors',
            severity: listeners.networkErrors.some(e => e.status >= 500) ? 'HIGH' : 'MEDIUM',
            message: `${listeners.networkErrors.length} failed API request(s)`,
            detail: listeners.networkErrors.map(e => `${e.status} ${e.url}`).join('\n'),
            fix: 'Fix backend endpoint or add error handling',
            reference: 'API health',
            screenshotPath,
            sourceFile: pageConfig.sourceFile,
          });
        }

        // Tab scanning
        if (pageConfig.tabs) {
          for (const tab of pageConfig.tabs) {
            const tabListeners = attachListeners(page);
            try {
              await page.goto(`${pageConfig.url}?tab=${tab.param}`);
              await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => {});

              const tabScreenshot = await screenshots.takeTab(page, pageConfig.id, tab.id);
              const tabPageId = `${pageConfig.id}/tab:${tab.id}`;

              if (!pageConfig.skipAxe) {
                const axeIssues = await runAxeScan(page, tabPageId);
                for (const issue of axeIssues) {
                  collector.add({
                    pageId: tabPageId,
                    ...issue,
                    screenshotPath: tabScreenshot,
                    sourceFile: pageConfig.sourceFile,
                  });
                }
              }

              for (const rule of AUDIT_RULES) {
                if (skipRules.has(rule.id)) continue;
                try {
                  const issues = await rule.check(page, pageConfig);
                  for (const issue of issues) {
                    collector.add({
                      pageId: tabPageId,
                      ...issue,
                      screenshotPath: tabScreenshot,
                      sourceFile: issue.sourceFile ?? pageConfig.sourceFile,
                    });
                  }
                } catch {
                  // Non-fatal
                }
              }
            } finally {
              tabListeners.detach();
            }
          }
        }
      } finally {
        listeners.detach();
      }
    });
  });
}

test.afterAll(async () => {
  const issues = collector.getAll();
  await saveRunnerIssues('platform', issues);
  const totalCount = await generateMergedReport();
  console.log(
    `\n  Platform Audit: ${issues.length} issues. Total merged: ${totalCount}. See .ui-audit/summary.md\n`,
  );
});
