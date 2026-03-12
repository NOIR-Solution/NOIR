import * as fs from 'fs/promises';
import * as path from 'path';
import type { CollectedIssue } from './rules/types';
import { SEVERITY_ORDER } from './rules/types';

// __dirname = e2e/tests/ui-audit → 6 levels up to project root (NOIR/)
const PROJECT_ROOT = path.resolve(__dirname, '..', '..', '..', '..', '..', '..');
const OUTPUT_DIR = path.join(PROJECT_ROOT, '.ui-audit');

export class PromptBuilder {
  constructor(private issues: CollectedIssue[]) {}

  async write(): Promise<void> {
    const content = this.build();
    await fs.mkdir(OUTPUT_DIR, { recursive: true });
    await fs.writeFile(path.join(OUTPUT_DIR, 'prompt.md'), content, 'utf-8');
  }

  private build(): string {
    const criticalHigh = this.issues.filter(
      i => i.severity === 'CRITICAL' || i.severity === 'HIGH',
    );
    const byFile = this.groupBySourceFile();
    const axeIssues = this.issues.filter(i => i.ruleId.startsWith('axe:'));
    const consoleIssues = this.issues.filter(i => i.ruleId === 'console-errors');
    const networkIssues = this.issues.filter(i => i.ruleId === 'network-errors');

    const sections: string[] = [
      `# UI/UX Audit Fix Task - NOIR Portal`,
      `Generated: ${new Date().toISOString()} | ${this.issues.length} issues (${criticalHigh.length} CRITICAL/HIGH)`,
      '',
      '## Instructions',
      '',
      'Fix all CRITICAL and HIGH issues. MEDIUM/LOW are recommended but optional.',
      'Rules from CLAUDE.md and .claude/rules/.',
      'After ALL fixes: `cd src/NOIR.Web/frontend && pnpm run build`',
      '',
      '## Do NOT:',
      '- Change UI behavior, only fix classes, components, attributes',
      '- Skip CRITICAL and HIGH severity issues',
      '- Add new dependencies',
      '',
    ];

    // Group issues by source file (most violations first)
    const fileEntries = Object.entries(byFile).sort(([, a], [, b]) => {
      const critA = a.filter(i => ['CRITICAL', 'HIGH'].includes(i.severity)).length;
      const critB = b.filter(i => ['CRITICAL', 'HIGH'].includes(i.severity)).length;
      return critB - critA || b.length - a.length;
    });

    if (fileEntries.length > 0) {
      sections.push('## Issues by File\n');

      for (const [file, fileIssues] of fileEntries) {
        // Skip axe/console/network issues (they have separate sections)
        const codeIssues = fileIssues.filter(
          i =>
            !i.ruleId.startsWith('axe:') &&
            i.ruleId !== 'console-errors' &&
            i.ruleId !== 'network-errors' &&
            i.ruleId !== 'dialog-open-failed' &&
            i.ruleId !== 'storybook:load-error',
        );
        if (codeIssues.length === 0) continue;

        sections.push(`### ${file}`);
        for (const issue of codeIssues.sort(
          (a, b) => (SEVERITY_ORDER[a.severity] ?? 4) - (SEVERITY_ORDER[b.severity] ?? 4),
        )) {
          sections.push(
            `- [${issue.severity}] ${issue.message}`,
            `  Fix: ${issue.fix}`,
            `  Rule: ${issue.reference}`,
            `  Page: ${issue.pageId}`,
            '',
          );
        }
      }
    }

    // Axe-core section
    if (axeIssues.length > 0) {
      sections.push('## Accessibility (axe-core)\n');
      for (const issue of axeIssues.sort(
        (a, b) => (SEVERITY_ORDER[a.severity] ?? 4) - (SEVERITY_ORDER[b.severity] ?? 4),
      )) {
        sections.push(
          `- [${issue.severity}] ${issue.pageId}: ${issue.message}`,
          `  ${issue.reference}`,
          '',
        );
      }
    }

    // Console errors section
    if (consoleIssues.length > 0) {
      sections.push('## Console Errors\n');
      for (const issue of consoleIssues) {
        sections.push(
          `- [${issue.severity}] ${issue.pageId}: ${issue.message}`,
          `  ${issue.detail?.split('\n')[0] ?? ''}`,
          '',
        );
      }
    }

    // Network errors section
    if (networkIssues.length > 0) {
      sections.push('## Network Errors\n');
      for (const issue of networkIssues) {
        sections.push(
          `- [${issue.severity}] ${issue.pageId}: ${issue.message}`,
          `  ${issue.detail?.split('\n')[0] ?? ''}`,
          '',
        );
      }
    }

    // Verification
    sections.push(
      '## Verification',
      '',
      '```bash',
      'cd src/NOIR.Web/frontend && pnpm run build',
      'cd e2e && npx playwright test --project=ui-audit --project=ui-audit-platform',
      '```',
      '',
    );

    return sections.join('\n');
  }

  private groupBySourceFile(): Record<string, CollectedIssue[]> {
    const groups: Record<string, CollectedIssue[]> = {};
    for (const issue of this.issues) {
      const file = issue.sourceFile ?? `unknown (${issue.pageId})`;
      (groups[file] ??= []).push(issue);
    }
    return groups;
  }
}
