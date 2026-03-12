import type { CollectedIssue } from './rules/types';
import { SEVERITY_ORDER } from './rules/types';

export class IssueCollector {
  private issues: CollectedIssue[] = [];
  private seen = new Set<string>();

  add(issue: CollectedIssue): void {
    const key = `${issue.pageId}::${issue.ruleId}::${issue.message.substring(0, 80)}`;
    if (this.seen.has(key)) return;
    this.seen.add(key);
    this.issues.push(issue);
  }

  addAll(issues: CollectedIssue[]): void {
    for (const issue of issues) {
      this.add(issue);
    }
  }

  getAll(): CollectedIssue[] {
    return [...this.issues].sort(
      (a, b) => (SEVERITY_ORDER[a.severity] ?? 4) - (SEVERITY_ORDER[b.severity] ?? 4),
    );
  }

  getByPage(pageId: string): CollectedIssue[] {
    return this.issues.filter(i => i.pageId === pageId);
  }

  getBySeverity(severity: CollectedIssue['severity']): CollectedIssue[] {
    return this.issues.filter(i => i.severity === severity);
  }

  get count(): number {
    return this.issues.length;
  }

  get summary(): Record<string, number> {
    const result: Record<string, number> = { CRITICAL: 0, HIGH: 0, MEDIUM: 0, LOW: 0, INFO: 0 };
    for (const issue of this.issues) {
      result[issue.severity] = (result[issue.severity] ?? 0) + 1;
    }
    return result;
  }
}
