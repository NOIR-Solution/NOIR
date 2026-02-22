import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { FilterMobileDrawer, FilterMobileTrigger } from '@/components/storefront/FilterMobileDrawer'
import { FacetCheckbox } from '@/components/storefront/facets/FacetCheckbox'
import { FacetColorSwatch } from '@/components/storefront/facets/FacetColorSwatch'
import { FacetPriceRange } from '@/components/storefront/facets/FacetPriceRange'
import { Separator } from '@uikit'
import type { FilterOption } from '@/types/filter'

// FilterMobileDrawer has no heavy external dependencies — imported directly.

const CATEGORY_OPTIONS: FilterOption[] = [
  { value: 'electronics', label: 'Electronics', count: 42 },
  { value: 'clothing', label: 'Clothing', count: 38 },
  { value: 'home', label: 'Home & Garden', count: 25 },
  { value: 'sports', label: 'Sports', count: 18 },
  { value: 'toys', label: 'Toys & Games', count: 12 },
]

const COLOR_OPTIONS: FilterOption[] = [
  { value: 'black', label: 'Black', count: 45, colorCode: '#000000' },
  { value: 'white', label: 'White', count: 38, colorCode: '#FFFFFF' },
  { value: 'red', label: 'Red', count: 22, colorCode: '#EF4444' },
  { value: 'blue', label: 'Blue', count: 30, colorCode: '#3B82F6' },
  { value: 'green', label: 'Green', count: 18, colorCode: '#22C55E' },
]

const FilterContent = () => {
  const [categories, setCategories] = useState<string[]>(['electronics'])
  const [colors, setColors] = useState<string[]>([])

  return (
    <div className="space-y-6">
      <FacetPriceRange min={0} max={1000} onChange={() => {}} currency="$" />
      <Separator />
      <FacetCheckbox
        name="Category"
        options={CATEGORY_OPTIONS}
        selectedValues={categories}
        onChange={setCategories}
      />
      <Separator />
      <FacetColorSwatch
        name="Color"
        options={COLOR_OPTIONS}
        selectedValues={colors}
        onChange={setColors}
      />
    </div>
  )
}

// Interactive demo with trigger + drawer
const FilterMobileDrawerDemo = ({
  activeFilterCount = 0,
  title,
  description,
  startOpen = false,
}: {
  activeFilterCount?: number
  title?: string
  description?: string
  startOpen?: boolean
}) => {
  const [open, setOpen] = useState(startOpen)

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Click the trigger below to open the mobile filter drawer:
      </p>
      <FilterMobileTrigger
        onClick={() => setOpen(true)}
        activeFilterCount={activeFilterCount}
      />
      <FilterMobileDrawer
        open={open}
        onOpenChange={setOpen}
        activeFilterCount={activeFilterCount}
        title={title}
        description={description}
      >
        <FilterContent />
      </FilterMobileDrawer>
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/FilterMobileDrawer',
  component: FilterMobileDrawerDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Slide-out sheet (left side) for mobile filter navigation. ' +
          'Contains filter content in a scrollable area with a "View Results" close button. ' +
          'Pair with FilterMobileTrigger to open. Shows active filter count badge.',
      },
    },
  },
} satisfies Meta<typeof FilterMobileDrawerDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: { story: 'Click "Filters" button to open the drawer.' },
    },
  },
  args: {
    activeFilterCount: 0,
  },
}

export const WithActiveFilters: Story = {
  parameters: {
    docs: {
      description: { story: 'Trigger badge shows the number of active filters.' },
    },
  },
  args: {
    activeFilterCount: 3,
  },
}

export const OpenByDefault: Story = {
  parameters: {
    docs: {
      description: { story: 'Drawer rendered in open state for easy visual inspection.' },
    },
  },
  args: {
    activeFilterCount: 1,
    startOpen: true,
  },
}

export const CustomTitleAndDescription: Story = {
  parameters: {
    docs: {
      description: { story: 'Custom drawer title and description text.' },
    },
  },
  args: {
    title: 'Refine Results',
    description: 'Narrow down your product search',
    activeFilterCount: 2,
    startOpen: true,
  },
}
