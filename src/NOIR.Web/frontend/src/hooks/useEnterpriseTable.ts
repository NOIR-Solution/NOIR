import { useState, useCallback, useMemo, useEffect, useRef } from 'react'
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getGroupedRowModel,
  getExpandedRowModel,
  functionalUpdate,
  type ColumnPinningState,
  type RowSelectionState,
  type TableOptions,
} from '@tanstack/react-table'
import { useDebouncedCallback } from 'use-debounce'
import type {
  EnterpriseTableOptions,
  EnterpriseTableReturn,
  EnterpriseTableSettings,
} from '@/types/enterprise-table'
import { createDefaultSettings } from '@/types/enterprise-table'
import {
  loadEnterpriseSettings,
  saveEnterpriseSettings,
  checkIsCustomized,
} from '@/lib/table/enterpriseSettingsStorage'

/**
 * Unified DataTable hook — replaces useServerTable.
 *
 * - Server-side state (pagination, sorting) → accepted as external props
 * - Enterprise UI state (visibility, order, sizing, pinning) → managed internally + persisted
 * - Row selection → managed internally, accessible via table.getState().rowSelection
 *
 * @example Server-side (production pages)
 * ```tsx
 * const { table, settings, isCustomized, resetToDefault } = useEnterpriseTable({
 *   data: data?.items ?? [],
 *   columns,
 *   tableKey: 'users',
 *   rowCount: data?.totalCount ?? 0,
 *   state: { pagination: { pageIndex, pageSize }, sorting },
 *   onPaginationChange, onSortingChange,
 *   enableRowSelection: true,
 *   getRowId: (row) => row.id,
 * })
 * ```
 *
 * @example Client-side (Storybook/demo)
 * ```tsx
 * const { table, settings } = useEnterpriseTable({
 *   data: allRows,
 *   columns,
 *   tableKey: 'demo',
 *   manualPagination: false, manualSorting: false, manualFiltering: false,
 *   enableGrouping: true,
 * })
 * ```
 */
export const useEnterpriseTable = <TData>({
  data,
  columns,
  tableKey,
  rowCount,
  defaultPinLeft = ['actions', 'select'],
  enablePinning = true,
  enableResizing = true,
  enableGrouping = false,
  enableRowSelection = false,
  manualPagination = true,
  manualSorting = true,
  manualFiltering = true,
  state: externalState,
  onPaginationChange,
  onSortingChange,
  getRowId,
  enableSortingRemoval = false,
  autoResetPageIndex = false,
  meta,
  onRowSelectionChange,
}: EnterpriseTableOptions<TData>): EnterpriseTableReturn<TData> => {
  // ─── Column IDs (accessor columns use accessorKey, not id) ──────────────────
  const columnIds = useMemo(() =>
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    columns.map(col => col.id ?? (col as any).accessorKey as string | undefined).filter((id): id is string => !!id),
    [columns]
  )

  // ─── Column meta (defaultHidden support) ──────────────────────────────────
  const columnMeta = useMemo(() => {
    const meta: Record<string, { defaultHidden?: boolean }> = {}
    for (const col of columns) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const id = col.id ?? (col as any).accessorKey as string | undefined
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      if (id && col.meta && (col.meta as any).defaultHidden) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        meta[id] = { defaultHidden: (col.meta as any).defaultHidden }
      }
    }
    return meta
  }, [columns])

  // ─── Enterprise settings (persisted to localStorage) ────────────────────────
  const [settings, setSettings] = useState<EnterpriseTableSettings>(() =>
    loadEnterpriseSettings(tableKey, columnIds, defaultPinLeft, columnMeta)
  )

  const defaultSettingsRef = useRef<EnterpriseTableSettings>(
    createDefaultSettings(columnIds, defaultPinLeft, columnMeta)
  )

  useEffect(() => {
    defaultSettingsRef.current = createDefaultSettings(columnIds, defaultPinLeft, columnMeta)
  }, [columnIds, defaultPinLeft, columnMeta])

  // Debounced save to localStorage
  const debouncedSave = useDebouncedCallback(
    (newSettings: EnterpriseTableSettings) => {
      saveEnterpriseSettings(tableKey, newSettings)
    },
    300
  )

  useEffect(() => {
    debouncedSave(settings)
  }, [settings, debouncedSave])

  const isCustomized = useMemo(
    () => checkIsCustomized(settings, defaultSettingsRef.current),
    [settings]
  )

  // ─── Settings updater ──────────────────────────────────────────────────────
  const updateSettings = useCallback(
    (updater: Partial<EnterpriseTableSettings> | ((prev: EnterpriseTableSettings) => EnterpriseTableSettings)) => {
      setSettings(prev => {
        const newSettings = typeof updater === 'function'
          ? updater(prev)
          : { ...prev, ...updater }
        return newSettings
      })
    },
    []
  )

  const resetToDefault = useCallback(() => {
    setSettings(createDefaultSettings(columnIds, defaultPinLeft, columnMeta))
  }, [columnIds, defaultPinLeft, columnMeta])

  // ─── Column actions ────────────────────────────────────────────────────────
  const toggleColumnVisibility = useCallback((columnId: string, visible: boolean) => {
    updateSettings(prev => ({
      ...prev,
      columnVisibility: { ...prev.columnVisibility, [columnId]: visible },
    }))
  }, [updateSettings])

  const reorderColumns = useCallback((oldIndex: number, newIndex: number) => {
    updateSettings(prev => {
      const newOrder = [...prev.columnOrder]
      const [removed] = newOrder.splice(oldIndex, 1)
      newOrder.splice(newIndex, 0, removed)
      return { ...prev, columnOrder: newOrder }
    })
  }, [updateSettings])

  const toggleColumnPin = useCallback((columnId: string, side: 'left' | 'right' | false) => {
    updateSettings(prev => {
      const newPinning = { ...prev.columnPinning }
      newPinning.left = newPinning.left.filter(id => id !== columnId)
      newPinning.right = newPinning.right.filter(id => id !== columnId)
      if (side === 'left') newPinning.left.push(columnId)
      else if (side === 'right') newPinning.right.push(columnId)
      return { ...prev, columnPinning: newPinning }
    })
  }, [updateSettings])

  // ─── Grouping actions ──────────────────────────────────────────────────────
  const setGrouping = useCallback((groupColumnIds: string[]) => {
    updateSettings(prev => ({ ...prev, grouping: groupColumnIds }))
  }, [updateSettings])

  const toggleGroupExpansion = useCallback((groupId: string) => {
    updateSettings(prev => {
      const current = prev.expanded === true ? {} : prev.expanded
      return { ...prev, expanded: { ...current, [groupId]: !current[groupId] } }
    })
  }, [updateSettings])

  const expandAllGroups = useCallback(() => {
    updateSettings(prev => ({ ...prev, expanded: true }))
  }, [updateSettings])

  const collapseAllGroups = useCallback(() => {
    updateSettings(prev => ({ ...prev, expanded: {} }))
  }, [updateSettings])

  const toggleFiltersRow = useCallback(() => {
    updateSettings(prev => ({ ...prev, showFiltersRow: !prev.showFiltersRow }))
  }, [updateSettings])

  // ─── Row selection (internal, ephemeral) ───────────────────────────────────
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})

  // ─── Optional row models — conditional on manual flags ──────────────────────
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const optionalRowModels = useMemo(() => {
    const models: Record<string, any> = {}
    if (!manualSorting) models.getSortedRowModel = getSortedRowModel()
    if (!manualFiltering) models.getFilteredRowModel = getFilteredRowModel()
    if (!manualPagination) models.getPaginationRowModel = getPaginationRowModel()
    if (enableGrouping) {
      models.getGroupedRowModel = getGroupedRowModel()
      models.getExpandedRowModel = getExpandedRowModel()
    }
    return models
  }, [enableGrouping, manualSorting, manualFiltering, manualPagination])

  // ─── Create TanStack table instance ────────────────────────────────────────
  const table = useReactTable({
    data,
    columns,

    // Features
    enableColumnPinning: enablePinning,
    enableColumnResizing: enableResizing,
    enableGrouping,
    enableRowSelection,
    enableSortingRemoval,
    // Prevent grouping from reordering columns — our colgroup uses getVisibleLeafColumns()
    // which would mismatch with headers that follow the user's columnOrder state
    groupedColumnMode: false,

    // Server-side mode
    manualPagination,
    manualSorting,
    manualFiltering,
    rowCount,
    autoResetPageIndex,

    // Stable selection across pages
    getRowId,

    // Column defaults
    defaultColumn: {
      minSize: 40,
      maxSize: 800,
    },

    // State — merge external server-side state with internal enterprise state
    state: {
      // Server-side state (from URL params)
      pagination: externalState?.pagination ?? { pageIndex: 0, pageSize: 10 },
      sorting: externalState?.sorting ?? [],
      // Enterprise state (from localStorage)
      columnPinning: settings.columnPinning,
      columnOrder: settings.columnOrder,
      columnSizing: settings.columnSizing,
      columnVisibility: settings.columnVisibility,
      grouping: settings.grouping,
      expanded: settings.expanded,
      // Ephemeral state
      rowSelection,
    },

    // Server-side state handlers — delegate to caller
    onPaginationChange,
    onSortingChange,

    // Enterprise state handlers — persist to settings
    onColumnPinningChange: (updater) => {
      updateSettings(prev => {
        const next = functionalUpdate(updater, prev.columnPinning) as ColumnPinningState
        return {
          ...prev,
          columnPinning: { left: next.left ?? [], right: next.right ?? [] },
        }
      })
    },
    onColumnOrderChange: (updater) => {
      updateSettings(prev => ({
        ...prev,
        columnOrder: functionalUpdate(updater, prev.columnOrder),
      }))
    },
    onColumnSizingChange: (updater) => {
      updateSettings(prev => ({
        ...prev,
        columnSizing: functionalUpdate(updater, prev.columnSizing),
      }))
    },
    onColumnVisibilityChange: (updater) => {
      updateSettings(prev => ({
        ...prev,
        columnVisibility: functionalUpdate(updater, prev.columnVisibility),
      }))
    },
    onGroupingChange: (updater) => {
      updateSettings(prev => ({
        ...prev,
        grouping: functionalUpdate(updater, prev.grouping),
      }))
    },
    onRowSelectionChange: (updater) => {
      setRowSelection(prev => functionalUpdate(updater, prev))
    },
    onExpandedChange: (updater) => {
      updateSettings(prev => {
        const next = functionalUpdate(updater, prev.expanded)
        return { ...prev, expanded: next as true | Record<string, boolean> }
      })
    },

    // Performance: use 'onEnd' for large datasets
    columnResizeMode: data.length > 100 ? 'onEnd' : 'onChange',

    // Row models
    getCoreRowModel: getCoreRowModel(),
    ...optionalRowModels,

    meta: meta as TableOptions<TData>['meta'],
  })

  // ─── Fire onRowSelectionChange callback ────────────────────────────────────
  useEffect(() => {
    if (!onRowSelectionChange) return
    const selectedRows = table.getSelectedRowModel().rows.map(r => r.original)
    onRowSelectionChange(selectedRows)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rowSelection, onRowSelectionChange])

  return {
    table,
    settings,
    updateSettings,
    resetToDefault,
    isCustomized,
    toggleColumnVisibility,
    reorderColumns,
    toggleColumnPin,
    setGrouping,
    toggleGroupExpansion,
    expandAllGroups,
    collapseAllGroups,
    toggleFiltersRow,
  }
}

// ─── Selection utilities (moved from useServerTable) ──────────────────────────

/**
 * useMemo-wrapped helper used in page components:
 *   const selectedIds = useSelectedIds(table.getState().rowSelection)
 */
export const useSelectedIds = (rowSelection: RowSelectionState): string[] =>
  // eslint-disable-next-line react-hooks/exhaustive-deps
  useMemo(() => Object.keys(rowSelection), [rowSelection])
