/**
 * VariantOptionsSelector Component
 *
 * Allows selecting option values when creating or editing a variant.
 * Displays option swatches for color options.
 */
import { useTranslation } from 'react-i18next'
import { Check } from 'lucide-react'
import { Label, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@uikit'

import { cn } from '@/lib/utils'
import { sanitizeColorCode, getContrastMode } from '@/lib/color-utils'
import type { ProductOption } from '@/types/product'

interface VariantOptionsSelectorProps {
  options: ProductOption[]
  selectedValues: Record<string, string>
  onChange: (optionName: string, value: string) => void
  disabled?: boolean
}

export function VariantOptionsSelector({
  options,
  selectedValues,
  onChange,
  disabled = false,
}: VariantOptionsSelectorProps) {
  const { t } = useTranslation()

  if (options.length === 0) {
    return (
      <p className="text-sm text-muted-foreground italic">
        {t('products.variants.noOptionsAvailable')}
      </p>
    )
  }

  return (
    <div className="space-y-4">
      {options.map((option) => {
        const isColorOption = option.name.toLowerCase().includes('color') ||
          option.name.toLowerCase().includes('colour') ||
          option.values.some((v) => v.colorCode)

        return (
          <div key={option.id} className="space-y-2">
            <Label>{option.displayName || option.name}</Label>

            {isColorOption ? (
              // Color swatch selector
              <div className="flex flex-wrap gap-2">
                {option.values.map((value) => {
                  const isSelected = selectedValues[option.name] === value.value
                  return (
                    <button
                      key={value.id}
                      type="button"
                      onClick={() => onChange(option.name, value.value)}
                      disabled={disabled}
                      className={cn(
                        'relative h-10 w-10 rounded-full border-2 transition-all cursor-pointer',
                        'hover:scale-110 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
                        isSelected
                          ? 'border-primary ring-2 ring-primary ring-offset-2'
                          : 'border-muted hover:border-muted-foreground/50',
                        disabled && 'opacity-50 cursor-not-allowed'
                      )}
                      style={{
                        backgroundColor: sanitizeColorCode(value.colorCode),
                      }}
                      title={value.displayValue || value.value}
                      aria-label={`${option.displayName || option.name}: ${value.displayValue || value.value}`}
                      aria-pressed={isSelected}
                    >
                      {isSelected && (
                        <Check
                          className={cn(
                            'absolute inset-0 m-auto h-5 w-5',
                            // Use white check on dark colors, black on light
                            value.colorCode && getContrastMode(sanitizeColorCode(value.colorCode)) === 'light'
                              ? 'text-gray-900'
                              : 'text-white'
                          )}
                        />
                      )}
                    </button>
                  )
                })}
              </div>
            ) : (
              // Standard dropdown selector
              <Select
                value={selectedValues[option.name] || ''}
                onValueChange={(value) => onChange(option.name, value)}
                disabled={disabled}
              >
                <SelectTrigger className="w-full cursor-pointer">
                  <SelectValue
                    placeholder={t('products.variants.selectOption', {
                      option: option.displayName || option.name,
                    })}
                  />
                </SelectTrigger>
                <SelectContent>
                  {option.values.map((value) => (
                    <SelectItem
                      key={value.id}
                      value={value.value}
                      className="cursor-pointer"
                    >
                      {value.displayValue || value.value}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>
        )
      })}
    </div>
  )
}

