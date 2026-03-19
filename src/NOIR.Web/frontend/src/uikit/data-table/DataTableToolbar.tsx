import type { Table, RowData } from '@tanstack/react-table'
import { Search, X, Download } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'
import { Input } from '../input/Input'
import { DataTableColumnsDropdown } from './DataTableColumnsDropdown'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '../dropdown-menu/DropdownMenu'

interface DataTableToolbarProps<TData extends RowData> {
  table: Table<TData>
  /** Controlled search input value */
  searchInput?: string
  onSearchChange?: (value: string) => void
  searchPlaceholder?: string
  /** True while search is deferred (show stale indicator on the input) */
  isSearchStale?: boolean
  /** Show column visibility toggle (defaults to true) */
  showColumnToggle?: boolean
  /** True when any filter is active — shows Reset button */
  hasActiveFilters?: boolean
  onResetFilters?: () => void
  /** Custom filter controls injected between search and column toggle */
  filterSlot?: React.ReactNode
  /** Extra controls injected at the right side (e.g., Create button, Export) */
  actionSlot?: React.ReactNode
  className?: string
  // ─── Enterprise features (optional) ─────────────────────────────────────────
  /**
   * Column order array — enables drag-to-reorder in the Columns dropdown.
   * Must be provided together with onColumnsReorder.
   */
  columnOrder?: string[]
  /** Called when user reorders columns via drag. Update your column order state. */
  onColumnsReorder?: (newOrder: string[]) => void
  /**
   * True when any table setting (order/visibility/sizing) differs from default.
   * Shows "Reset to default" button in the Columns dropdown.
   */
  isCustomized?: boolean
  /** Called when user clicks "Reset to default" (full settings reset). */
  onResetSettings?: () => void
  /**
   * Called to export table data as CSV.
   * When provided, shows an Export button.
   */
  onExportCSV?: () => void
  /**
   * Called to export table data as Excel.
   * When provided alongside onExportCSV, shows a dropdown with both options.
   */
  onExportExcel?: () => void
  /** Groupable column IDs for the Group By selector in the Columns dropdown. */
  groupableColumnIds?: string[]
  /** Currently active group-by column IDs. */
  grouping?: string[]
  /** Called when user changes the Group By selection. */
  onGroupingChange?: (columnIds: string[]) => void
}

/**
 * Reusable table toolbar with search input, filter slot, column management dropdown,
 * optional density toggle, optional export, and action slot. Use above <DataTable />.
 *
 * The Columns dropdown supports basic visibility toggle (all pages) plus optional
 * enterprise features: drag-to-reorder, sort controls, group-by, and reset-to-default.
 */
export const DataTableToolbar = <TData extends RowData>({
  table,
  searchInput,
  onSearchChange,
  searchPlaceholder,
  isSearchStale = false,
  showColumnToggle = true,
  hasActiveFilters = false,
  onResetFilters,
  filterSlot,
  actionSlot,
  className,
  columnOrder,
  onColumnsReorder,
  isCustomized = false,
  onResetSettings,
  onExportCSV,
  onExportExcel,
  groupableColumnIds,
  grouping,
  onGroupingChange,
}: DataTableToolbarProps<TData>) => {
  const { t } = useTranslation('common')

  const hasExport = onExportCSV || onExportExcel

  return (
    <div className={cn('flex flex-col gap-3 sm:flex-row sm:items-center', className)}>
      {/* Left: search + filters */}
      <div className="flex flex-1 flex-wrap items-center gap-2">
        {onSearchChange !== undefined && (
          <div className="relative flex-1 min-w-[200px]">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder={searchPlaceholder ?? t('labels.search', 'Search…')}
              value={searchInput ?? ''}
              onChange={(e) => onSearchChange(e.target.value)}
              className={cn(
                'h-9 w-full pl-8',
                isSearchStale && 'opacity-70',
              )}
            />
          </div>
        )}

        {filterSlot}

        {hasActiveFilters && onResetFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onResetFilters}
            className="h-9 cursor-pointer text-muted-foreground hover:text-foreground"
          >
            <X className="mr-1.5 h-4 w-4" />
            {t('buttons.reset', 'Reset')}
          </Button>
        )}
      </div>

      {/* Right: density + export + column management + actions */}
      <div className="flex items-center gap-2">
        {/* Export button */}
        {hasExport && (
          onExportCSV && !onExportExcel ? (
            // CSV-only: simple button
            <Button
              variant="outline"
              size="sm"
              className="h-9 cursor-pointer"
              onClick={onExportCSV}
              aria-label={t('labels.exportCSV', 'Export CSV')}
            >
              <Download className="mr-1.5 h-4 w-4" />
              {t('labels.export', 'Export')}
            </Button>
          ) : (
            // Both CSV + Excel: dropdown
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  size="sm"
                  className="h-9 cursor-pointer"
                  aria-label={t('labels.export', 'Export')}
                >
                  <Download className="mr-1.5 h-4 w-4" />
                  {t('labels.export', 'Export')}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {onExportCSV && (
                  <DropdownMenuItem className="cursor-pointer" onClick={onExportCSV}>
                    {t('labels.exportCSV', 'Export as CSV')}
                  </DropdownMenuItem>
                )}
                {onExportExcel && (
                  <DropdownMenuItem className="cursor-pointer" onClick={onExportExcel}>
                    {t('labels.exportExcel', 'Export as Excel')}
                  </DropdownMenuItem>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          )
        )}

        {showColumnToggle && (
          <DataTableColumnsDropdown
            table={table}
            columnOrder={columnOrder}
            onColumnsReorder={onColumnsReorder}
            isCustomized={isCustomized}
            onResetSettings={onResetSettings}
            groupableColumnIds={groupableColumnIds}
            grouping={grouping}
            onGroupingChange={onGroupingChange}
          />
        )}

        {actionSlot}
      </div>
    </div>
  )
}
