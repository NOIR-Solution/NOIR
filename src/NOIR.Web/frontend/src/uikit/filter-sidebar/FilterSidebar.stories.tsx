import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { FilterSidebar } from '@/components/storefront/FilterSidebar'
import type { AppliedFilter, AvailableFilters, FilterState } from '@/types/filter'

// FilterSidebar has no heavy external dependencies — imported directly.

const AVAILABLE_FILTERS: AvailableFilters = {
  facets: [
    {
      code: 'category',
      name: 'Category',
      type: 'checkbox',
      options: [
        { value: 'electronics', label: 'Electronics', count: 42 },
        { value: 'clothing', label: 'Clothing', count: 38 },
        { value: 'home', label: 'Home & Garden', count: 25 },
        { value: 'sports', label: 'Sports', count: 18 },
        { value: 'toys', label: 'Toys & Games', count: 12 },
        { value: 'books', label: 'Books', count: 45 },
      ],
    },
    {
      code: 'brand',
      name: 'Brand',
      type: 'checkbox',
      options: [
        { value: 'apple', label: 'Apple', count: 15 },
        { value: 'samsung', label: 'Samsung', count: 12 },
        { value: 'sony', label: 'Sony', count: 8 },
        { value: 'nike', label: 'Nike', count: 20 },
        { value: 'adidas', label: 'Adidas', count: 18 },
      ],
    },
    {
      code: 'color',
      name: 'Color',
      type: 'color',
      options: [
        { value: 'black', label: 'Black', count: 45, colorCode: '#000000' },
        { value: 'white', label: 'White', count: 38, colorCode: '#FFFFFF' },
        { value: 'red', label: 'Red', count: 22, colorCode: '#EF4444' },
        { value: 'blue', label: 'Blue', count: 30, colorCode: '#3B82F6' },
        { value: 'green', label: 'Green', count: 18, colorCode: '#22C55E' },
        { value: 'yellow', label: 'Yellow', count: 12, colorCode: '#EAB308' },
        { value: 'purple', label: 'Purple', count: 15, colorCode: '#A855F7' },
        { value: 'orange', label: 'Orange', count: 10, colorCode: '#F97316' },
      ],
    },
    {
      code: 'size',
      name: 'Size',
      type: 'checkbox',
      options: [
        { value: 'xs', label: 'XS', count: 8 },
        { value: 's', label: 'S', count: 15 },
        { value: 'm', label: 'M', count: 22 },
        { value: 'l', label: 'L', count: 18 },
        { value: 'xl', label: 'XL', count: 12 },
        { value: 'xxl', label: 'XXL', count: 5 },
      ],
    },
  ],
  minPrice: 0,
  maxPrice: 1000,
  currency: 'USD',
}

const APPLIED_FILTERS: AppliedFilter[] = [
  { type: 'checkbox', code: 'category', value: 'electronics', label: 'Electronics' },
  { type: 'color', code: 'color', value: 'blue', label: 'Blue' },
  { type: 'price', code: 'min', value: '50', label: '$50' },
]

const FilterSidebarDemo = ({
  appliedFilters = [],
  availableFilters,
  isLoading = false,
}: {
  appliedFilters?: AppliedFilter[]
  availableFilters?: AvailableFilters | null
  isLoading?: boolean
}) => {
  const [applied, setApplied] = useState<AppliedFilter[]>(appliedFilters)
  const [filterState, setFilterState] = useState<FilterState | null>(null)

  return (
    <div className="flex gap-8">
      <FilterSidebar
        appliedFilters={applied}
        availableFilters={availableFilters}
        isLoading={isLoading}
        onFilterChange={(state) => {
          setFilterState(state)
        }}
      />
      {filterState && (
        <div className="flex-1 text-xs text-muted-foreground font-mono">
          <p className="font-semibold text-foreground mb-2">Filter state:</p>
          <pre className="bg-muted rounded p-3 overflow-auto max-h-64">
            {JSON.stringify(filterState, null, 2)}
          </pre>
        </div>
      )}
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/FilterSidebar',
  component: FilterSidebarDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Main filter sidebar for storefront. Renders collapsible sections for Price, then dynamic facets. ' +
          'Color facets use FacetColorSwatch; others use FacetCheckbox. ' +
          'Filter state is logged to the right panel when interactions occur.',
      },
    },
  },
} satisfies Meta<typeof FilterSidebarDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Default state with placeholder filters. All sections expanded. ' +
          'Interact with any filter to see the state output on the right.',
      },
    },
  },
  args: {
    appliedFilters: [],
  },
}

export const WithAppliedFilters: Story = {
  parameters: {
    docs: {
      description: { story: 'Sidebar initialized with pre-applied filters (category + color).' },
    },
  },
  args: {
    appliedFilters: APPLIED_FILTERS,
  },
}

export const WithCustomFilters: Story = {
  parameters: {
    docs: {
      description: { story: 'Custom filter data provided via availableFilters prop.' },
    },
  },
  args: {
    appliedFilters: [],
    availableFilters: AVAILABLE_FILTERS,
  },
}

export const Loading: Story = {
  parameters: {
    docs: {
      description: { story: 'Loading skeleton state while filters are being fetched.' },
    },
  },
  args: {
    appliedFilters: [],
    isLoading: true,
  },
}

export const NoFiltersAvailable: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'When availableFilters is null, falls back to the built-in placeholder data — ' +
          'useful for development before the API is connected.',
      },
    },
  },
  args: {
    appliedFilters: [],
    availableFilters: null,
  },
}
