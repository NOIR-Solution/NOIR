import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { Label, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@uikit'
import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'

// --- Visual Replica ---
// VariantOptionsSelector uses react-i18next and color utility functions.
// This self-contained demo replicates the visual output without those dependencies.

interface DemoOptionValue {
  id: string
  value: string
  displayValue: string
  colorCode?: string | null
  sortOrder: number
}

interface DemoOption {
  id: string
  name: string
  displayName?: string
  sortOrder: number
  values: DemoOptionValue[]
}

const sanitizeColor = (code?: string | null) => {
  if (!code) return 'transparent'
  return code.startsWith('#') ? code : `#${code}`
}

const isLightColor = (hex: string): boolean => {
  const clean = hex.replace('#', '')
  if (clean.length !== 6) return false
  const r = parseInt(clean.slice(0, 2), 16)
  const g = parseInt(clean.slice(2, 4), 16)
  const b = parseInt(clean.slice(4, 6), 16)
  return (0.299 * r + 0.587 * g + 0.114 * b) / 255 > 0.5
}

interface VariantOptionsSelectorDemoProps {
  options: DemoOption[]
  disabled?: boolean
}

const VariantOptionsSelectorDemo = ({
  options,
  disabled = false,
}: VariantOptionsSelectorDemoProps) => {
  const [selectedValues, setSelectedValues] = useState<Record<string, string>>({})

  const handleChange = (optionName: string, value: string) => {
    setSelectedValues((prev) => ({ ...prev, [optionName]: value }))
  }

  if (options.length === 0) {
    return (
      <p className="text-sm text-muted-foreground italic">No options available for this product.</p>
    )
  }

  return (
    <div className="space-y-4">
      {options.map((option) => {
        const isColorOption =
          option.name.toLowerCase().includes('color') ||
          option.name.toLowerCase().includes('colour') ||
          option.values.some((v) => v.colorCode)

        return (
          <div key={option.id} className="space-y-2">
            <Label>{option.displayName || option.name}</Label>

            {isColorOption ? (
              <div className="flex flex-wrap gap-2">
                {option.values.map((value) => {
                  const isSelected = selectedValues[option.name] === value.value
                  const colorCode = sanitizeColor(value.colorCode)
                  const useWhiteCheck = !isLightColor(colorCode)

                  return (
                    <button
                      key={value.id}
                      type="button"
                      onClick={() => handleChange(option.name, value.value)}
                      disabled={disabled}
                      className={cn(
                        'relative h-10 w-10 rounded-full border-2 transition-all cursor-pointer',
                        'hover:scale-110 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
                        isSelected
                          ? 'border-primary ring-2 ring-primary ring-offset-2'
                          : 'border-muted hover:border-muted-foreground/50',
                        disabled && 'opacity-50 cursor-not-allowed'
                      )}
                      style={{ backgroundColor: colorCode }}
                      title={value.displayValue || value.value}
                      aria-label={`${option.displayName || option.name}: ${value.displayValue || value.value}`}
                      aria-pressed={isSelected}
                    >
                      {isSelected && (
                        <Check
                          className={cn(
                            'absolute inset-0 m-auto h-5 w-5',
                            useWhiteCheck ? 'text-white' : 'text-gray-900'
                          )}
                        />
                      )}
                    </button>
                  )
                })}
              </div>
            ) : (
              <Select
                value={selectedValues[option.name] || ''}
                onValueChange={(value) => handleChange(option.name, value)}
                disabled={disabled}
              >
                <SelectTrigger className="w-full cursor-pointer">
                  <SelectValue placeholder={`Select ${option.displayName || option.name}...`} />
                </SelectTrigger>
                <SelectContent>
                  {option.values.map((value) => (
                    <SelectItem key={value.id} value={value.value} className="cursor-pointer">
                      {value.displayValue || value.value}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>
        )
      })}

      {/* Selection summary */}
      {Object.keys(selectedValues).length > 0 && (
        <div className="mt-2 p-3 rounded-lg bg-muted/50 text-sm text-muted-foreground">
          <p className="font-medium text-foreground mb-1">Selected:</p>
          {Object.entries(selectedValues).map(([key, value]) => (
            <p key={key}>
              {key}: <span className="text-foreground font-medium">{value}</span>
            </p>
          ))}
        </div>
      )}
    </div>
  )
}

// --- Fixtures ---

const COLOR_OPTION: DemoOption = {
  id: 'opt-color',
  name: 'Color',
  displayName: 'Color',
  sortOrder: 0,
  values: [
    { id: 'c1', value: 'Black', displayValue: 'Black', colorCode: '#000000', sortOrder: 0 },
    { id: 'c2', value: 'White', displayValue: 'White', colorCode: '#FFFFFF', sortOrder: 1 },
    { id: 'c3', value: 'Navy', displayValue: 'Navy Blue', colorCode: '#1E3A5F', sortOrder: 2 },
    { id: 'c4', value: 'Red', displayValue: 'Crimson Red', colorCode: '#EF4444', sortOrder: 3 },
    { id: 'c5', value: 'Green', displayValue: 'Forest Green', colorCode: '#166534', sortOrder: 4 },
    { id: 'c6', value: 'Gray', displayValue: 'Slate Gray', colorCode: '#64748B', sortOrder: 5 },
  ],
}

const SIZE_OPTION: DemoOption = {
  id: 'opt-size',
  name: 'Size',
  displayName: 'Size',
  sortOrder: 1,
  values: [
    { id: 's1', value: 'XS', displayValue: 'XS', sortOrder: 0 },
    { id: 's2', value: 'S', displayValue: 'S', sortOrder: 1 },
    { id: 's3', value: 'M', displayValue: 'M', sortOrder: 2 },
    { id: 's4', value: 'L', displayValue: 'L', sortOrder: 3 },
    { id: 's5', value: 'XL', displayValue: 'XL', sortOrder: 4 },
    { id: 's6', value: 'XXL', displayValue: 'XXL', sortOrder: 5 },
  ],
}

const MATERIAL_OPTION: DemoOption = {
  id: 'opt-material',
  name: 'Material',
  displayName: 'Material',
  sortOrder: 2,
  values: [
    { id: 'm1', value: 'cotton', displayValue: '100% Cotton', sortOrder: 0 },
    { id: 'm2', value: 'polyester', displayValue: 'Polyester Blend', sortOrder: 1 },
    { id: 'm3', value: 'linen', displayValue: 'Pure Linen', sortOrder: 2 },
    { id: 'm4', value: 'wool', displayValue: 'Merino Wool', sortOrder: 3 },
  ],
}

// --- Meta ---

const meta = {
  title: 'UIKit/VariantOptionsSelector',
  component: VariantOptionsSelectorDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Allows selecting option values when adding or editing a product variant. ' +
          'Color options automatically render as circular swatches with check overlay. ' +
          'Non-color options render as dropdowns. Selected combination is shown below.',
      },
    },
  },
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 400 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof VariantOptionsSelectorDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: { story: 'Color swatches and size dropdown together.' },
    },
  },
  args: {
    options: [COLOR_OPTION, SIZE_OPTION],
  },
}

export const ColorOnly: Story = {
  parameters: {
    docs: {
      description: { story: 'Only color swatches — includes light colors (white) with inverted check.' },
    },
  },
  args: {
    options: [COLOR_OPTION],
  },
}

export const DropdownsOnly: Story = {
  parameters: {
    docs: {
      description: { story: 'Multiple dropdown selectors — no color swatches.' },
    },
  },
  args: {
    options: [SIZE_OPTION, MATERIAL_OPTION],
  },
}

export const ThreeOptions: Story = {
  parameters: {
    docs: {
      description: { story: 'Three option dimensions — Color, Size, and Material.' },
    },
  },
  args: {
    options: [COLOR_OPTION, SIZE_OPTION, MATERIAL_OPTION],
  },
}

export const Empty: Story = {
  parameters: {
    docs: {
      description: { story: 'No options defined — shows the empty state message.' },
    },
  },
  args: {
    options: [],
  },
}

export const Disabled: Story = {
  parameters: {
    docs: {
      description: { story: 'Disabled state — swatches and dropdowns are non-interactive.' },
    },
  },
  args: {
    options: [COLOR_OPTION, SIZE_OPTION],
    disabled: true,
  },
}
