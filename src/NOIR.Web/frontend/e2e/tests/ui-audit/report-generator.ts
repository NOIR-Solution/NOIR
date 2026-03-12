import * as fs from 'fs/promises';
import * as path from 'path';
import type { CollectedIssue } from './rules/types';
import { SEVERITY_ORDER } from './rules/types';
import { PromptBuilder } from './prompt-builder';

// __dirname = e2e/tests/ui-audit → 6 levels up to project root (NOIR/)
const PROJECT_ROOT = path.resolve(__dirname, '..', '..', '..', '..', '..', '..');
const OUTPUT_DIR = path.join(PROJECT_ROOT, '.ui-audit');
const RAW_DIR = path.join(OUTPUT_DIR, 'raw');

/**
 * Cleans up stale runner issue files from previous runs.
 * Call once at the start of each runner to ensure fresh data.
 */
export async function cleanStaleIssues(runnerName: string): Promise<void> {
  try {
    const filePath = path.join(RAW_DIR, `${runnerName}-issues.json`);
    await fs.unlink(filePath);
  } catch {
    // File may not exist — that's fine
  }
}

/**
 * Saves issues for a specific runner to a JSON file.
 * Multiple runners (admin, platform, storybook) each write their own file.
 */
export async function saveRunnerIssues(
  runnerName: string,
  issues: CollectedIssue[],
): Promise<void> {
  await fs.mkdir(RAW_DIR, { recursive: true });
  await fs.writeFile(
    path.join(RAW_DIR, `${runnerName}-issues.json`),
    JSON.stringify(issues, null, 2),
    'utf-8',
  );
}

/**
 * Loads all runner issue files from disk and merges them.
 * This ensures whichever afterAll runs last produces the complete report.
 */
async function loadAllRunnerIssues(): Promise<CollectedIssue[]> {
  try {
    const files = await fs.readdir(RAW_DIR);
    const issueFiles = files.filter(f => f.endsWith('-issues.json'));
    const all: CollectedIssue[] = [];
    for (const file of issueFiles) {
      const content = await fs.readFile(path.join(RAW_DIR, file), 'utf-8');
      const parsed = JSON.parse(content) as CollectedIssue[];
      all.push(...parsed);
    }
    return all;
  } catch {
    return [];
  }
}

/**
 * Generates the full .ui-audit/ report from ALL runner issue files on disk.
 * Called by each runner's afterAll — the last to finish produces the complete report.
 */
export async function generateMergedReport(): Promise<number> {
  const allIssues = await loadAllRunnerIssues();
  if (allIssues.length === 0) return 0;

  const gen = new ReportGenerator(allIssues);
  await gen.writeAll();
  // Also regenerate prompt.md with ALL merged issues
  const pb = new PromptBuilder(allIssues);
  await pb.write();
  return allIssues.length;
}

export class ReportGenerator {
  constructor(private issues: CollectedIssue[]) {}

  async writeAll(): Promise<void> {
    await fs.mkdir(path.join(OUTPUT_DIR, 'issues'), { recursive: true });
    await fs.mkdir(RAW_DIR, { recursive: true });

    await Promise.all([
      this.writeSummary(),
      this.writeIssueFiles(),
      this.writeRawJson(),
    ]);
  }

  private async writeSummary(): Promise<void> {
    const summary = this.buildSummary();
    await fs.writeFile(path.join(OUTPUT_DIR, 'summary.md'), summary, 'utf-8');
  }

  private buildSummary(): string {
    const bySeverity = this.groupBy(this.issues, i => i.severity);
    const byPage = this.groupBy(this.issues, i => i.pageId);
    const byRule = this.groupBy(this.issues, i => i.ruleId);
    const uniquePages = new Set(this.issues.map(i => i.pageId));

    const lines: string[] = [
      '# UI/UX Audit Summary',
      `Generated: ${new Date().toISOString()}`,
      `Pages audited: ${uniquePages.size}`,
      '',
      '## Issue Counts',
      '',
      '| Severity | Count |',
      '|----------|-------|',
      `| CRITICAL | ${bySeverity['CRITICAL']?.length ?? 0} |`,
      `| HIGH     | ${bySeverity['HIGH']?.length ?? 0} |`,
      `| MEDIUM   | ${bySeverity['MEDIUM']?.length ?? 0} |`,
      `| LOW      | ${bySeverity['LOW']?.length ?? 0} |`,
      `| INFO     | ${bySeverity['INFO']?.length ?? 0} |`,
      `| **Total**| **${this.issues.length}** |`,
      '',
      '## Top Rules by Violation Count',
      '',
      ...Object.entries(byRule)
        .sort(([, a], [, b]) => b.length - a.length)
        .slice(0, 10)
        .map(([rule, issues]) => `- \`${rule}\`: ${issues.length} violation(s)`),
      '',
      '## Pages With Most Issues',
      '',
      ...Object.entries(byPage)
        .sort(([, a], [, b]) => b.length - a.length)
        .slice(0, 15)
        .map(([page, issues]) => `- \`${page}\`: ${issues.length} issue(s)`),
      '',
      '## Next Step',
      '',
      'Feed to Claude Code CLI:',
      '```bash',
      'claude < .ui-audit/prompt.md',
      '```',
      '',
    ];

    return lines.join('\n');
  }

  private async writeIssueFiles(): Promise<void> {
    // Clean old issue files first
    try {
      const existing = await fs.readdir(path.join(OUTPUT_DIR, 'issues'));
      for (const file of existing) {
        await fs.unlink(path.join(OUTPUT_DIR, 'issues', file));
      }
    } catch {
      // Directory may not exist yet
    }

    const sorted = [...this.issues].sort(
      (a, b) => (SEVERITY_ORDER[a.severity] ?? 4) - (SEVERITY_ORDER[b.severity] ?? 4),
    );

    for (let i = 0; i < sorted.length; i++) {
      const issue = sorted[i];
      const idx = String(i + 1).padStart(3, '0');
      const ruleSlug = issue.ruleId.replace(/[:/\\]/g, '-');
      const filename = `${idx}-${issue.severity}-${ruleSlug}.md`;

      const content = [
        `# ${issue.message}`,
        '',
        `**Severity:** ${issue.severity}`,
        `**Page:** ${issue.pageId}`,
        `**Rule:** ${issue.ruleId}`,
        `**Reference:** ${issue.reference}`,
        issue.sourceFile ? `**Source:** ${issue.sourceFile}` : '',
        '',
        '## Detail',
        '',
        issue.detail ?? 'No additional detail.',
        '',
        '## Fix',
        '',
        issue.fix,
        '',
        issue.screenshotPath
          ? `## Screenshot\n\n![Screenshot](../${issue.screenshotPath})`
          : '',
        '',
      ]
        .filter(Boolean)
        .join('\n');

      await fs.writeFile(path.join(OUTPUT_DIR, 'issues', filename), content, 'utf-8');
    }
  }

  private async writeRawJson(): Promise<void> {
    const axeIssues = this.issues.filter(i => i.ruleId.startsWith('axe:'));
    const ruleIssues = this.issues.filter(i => !i.ruleId.startsWith('axe:'));

    await fs.writeFile(
      path.join(RAW_DIR, 'axe-results.json'),
      JSON.stringify(axeIssues, null, 2),
      'utf-8',
    );
    await fs.writeFile(
      path.join(RAW_DIR, 'rule-results.json'),
      JSON.stringify(ruleIssues, null, 2),
      'utf-8',
    );
  }

  private groupBy<T>(items: T[], keyFn: (item: T) => string): Record<string, T[]> {
    const result: Record<string, T[]> = {};
    for (const item of items) {
      const key = keyFn(item);
      (result[key] ??= []).push(item);
    }
    return result;
  }
}
