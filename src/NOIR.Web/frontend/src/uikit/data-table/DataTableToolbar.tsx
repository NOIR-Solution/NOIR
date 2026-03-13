import type { Table, RowData } from '@tanstack/react-table'
import { RotateCcw, Search, SlidersHorizontal, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'
import { Input } from '../input/Input'
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuSeparator,
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
  /** Callback to reset column visibility to default (show all) */
  onResetColumnVisibility?: () => void
  /** Extra controls injected at the right side (e.g., Create button, Export) */
  actionSlot?: React.ReactNode
  className?: string
}

/**
 * Reusable table toolbar with search input, filter slot, column visibility toggle,
 * and action slot. Use above <DataTable />.
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
  onResetColumnVisibility,
  filterSlot,
  actionSlot,
  className,
}: DataTableToolbarProps<TData>) => {
  const { t } = useTranslation('common')
  const hasHiddenColumns = showColumnToggle && table.getAllColumns().some((col) => col.getCanHide() && !col.getIsVisible())

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

      {/* Right: column visibility + actions */}
      <div className="flex items-center gap-2">
        {showColumnToggle && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="h-9 cursor-pointer"
                aria-label={t('labels.toggleColumns', 'Toggle columns')}
              >
                <SlidersHorizontal className="mr-1.5 h-4 w-4" />
                {t('labels.columns', 'Columns')}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-[180px]">
              <DropdownMenuLabel>{t('labels.toggleColumns', 'Toggle columns')}</DropdownMenuLabel>
              <DropdownMenuSeparator />
              {table
                .getAllColumns()
                .filter((col) => col.getCanHide())
                .map((col) => (
                  <DropdownMenuCheckboxItem
                    key={col.id}
                    className="cursor-pointer capitalize"
                    checked={col.getIsVisible()}
                    onCheckedChange={(value) => col.toggleVisibility(!!value)}
                    onSelect={(e) => e.preventDefault()}
                  >
                    {col.columnDef.meta?.label
                      ?? (typeof col.columnDef.header === 'string' ? col.columnDef.header : col.id)}
                  </DropdownMenuCheckboxItem>
                ))}
              {hasHiddenColumns && onResetColumnVisibility && (
                <>
                  <DropdownMenuSeparator />
                  <button
                    type="button"
                    className="flex w-full cursor-pointer items-center rounded-sm px-2 py-1.5 text-sm text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                    onClick={() => {
                      onResetColumnVisibility()
                    }}
                  >
                    <RotateCcw className="mr-2 h-3.5 w-3.5" />
                    {t('labels.resetToDefault', 'Reset to default')}
                  </button>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )}

        {actionSlot}
      </div>
    </div>
  )
}
