import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  Input,
} from '@uikit'
import {
  Plus,
  Trash2,
  Palette,
  ChevronDown,
  ChevronRight,
  GripVertical,
  X,
} from 'lucide-react'

// --- Visual Replica ---
// ProductOptionsManager uses react-i18next.
// This self-contained demo replicates the visual appearance without that context.

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

const sanitizeColor = (code?: string | null, fallback = 'transparent') => {
  if (!code) return fallback
  return code.startsWith('#') ? code : `#${code}`
}

interface ProductOptionsManagerDemoProps {
  initialOptions?: DemoOption[]
  disabled?: boolean
}

const ProductOptionsManagerDemo = ({
  initialOptions = [],
  disabled = false,
}: ProductOptionsManagerDemoProps) => {
  const [options, setOptions] = useState<DemoOption[]>(initialOptions)
  const [expandedOptions, setExpandedOptions] = useState<Set<string>>(
    new Set(initialOptions.map((o) => o.id))
  )
  const [newOptionName, setNewOptionName] = useState('')
  const [newValueInputs, setNewValueInputs] = useState<Record<string, string>>({})
  const [deleteTarget, setDeleteTarget] = useState<{
    type: 'option' | 'value'
    optionId: string
    valueId?: string
    name: string
  } | null>(null)

  const toggleOption = (optionId: string) => {
    const next = new Set(expandedOptions)
    if (next.has(optionId)) {
      next.delete(optionId)
    } else {
      next.add(optionId)
    }
    setExpandedOptions(next)
  }

  const handleAddOption = () => {
    const name = newOptionName.trim()
    if (!name) return
    const newOpt: DemoOption = {
      id: `opt-${Date.now()}`,
      name,
      displayName: name,
      sortOrder: options.length,
      values: [],
    }
    setOptions((prev) => [...prev, newOpt])
    setExpandedOptions((prev) => new Set([...prev, newOpt.id]))
    setNewOptionName('')
  }

  const handleAddValue = (optionId: string) => {
    const rawValue = newValueInputs[optionId]?.trim()
    if (!rawValue) return
    const option = options.find((o) => o.id === optionId)
    const newValue: DemoOptionValue = {
      id: `val-${Date.now()}`,
      value: rawValue,
      displayValue: rawValue,
      sortOrder: option?.values.length ?? 0,
    }
    setOptions((prev) =>
      prev.map((o) => (o.id === optionId ? { ...o, values: [...o.values, newValue] } : o))
    )
    setNewValueInputs((prev) => ({ ...prev, [optionId]: '' }))
  }

  const handleConfirmDelete = () => {
    if (!deleteTarget) return
    if (deleteTarget.type === 'option') {
      setOptions((prev) => prev.filter((o) => o.id !== deleteTarget.optionId))
    } else if (deleteTarget.valueId) {
      setOptions((prev) =>
        prev.map((o) =>
          o.id === deleteTarget.optionId
            ? { ...o, values: o.values.filter((v) => v.id !== deleteTarget.valueId) }
            : o
        )
      )
    }
    setDeleteTarget(null)
  }

  return (
    <div className="space-y-4">
      {/* Option List */}
      <div className="space-y-2">
        {options.map((option) => (
          <Collapsible
            key={option.id}
            open={expandedOptions.has(option.id)}
            onOpenChange={() => toggleOption(option.id)}
          >
            <div className="rounded-lg border bg-card">
              <div className="flex items-center gap-2 p-3">
                <GripVertical className="h-4 w-4 text-muted-foreground cursor-grab" />
                <CollapsibleTrigger asChild>
                  <Button variant="ghost" size="sm" className="p-0 h-auto cursor-pointer">
                    {expandedOptions.has(option.id) ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </Button>
                </CollapsibleTrigger>
                <span className="font-medium flex-1">{option.displayName || option.name}</span>
                <span className="text-sm text-muted-foreground">
                  {option.values.length} values
                </span>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8 text-destructive hover:text-destructive cursor-pointer"
                  onClick={() =>
                    setDeleteTarget({
                      type: 'option',
                      optionId: option.id,
                      name: option.displayName || option.name,
                    })
                  }
                  disabled={disabled}
                  aria-label={`Delete option ${option.displayName || option.name}`}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>

              <CollapsibleContent>
                <div className="border-t p-3 space-y-3">
                  <div className="flex flex-wrap gap-2">
                    {option.values.map((value) => (
                      <div
                        key={value.id}
                        className="flex items-center gap-2 rounded-md border bg-muted/50 px-2 py-1"
                      >
                        {value.colorCode && (
                          <div
                            className="h-4 w-4 rounded-full border"
                            style={{ backgroundColor: sanitizeColor(value.colorCode) }}
                          />
                        )}
                        <span className="text-sm">{value.displayValue || value.value}</span>
                        <label className="cursor-pointer">
                          <Palette className="h-3.5 w-3.5 text-muted-foreground hover:text-foreground" />
                          <input
                            type="color"
                            className="sr-only"
                            defaultValue={sanitizeColor(value.colorCode, '#000000')}
                            onChange={(e) => {
                              setOptions((prev) =>
                                prev.map((o) =>
                                  o.id === option.id
                                    ? {
                                        ...o,
                                        values: o.values.map((v) =>
                                          v.id === value.id
                                            ? { ...v, colorCode: e.target.value }
                                            : v
                                        ),
                                      }
                                    : o
                                )
                              )
                            }}
                            disabled={disabled}
                            aria-label={`Change color for ${value.displayValue || value.value}`}
                          />
                        </label>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-5 w-5 hover:bg-destructive/10 cursor-pointer"
                          onClick={() =>
                            setDeleteTarget({
                              type: 'value',
                              optionId: option.id,
                              valueId: value.id,
                              name: value.displayValue || value.value,
                            })
                          }
                          disabled={disabled}
                          aria-label={`Delete value ${value.displayValue || value.value}`}
                        >
                          <X className="h-3 w-3" />
                        </Button>
                      </div>
                    ))}
                  </div>

                  <div className="flex gap-2">
                    <Input
                      placeholder="Add value (e.g. Red, XL)"
                      value={newValueInputs[option.id] || ''}
                      onChange={(e) =>
                        setNewValueInputs((prev) => ({ ...prev, [option.id]: e.target.value }))
                      }
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault()
                          handleAddValue(option.id)
                        }
                      }}
                      disabled={disabled}
                      className="flex-1"
                    />
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => handleAddValue(option.id)}
                      disabled={disabled || !newValueInputs[option.id]?.trim()}
                      className="cursor-pointer"
                    >
                      <Plus className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CollapsibleContent>
            </div>
          </Collapsible>
        ))}
      </div>

      {/* Add New Option */}
      <div className="flex gap-2">
        <Input
          placeholder="Add option (e.g. Color, Size)"
          value={newOptionName}
          onChange={(e) => setNewOptionName(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault()
              handleAddOption()
            }
          }}
          disabled={disabled}
          className="flex-1"
        />
        <Button
          variant="secondary"
          onClick={handleAddOption}
          disabled={disabled || !newOptionName.trim()}
          className="cursor-pointer"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add Option
        </Button>
      </div>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={!!deleteTarget} onOpenChange={() => setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {deleteTarget?.type === 'option' ? 'Delete option?' : 'Delete value?'}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {deleteTarget?.type === 'option'
                ? `This will permanently delete the "${deleteTarget?.name}" option and all its values.`
                : `This will permanently delete the "${deleteTarget?.name}" value.`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmDelete}
              className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
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
    { id: 'v1', value: 'Black', displayValue: 'Black', colorCode: '#000000', sortOrder: 0 },
    { id: 'v2', value: 'White', displayValue: 'White', colorCode: '#FFFFFF', sortOrder: 1 },
    { id: 'v3', value: 'Navy', displayValue: 'Navy', colorCode: '#1E3A5F', sortOrder: 2 },
    { id: 'v4', value: 'Red', displayValue: 'Red', colorCode: '#EF4444', sortOrder: 3 },
  ],
}

const SIZE_OPTION: DemoOption = {
  id: 'opt-size',
  name: 'Size',
  displayName: 'Size',
  sortOrder: 1,
  values: [
    { id: 'vs1', value: 'XS', displayValue: 'XS', sortOrder: 0 },
    { id: 'vs2', value: 'S', displayValue: 'S', sortOrder: 1 },
    { id: 'vs3', value: 'M', displayValue: 'M', sortOrder: 2 },
    { id: 'vs4', value: 'L', displayValue: 'L', sortOrder: 3 },
    { id: 'vs5', value: 'XL', displayValue: 'XL', sortOrder: 4 },
  ],
}

// --- Meta ---

const meta = {
  title: 'UIKit/ProductOptionsManager',
  component: ProductOptionsManagerDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Manages product options (Color, Size, Material, etc.) and their values. ' +
          'Supports collapsible sections, color swatches, add/remove values, and delete confirmation dialogs.',
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
} satisfies Meta<typeof ProductOptionsManagerDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Empty: Story = {
  parameters: {
    docs: {
      description: { story: 'No options yet — only the "Add Option" input is shown.' },
    },
  },
  args: {
    initialOptions: [],
  },
}

export const WithColorOption: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Single option with color swatches. Click the palette icon to open the native color picker. ' +
          'Click X to delete a value, or the trash icon in the header to delete the entire option.',
      },
    },
  },
  args: {
    initialOptions: [COLOR_OPTION],
  },
}

export const WithMultipleOptions: Story = {
  parameters: {
    docs: {
      description: {
        story: 'Two options — Color (with swatches) and Size (text values). Both start expanded.',
      },
    },
  },
  args: {
    initialOptions: [COLOR_OPTION, SIZE_OPTION],
  },
}

export const EmptyValues: Story = {
  parameters: {
    docs: {
      description: { story: 'Options with no values yet — ready for values to be added.' },
    },
  },
  args: {
    initialOptions: [
      { id: 'opt-1', name: 'Material', displayName: 'Material', sortOrder: 0, values: [] },
      { id: 'opt-2', name: 'Finish', displayName: 'Finish', sortOrder: 1, values: [] },
    ],
  },
}

export const Disabled: Story = {
  parameters: {
    docs: {
      description: { story: 'Read-only state — all interactive elements are disabled.' },
    },
  },
  args: {
    initialOptions: [COLOR_OPTION, SIZE_OPTION],
    disabled: true,
  },
}
