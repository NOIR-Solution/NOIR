/**
 * VariantGenerator Component
 *
 * Auto-generates variants from all combinations of product options.
 * Allows setting default price and stock for generated variants.
 */
import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Wand2, AlertTriangle, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Alert,
  AlertDescription,
} from '@/components/ui/alert'
import { ScrollArea } from '@/components/ui/scroll-area'
import type { ProductOption, ProductVariant } from '@/types/product'

interface VariantGeneratorProps {
  options: ProductOption[]
  existingVariants: ProductVariant[]
  basePrice: number
  onGenerate: (variants: GeneratedVariant[]) => Promise<void>
  disabled?: boolean
}

export interface GeneratedVariant {
  name: string
  options: Record<string, string>
  price: number
  stockQuantity: number
}

export function VariantGenerator({
  options,
  existingVariants,
  basePrice,
  onGenerate,
  disabled = false,
}: VariantGeneratorProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const [defaultPrice, setDefaultPrice] = useState(basePrice.toString())
  const [defaultStock, setDefaultStock] = useState('0')
  const [selectedCombinations, setSelectedCombinations] = useState<Set<string>>(new Set())
  const [isGenerating, setIsGenerating] = useState(false)

  // Generate all possible combinations
  const allCombinations = useMemo(() => {
    if (options.length === 0) return []

    const combinations: { name: string; options: Record<string, string>; key: string }[] = []

    const generateCombos = (
      optionIndex: number,
      currentOptions: Record<string, string>,
      nameParts: string[]
    ) => {
      if (optionIndex >= options.length) {
        const key = Object.entries(currentOptions)
          .sort(([a], [b]) => a.localeCompare(b))
          .map(([k, v]) => `${k}:${v}`)
          .join('|')
        combinations.push({
          name: nameParts.join(' / '),
          options: { ...currentOptions },
          key,
        })
        return
      }

      const option = options[optionIndex]
      for (const value of option.values) {
        generateCombos(
          optionIndex + 1,
          { ...currentOptions, [option.name]: value.value },
          [...nameParts, value.displayValue || value.value]
        )
      }
    }

    generateCombos(0, {}, [])
    return combinations
  }, [options])

  // Check which combinations already exist as variants
  const existingKeys = useMemo(() => {
    const keys = new Set<string>()
    for (const variant of existingVariants) {
      if (variant.options) {
        const key = Object.entries(variant.options)
          .sort(([a], [b]) => a.localeCompare(b))
          .map(([k, v]) => `${k}:${v}`)
          .join('|')
        keys.add(key)
      }
    }
    return keys
  }, [existingVariants])

  // New combinations that don't exist yet
  const newCombinations = useMemo(() => {
    return allCombinations.filter((c) => !existingKeys.has(c.key))
  }, [allCombinations, existingKeys])

  // Initialize selection with all new combinations when dialog opens
  const handleOpen = () => {
    setSelectedCombinations(new Set(newCombinations.map((c) => c.key)))
    setDefaultPrice(basePrice.toString())
    setDefaultStock('0')
    setIsOpen(true)
  }

  const handleToggle = (key: string) => {
    const next = new Set(selectedCombinations)
    if (next.has(key)) {
      next.delete(key)
    } else {
      next.add(key)
    }
    setSelectedCombinations(next)
  }

  const handleSelectAll = () => {
    setSelectedCombinations(new Set(newCombinations.map((c) => c.key)))
  }

  const handleSelectNone = () => {
    setSelectedCombinations(new Set())
  }

  const handleGenerate = async () => {
    if (selectedCombinations.size === 0) return

    setIsGenerating(true)
    try {
      const variants: GeneratedVariant[] = newCombinations
        .filter((c) => selectedCombinations.has(c.key))
        .map((c) => ({
          name: c.name,
          options: c.options,
          price: parseFloat(defaultPrice) || basePrice,
          stockQuantity: parseInt(defaultStock) || 0,
        }))

      await onGenerate(variants)
      setIsOpen(false)
    } finally {
      setIsGenerating(false)
    }
  }

  const canGenerate = options.length > 0 && options.every((o) => o.values.length > 0)

  return (
    <>
      <Button
        variant="outline"
        onClick={handleOpen}
        disabled={disabled || !canGenerate}
        className="cursor-pointer"
      >
        <Wand2 className="h-4 w-4 mr-2" />
        {t('products.variants.generateVariants')}
      </Button>

      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="sm:max-w-[600px]">
          <DialogHeader>
            <DialogTitle>{t('products.variants.generateTitle')}</DialogTitle>
            <DialogDescription>
              {t('products.variants.generateDescription')}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {/* Default values */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>{t('products.variants.defaultPrice')}</Label>
                <Input
                  type="number"
                  step="0.01"
                  min="0"
                  value={defaultPrice}
                  onChange={(e) => setDefaultPrice(e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label>{t('products.variants.defaultStock')}</Label>
                <Input
                  type="number"
                  step="1"
                  min="0"
                  value={defaultStock}
                  onChange={(e) => setDefaultStock(e.target.value)}
                />
              </div>
            </div>

            {/* Warning if some already exist */}
            {existingKeys.size > 0 && (
              <Alert>
                <AlertTriangle className="h-4 w-4" />
                <AlertDescription>
                  {t('products.variants.someExist', { count: existingKeys.size })}
                </AlertDescription>
              </Alert>
            )}

            {/* Combination selection */}
            {newCombinations.length > 0 ? (
              <>
                <div className="flex items-center justify-between">
                  <Label>
                    {t('products.variants.selectCombinations')} ({selectedCombinations.size}/{newCombinations.length})
                  </Label>
                  <div className="space-x-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={handleSelectAll}
                      className="cursor-pointer"
                    >
                      {t('buttons.selectAll')}
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={handleSelectNone}
                      className="cursor-pointer"
                    >
                      {t('buttons.selectNone')}
                    </Button>
                  </div>
                </div>

                <ScrollArea className="h-[200px] rounded-md border p-2">
                  <div className="space-y-2">
                    {newCombinations.map((combo) => (
                      <div
                        key={combo.key}
                        className="flex items-center space-x-2 rounded-md p-2 hover:bg-muted/50"
                      >
                        <Checkbox
                          id={combo.key}
                          checked={selectedCombinations.has(combo.key)}
                          onCheckedChange={() => handleToggle(combo.key)}
                          className="cursor-pointer"
                        />
                        <label
                          htmlFor={combo.key}
                          className="flex-1 text-sm cursor-pointer"
                        >
                          {combo.name}
                        </label>
                      </div>
                    ))}
                  </div>
                </ScrollArea>
              </>
            ) : (
              <Alert>
                <Check className="h-4 w-4" />
                <AlertDescription>
                  {t('products.variants.allCombinationsExist')}
                </AlertDescription>
              </Alert>
            )}
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsOpen(false)}
              className="cursor-pointer"
            >
              {t('buttons.cancel')}
            </Button>
            <Button
              onClick={handleGenerate}
              disabled={isGenerating || selectedCombinations.size === 0}
              className="cursor-pointer"
            >
              {isGenerating
                ? t('buttons.generating')
                : t('products.variants.generateCount', { count: selectedCombinations.size })}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
