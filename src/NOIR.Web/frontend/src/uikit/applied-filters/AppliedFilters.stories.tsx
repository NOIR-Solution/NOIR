import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { AppliedFilters } from '@/components/storefront/AppliedFilters'
import type { AppliedFilter } from '@/types/filter'

// AppliedFilters has no heavy external dependencies — imported directly.

const SAMPLE_FILTERS: AppliedFilter[] = [
  { type: 'checkbox', code: 'category', value: 'electronics', label: 'Electronics' },
  { type: 'color', code: 'color', value: 'blue', label: 'Blue' },
  { type: 'color', code: 'color', value: 'red', label: 'Red' },
  { type: 'checkbox', code: 'brand', value: 'apple', label: 'Apple' },
  { type: 'price', code: 'min', value: '50', label: '$50' },
  { type: 'price', code: 'max', value: '500', label: '$500' },
]

const AppliedFiltersDemo = ({
  initialFilters = [],
}: {
  initialFilters?: AppliedFilter[]
}) => {
  const [filters, setFilters] = useState<AppliedFilter[]>(initialFilters)

  const handleRemove = (filter: AppliedFilter) => {
    setFilters((prev) =>
      prev.filter((f) => !(f.code === filter.code && f.value === filter.value))
    )
  }

  const handleClearAll = () => {
    setFilters([])
  }

  if (filters.length === 0) {
    return (
      <p className="text-sm text-muted-foreground italic">
        No active filters — component renders null when empty.
      </p>
    )
  }

  return (
    <AppliedFilters
      filters={filters}
      onRemove={handleRemove}
      onClearAll={handleClearAll}
    />
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/AppliedFilters',
  component: AppliedFiltersDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Displays active filters as removable chips/badges. ' +
          'Shows a "Clear all" button when more than 1 filter is active. ' +
          'Renders null when no filters are applied.',
      },
    },
  },
} satisfies Meta<typeof AppliedFiltersDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story: 'Multiple active filters. Click X on any badge to remove it. Click "Clear all" to reset.',
      },
    },
  },
  args: {
    initialFilters: SAMPLE_FILTERS,
  },
}

export const SingleFilter: Story = {
  parameters: {
    docs: {
      description: {
        story: 'Only one filter — "Clear all" button is hidden (requires 2+ filters).',
      },
    },
  },
  args: {
    initialFilters: [
      { type: 'checkbox', code: 'category', value: 'electronics', label: 'Electronics' },
    ],
  },
}

export const TwoFilters: Story = {
  parameters: {
    docs: {
      description: {
        story: 'Two filters — minimum needed to show "Clear all" button.',
      },
    },
  },
  args: {
    initialFilters: [
      { type: 'checkbox', code: 'category', value: 'clothing', label: 'Clothing' },
      { type: 'color', code: 'color', value: 'blue', label: 'Blue' },
    ],
  },
}

export const Empty: Story = {
  parameters: {
    docs: {
      description: {
        story: 'No active filters — component renders null (shown as placeholder text in this demo).',
      },
    },
  },
  args: {
    initialFilters: [],
  },
}

export const ManyFilters: Story = {
  parameters: {
    docs: {
      description: {
        story: 'Many active filters wrapping across multiple lines.',
      },
    },
  },
  args: {
    initialFilters: [
      ...SAMPLE_FILTERS,
      { type: 'checkbox', code: 'brand', value: 'samsung', label: 'Samsung' },
      { type: 'checkbox', code: 'size', value: 'm', label: 'M' },
      { type: 'checkbox', code: 'size', value: 'l', label: 'L' },
    ],
  },
}
