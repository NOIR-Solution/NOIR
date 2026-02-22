import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { FacetColorSwatch } from '@/components/storefront/facets/FacetColorSwatch'
import type { FilterOption } from '@/types/filter'

// FacetColorSwatch has no heavy external dependencies — imported directly.

const COLOR_OPTIONS: FilterOption[] = [
  { value: 'black', label: 'Black', count: 45, colorCode: '#000000' },
  { value: 'white', label: 'White', count: 38, colorCode: '#FFFFFF' },
  { value: 'red', label: 'Red', count: 22, colorCode: '#EF4444' },
  { value: 'blue', label: 'Blue', count: 30, colorCode: '#3B82F6' },
  { value: 'green', label: 'Green', count: 18, colorCode: '#22C55E' },
  { value: 'yellow', label: 'Yellow', count: 12, colorCode: '#EAB308' },
  { value: 'purple', label: 'Purple', count: 15, colorCode: '#A855F7' },
  { value: 'orange', label: 'Orange', count: 10, colorCode: '#F97316' },
  { value: 'pink', label: 'Pink', count: 8, colorCode: '#EC4899' },
  { value: 'teal', label: 'Teal', count: 6, colorCode: '#14B8A6' },
]

const FacetColorSwatchDemo = ({
  name,
  options,
  initialSelected = [],
}: {
  name: string
  options: FilterOption[]
  initialSelected?: string[]
}) => {
  const [selected, setSelected] = useState<string[]>(initialSelected)

  return (
    <div>
      <FacetColorSwatch
        name={name}
        options={options}
        selectedValues={selected}
        onChange={setSelected}
      />
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/FacetColorSwatch',
  component: FacetColorSwatchDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Color swatch filter for storefront filtering. ' +
          'Renders circular buttons with color backgrounds. ' +
          'Selected swatches show a checkmark with auto-contrast (white on dark, dark on light).',
      },
    },
  },
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 300 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof FacetColorSwatchDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  args: {
    name: 'Color',
    options: COLOR_OPTIONS,
    initialSelected: [],
  },
}

export const WithSelections: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Multiple colors selected — shows check icon with auto-contrast (white on dark colors, dark on light colors like White and Yellow).',
      },
    },
  },
  args: {
    name: 'Color',
    options: COLOR_OPTIONS,
    initialSelected: ['black', 'white', 'blue', 'yellow'],
  },
}

export const SingleSelection: Story = {
  parameters: {
    docs: {
      description: { story: 'Single color selected.' },
    },
  },
  args: {
    name: 'Color',
    options: COLOR_OPTIONS,
    initialSelected: ['red'],
  },
}

export const FewColors: Story = {
  parameters: {
    docs: {
      description: { story: 'Small palette — 4 colors only.' },
    },
  },
  args: {
    name: 'Color',
    options: COLOR_OPTIONS.slice(0, 4),
    initialSelected: [],
  },
}

export const AllSelected: Story = {
  parameters: {
    docs: {
      description: { story: 'All colors selected — selected names shown below the swatches.' },
    },
  },
  args: {
    name: 'Color',
    options: COLOR_OPTIONS.slice(0, 6),
    initialSelected: COLOR_OPTIONS.slice(0, 6).map((o) => o.value),
  },
}
