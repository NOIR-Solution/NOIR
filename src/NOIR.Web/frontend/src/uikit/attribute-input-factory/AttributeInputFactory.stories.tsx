import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { AttributeInputFactory } from '@/components/products/AttributeInputs/AttributeInputFactory'
import type { AttributeInputProps, AttributeValue } from '@/components/products/AttributeInputs/types'
import type { ProductAttributeFormField } from '@/types/productAttribute'
import type { AttributeType } from '@/types/productAttribute'

// AttributeInputFactory uses react-i18next and react-dropzone.
// The component is imported directly — it gracefully handles missing i18n context
// by falling back to translation key strings.

// Wrapper that adds interactive state management around each input
const AttributeInputWrapper = ({
  field,
  initialValue = null,
  disabled = false,
  error,
}: {
  field: ProductAttributeFormField
  initialValue?: AttributeValue
  disabled?: boolean
  error?: string
}) => {
  const [value, setValue] = useState<AttributeValue>(initialValue)

  return (
    <div className="space-y-2">
      <div className="text-xs text-muted-foreground">
        Current value: <code className="bg-muted px-1 py-0.5 rounded">{JSON.stringify(value)}</code>
      </div>
      <AttributeInputFactory
        field={field}
        value={value}
        onChange={setValue}
        disabled={disabled}
        error={error}
      />
    </div>
  )
}

// Show multiple attribute types in a grid
interface AttributeShowcaseDemoProps {
  fields: Array<{ field: ProductAttributeFormField; initialValue?: AttributeValue }>
  disabled?: boolean
}

const AttributeShowcaseDemo = ({ fields, disabled = false }: AttributeShowcaseDemoProps) => {
  return (
    <div className="grid gap-6">
      {fields.map(({ field, initialValue }) => (
        <div key={field.attributeId} className="border rounded-lg p-4">
          <p className="text-xs font-mono text-muted-foreground mb-3">type: {field.type}</p>
          <AttributeInputWrapper field={field} initialValue={initialValue} disabled={disabled} />
        </div>
      ))}
    </div>
  )
}

// --- Field Fixtures ---

const makeField = (
  overrides: Partial<ProductAttributeFormField> & { type: AttributeType }
): ProductAttributeFormField => ({
  attributeId: `attr-${overrides.type?.toLowerCase()}`,
  code: overrides.type?.toLowerCase() || 'unknown',
  name: overrides.name || overrides.type || 'Attribute',
  type: overrides.type,
  isRequired: false,
  unit: null,
  placeholder: null,
  helpText: null,
  minValue: null,
  maxValue: null,
  maxLength: null,
  defaultValue: null,
  validationRegex: null,
  options: null,
  ...overrides,
})

const TEXT_FIELD = makeField({
  type: 'Text',
  name: 'Product Brand',
  placeholder: 'e.g. Nike, Adidas',
  maxLength: 100,
  helpText: 'Enter the brand name for this product.',
})

const TEXTAREA_FIELD = makeField({
  type: 'TextArea',
  name: 'Material Composition',
  placeholder: '100% organic cotton...',
  maxLength: 500,
})

const NUMBER_FIELD = makeField({
  type: 'Number',
  name: 'Weight (g)',
  unit: 'g',
  minValue: 0,
  maxValue: 10000,
  placeholder: '0',
})

const DECIMAL_FIELD = makeField({
  type: 'Decimal',
  name: 'Rating Score',
  minValue: 0,
  maxValue: 5,
  placeholder: '0.0',
})

const BOOLEAN_FIELD = makeField({
  type: 'Boolean',
  name: 'Is Organic',
  helpText: 'Check if the product is certified organic.',
})

const DATE_FIELD = makeField({
  type: 'Date',
  name: 'Release Date',
  placeholder: 'Pick a date',
})

const DATETIME_FIELD = makeField({
  type: 'DateTime',
  name: 'Available From',
  placeholder: 'Pick a date and time',
})

const COLOR_FIELD = makeField({
  type: 'Color',
  name: 'Primary Color',
  options: [
    { id: 'c1', value: '#EF4444', displayValue: 'Red', colorCode: '#EF4444', swatchUrl: null, iconUrl: null, sortOrder: 0, isActive: true, productCount: 12 },
    { id: 'c2', value: '#3B82F6', displayValue: 'Blue', colorCode: '#3B82F6', swatchUrl: null, iconUrl: null, sortOrder: 1, isActive: true, productCount: 8 },
    { id: 'c3', value: '#22C55E', displayValue: 'Green', colorCode: '#22C55E', swatchUrl: null, iconUrl: null, sortOrder: 2, isActive: true, productCount: 5 },
    { id: 'c4', value: '#EAB308', displayValue: 'Yellow', colorCode: '#EAB308', swatchUrl: null, iconUrl: null, sortOrder: 3, isActive: true, productCount: 4 },
    { id: 'c5', value: '#A855F7', displayValue: 'Purple', colorCode: '#A855F7', swatchUrl: null, iconUrl: null, sortOrder: 4, isActive: true, productCount: 7 },
    { id: 'c6', value: '#000000', displayValue: 'Black', colorCode: '#000000', swatchUrl: null, iconUrl: null, sortOrder: 5, isActive: true, productCount: 30 },
    { id: 'c7', value: '#FFFFFF', displayValue: 'White', colorCode: '#FFFFFF', swatchUrl: null, iconUrl: null, sortOrder: 6, isActive: true, productCount: 25 },
  ],
})

const SELECT_FIELD = makeField({
  type: 'Select',
  name: 'Fit Style',
  placeholder: 'Select a fit...',
  options: [
    { id: 's1', value: 'slim', displayValue: 'Slim Fit', colorCode: null, swatchUrl: null, iconUrl: null, sortOrder: 0, isActive: true, productCount: 20 },
    { id: 's2', value: 'regular', displayValue: 'Regular Fit', colorCode: null, swatchUrl: null, iconUrl: null, sortOrder: 1, isActive: true, productCount: 45 },
    { id: 's3', value: 'relaxed', displayValue: 'Relaxed Fit', colorCode: null, swatchUrl: null, iconUrl: null, sortOrder: 2, isActive: true, productCount: 15 },
    { id: 's4', value: 'oversized', displayValue: 'Oversized', colorCode: null, swatchUrl: null, iconUrl: null, sortOrder: 3, isActive: true, productCount: 8 },
  ],
})

const MULTISELECT_FIELD = makeField({
  type: 'MultiSelect',
  name: 'Compatible Seasons',
  options: [
    { id: 'm1', value: 'spring', displayValue: 'Spring', colorCode: null, swatchUrl: null, iconUrl: null, sortOrder: 0, isActive: true, productCount: 30 },
    { id: 'm2', value: 'summer', displayValue: 'Summer', colorCode: null, swatchUrl: null, iconUrl: null, sortOrder: 1, isActive: true, productCount: 42 },
    { id: 'm3', value: 'autumn', displayValue: 'Autumn', colorCode: null, swatchUrl: null, iconUrl: null, sortOrder: 2, isActive: true, productCount: 28 },
    { id: 'm4', value: 'winter', displayValue: 'Winter', colorCode: null, swatchUrl: null, iconUrl: null, sortOrder: 3, isActive: true, productCount: 15 },
  ],
})

const RANGE_FIELD = makeField({
  type: 'Range',
  name: 'Size Range (EU)',
  unit: 'EU',
  minValue: 36,
  maxValue: 48,
})

const URL_FIELD = makeField({
  type: 'Url',
  name: 'Product Manual URL',
  placeholder: 'https://example.com/manual.pdf',
})

const FILE_FIELD = makeField({
  type: 'File',
  name: 'Certification Document',
})

// --- Meta ---

const meta = {
  title: 'UIKit/AttributeInputFactory',
  component: AttributeShowcaseDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Factory component that renders the correct input for each of the 13 attribute types: ' +
          'Text, TextArea, Number, Decimal, Boolean, Date, DateTime, Color, Select, MultiSelect, Range, Url, File. ' +
          'Current value is shown above each input for demonstration.',
      },
    },
  },
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 560 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof AttributeShowcaseDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const TextInputs: Story = {
  parameters: {
    docs: {
      description: { story: 'Text and TextArea inputs with max-length constraints.' },
    },
  },
  args: {
    fields: [
      { field: TEXT_FIELD, initialValue: '' },
      { field: TEXTAREA_FIELD, initialValue: '' },
    ],
  },
}

export const NumericInputs: Story = {
  parameters: {
    docs: {
      description: { story: 'Number and Decimal inputs with increment/decrement buttons and min/max bounds.' },
    },
  },
  args: {
    fields: [
      { field: NUMBER_FIELD, initialValue: 250 },
      { field: DECIMAL_FIELD, initialValue: 4.5 },
    ],
  },
}

export const BooleanInput: Story = {
  parameters: {
    docs: {
      description: { story: 'Toggle switch for boolean attributes.' },
    },
  },
  args: {
    fields: [
      { field: BOOLEAN_FIELD, initialValue: false },
      { field: { ...BOOLEAN_FIELD, attributeId: 'bool-2', name: 'Is Featured', isRequired: true }, initialValue: true },
    ],
  },
}

export const DateInputs: Story = {
  parameters: {
    docs: {
      description: { story: 'Date picker (calendar only) and DateTime picker (calendar + time presets + manual time input).' },
    },
  },
  args: {
    fields: [
      { field: DATE_FIELD, initialValue: null },
      { field: DATETIME_FIELD, initialValue: null },
    ],
  },
}

export const ColorInput: Story = {
  parameters: {
    docs: {
      description: { story: 'Color picker with native color input, hex input, and predefined color swatches.' },
    },
  },
  args: {
    fields: [
      { field: COLOR_FIELD, initialValue: '#3B82F6' },
    ],
  },
}

export const SelectInputs: Story = {
  parameters: {
    docs: {
      description: { story: 'Single-select dropdown and multi-select checkbox grid.' },
    },
  },
  args: {
    fields: [
      { field: SELECT_FIELD, initialValue: 'regular' },
      { field: MULTISELECT_FIELD, initialValue: ['spring', 'summer'] },
    ],
  },
}

export const RangeInput: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Dual-thumb range slider for min/max selection. ' +
          'Drag the sliders or type precise values in the numeric inputs below.',
      },
    },
  },
  args: {
    fields: [
      { field: RANGE_FIELD, initialValue: '38-44' },
    ],
  },
}

export const UrlAndFileInputs: Story = {
  parameters: {
    docs: {
      description: { story: 'URL input with validation indicator and File drop zone.' },
    },
  },
  args: {
    fields: [
      { field: URL_FIELD, initialValue: 'https://example.com/docs/manual.pdf' },
      { field: FILE_FIELD, initialValue: null },
    ],
  },
}

export const AllTypes: Story = {
  parameters: {
    docs: {
      description: { story: 'All 13 attribute types in a single showcase.' },
    },
  },
  args: {
    fields: [
      { field: TEXT_FIELD },
      { field: TEXTAREA_FIELD },
      { field: NUMBER_FIELD, initialValue: 250 },
      { field: DECIMAL_FIELD, initialValue: 4.5 },
      { field: BOOLEAN_FIELD, initialValue: true },
      { field: DATE_FIELD },
      { field: DATETIME_FIELD },
      { field: COLOR_FIELD, initialValue: '#EF4444' },
      { field: SELECT_FIELD, initialValue: 'slim' },
      { field: MULTISELECT_FIELD, initialValue: ['summer', 'autumn'] },
      { field: RANGE_FIELD, initialValue: '38-44' },
      { field: URL_FIELD },
      { field: FILE_FIELD },
    ],
  },
}

export const WithError: Story = {
  parameters: {
    docs: {
      description: { story: 'Attribute input with a validation error message displayed below.' },
    },
  },
  args: {
    fields: [
      {
        field: { ...TEXT_FIELD, name: 'Brand (required)', isRequired: true },
        initialValue: '',
      },
    ],
  },
  render: (args) => (
    <div style={{ maxWidth: 560 }}>
      <div className="border rounded-lg p-4">
        <AttributeInputFactory
          field={args.fields[0].field}
          value=""
          onChange={() => {}}
          error="This field is required"
        />
      </div>
    </div>
  ),
}

export const Disabled: Story = {
  parameters: {
    docs: {
      description: { story: 'All inputs in disabled (read-only) state.' },
    },
  },
  args: {
    disabled: true,
    fields: [
      { field: TEXT_FIELD, initialValue: 'Nike' },
      { field: NUMBER_FIELD, initialValue: 320 },
      { field: BOOLEAN_FIELD, initialValue: true },
      { field: SELECT_FIELD, initialValue: 'slim' },
      { field: COLOR_FIELD, initialValue: '#EF4444' },
    ],
  },
}
