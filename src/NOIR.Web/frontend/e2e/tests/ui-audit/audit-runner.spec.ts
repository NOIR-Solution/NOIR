import { test } from '@playwright/test';
import { ADMIN_PAGE_REGISTRY } from './page-registry';
import { AUDIT_RULES, dialogFooterRule } from './rules';
import { IssueCollector } from './issue-collector';
import { ScreenshotManager } from './screenshot-manager';
import { runAxeScan } from './axe-scanner';
import { lockEnvironment, attachListeners } from './environment-setup';
import { saveRunnerIssues, generateMergedReport, cleanStaleIssues } from './report-generator';
import { ApiHelper } from '../../fixtures/api.fixture';

const API_URL = process.env.API_URL ?? 'http://localhost:4000';
const collector = new IssueCollector();
const screenshots = new ScreenshotManager();

// Clean stale issues from previous runs before starting
test.beforeAll(async () => {
  await cleanStaleIssues('admin');
});

async function createAuthenticatedApi(apiRequest: any): Promise<ApiHelper> {
  // playwright.request is APIRequest — must call newContext() to get APIRequestContext
  const tempContext = await apiRequest.newContext({
    baseURL: API_URL,
    extraHTTPHeaders: { 'X-Tenant': 'default' },
  });

  // Login to get bearer token
  const loginRes = await tempContext.post(`${API_URL}/api/auth/login`, {
    data: { email: 'admin@noir.local', password: '123qwe' },
    headers: { 'Content-Type': 'application/json', 'X-Tenant': 'default' },
  });
  let token = '';
  if (loginRes.ok()) {
    const loginBody = await loginRes.json();
    token = loginBody?.auth?.accessToken ?? '';
  }
  await tempContext.dispose();

  // Create new context with auth header
  const authContext = await apiRequest.newContext({
    baseURL: API_URL,
    extraHTTPHeaders: {
      'X-Tenant': 'default',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  return new ApiHelper(authContext);
}

for (const pageConfig of ADMIN_PAGE_REGISTRY) {
  test.describe(`audit: ${pageConfig.domain}/${pageConfig.id}`, () => {
    test(`page: ${pageConfig.id}`, async ({ page, context, playwright }) => {
      // Lock environment for consistent rendering
      await lockEnvironment(context);
      const listeners = attachListeners(page);

      let cleanup: (() => Promise<void>) | undefined;
      let resolvedUrl = pageConfig.url;

      try {
        // 1. Seed data if needed
        if (pageConfig.requiresData && pageConfig.seedFn) {
          const api = await createAuthenticatedApi(playwright.request);
          const seed = await pageConfig.seedFn(api);
          cleanup = seed.cleanup;
          if (seed.routeParam) {
            resolvedUrl = resolvedUrl.replace(':id', seed.routeParam);
          }
        }

        // Skip pages with unresolved :id (seed failed or returned no data)
        if (resolvedUrl.includes(':id')) {
          collector.add({
            pageId: pageConfig.id,
            ruleId: 'seed-data-missing',
            severity: 'INFO',
            message: `Skipped "${pageConfig.id}" — no seed data available (URL still contains :id)`,
            fix: 'Ensure test data exists or seed function returns a valid routeParam',
            reference: 'Audit infrastructure',
          });
          return;
        }

        // 2. Navigate and wait for page load
        await page.goto(resolvedUrl);
        await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {});
        if (pageConfig.waitFor) {
          await page.waitForSelector(pageConfig.waitFor, { timeout: 10_000 }).catch(() => {});
        }

        // 3. Take page screenshot
        const screenshotPath = await screenshots.takePage(page, pageConfig.id);

        // 4. Run axe-core scan
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

        // 5. Run custom rules
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
            // Non-fatal: rule may fail on some pages
          }
        }

        // 6. Add console/network errors as issues
        if (listeners.consoleErrors.length > 0) {
          const hasReactError = listeners.consoleErrors.some(e =>
            e.text.includes('Maximum update depth') || e.text.includes('Cannot update'),
          );
          collector.add({
            pageId: pageConfig.id,
            ruleId: 'console-errors',
            severity: hasReactError ? 'HIGH' : 'MEDIUM',
            message: `${listeners.consoleErrors.length} console error(s) during page load`,
            detail: listeners.consoleErrors.map(e => e.text).join('\n---\n'),
            fix: 'Investigate and fix console errors',
            reference: 'React error boundaries / runtime errors',
            screenshotPath,
            sourceFile: pageConfig.sourceFile,
          });
        }

        if (listeners.networkErrors.length > 0) {
          const has500 = listeners.networkErrors.some(e => e.status >= 500);
          collector.add({
            pageId: pageConfig.id,
            ruleId: 'network-errors',
            severity: has500 ? 'HIGH' : 'MEDIUM',
            message: `${listeners.networkErrors.length} failed API request(s)`,
            detail: listeners.networkErrors.map(e => `${e.status} ${e.url}`).join('\n'),
            fix: 'Fix backend endpoint or add error handling',
            reference: 'API health',
            screenshotPath,
            sourceFile: pageConfig.sourceFile,
          });
        }

        // 7. Scan each tab (if page has tabs)
        if (pageConfig.tabs && pageConfig.tabs.length > 0) {
          for (const tab of pageConfig.tabs) {
            const tabListeners = attachListeners(page);
            try {
              const tabUrl = `${resolvedUrl}?tab=${tab.param}`;
              await page.goto(tabUrl);
              await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => {});
              // Wait for tab to be visible
              await page
                .getByRole('tab', { name: new RegExp(tab.id, 'i') })
                .waitFor({ state: 'visible', timeout: 5_000 })
                .catch(() => {});

              const tabScreenshot = await screenshots.takeTab(page, pageConfig.id, tab.id);
              const tabPageId = `${pageConfig.id}/tab:${tab.id}`;

              // Run axe on tab
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

              // Run custom rules on tab
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

              // Tab console/network errors
              if (tabListeners.consoleErrors.length > 0) {
                collector.add({
                  pageId: tabPageId,
                  ruleId: 'console-errors',
                  severity: 'MEDIUM',
                  message: `${tabListeners.consoleErrors.length} console error(s) on tab ${tab.id}`,
                  detail: tabListeners.consoleErrors.map(e => e.text).join('\n---\n'),
                  fix: 'Investigate tab-specific console errors',
                  reference: 'React error boundaries / runtime errors',
                  screenshotPath: tabScreenshot,
                  sourceFile: pageConfig.sourceFile,
                });
              }
            } finally {
              tabListeners.detach();
            }
          }
        }

        // 8. Scan dialogs
        for (const trigger of pageConfig.dialogTriggers ?? []) {
          try {
            // Navigate back to the main page URL (tabs may have changed it)
            if (pageConfig.tabs) {
              await page.goto(resolvedUrl);
              await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => {});
              if (pageConfig.waitFor) {
                await page.waitForSelector(pageConfig.waitFor, { timeout: 10_000 }).catch(() => {});
              }
            }

            // Click dialog trigger
            if (trigger.triggerSelector) {
              await page.click(trigger.triggerSelector, { timeout: 5_000 });
            } else {
              await page
                .getByRole('button', { name: trigger.label })
                .first()
                .click({ timeout: 5_000 });
            }

            // Wait for dialog to open
            const dialogSelector = trigger.waitForSelector ?? '[role="dialog"]';
            await page.waitForSelector(dialogSelector, { state: 'visible', timeout: 5_000 });
            await page.waitForTimeout(300); // Animation settle

            const dialogScreenshot = await screenshots.takeDialog(
              page,
              pageConfig.id,
              trigger.id,
            );
            const dialogPageId = `${pageConfig.id}/dialog:${trigger.id}`;

            // Run dialog-specific rules (footer check)
            try {
              const footerIssues = await dialogFooterRule.check(page, pageConfig);
              for (const issue of footerIssues) {
                collector.add({
                  pageId: dialogPageId,
                  ...issue,
                  screenshotPath: dialogScreenshot,
                  sourceFile: pageConfig.sourceFile,
                });
              }
            } catch {
              // Non-fatal
            }

            // Run axe on dialog
            try {
              const dialogAxe = await runAxeScan(page, dialogPageId);
              for (const issue of dialogAxe) {
                collector.add({
                  pageId: dialogPageId,
                  ...issue,
                  screenshotPath: dialogScreenshot,
                  sourceFile: pageConfig.sourceFile,
                });
              }
            } catch {
              // Non-fatal
            }

            // Close dialog
            await page.keyboard.press('Escape');
            await page
              .locator('[role="dialog"]')
              .waitFor({ state: 'hidden', timeout: 3_000 })
              .catch(() => {});
          } catch {
            // Dialog could not be opened (may need data or different trigger)
            collector.add({
              pageId: pageConfig.id,
              ruleId: 'dialog-open-failed',
              severity: 'INFO',
              message: `Dialog "${trigger.id}" could not be opened`,
              fix: 'May need seeded data or different trigger selector',
              reference: 'Audit infrastructure',
            });
          }
        }
      } finally {
        listeners.detach();
        try {
          await cleanup?.();
        } catch {
          // Best-effort cleanup
        }
      }
    });
  });
}

// Save this runner's issues and generate merged report (includes prompt.md)
test.afterAll(async () => {
  const issues = collector.getAll();
  await saveRunnerIssues('admin', issues);
  const totalCount = await generateMergedReport();
  console.log(
    `\n  Admin Audit: ${issues.length} issues. Total merged: ${totalCount}. See .ui-audit/summary.md\n`,
  );
});
