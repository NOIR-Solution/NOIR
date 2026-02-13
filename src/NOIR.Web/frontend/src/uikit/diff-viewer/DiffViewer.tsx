import { cn } from '@/lib/utils'
import { ArrowRight, Plus, Minus, RefreshCw } from 'lucide-react'

interface DiffChange {
  from?: unknown
  to?: unknown
}

interface DiffViewerProps {
  /** Diff data in format: { fieldName: { from: oldValue, to: newValue } } */
  data: string | Record<string, DiffChange>
  className?: string
}

// System fields to exclude from diff display (audit/timestamp fields)
const EXCLUDED_FIELDS = new Set([
  'modifiedAt',
  'modifiedBy',
  'createdAt',
  'createdBy',
  'lastModifiedAt',
  'lastModifiedBy',
  'updatedAt',
  'updatedBy',
  'rowVersion',
  'version',
  'concurrencyStamp',
  'securityStamp',
  'lockoutEnd',
  'lastLoginAt',
  'lastActivityAt',
  'passwordChangedAt',
])

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return '(null)'
  if (typeof value === 'string') return value
  if (typeof value === 'boolean') return value ? 'true' : 'false'
  if (typeof value === 'number') return value.toString()
  if (value instanceof Date) return value.toISOString()
  return JSON.stringify(value, null, 2)
}

// Find common prefix length between two strings
function commonPrefixLength(a: string, b: string): number {
  let i = 0
  while (i < a.length && i < b.length && a[i] === b[i]) {
    i++
  }
  return i
}

// Find common suffix length between two strings (excluding prefix)
function commonSuffixLength(a: string, b: string, prefixLen: number): number {
  let i = 0
  const maxLen = Math.min(a.length - prefixLen, b.length - prefixLen)
  while (i < maxLen && a[a.length - 1 - i] === b[b.length - 1 - i]) {
    i++
  }
  return i
}

// Split string into [unchanged, changed, unchanged] parts for inline diff
function getInlineDiff(
  oldStr: string,
  newStr: string
): { old: { prefix: string; changed: string; suffix: string }; new: { prefix: string; changed: string; suffix: string } } {
  const prefixLen = commonPrefixLength(oldStr, newStr)
  const suffixLen = commonSuffixLength(oldStr, newStr, prefixLen)

  return {
    old: {
      prefix: oldStr.slice(0, prefixLen),
      changed: oldStr.slice(prefixLen, oldStr.length - suffixLen || undefined),
      suffix: oldStr.slice(oldStr.length - suffixLen || oldStr.length),
    },
    new: {
      prefix: newStr.slice(0, prefixLen),
      changed: newStr.slice(prefixLen, newStr.length - suffixLen || undefined),
      suffix: newStr.slice(newStr.length - suffixLen || newStr.length),
    },
  }
}

function formatFieldName(name: string): string {
  // Convert camelCase to Title Case with spaces
  return name
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (str) => str.toUpperCase())
    .trim()
}

function getChangeType(change: DiffChange): 'added' | 'removed' | 'modified' {
  const hasFrom = change.from !== undefined && change.from !== null
  const hasTo = change.to !== undefined && change.to !== null

  if (!hasFrom && hasTo) return 'added'
  if (hasFrom && !hasTo) return 'removed'
  return 'modified'
}

// Inline diff value display - highlights only changed parts
function InlineDiffValue({
  parts,
  type,
}: {
  parts: { prefix: string; changed: string; suffix: string }
  type: 'old' | 'new'
}) {
  const isOld = type === 'old'
  const baseClasses = isOld
    ? 'bg-red-50 dark:bg-red-950/50 border-red-200 dark:border-red-800'
    : 'bg-green-50 dark:bg-green-950/50 border-green-200 dark:border-green-800'
  const highlightClasses = isOld
    ? 'bg-red-200 dark:bg-red-800 text-red-900 dark:text-red-100 line-through decoration-red-500'
    : 'bg-green-200 dark:bg-green-800 text-green-900 dark:text-green-100'

  return (
    <code
      className={cn(
        'px-2 py-0.5 rounded border text-sm max-w-[250px] truncate inline-flex',
        baseClasses
      )}
      title={parts.prefix + parts.changed + parts.suffix}
    >
      {parts.prefix && <span className="text-muted-foreground">{parts.prefix}</span>}
      {parts.changed && <span className={cn('rounded-sm px-0.5', highlightClasses)}>{parts.changed}</span>}
      {parts.suffix && <span className="text-muted-foreground">{parts.suffix}</span>}
      {!parts.prefix && !parts.changed && !parts.suffix && (
        <span className="text-muted-foreground italic">(empty)</span>
      )}
    </code>
  )
}

function DiffRow({
  fieldName,
  change,
}: {
  fieldName: string
  change: DiffChange
}) {
  const changeType = getChangeType(change)
  const fromValue = formatValue(change.from)
  const toValue = formatValue(change.to)

  // For modified strings, compute inline diff
  const useInlineDiff =
    changeType === 'modified' &&
    typeof change.from === 'string' &&
    typeof change.to === 'string'
  const inlineDiff = useInlineDiff ? getInlineDiff(fromValue, toValue) : null

  return (
    <div className="flex items-start gap-3 py-2 px-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors">
      {/* Icon */}
      <div className="mt-0.5">
        {changeType === 'added' && (
          <div className="p-1 rounded bg-green-100 dark:bg-green-900/30">
            <Plus className="h-3.5 w-3.5 text-green-600 dark:text-green-400" />
          </div>
        )}
        {changeType === 'removed' && (
          <div className="p-1 rounded bg-red-100 dark:bg-red-900/30">
            <Minus className="h-3.5 w-3.5 text-red-600 dark:text-red-400" />
          </div>
        )}
        {changeType === 'modified' && (
          <div className="p-1 rounded bg-amber-100 dark:bg-amber-900/30">
            <RefreshCw className="h-3.5 w-3.5 text-amber-600 dark:text-amber-400" />
          </div>
        )}
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0">
        {/* Field Name */}
        <div className="font-medium text-sm mb-1">{formatFieldName(fieldName)}</div>

        {/* Values */}
        <div className="flex items-center gap-2 flex-wrap text-sm">
          {changeType === 'added' && (
            <code className="px-2 py-0.5 rounded bg-green-50 dark:bg-green-950 text-green-700 dark:text-green-300 border border-green-200 dark:border-green-800 max-w-[250px] truncate" title={toValue}>
              {toValue}
            </code>
          )}

          {changeType === 'removed' && (
            <code className="px-2 py-0.5 rounded bg-red-50 dark:bg-red-950 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800 line-through decoration-red-400 max-w-[250px] truncate" title={fromValue}>
              {fromValue}
            </code>
          )}

          {changeType === 'modified' && inlineDiff && (
            <>
              <InlineDiffValue parts={inlineDiff.old} type="old" />
              <ArrowRight className="h-4 w-4 text-muted-foreground flex-shrink-0" />
              <InlineDiffValue parts={inlineDiff.new} type="new" />
            </>
          )}

          {changeType === 'modified' && !inlineDiff && (
            <>
              <code className="px-2 py-0.5 rounded bg-red-50 dark:bg-red-950 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800 line-through decoration-red-400 max-w-[250px] truncate" title={fromValue}>
                {fromValue}
              </code>
              <ArrowRight className="h-4 w-4 text-muted-foreground flex-shrink-0" />
              <code className="px-2 py-0.5 rounded bg-green-50 dark:bg-green-950 text-green-700 dark:text-green-300 border border-green-200 dark:border-green-800 max-w-[250px] truncate" title={toValue}>
                {toValue}
              </code>
            </>
          )}
        </div>
      </div>
    </div>
  )
}

export function DiffViewer({ data, className }: DiffViewerProps) {
  // Parse if string
  const diffData: Record<string, DiffChange> =
    typeof data === 'string' ? JSON.parse(data) : data

  // Filter out system fields (audit timestamps, versions, etc.)
  const entries = Object.entries(diffData).filter(
    ([fieldName]) => !EXCLUDED_FIELDS.has(fieldName)
  )

  if (entries.length === 0) {
    return (
      <div className="text-center py-4 text-muted-foreground text-sm">
        No changes detected
      </div>
    )
  }

  return (
    <div className={cn('space-y-2', className)}>
      {entries.map(([fieldName, change]) => (
        <DiffRow key={fieldName} fieldName={fieldName} change={change} />
      ))}
    </div>
  )
}
