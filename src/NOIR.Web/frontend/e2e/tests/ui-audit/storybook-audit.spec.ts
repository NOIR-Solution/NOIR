import { test } from '@playwright/test';
import { IssueCollector } from './issue-collector';
import { runAxeScan } from './axe-scanner';
import { saveRunnerIssues, generateMergedReport, cleanStaleIssues } from './report-generator';

const STORYBOOK_URL = process.env.STORYBOOK_URL ?? 'http://localhost:6006';
const collector = new IssueCollector();

// Clean stale issues from previous runs before starting
test.beforeAll(async () => {
  await cleanStaleIssues('storybook');
});

test.describe('storybook audit', () => {
  test('scan all stories with axe-core', async ({ page }) => {
    // 677 stories × ~1s each = ~15-20min — allow 30min for full scan
    test.setTimeout(1_800_000);
    // Try to fetch the story index
    let storyEntries: Array<{ id: string; title: string; name: string }>;

    try {
      const res = await page.request.get(`${STORYBOOK_URL}/index.json`);
      if (!res.ok()) {
        console.warn(`Storybook not running at ${STORYBOOK_URL} — skipping storybook audit`);
        return;
      }
      const index = await res.json();
      // Storybook 7+ uses `entries`, older uses `v`
      const entries = index.entries ?? index.v ?? {};
      storyEntries = Object.values(entries).filter(
        (e: any) => e.type === 'story',
      ) as Array<{ id: string; title: string; name: string }>;
    } catch {
      console.warn(`Could not connect to Storybook at ${STORYBOOK_URL} — skipping`);
      return;
    }

    console.log(`Found ${storyEntries.length} stories to audit`);

    for (const story of storyEntries) {
      try {
        // Navigate to the story in iframe mode
        await page.goto(
          `${STORYBOOK_URL}/iframe.html?id=${story.id}&viewMode=story`,
          { timeout: 15_000 },
        );
        await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => {});
        await page.waitForTimeout(500); // Render settle

        // Run axe-core
        const axeIssues = await runAxeScan(page, `storybook:${story.id}`);

        // Map story title to source file path
        const componentName = story.title.replace('UIKit/', '').toLowerCase();
        const sourceFile = `uikit/${componentName}/`;

        for (const issue of axeIssues) {
          collector.add({
            pageId: `storybook:${story.id}`,
            ...issue,
            sourceFile: issue.sourceFile ?? sourceFile,
          });
        }
      } catch {
        // Non-fatal: individual story may fail to load
        collector.add({
          pageId: `storybook:${story.id}`,
          ruleId: 'storybook:load-error',
          severity: 'INFO',
          message: `Story "${story.title}/${story.name}" failed to load`,
          fix: 'Check if the story renders correctly in Storybook',
          reference: 'Storybook component health',
        });
      }
    }
  });
});

test.afterAll(async () => {
  const issues = collector.getAll();
  await saveRunnerIssues('storybook', issues);
  const totalCount = await generateMergedReport();
  console.log(
    `\n  Storybook Audit: ${issues.length} issues. Total merged: ${totalCount}.\n`,
  );
});
