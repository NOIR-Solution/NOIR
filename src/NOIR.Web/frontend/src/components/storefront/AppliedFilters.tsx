import { X } from 'lucide-react'
import { Badge, Button } from '@uikit'

import { cn } from '@/lib/utils'
import type { AppliedFilter } from '@/types/filter'

export interface AppliedFiltersProps {
  /** List of currently applied filters */
  filters: AppliedFilter[]
  /** Callback when a single filter is removed */
  onRemove: (filter: AppliedFilter) => void
  /** Callback when all filters are cleared */
  onClearAll: () => void
  /** Optional className for the container */
  className?: string
}

/**
 * Displays active filters as removable chips/badges
 * Shows individual filter badges with remove buttons and a "Clear all" button
 */
export const AppliedFilters = ({
  filters,
  onRemove,
  onClearAll,
  className,
}: AppliedFiltersProps) => {
  if (filters.length === 0) {
    return null
  }

  return (
    <div
      className={cn('flex flex-wrap items-center gap-2', className)}
      role="region"
      aria-label="Applied filters"
    >
      {filters.map((filter) => {
        const filterKey = `${filter.code}-${filter.value}`

        return (
          <Badge
            key={filterKey}
            variant="secondary"
            className="gap-1 pr-1 group"
          >
            <span className="text-xs">
              <span className="text-muted-foreground">{filter.code}:</span>{' '}
              {filter.label}
            </span>
            <button
              type="button"
              onClick={() => onRemove(filter)}
              className={cn(
                'ml-1 rounded-full p-0.5 cursor-pointer',
                'hover:bg-muted-foreground/20 transition-colors',
                'focus:outline-none focus-visible:ring-2 focus-visible:ring-ring'
              )}
              aria-label={`Remove ${filter.label} filter`}
            >
              <X className="size-3" aria-hidden="true" />
            </button>
          </Badge>
        )
      })}

      {filters.length > 1 && (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onClearAll}
          className="h-6 px-2 text-xs text-muted-foreground hover:text-foreground"
        >
          Clear all
        </Button>
      )}
    </div>
  )
}
