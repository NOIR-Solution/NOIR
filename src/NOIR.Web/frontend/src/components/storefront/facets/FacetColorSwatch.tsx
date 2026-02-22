import { useTranslation } from 'react-i18next'
import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { FilterOption } from '@/types/filter'

export interface FacetColorSwatchProps {
  /** Display name for the facet */
  name: string
  /** Available color options */
  options: FilterOption[]
  /** Currently selected values */
  selectedValues: string[]
  /** Callback when selection changes */
  onChange: (values: string[]) => void
  /** Optional className for the container */
  className?: string
}

/**
 * Color swatch filter component for storefront filtering
 * Shows circular buttons with color backgrounds and selection state
 */
export const FacetColorSwatch = ({
  name,
  options,
  selectedValues,
  onChange,
  className,
}: FacetColorSwatchProps) => {
  const { t } = useTranslation('common')
  const handleToggle = (value: string) => {
    const newValues = selectedValues.includes(value)
      ? selectedValues.filter((v) => v !== value)
      : [...selectedValues, value]
    onChange(newValues)
  }

  // Determine if a color is light (for contrast)
  const isLightColor = (hexColor: string | undefined): boolean => {
    if (!hexColor) return false
    const hex = hexColor.replace('#', '')
    const r = parseInt(hex.slice(0, 2), 16)
    const g = parseInt(hex.slice(2, 4), 16)
    const b = parseInt(hex.slice(4, 6), 16)
    // Using relative luminance formula
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
    return luminance > 0.6
  }

  return (
    <div className={cn('space-y-3', className)}>
      <h4 className="text-sm font-medium text-foreground">{name}</h4>
      <div className="flex flex-wrap gap-2">
        {options.map((option) => {
          const isSelected = selectedValues.includes(option.value)
          const colorCode = option.colorCode || '#e5e7eb'
          const isLight = isLightColor(colorCode)

          return (
            <button
              key={option.value}
              type="button"
              onClick={() => handleToggle(option.value)}
              title={`${option.label} (${option.count})`}
              aria-label={t('storefront.filterByColor', { color: option.label, defaultValue: `Filter by color ${option.label}` })}
              aria-pressed={isSelected}
              className={cn(
                'relative size-8 rounded-full border-2 transition-all cursor-pointer',
                'focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
                isSelected
                  ? 'ring-2 ring-primary ring-offset-2 border-primary'
                  : 'border-border hover:border-primary/50'
              )}
              style={{ backgroundColor: colorCode }}
            >
              {isSelected && (
                <span
                  className={cn(
                    'absolute inset-0 flex items-center justify-center',
                    isLight ? 'text-gray-800' : 'text-white'
                  )}
                >
                  <Check className="size-4" aria-hidden="true" />
                </span>
              )}
              <span className="sr-only">
                {option.label} ({option.count} products)
              </span>
            </button>
          )
        })}
      </div>

      {/* Optional: Show selected color names */}
      {selectedValues.length > 0 && (
        <div className="text-xs text-muted-foreground">
          Selected:{' '}
          {selectedValues
            .map((v) => options.find((o) => o.value === v)?.label || v)
            .join(', ')}
        </div>
      )}
    </div>
  )
}
