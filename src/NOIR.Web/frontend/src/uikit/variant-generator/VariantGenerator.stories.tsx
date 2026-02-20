import type { Meta, StoryObj } from 'storybook'
import { useState, useMemo } from 'react'
import { Wand2, AlertTriangle, Check } from 'lucide-react'
import {
  Alert,
  AlertDescription,
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  ScrollArea,
} from '@uikit'

// --- Visual Replica ---
// VariantGenerator depends on react-i18next, ProductOption/ProductVariant types,
// and API hooks. This self-contained demo replicates the visual appearance and
// variant generation logic without requiring those external contexts.

interface DemoOptionValue {
  value: string
  displayValue?: string
}

interface DemoOption {
  name: string
  values: DemoOptionValue[]
}

interface DemoExistingVariant {
  name: string
  options: Record<string, string>
}

// --- Demo Component ---

interface VariantGeneratorDemoProps {
  options: DemoOption[]
  existingVariants?: DemoExistingVariant[]
  basePrice?: number
  /** Start with the dialog already open */
  defaultOpen?: boolean
}

const VariantGeneratorDemo = ({
  options,
  existingVariants = [],
  basePrice = 29.99,
  defaultOpen = true,
}: VariantGeneratorDemoProps) => {
  const [isOpen, setIsOpen] = useState(defaultOpen)
  const [defaultPrice, setDefaultPrice] = useState(basePrice.toString())
  const [defaultStock, setDefaultStock] = useState('0')
  const [selectedCombinations, setSelectedCombinations] = useState<Set<string>>(new Set())
  const [isGenerating, setIsGenerating] = useState(false)
  const [generated, setGenerated] = useState(false)

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
  const newCombinations = useMemo(
    () => allCombinations.filter((c) => !existingKeys.has(c.key)),
    [allCombinations, existingKeys]
  )

  const handleOpen = () => {
    setSelectedCombinations(new Set(newCombinations.map((c) => c.key)))
    setDefaultPrice(basePrice.toString())
    setDefaultStock('0')
    setGenerated(false)
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

  const handleGenerate = () => {
    if (selectedCombinations.size === 0) return

    setIsGenerating(true)
    // Simulate generation
    setTimeout(() => {
      setIsGenerating(false)
      setGenerated(true)
    }, 800)
  }

  const canGenerate = options.length > 0 && options.every((o) => o.values.length > 0)

  return (
    <>
      <Button
        variant="outline"
        onClick={handleOpen}
        disabled={!canGenerate}
        className="cursor-pointer"
      >
        <Wand2 className="h-4 w-4 mr-2" />
        Generate Variants
      </Button>

      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="sm:max-w-[600px]">
          <DialogHeader>
            <DialogTitle>Generate Variants</DialogTitle>
            <DialogDescription>
              Auto-generate variants from all combinations of your product options. Set default
              pricing and stock levels below.
            </DialogDescription>
          </DialogHeader>

          {generated ? (
            <div className="py-8 text-center space-y-3">
              <div className="mx-auto w-12 h-12 rounded-full bg-green-100 dark:bg-green-950/30 flex items-center justify-center">
                <Check className="h-6 w-6 text-green-600 dark:text-green-400" />
              </div>
              <div>
                <p className="font-medium text-foreground">
                  {selectedCombinations.size} variants generated!
                </p>
                <p className="text-sm text-muted-foreground mt-1">
                  Variants have been created with default price ${parseFloat(defaultPrice).toFixed(2)} and
                  stock {defaultStock || '0'}.
                </p>
              </div>
              <Button onClick={() => setIsOpen(false)} className="cursor-pointer mt-2">
                Done
              </Button>
            </div>
          ) : (
            <>
              <div className="space-y-4 py-4">
                {/* Default values */}
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Default Price</Label>
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      value={defaultPrice}
                      onChange={(e) => setDefaultPrice(e.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>Default Stock</Label>
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
                      {existingKeys.size} combination{existingKeys.size > 1 ? 's' : ''} already
                      exist as variants and will be skipped.
                    </AlertDescription>
                  </Alert>
                )}

                {/* Combination selection */}
                {newCombinations.length > 0 ? (
                  <>
                    <div className="flex items-center justify-between">
                      <Label>
                        Select combinations ({selectedCombinations.size}/{newCombinations.length})
                      </Label>
                      <div className="space-x-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={handleSelectAll}
                          className="cursor-pointer"
                        >
                          Select All
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={handleSelectNone}
                          className="cursor-pointer"
                        >
                          Select None
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
                            <label htmlFor={combo.key} className="flex-1 text-sm cursor-pointer">
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
                      All possible combinations already exist as variants.
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
                  Cancel
                </Button>
                <Button
                  onClick={handleGenerate}
                  disabled={isGenerating || selectedCombinations.size === 0}
                  className="cursor-pointer"
                >
                  {isGenerating
                    ? 'Generating...'
                    : `Generate ${selectedCombinations.size} variant${selectedCombinations.size !== 1 ? 's' : ''}`}
                </Button>
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>
    </>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/VariantGenerator',
  component: VariantGeneratorDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
} satisfies Meta<typeof VariantGeneratorDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Variant generator with Size and Color options. ' +
          'Select Size (S, M, L) and Color (Red, Blue) to generate 6 variant combinations. ' +
          'This is a visual replica — the real component uses react-i18next and API hooks.',
      },
    },
  },
  args: {
    options: [
      {
        name: 'Size',
        values: [
          { value: 'S', displayValue: 'Small' },
          { value: 'M', displayValue: 'Medium' },
          { value: 'L', displayValue: 'Large' },
        ],
      },
      {
        name: 'Color',
        values: [
          { value: 'Red', displayValue: 'Red' },
          { value: 'Blue', displayValue: 'Blue' },
        ],
      },
    ],
    basePrice: 29.99,
  },
}

export const WithAttributes: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Three attribute axes (Size, Color, Material) producing 18 combinations. ' +
          'Demonstrates the scrollable combination list.',
      },
    },
  },
  args: {
    options: [
      {
        name: 'Size',
        values: [
          { value: 'S', displayValue: 'Small' },
          { value: 'M', displayValue: 'Medium' },
          { value: 'L', displayValue: 'Large' },
        ],
      },
      {
        name: 'Color',
        values: [
          { value: 'Red', displayValue: 'Red' },
          { value: 'Blue', displayValue: 'Blue' },
          { value: 'Green', displayValue: 'Green' },
        ],
      },
      {
        name: 'Material',
        values: [
          { value: 'Cotton', displayValue: 'Cotton' },
          { value: 'Polyester', displayValue: 'Polyester' },
        ],
      },
    ],
    basePrice: 49.99,
  },
}

export const Generated: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Shows the scenario where some combinations already exist as variants. ' +
          'A warning is displayed and existing combinations are excluded from the list.',
      },
    },
  },
  args: {
    options: [
      {
        name: 'Size',
        values: [
          { value: 'S', displayValue: 'Small' },
          { value: 'M', displayValue: 'Medium' },
          { value: 'L', displayValue: 'Large' },
        ],
      },
      {
        name: 'Color',
        values: [
          { value: 'Red', displayValue: 'Red' },
          { value: 'Blue', displayValue: 'Blue' },
        ],
      },
    ],
    existingVariants: [
      { name: 'Small / Red', options: { Size: 'S', Color: 'Red' } },
      { name: 'Medium / Red', options: { Size: 'M', Color: 'Red' } },
      { name: 'Large / Red', options: { Size: 'L', Color: 'Red' } },
    ],
    basePrice: 29.99,
  },
}

export const Empty: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'When no product options are defined, the generator button is disabled. ' +
          'Options must be configured before variants can be generated.',
      },
    },
  },
  args: {
    options: [],
    defaultOpen: false,
  },
}
