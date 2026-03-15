import { useState, useCallback, useEffect } from 'react'
import type { Table } from '@tanstack/react-table'

interface UseKeyboardNavigationOptions<TData> {
  table: Table<TData>
  onRowClick?: (row: TData) => void
  enabled?: boolean
}

interface UseKeyboardNavigationReturn {
  tableBodyProps: {
    tabIndex: number
    role: string
    onKeyDown: (e: React.KeyboardEvent) => void
    onBlur: () => void
    'aria-activedescendant': string | undefined
  }
  focusedRowIndex: number | null
  clearFocus: () => void
}

export const useKeyboardNavigation = <TData>({
  table,
  onRowClick,
  enabled = true,
}: UseKeyboardNavigationOptions<TData>): UseKeyboardNavigationReturn => {
  const [focusedRowIndex, setFocusedRowIndex] = useState<number | null>(null)

  const rows = table.getRowModel().rows
  const rowCount = rows.length

  // Reset focus when row count changes (data refresh, pagination, filtering)
  useEffect(() => {
    setFocusedRowIndex(null)
  }, [rowCount])

  const clearFocus = useCallback(() => {
    setFocusedRowIndex(null)
  }, [])

  const scrollRowIntoView = useCallback((index: number) => {
    const el = document.querySelector(`[data-row-index="${index}"]`)
    if (el) {
      el.scrollIntoView({ block: 'nearest' })
    }
  }, [])

  const moveFocus = useCallback(
    (nextIndex: number) => {
      const clamped = Math.max(0, Math.min(nextIndex, rowCount - 1))
      setFocusedRowIndex(clamped)
      scrollRowIntoView(clamped)
    },
    [rowCount, scrollRowIntoView],
  )

  const onKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (!enabled || rowCount === 0) return

      switch (e.key) {
        case 'ArrowDown': {
          e.preventDefault()
          const next = focusedRowIndex === null ? 0 : focusedRowIndex + 1
          moveFocus(next)
          break
        }
        case 'ArrowUp': {
          e.preventDefault()
          const next = focusedRowIndex === null ? rowCount - 1 : focusedRowIndex - 1
          moveFocus(next)
          break
        }
        case 'Home': {
          e.preventDefault()
          moveFocus(0)
          break
        }
        case 'End': {
          e.preventDefault()
          moveFocus(rowCount - 1)
          break
        }
        case 'Enter': {
          if (focusedRowIndex !== null && onRowClick) {
            e.preventDefault()
            const row = rows[focusedRowIndex]
            if (row) {
              onRowClick(row.original)
            }
          }
          break
        }
        case ' ': {
          if (focusedRowIndex !== null && table.options.enableRowSelection) {
            e.preventDefault()
            const row = rows[focusedRowIndex]
            if (row) {
              row.toggleSelected()
            }
          }
          break
        }
        case 'Escape': {
          clearFocus()
          break
        }
        default:
          break
      }
    },
    [enabled, rowCount, focusedRowIndex, moveFocus, clearFocus, onRowClick, rows, table.options.enableRowSelection],
  )

  const onBlur = useCallback(() => {
    clearFocus()
  }, [clearFocus])

  const activeDescendant =
    focusedRowIndex !== null ? `table-row-${focusedRowIndex}` : undefined

  const tableBodyProps = {
    tabIndex: enabled ? 0 : -1,
    role: 'grid' as const,
    onKeyDown,
    onBlur,
    'aria-activedescendant': activeDescendant,
  }

  return {
    tableBodyProps,
    focusedRowIndex,
    clearFocus,
  }
}
