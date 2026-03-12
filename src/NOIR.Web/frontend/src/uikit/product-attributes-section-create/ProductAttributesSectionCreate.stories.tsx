import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import {
  Package,
  AlertCircle,
  ChevronDown,
  ChevronUp,
  FolderOpen,
  HelpCircle,
} from 'lucide-react'
import {
  Alert,
  AlertDescription,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Switch,
  Textarea,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@uikit'

// --- Visual Replica ---
// ProductAttributesSectionCreate depends on useCategoryAttributeFormQuery (TanStack Query)
// and react-i18next. This self-contained demo replicates all visual states
// without requiring those external contexts.
// The Create variant differs from the Edit variant: it uses categoryId only (no productId),
// initializes fields with default values, and has no unsaved-changes tracking.

interface MockField {
  attributeId: string
  name: string
  type: 'Select' | 'Text' | 'TextArea' | 'Number' | 'Boolean' | 'Color'
  isRequired: boolean
  helpText?: string
  unit?: string
  placeholder?: string
  defaultValue?: string
  options?: { id: string; value: string; displayValue: string }[]
}

interface ProductAttributesSectionCreateDemoProps {
  state?: 'loading' | 'error' | 'empty-no-category' | 'empty-no-attributes' | 'populated'
  categoryName?: string
  fields?: MockField[]
  isViewMode?: boolean
  showValidationWarning?: boolean
  isCollapsed?: boolean
}

const DEFAULT_FIELDS: MockField[] = [
  {
    attributeId: 'attr-1',
    name: 'Material',
    type: 'Select',
    isRequired: true,
    helpText: 'Primary material of the product',
    options: [
      { id: '1', value: 'cotton', displayValue: 'Cotton' },
      { id: '2', value: 'polyester', displayValue: 'Polyester' },
      { id: '3', value: 'silk', displayValue: 'Silk' },
      { id: '4', value: 'wool', displayValue: 'Wool' },
    ],
  },
  {
    attributeId: 'attr-2',
    name: 'Weight',
    type: 'Number',
    isRequired: false,
    unit: 'g',
    placeholder: '0',
    helpText: 'Product weight in grams',
  },
  {
    attributeId: 'attr-3',
    name: 'Description',
    type: 'TextArea',
    isRequired: false,
    placeholder: 'Enter product description...',
  },
  {
    attributeId: 'attr-4',
    name: 'Is Handmade',
    type: 'Boolean',
    isRequired: false,
    defaultValue: 'false',
  },
  {
    attributeId: 'attr-5',
    name: 'Color',
    type: 'Color',
    isRequired: true,
    helpText: 'Primary product color',
    defaultValue: '#3B82F6',
  },
  {
    attributeId: 'attr-6',
    name: 'Brand Name',
    type: 'Text',
    isRequired: true,
    placeholder: 'e.g. NOIR Collection',
  },
]

const ProductAttributesSectionCreateDemo = ({
  state = 'populated',
  categoryName = 'Apparel',
  fields = DEFAULT_FIELDS,
  isViewMode = false,
  showValidationWarning = false,
  isCollapsed: initialCollapsed = false,
}: ProductAttributesSectionCreateDemoProps) => {
  const [isOpen, setIsOpen] = useState(!initialCollapsed)
  const [localValues, setLocalValues] = useState<Record<string, unknown>>(() => {
    // Initialize with default values like the real component
    const defaults: Record<string, unknown> = {}
    fields.forEach((f) => {
      defaults[f.attributeId] = f.defaultValue ?? null
    })
    return defaults
  })

  const handleChange = (attributeId: string, value: unknown) => {
    setLocalValues((prev) => ({ ...prev, [attributeId]: value }))
  }

  // Loading state
  if (state === 'loading') {
    return (
      <Card className="shadow-sm">
        <CardHeader>
          <div className="flex items-center gap-3">
            <Skeleton className="h-6 w-6 rounded" />
            <Skeleton className="h-6 w-40" />
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="space-y-2">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-10 w-full" />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  // Error state
  if (state === 'error') {
    return (
      <Card className="shadow-sm border-destructive/30">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-destructive">
            <AlertCircle className="h-5 w-5" />
            Failed to load attributes
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              Unable to fetch category attribute schema. The category may not exist or the server is unreachable.
            </AlertDescription>
          </Alert>
          <Button variant="outline" size="sm" className="mt-4 cursor-pointer">
            Retry
          </Button>
        </CardContent>
      </Card>
    )
  }

  // Empty states
  if (state === 'empty-no-category' || state === 'empty-no-attributes') {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-muted">
              <Package className="h-4 w-4 text-muted-foreground" />
            </div>
            Product Attributes
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-8 text-center">
            <div className="p-3 rounded-xl bg-muted/50 mb-3">
              <FolderOpen className="h-8 w-8 text-muted-foreground" />
            </div>
            <p className="text-muted-foreground">
              {state === 'empty-no-category'
                ? 'Select a category to see available attributes'
                : 'No attributes defined for this category'}
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  // Populated state
  const missingRequiredCount = showValidationWarning
    ? fields.filter((f) => f.isRequired && !localValues[f.attributeId]).length
    : 0

  return (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <Collapsible open={isOpen} onOpenChange={setIsOpen}>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-primary/20 to-primary/10 shadow-sm">
                <Package className="h-4 w-4 text-primary" />
              </div>
              <div>
                <CardTitle>Product Attributes</CardTitle>
                <CardDescription>
                  {categoryName} &middot; {fields.length} attributes
                </CardDescription>
              </div>
            </div>
            <CollapsibleTrigger asChild>
              <Button
                variant="ghost"
                size="sm"
                className="cursor-pointer"
                aria-label={isOpen ? 'Collapse' : 'Expand'}
              >
                {isOpen ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
              </Button>
            </CollapsibleTrigger>
          </div>
        </CardHeader>

        <CollapsibleContent>
          <CardContent className="pt-0">
            {missingRequiredCount > 0 && !isViewMode && (
              <Alert className="mb-4 border-amber-500/30 bg-amber-50/50 dark:bg-amber-950/20">
                <AlertCircle className="h-4 w-4 text-amber-500" />
                <AlertDescription className="text-amber-700 dark:text-amber-400">
                  {missingRequiredCount} required attribute(s) are missing values
                </AlertDescription>
              </Alert>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {fields.map((field) => (
                <div key={field.attributeId} className="space-y-2">
                  <div className="flex items-center gap-2">
                    <Label className="text-sm font-medium">
                      {field.name}
                      {field.isRequired && (
                        <span className="text-destructive ml-1" aria-hidden="true">*</span>
                      )}
                      {field.unit && (
                        <span className="text-muted-foreground font-normal ml-1">({field.unit})</span>
                      )}
                    </Label>
                    {field.helpText && (
                      <TooltipProvider>
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <button type="button" className="text-muted-foreground hover:text-foreground transition-colors cursor-pointer" aria-label={`Help for ${field.name}`}>
                              <HelpCircle className="h-4 w-4" />
                            </button>
                          </TooltipTrigger>
                          <TooltipContent side="top" className="max-w-xs">
                            <p>{field.helpText}</p>
                          </TooltipContent>
                        </Tooltip>
                      </TooltipProvider>
                    )}
                  </div>

                  {field.type === 'Select' && (
                    <Select
                      value={(localValues[field.attributeId] as string) || ''}
                      onValueChange={(val) => handleChange(field.attributeId, val)}
                      disabled={isViewMode}
                    >
                      <SelectTrigger className="cursor-pointer">
                        <SelectValue placeholder="Select..." />
                      </SelectTrigger>
                      <SelectContent>
                        {field.options?.map((opt) => (
                          <SelectItem key={opt.id} value={opt.value} className="cursor-pointer">
                            {opt.displayValue}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                  {field.type === 'Text' && (
                    <Input
                      value={(localValues[field.attributeId] as string) || ''}
                      onChange={(e) => handleChange(field.attributeId, e.target.value)}
                      placeholder={field.placeholder || ''}
                      disabled={isViewMode}
                      aria-label={field.name}
                    />
                  )}
                  {field.type === 'TextArea' && (
                    <Textarea
                      value={(localValues[field.attributeId] as string) || ''}
                      onChange={(e) => handleChange(field.attributeId, e.target.value)}
                      placeholder={field.placeholder || ''}
                      disabled={isViewMode}
                      rows={3}
                      aria-label={field.name}
                    />
                  )}
                  {field.type === 'Number' && (
                    <Input
                      type="number"
                      value={(localValues[field.attributeId] as string) || ''}
                      onChange={(e) => handleChange(field.attributeId, e.target.value)}
                      placeholder={field.placeholder || '0'}
                      disabled={isViewMode}
                      aria-label={field.name}
                    />
                  )}
                  {field.type === 'Boolean' && (
                    <div className="flex items-center gap-3">
                      <Switch
                        checked={!!localValues[field.attributeId]}
                        onCheckedChange={(checked) => handleChange(field.attributeId, checked)}
                        disabled={isViewMode}
                        className="cursor-pointer"
                        aria-label={field.name}
                      />
                      <span className="text-sm text-muted-foreground">
                        {localValues[field.attributeId] ? 'Yes' : 'No'}
                      </span>
                    </div>
                  )}
                  {field.type === 'Color' && (
                    <div className="flex items-center gap-3">
                      <input
                        type="color"
                        value={(localValues[field.attributeId] as string) || '#000000'}
                        onChange={(e) => handleChange(field.attributeId, e.target.value)}
                        disabled={isViewMode}
                        aria-label={`${field.name} color picker`}
                        className="w-12 h-10 rounded-lg cursor-pointer border-2 border-input hover:border-primary/50 transition-colors [&::-webkit-color-swatch-wrapper]:p-1 [&::-webkit-color-swatch]:rounded-md"
                      />
                      <Input
                        value={(localValues[field.attributeId] as string) || ''}
                        onChange={(e) => handleChange(field.attributeId, e.target.value)}
                        placeholder="#RRGGBB"
                        maxLength={7}
                        disabled={isViewMode}
                        className="font-mono uppercase flex-1"
                        aria-label={`${field.name} hex value`}
                      />
                    </div>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </CollapsibleContent>
      </Collapsible>
    </Card>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/ProductAttributesSectionCreate',
  component: ProductAttributesSectionCreateDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Dynamic attribute form for new product creation. Unlike the edit variant (ProductAttributesSection), ' +
          'this component uses only a categoryId (no productId) and initializes fields with default values. ' +
          'The real component fetches the form schema from useCategoryAttributeFormQuery; this demo replicates the visual appearance.',
      },
    },
  },
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 720 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ProductAttributesSectionCreateDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: { story: 'Populated state with 6 attributes, initialized with default values where defined.' },
    },
  },
  args: {
    state: 'populated',
    categoryName: 'Apparel',
  },
}

export const Loading: Story = {
  parameters: {
    docs: {
      description: { story: 'Skeleton loading state while fetching category attribute schema.' },
    },
  },
  args: {
    state: 'loading',
  },
}

export const Error: Story = {
  parameters: {
    docs: {
      description: { story: 'Error state when category attribute schema fetch fails.' },
    },
  },
  args: {
    state: 'error',
  },
}

export const EmptyNoCategory: Story = {
  parameters: {
    docs: {
      description: { story: 'No category selected yet — prompts the user to select a category first.' },
    },
  },
  args: {
    state: 'empty-no-category',
  },
}

export const EmptyNoAttributes: Story = {
  parameters: {
    docs: {
      description: { story: 'Category selected but it has no attributes configured.' },
    },
  },
  args: {
    state: 'empty-no-attributes',
  },
}

export const WithDefaults: Story = {
  parameters: {
    docs: {
      description: { story: 'Fields pre-populated with default values from the category schema.' },
    },
  },
  args: {
    state: 'populated',
    categoryName: 'Furniture',
    fields: [
      {
        attributeId: 'attr-1',
        name: 'Finish',
        type: 'Select',
        isRequired: true,
        defaultValue: 'matte',
        options: [
          { id: '1', value: 'matte', displayValue: 'Matte' },
          { id: '2', value: 'gloss', displayValue: 'Gloss' },
          { id: '3', value: 'satin', displayValue: 'Satin' },
        ],
      },
      {
        attributeId: 'attr-2',
        name: 'Assembly Required',
        type: 'Boolean',
        isRequired: false,
        defaultValue: 'true',
      },
      {
        attributeId: 'attr-3',
        name: 'Max Load',
        type: 'Number',
        isRequired: true,
        unit: 'kg',
        placeholder: '0',
        defaultValue: '150',
      },
    ],
  },
}

export const ValidationWarning: Story = {
  parameters: {
    docs: {
      description: { story: 'Validation warning showing required fields that have not been filled.' },
    },
  },
  args: {
    state: 'populated',
    showValidationWarning: true,
  },
}

export const ViewMode: Story = {
  parameters: {
    docs: {
      description: { story: 'View-only mode — all inputs are disabled.' },
    },
  },
  args: {
    state: 'populated',
    isViewMode: true,
  },
}

export const Collapsed: Story = {
  parameters: {
    docs: {
      description: { story: 'Section collapsed — only header visible. Click to expand.' },
    },
  },
  args: {
    state: 'populated',
    isCollapsed: true,
  },
}
