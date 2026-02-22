import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { FacetCheckbox } from '@/components/storefront/facets/FacetCheckbox'
import type { FilterOption } from '@/types/filter'

// FacetCheckbox has no heavy external dependencies — imported directly.

const BRAND_OPTIONS: FilterOption[] = [
  { value: 'apple', label: 'Apple', count: 15 },
  { value: 'samsung', label: 'Samsung', count: 12 },
  { value: 'sony', label: 'Sony', count: 8 },
  { value: 'nike', label: 'Nike', count: 20 },
  { value: 'adidas', label: 'Adidas', count: 18 },
  { value: 'puma', label: 'Puma', count: 9 },
  { value: 'reebok', label: 'Reebok', count: 6 },
  { value: 'new-balance', label: 'New Balance', count: 11 },
]

const CATEGORY_OPTIONS: FilterOption[] = [
  { value: 'electronics', label: 'Electronics', count: 42 },
  { value: 'clothing', label: 'Clothing', count: 38 },
  { value: 'home', label: 'Home & Garden', count: 25 },
  { value: 'sports', label: 'Sports', count: 18 },
]

// Interactive wrapper for stateful demonstration
const FacetCheckboxDemo = ({
  name,
  options,
  initialSelected = [],
  maxVisible,
}: {
  name: string
  options: FilterOption[]
  initialSelected?: string[]
  maxVisible?: number
}) => {
  const [selected, setSelected] = useState<string[]>(initialSelected)

  return (
    <div>
      <FacetCheckbox
        name={name}
        options={options}
        selectedValues={selected}
        onChange={setSelected}
        maxVisible={maxVisible}
      />
      {selected.length > 0 && (
        <p className="mt-3 text-xs text-muted-foreground">
          Active: {selected.join(', ')}
        </p>
      )}
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/FacetCheckbox',
  component: FacetCheckboxDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Multi-select checkbox filter for storefront filtering. ' +
          'Shows options with product counts and a "Show more/less" toggle when options exceed maxVisible.',
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
} satisfies Meta<typeof FacetCheckboxDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  args: {
    name: 'Brand',
    options: BRAND_OPTIONS,
    maxVisible: 5,
  },
}

export const WithSelections: Story = {
  parameters: {
    docs: {
      description: { story: 'Some options pre-selected.' },
    },
  },
  args: {
    name: 'Brand',
    options: BRAND_OPTIONS,
    initialSelected: ['nike', 'adidas'],
    maxVisible: 5,
  },
}

export const AllVisible: Story = {
  parameters: {
    docs: {
      description: { story: 'maxVisible set high enough that "Show more" is not needed.' },
    },
  },
  args: {
    name: 'Category',
    options: CATEGORY_OPTIONS,
    maxVisible: 10,
  },
}

export const ShowMoreExpanded: Story = {
  parameters: {
    docs: {
      description: { story: 'Shows "Show 3 more" button — click to reveal all 8 options.' },
    },
  },
  args: {
    name: 'Brand',
    options: BRAND_OPTIONS,
    maxVisible: 5,
  },
}

export const FewOptions: Story = {
  parameters: {
    docs: {
      description: { story: 'Less than maxVisible options — no "Show more" button shown.' },
    },
  },
  args: {
    name: 'Category',
    options: CATEGORY_OPTIONS,
    maxVisible: 5,
  },
}

export const AllSelected: Story = {
  parameters: {
    docs: {
      description: { story: 'All options selected.' },
    },
  },
  args: {
    name: 'Category',
    options: CATEGORY_OPTIONS,
    initialSelected: CATEGORY_OPTIONS.map((o) => o.value),
    maxVisible: 10,
  },
}
