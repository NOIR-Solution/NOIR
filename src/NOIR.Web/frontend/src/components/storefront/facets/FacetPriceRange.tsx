import * as React from 'react'
import { Button, Input, Label } from '@uikit'

import { cn } from '@/lib/utils'

export interface FacetPriceRangeProps {
  /** Minimum price available */
  min: number
  /** Maximum price available */
  max: number
  /** Currently selected minimum */
  selectedMin?: number
  /** Currently selected maximum */
  selectedMax?: number
  /** Callback when price range changes */
  onChange: (min?: number, max?: number) => void
  /** Currency symbol to display */
  currency?: string
  /** Optional className for the container */
  className?: string
}

/**
 * Price range filter component with min/max inputs
 * Uses shadcn Input components and supports currency display
 */
export const FacetPriceRange = ({
  min,
  max,
  selectedMin,
  selectedMax,
  onChange,
  currency = '$',
  className,
}: FacetPriceRangeProps) => {
  const [localMin, setLocalMin] = React.useState<string>(
    selectedMin?.toString() || ''
  )
  const [localMax, setLocalMax] = React.useState<string>(
    selectedMax?.toString() || ''
  )

  // Sync local state with props when they change externally
  React.useEffect(() => {
    setLocalMin(selectedMin?.toString() || '')
  }, [selectedMin])

  React.useEffect(() => {
    setLocalMax(selectedMax?.toString() || '')
  }, [selectedMax])

  const handleApply = () => {
    const minVal = localMin ? parseFloat(localMin) : undefined
    const maxVal = localMax ? parseFloat(localMax) : undefined

    // Validate values
    if (minVal !== undefined && maxVal !== undefined && minVal > maxVal) {
      // Swap if min > max
      onChange(maxVal, minVal)
      setLocalMin(maxVal.toString())
      setLocalMax(minVal.toString())
      return
    }

    onChange(minVal, maxVal)
  }

  const handleClear = () => {
    setLocalMin('')
    setLocalMax('')
    onChange(undefined, undefined)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleApply()
    }
  }

  const hasValue = localMin !== '' || localMax !== ''

  return (
    <div className={cn('space-y-3', className)}>
      <h4 className="text-sm font-medium text-foreground">Price Range</h4>

      <div className="flex items-center gap-2">
        <div className="flex-1">
          <Label htmlFor="price-min" className="sr-only">
            Minimum price
          </Label>
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground text-sm">
              {currency}
            </span>
            <Input
              id="price-min"
              type="number"
              placeholder={min.toString()}
              value={localMin}
              onChange={(e) => setLocalMin(e.target.value)}
              onKeyDown={handleKeyDown}
              min={0}
              className="pl-7"
              aria-label={`Minimum price in ${currency}`}
            />
          </div>
        </div>

        <span className="text-muted-foreground text-sm">to</span>

        <div className="flex-1">
          <Label htmlFor="price-max" className="sr-only">
            Maximum price
          </Label>
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground text-sm">
              {currency}
            </span>
            <Input
              id="price-max"
              type="number"
              placeholder={max.toString()}
              value={localMax}
              onChange={(e) => setLocalMax(e.target.value)}
              onKeyDown={handleKeyDown}
              min={0}
              className="pl-7"
              aria-label={`Maximum price in ${currency}`}
            />
          </div>
        </div>
      </div>

      <div className="flex items-center gap-2">
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={handleApply}
          className="flex-1"
        >
          Apply
        </Button>
        {hasValue && (
          <Button
            type="button"
            size="sm"
            variant="ghost"
            onClick={handleClear}
            className="flex-1"
          >
            Clear
          </Button>
        )}
      </div>

      <p className="text-xs text-muted-foreground">
        Range: {currency}
        {min.toLocaleString()} - {currency}
        {max.toLocaleString()}
      </p>
    </div>
  )
}
