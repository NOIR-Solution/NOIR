import * as React from 'react'
import { Checkbox, Label } from '@uikit'

import { cn } from '@/lib/utils'
import type { FilterOption } from '@/types/filter'

export interface FacetCheckboxProps {
  /** Display name for the facet */
  name: string
  /** Available options to select from */
  options: FilterOption[]
  /** Currently selected values */
  selectedValues: string[]
  /** Callback when selection changes */
  onChange: (values: string[]) => void
  /** Maximum options to show before "Show more" */
  maxVisible?: number
  /** Optional className for the container */
  className?: string
}

/**
 * Multi-select checkbox filter component for storefront filtering
 * Shows options with product counts and supports "Show more/less"
 */
export function FacetCheckbox({
  name,
  options,
  selectedValues,
  onChange,
  maxVisible = 5,
  className,
}: FacetCheckboxProps) {
  const [showAll, setShowAll] = React.useState(false)

  const visibleOptions = showAll ? options : options.slice(0, maxVisible)
  const hasMore = options.length > maxVisible

  const handleToggle = (value: string) => {
    const newValues = selectedValues.includes(value)
      ? selectedValues.filter((v) => v !== value)
      : [...selectedValues, value]
    onChange(newValues)
  }

  return (
    <div className={cn('space-y-3', className)}>
      <h4 className="text-sm font-medium text-foreground">{name}</h4>
      <div className="space-y-2">
        {visibleOptions.map((option) => {
          const isSelected = selectedValues.includes(option.value)
          const checkboxId = `facet-${name}-${option.value}`

          return (
            <div
              key={option.value}
              className="flex items-center justify-between gap-2"
            >
              <div className="flex items-center gap-2 min-w-0">
                <Checkbox
                  id={checkboxId}
                  checked={isSelected}
                  onCheckedChange={() => handleToggle(option.value)}
                  aria-label={`Filter by ${option.label}`}
                />
                <Label
                  htmlFor={checkboxId}
                  className="text-sm font-normal cursor-pointer truncate"
                >
                  {option.label}
                </Label>
              </div>
              <span className="text-xs text-muted-foreground shrink-0">
                ({option.count})
              </span>
            </div>
          )
        })}
      </div>

      {hasMore && (
        <button
          type="button"
          onClick={() => setShowAll(!showAll)}
          className="text-sm text-primary hover:underline cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 rounded-sm"
        >
          {showAll
            ? `Show less`
            : `Show ${options.length - maxVisible} more`}
        </button>
      )}
    </div>
  )
}
