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
// ProductAttributesSection depends on useProductAttributeFormQuery (TanStack Query)
// and react-i18next. This self-contained demo replicates all visual states
// without requiring those external contexts.

interface MockField {
  attributeId: string
  name: string
  type: 'Select' | 'Text' | 'TextArea' | 'Number' | 'Boolean' | 'Color'
  isRequired: boolean
  helpText?: string
  unit?: string
  placeholder?: string
  options?: { id: string; value: string; displayValue: string }[]
}

interface ProductAttributesSectionDemoProps {
  state?: 'loading' | 'error' | 'empty-no-category' | 'empty-no-attributes' | 'populated'
  categoryName?: string
  fields?: MockField[]
  isViewMode?: boolean
  showUnsaved?: boolean
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
    name: 'Care Instructions',
    type: 'TextArea',
    isRequired: false,
    placeholder: 'Enter care instructions...',
  },
  {
    attributeId: 'attr-4',
    name: 'Is Organic',
    type: 'Boolean',
    isRequired: false,
  },
  {
    attributeId: 'attr-5',
    name: 'Color',
    type: 'Color',
    isRequired: true,
    helpText: 'Primary product color',
  },
  {
    attributeId: 'attr-6',
    name: 'Country of Origin',
    type: 'Text',
    isRequired: true,
    placeholder: 'e.g. Vietnam',
  },
]

const ProductAttributesSectionDemo = ({
  state = 'populated',
  categoryName = 'Apparel',
  fields = DEFAULT_FIELDS,
  isViewMode = false,
  showUnsaved = false,
  showValidationWarning = false,
  isCollapsed: initialCollapsed = false,
}: ProductAttributesSectionDemoProps) => {
  const [isOpen, setIsOpen] = useState(!initialCollapsed)
  const [localValues, setLocalValues] = useState<Record<string, unknown>>({})

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
              Network error: Unable to fetch attribute form schema. Please check your connection and try again.
            </AlertDescription>
          </Alert>
          <Button variant="outline" size="sm" className="mt-4 cursor-pointer">
            Retry
          </Button>
        </CardContent>
      </Card>
    )
  }

  // Empty state
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
                <CardTitle className="flex items-center gap-2">
                  Product Attributes
                  {showUnsaved && !isViewMode && (
                    <span className="text-xs font-normal text-amber-700">
                      (unsaved changes)
                    </span>
                  )}
                </CardTitle>
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
                      <SelectTrigger className="cursor-pointer" aria-label={field.name}>
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
  title: 'UIKit/ProductAttributesSection',
  component: ProductAttributesSectionDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Dynamic attribute form for product editing. Renders attribute inputs based on the product\'s category. ' +
          'Supports loading, error, empty, and populated states. The real component fetches the form schema from ' +
          'the API via useProductAttributeFormQuery; this demo replicates the visual appearance with mock data.',
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
} satisfies Meta<typeof ProductAttributesSectionDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: { story: 'Populated state with 6 attributes across different input types.' },
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
      description: { story: 'Skeleton loading state while fetching attribute form schema.' },
    },
  },
  args: {
    state: 'loading',
  },
}

export const Error: Story = {
  parameters: {
    docs: {
      description: { story: 'Error state when API call fails, with retry button.' },
    },
  },
  args: {
    state: 'error',
  },
}

export const EmptyNoCategory: Story = {
  parameters: {
    docs: {
      description: { story: 'Empty state when no category is selected — prompts user to choose one.' },
    },
  },
  args: {
    state: 'empty-no-category',
  },
}

export const EmptyNoAttributes: Story = {
  parameters: {
    docs: {
      description: { story: 'Empty state when category has no attributes defined.' },
    },
  },
  args: {
    state: 'empty-no-attributes',
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

export const UnsavedChanges: Story = {
  parameters: {
    docs: {
      description: { story: 'Shows the unsaved changes indicator in the card header.' },
    },
  },
  args: {
    state: 'populated',
    showUnsaved: true,
  },
}

export const ValidationWarning: Story = {
  parameters: {
    docs: {
      description: { story: 'Validation alert when required attributes are missing values.' },
    },
  },
  args: {
    state: 'populated',
    showValidationWarning: true,
  },
}

export const Collapsed: Story = {
  parameters: {
    docs: {
      description: { story: 'Section collapsed — only header is visible. Click chevron to expand.' },
    },
  },
  args: {
    state: 'populated',
    isCollapsed: true,
  },
}

export const FewAttributes: Story = {
  parameters: {
    docs: {
      description: { story: 'Only two attributes — demonstrates layout with minimal fields.' },
    },
  },
  args: {
    state: 'populated',
    categoryName: 'Electronics',
    fields: [
      {
        attributeId: 'attr-1',
        name: 'Wattage',
        type: 'Number',
        isRequired: true,
        unit: 'W',
        placeholder: '0',
      },
      {
        attributeId: 'attr-2',
        name: 'Energy Rating',
        type: 'Select',
        isRequired: false,
        options: [
          { id: '1', value: 'a', displayValue: 'A+++' },
          { id: '2', value: 'b', displayValue: 'A++' },
          { id: '3', value: 'c', displayValue: 'A+' },
        ],
      },
    ],
  },
}
