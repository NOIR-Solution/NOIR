import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Filter, X } from 'lucide-react'
import {
  Badge,
  Button,
  Checkbox,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaTrigger,
} from '@uikit'
import { cn } from '@/lib/utils'
import type { ProductAttribute } from '@/types/productAttribute'

interface AttributeFilterDialogProps {
  attributes: ProductAttribute[]
  activeFilters?: Record<string, string[]>
  onApply: (filters: Record<string, string[]> | undefined) => void
}

export const AttributeFilterDialog = ({
  attributes,
  activeFilters,
  onApply,
}: AttributeFilterDialogProps) => {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)

  // Draft selections — only applied when user clicks "Apply"
  const [draft, setDraft] = useState<Record<string, Set<string>>>({})

  // Sync draft from active filters when dialog opens
  useEffect(() => {
    if (open) {
      const initial: Record<string, Set<string>> = {}
      if (activeFilters) {
        for (const [code, values] of Object.entries(activeFilters)) {
          initial[code] = new Set(values)
        }
      }
      setDraft(initial)
    }
  }, [open, activeFilters])

  const toggleValue = (attrCode: string, displayValue: string) => {
    setDraft(prev => {
      const next = { ...prev }
      const values = new Set(next[attrCode] || [])
      if (values.has(displayValue)) {
        values.delete(displayValue)
      } else {
        values.add(displayValue)
      }
      if (values.size === 0) {
        delete next[attrCode]
      } else {
        next[attrCode] = values
      }
      return next
    })
  }

  const totalSelected = Object.values(draft).reduce((sum, set) => sum + set.size, 0)

  const handleApply = () => {
    if (totalSelected === 0) {
      onApply(undefined)
    } else {
      const filters: Record<string, string[]> = {}
      for (const [code, values] of Object.entries(draft)) {
        if (values.size > 0) {
          filters[code] = Array.from(values)
        }
      }
      onApply(filters)
    }
    setOpen(false)
  }

  const handleClear = () => {
    setDraft({})
  }

  // Count of currently active API filters (for the trigger button)
  const activeCount = activeFilters
    ? Object.values(activeFilters).reduce((sum, arr) => sum + arr.length, 0)
    : 0

  return (
    <Credenza open={open} onOpenChange={setOpen}>
      <CredenzaTrigger asChild>
        <Button
          variant="outline"
          className="w-full sm:w-auto h-9 cursor-pointer transition-all duration-200 hover:border-primary/50"
        >
          <Filter className="h-4 w-4 mr-2" />
          {activeCount > 0
            ? `${t('products.filterByAttribute', 'Filter by Attribute')}: ${activeCount} ${t('labels.selected', 'selected')}`
            : t('products.filterByAttribute', 'Filter by Attribute')}
          {activeCount > 0 && (
            <X
              className="h-3 w-3 ml-2 hover:text-destructive cursor-pointer"
              onClick={(e) => {
                e.stopPropagation()
                onApply(undefined)
              }}
            />
          )}
        </Button>
      </CredenzaTrigger>
      <CredenzaContent className="sm:max-w-lg">
        <CredenzaHeader>
          <CredenzaTitle>{t('products.filterByAttribute', 'Filter by Attribute')}</CredenzaTitle>
          <CredenzaDescription>
            {t('products.filterByAttributeDescription', 'Select values across attributes to filter products')}
          </CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          <div className="space-y-5">
            {attributes.map(attr => {
              const selected = draft[attr.code] || new Set<string>()
              const activeValues = attr.values.filter(v => v.isActive)

              return (
                <div key={attr.id} className="space-y-2.5">
                  {/* Attribute header */}
                  <div className="flex items-center justify-between">
                    <h4 className="text-sm font-medium text-foreground">{attr.name}</h4>
                    {selected.size > 0 && (
                      <Badge variant="secondary" className="text-xs">
                        {selected.size} {t('labels.selected', 'selected')}
                      </Badge>
                    )}
                  </div>

                  {/* Attribute values — pill toggle or checkbox list based on type */}
                  {attr.type === 'Select' && activeValues.every(v => v.colorCode) ? (
                    // Color attributes: render as color swatches
                    <div className="flex flex-wrap gap-2">
                      {activeValues.map(value => {
                        const isChecked = selected.has(value.displayValue)
                        return (
                          <button
                            key={value.id}
                            type="button"
                            className={cn(
                              'flex items-center gap-2 px-3 py-1.5 rounded-full border text-sm cursor-pointer transition-all',
                              isChecked
                                ? 'bg-primary/10 border-primary ring-1 ring-primary/30'
                                : 'bg-background border-border hover:bg-accent'
                            )}
                            onClick={() => toggleValue(attr.code, value.displayValue)}
                          >
                            <span
                              className="w-4 h-4 rounded-full border border-border/60 shrink-0"
                              style={{ backgroundColor: value.colorCode ?? undefined }}
                            />
                            <span className="text-sm">{value.displayValue}</span>
                          </button>
                        )
                      })}
                    </div>
                  ) : (
                    // Other attributes: render as checkbox list
                    <div className="flex flex-wrap gap-2">
                      {activeValues.map(value => {
                        const isChecked = selected.has(value.displayValue)
                        return (
                          <button
                            key={value.id}
                            type="button"
                            className={cn(
                              'flex items-center gap-2 px-3 py-1.5 rounded-md border text-sm cursor-pointer transition-all',
                              isChecked
                                ? 'bg-primary/10 border-primary text-primary'
                                : 'bg-background border-border text-foreground hover:bg-accent'
                            )}
                            onClick={() => toggleValue(attr.code, value.displayValue)}
                          >
                            <Checkbox
                              checked={isChecked}
                              tabIndex={-1}
                              className="pointer-events-none h-3.5 w-3.5"
                              aria-hidden
                            />
                            <span>{value.displayValue}</span>
                          </button>
                        )
                      })}
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </CredenzaBody>
        <CredenzaFooter className="flex justify-between sm:justify-between">
          <Button
            variant="ghost"
            onClick={handleClear}
            disabled={totalSelected === 0}
            className="cursor-pointer"
          >
            {t('products.clearAllFilters', 'Clear All')}
          </Button>
          <Button onClick={handleApply} className="cursor-pointer">
            {totalSelected > 0
              ? `${t('buttons.apply', 'Apply')} (${totalSelected})`
              : t('buttons.apply', 'Apply')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
