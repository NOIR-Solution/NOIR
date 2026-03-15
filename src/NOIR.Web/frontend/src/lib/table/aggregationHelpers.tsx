import type { CellContext } from '@tanstack/react-table'

/**
 * Pre-built aggregatedCell renderers for TanStack Table group rows.
 *
 * Usage in column definition:
 * ```tsx
 * ch.accessor('total', {
 *   aggregationFn: 'sum',
 *   aggregatedCell: aggregatedCells.currency('VND'),
 * })
 * ```
 */

// ─── Aggregated cell renderers ──────────────────────────────────────────────

/** Renders "12 items" */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const aggregatedCount = () => ({ getValue }: CellContext<any, unknown>) => {
  const value = getValue()
  return <span className="text-xs font-medium text-muted-foreground">{String(value ?? 0)} items</span>
}

/** Renders a formatted currency value. Uses Intl.NumberFormat. */
export const aggregatedCurrency = (
  currency = 'VND',
  locale = 'en-US',
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
) => ({ getValue }: CellContext<any, unknown>) => {
  const value = Number(getValue() ?? 0)
  const formatted = new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value)
  return <span className="text-xs font-semibold tabular-nums">{formatted}</span>
}

/** Renders a plain numeric sum */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const aggregatedSum = () => ({ getValue }: CellContext<any, unknown>) => {
  const value = Number(getValue() ?? 0)
  return <span className="text-xs font-medium tabular-nums">{value.toLocaleString()}</span>
}

/** Renders "avg: 4.5" */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const aggregatedAverage = (decimals = 1) => ({ getValue }: CellContext<any, unknown>) => {
  const value = Number(getValue() ?? 0)
  return (
    <span className="text-xs text-muted-foreground tabular-nums">
      avg: {value.toFixed(decimals)}
    </span>
  )
}

/** Renders "100 – 500" range from min/max */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const aggregatedRange = () => ({ column, table }: CellContext<any, unknown>) => {
  const rows = table.getGroupedRowModel().flatRows
  const values = rows
    .map(r => Number(r.getValue(column.id)))
    .filter(v => !isNaN(v))
  if (values.length === 0) return null
  const min = Math.min(...values)
  const max = Math.max(...values)
  return (
    <span className="text-xs text-muted-foreground tabular-nums">
      {min.toLocaleString()} – {max.toLocaleString()}
    </span>
  )
}

// ─── Convenience namespace ──────────────────────────────────────────────────

export const aggregatedCells = {
  count: aggregatedCount,
  currency: aggregatedCurrency,
  sum: aggregatedSum,
  average: aggregatedAverage,
  range: aggregatedRange,
} as const
