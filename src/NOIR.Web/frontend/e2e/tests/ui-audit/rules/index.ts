import type { AuditRule } from './types';
import { cursorPointerRule } from './cursor-pointer.rule';
import { ariaLabelRule } from './aria-label.rule';
import { datatableActionsRule } from './datatable-actions.rule';
import { dialogFooterRule } from './dialog-footer.rule';
import { badgeVariantRule } from './badge-variant.rule';
import { emptyStateRule } from './empty-state.rule';
import { nativeTitleRule } from './native-title.rule';
import { gradientTextRule } from './gradient-text.rule';
import { destructiveButtonRule } from './destructive-button.rule';
import { consoleErrorsRule } from './console-errors.rule';
import { networkErrorsRule } from './network-errors.rule';
import { viewModeToggleRule } from './view-mode-toggle.rule';

export const AUDIT_RULES: AuditRule[] = [
  cursorPointerRule,
  ariaLabelRule,
  datatableActionsRule,
  dialogFooterRule,
  badgeVariantRule,
  emptyStateRule,
  nativeTitleRule,
  gradientTextRule,
  destructiveButtonRule,
  consoleErrorsRule,
  networkErrorsRule,
  viewModeToggleRule,
];

export { dialogFooterRule } from './dialog-footer.rule';
