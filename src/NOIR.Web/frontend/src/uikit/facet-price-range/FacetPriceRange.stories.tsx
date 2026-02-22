import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { FacetPriceRange } from '@/components/storefront/facets/FacetPriceRange'

// FacetPriceRange has no heavy external dependencies — imported directly.

const FacetPriceRangeDemo = ({
  min,
  max,
  initialMin,
  initialMax,
  currency,
}: {
  min: number
  max: number
  initialMin?: number
  initialMax?: number
  currency?: string
}) => {
  const [selectedMin, setSelectedMin] = useState<number | undefined>(initialMin)
  const [selectedMax, setSelectedMax] = useState<number | undefined>(initialMax)

  return (
    <div>
      <FacetPriceRange
        min={min}
        max={max}
        selectedMin={selectedMin}
        selectedMax={selectedMax}
        onChange={(min, max) => {
          setSelectedMin(min)
          setSelectedMax(max)
        }}
        currency={currency}
      />
      <div className="mt-3 text-xs text-muted-foreground">
        Active filter: {selectedMin !== undefined || selectedMax !== undefined
          ? `${selectedMin ?? min} – ${selectedMax ?? max}`
          : 'none'}
      </div>
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/FacetPriceRange',
  component: FacetPriceRangeDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Price range filter with min/max inputs. ' +
          'Press Enter or click Apply to apply the filter. ' +
          'If min > max, values are automatically swapped. ' +
          'Clear button appears when a value is entered.',
      },
    },
  },
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 280 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof FacetPriceRangeDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  args: {
    min: 0,
    max: 1000,
    currency: '$',
  },
}

export const WithValues: Story = {
  parameters: {
    docs: {
      description: { story: 'Pre-filled with a price range — Clear button visible.' },
    },
  },
  args: {
    min: 0,
    max: 1000,
    initialMin: 50,
    initialMax: 500,
    currency: '$',
  },
}

export const HighPriceRange: Story = {
  parameters: {
    docs: {
      description: { story: 'Luxury goods range — higher price bounds.' },
    },
  },
  args: {
    min: 100,
    max: 50000,
    currency: '$',
  },
}

export const VietnamDong: Story = {
  parameters: {
    docs: {
      description: { story: 'Vietnamese Dong currency — no decimal places needed.' },
    },
  },
  args: {
    min: 50000,
    max: 5000000,
    currency: '₫',
  },
}

export const NarrowRange: Story = {
  parameters: {
    docs: {
      description: { story: 'Narrow range — min and max are close together.' },
    },
  },
  args: {
    min: 10,
    max: 50,
    initialMin: 15,
    initialMax: 40,
    currency: '€',
  },
}
