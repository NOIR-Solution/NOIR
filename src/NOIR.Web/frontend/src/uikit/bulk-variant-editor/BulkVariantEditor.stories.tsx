import type { Meta, StoryObj } from 'storybook'
import { useState, useMemo } from 'react'
import { Edit2, Percent, Save } from 'lucide-react'
import {
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  ScrollArea,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'

// --- Visual Replica ---
// BulkVariantEditor depends on react-i18next, product types, and API mutation hooks.
// This self-contained demo replicates the visual appearance and bulk-edit interactions
// without requiring those external contexts.

interface DemoVariant {
  id: string
  name: string
  sku: string
  price: number
  stockQuantity: number
}

type BulkAction = 'setPrice' | 'adjustPrice' | 'percentPrice' | 'setStock' | 'adjustStock'

const BULK_ACTION_LABELS: Record<BulkAction, string> = {
  setPrice: 'Set price to',
  adjustPrice: 'Adjust price by',
  percentPrice: 'Adjust price by %',
  setStock: 'Set stock to',
  adjustStock: 'Adjust stock by',
}

// --- Demo Component ---

interface BulkVariantEditorDemoProps {
  variants: DemoVariant[]
  /** Start with the dialog already open */
  defaultOpen?: boolean
}

const BulkVariantEditorDemo = ({
  variants: initialVariants,
  defaultOpen = true,
}: BulkVariantEditorDemoProps) => {
  const [isOpen, setIsOpen] = useState(defaultOpen)
  const [variants] = useState(initialVariants)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [editedVariants, setEditedVariants] = useState<Map<string, Partial<DemoVariant>>>(
    new Map()
  )
  const [bulkAction, setBulkAction] = useState<BulkAction>('setPrice')
  const [bulkValue, setBulkValue] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  // Merge original variants with edits
  const displayVariants = useMemo(
    () =>
      variants.map((v) => ({
        ...v,
        ...editedVariants.get(v.id),
      })),
    [variants, editedVariants]
  )

  const handleOpen = () => {
    setSelectedIds(new Set())
    setEditedVariants(new Map())
    setBulkValue('')
    setIsOpen(true)
  }

  const handleToggleSelect = (id: string) => {
    const next = new Set(selectedIds)
    if (next.has(id)) {
      next.delete(id)
    } else {
      next.add(id)
    }
    setSelectedIds(next)
  }

  const handleSelectAll = () => {
    setSelectedIds(new Set(variants.map((v) => v.id)))
  }

  const handleSelectNone = () => {
    setSelectedIds(new Set())
  }

  const handleCellChange = (id: string, field: keyof DemoVariant, value: string) => {
    const current = editedVariants.get(id) || {}
    const numValue = parseFloat(value) || 0

    setEditedVariants(
      new Map(editedVariants).set(id, {
        ...current,
        [field]: field === 'sku' ? value : numValue,
      })
    )
  }

  const handleApplyBulkAction = () => {
    if (selectedIds.size === 0 || !bulkValue) return

    const value = parseFloat(bulkValue) || 0
    const updates = new Map(editedVariants)

    selectedIds.forEach((id) => {
      const original = variants.find((v) => v.id === id)
      if (!original) return

      const current = updates.get(id) || {}
      let newValue: number

      switch (bulkAction) {
        case 'setPrice':
          updates.set(id, { ...current, price: value })
          break
        case 'adjustPrice':
          newValue = (current.price ?? original.price) + value
          updates.set(id, { ...current, price: Math.max(0, newValue) })
          break
        case 'percentPrice':
          newValue = (current.price ?? original.price) * (1 + value / 100)
          updates.set(id, { ...current, price: Math.max(0, Math.round(newValue * 100) / 100) })
          break
        case 'setStock':
          updates.set(id, { ...current, stockQuantity: Math.max(0, Math.floor(value)) })
          break
        case 'adjustStock':
          newValue = (current.stockQuantity ?? original.stockQuantity) + Math.floor(value)
          updates.set(id, { ...current, stockQuantity: Math.max(0, newValue) })
          break
      }
    })

    setEditedVariants(updates)
    setBulkValue('')
  }

  const handleSave = () => {
    setIsSaving(true)
    // Simulate saving
    setTimeout(() => {
      setIsSaving(false)
      setIsOpen(false)
    }, 1000)
  }

  const hasChanges = editedVariants.size > 0

  return (
    <>
      <Button
        variant="outline"
        onClick={handleOpen}
        disabled={variants.length === 0}
        className="cursor-pointer"
      >
        <Edit2 className="h-4 w-4 mr-2" />
        Bulk Edit Variants
      </Button>

      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="sm:max-w-[900px]">
          <DialogHeader>
            <DialogTitle>Bulk Edit Variants</DialogTitle>
            <DialogDescription>
              Edit pricing and stock for multiple variants at once. Select variants and apply bulk
              actions.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {/* Bulk action bar */}
            <div className="flex items-end gap-2 p-3 rounded-lg bg-muted/50">
              <div className="space-y-1">
                <Label className="text-xs">Bulk Action</Label>
                <Select
                  value={bulkAction}
                  onValueChange={(v) => setBulkAction(v as BulkAction)}
                >
                  <SelectTrigger className="w-[180px] cursor-pointer">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {(Object.keys(BULK_ACTION_LABELS) as BulkAction[]).map((action) => (
                      <SelectItem key={action} value={action} className="cursor-pointer">
                        {BULK_ACTION_LABELS[action]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1">
                <Label className="text-xs">Value</Label>
                <div className="relative">
                  <Input
                    type="number"
                    value={bulkValue}
                    onChange={(e) => setBulkValue(e.target.value)}
                    className="w-[120px]"
                    placeholder="0"
                  />
                  {bulkAction === 'percentPrice' && (
                    <Percent className="absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  )}
                </div>
              </div>

              <Button
                onClick={handleApplyBulkAction}
                disabled={selectedIds.size === 0 || !bulkValue}
                className="cursor-pointer"
              >
                Apply to {selectedIds.size} selected
              </Button>

              <div className="flex-1" />

              <Button
                variant="ghost"
                size="sm"
                onClick={handleSelectAll}
                className="cursor-pointer"
              >
                Select All
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleSelectNone}
                className="cursor-pointer"
              >
                Select None
              </Button>
            </div>

            {/* Variants table */}
            <ScrollArea className="h-[400px] rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[40px]" />
                    <TableHead>Variant Name</TableHead>
                    <TableHead className="w-[100px]">SKU</TableHead>
                    <TableHead className="w-[120px]">Price</TableHead>
                    <TableHead className="w-[100px]">Stock</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {displayVariants.map((variant) => {
                    const isEdited = editedVariants.has(variant.id)
                    return (
                      <TableRow
                        key={variant.id}
                        className={isEdited ? 'bg-yellow-50 dark:bg-yellow-950/20' : ''}
                      >
                        <TableCell>
                          <Checkbox
                            checked={selectedIds.has(variant.id)}
                            onCheckedChange={() => handleToggleSelect(variant.id)}
                            className="cursor-pointer"
                          />
                        </TableCell>
                        <TableCell className="font-medium">{variant.name}</TableCell>
                        <TableCell>
                          <Input
                            value={variant.sku || ''}
                            onChange={(e) => handleCellChange(variant.id, 'sku', e.target.value)}
                            className="h-8"
                          />
                        </TableCell>
                        <TableCell>
                          <Input
                            type="number"
                            step="0.01"
                            min="0"
                            value={variant.price}
                            onChange={(e) => handleCellChange(variant.id, 'price', e.target.value)}
                            className="h-8"
                          />
                        </TableCell>
                        <TableCell>
                          <Input
                            type="number"
                            step="1"
                            min="0"
                            value={variant.stockQuantity}
                            onChange={(e) =>
                              handleCellChange(variant.id, 'stockQuantity', e.target.value)
                            }
                            className="h-8"
                          />
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            </ScrollArea>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsOpen(false)} className="cursor-pointer">
              Cancel
            </Button>
            <Button
              onClick={handleSave}
              disabled={isSaving || !hasChanges}
              className="cursor-pointer"
            >
              <Save className="h-4 w-4 mr-2" />
              {isSaving ? 'Saving...' : 'Save Changes'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/BulkVariantEditor',
  component: BulkVariantEditorDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
} satisfies Meta<typeof BulkVariantEditorDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Helpers ---

const makeVariant = (
  id: string,
  name: string,
  sku: string,
  price: number,
  stock: number
): DemoVariant => ({
  id,
  name,
  sku,
  price,
  stockQuantity: stock,
})

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Spreadsheet-like bulk editor for product variants. ' +
          'Select variants via checkboxes and apply bulk price/stock changes. ' +
          'Edited rows are highlighted in yellow. ' +
          'This is a visual replica — the real component uses react-i18next and API mutation hooks.',
      },
    },
  },
  args: {
    variants: [
      makeVariant('1', 'Size: S / Color: Red', 'SHIRT-S-RED', 29.99, 50),
      makeVariant('2', 'Size: M / Color: Red', 'SHIRT-M-RED', 29.99, 30),
      makeVariant('3', 'Size: L / Color: Red', 'SHIRT-L-RED', 29.99, 10),
      makeVariant('4', 'Size: S / Color: Blue', 'SHIRT-S-BLU', 34.99, 45),
      makeVariant('5', 'Size: M / Color: Blue', 'SHIRT-M-BLU', 34.99, 25),
      makeVariant('6', 'Size: L / Color: Blue', 'SHIRT-L-BLU', 34.99, 8),
    ],
  },
}

export const Empty: Story = {
  parameters: {
    docs: {
      description: {
        story: 'When no variants exist, the bulk edit button is disabled.',
      },
    },
  },
  args: {
    variants: [],
    defaultOpen: false,
  },
}

export const WithSelection: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Demonstrates the selection flow. Use "Select All" to select all variants, ' +
          'choose a bulk action (e.g., "Set price to"), enter a value, and click "Apply".',
      },
    },
  },
  args: {
    variants: [
      makeVariant('1', 'Size: XS / Color: Black', 'JACK-XS-BLK', 149.99, 100),
      makeVariant('2', 'Size: S / Color: Black', 'JACK-S-BLK', 149.99, 75),
      makeVariant('3', 'Size: M / Color: Black', 'JACK-M-BLK', 149.99, 50),
      makeVariant('4', 'Size: L / Color: Black', 'JACK-L-BLK', 149.99, 25),
      makeVariant('5', 'Size: XL / Color: Black', 'JACK-XL-BLK', 149.99, 0),
    ],
  },
}

export const ManyVariants: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'A large variant set showing scrollable table behavior. ' +
          'The table scrolls within a 400px container.',
      },
    },
  },
  args: {
    variants: [
      makeVariant('1', 'Size: XS / Color: Red', 'TEE-XS-RED', 24.99, 80),
      makeVariant('2', 'Size: S / Color: Red', 'TEE-S-RED', 24.99, 65),
      makeVariant('3', 'Size: M / Color: Red', 'TEE-M-RED', 24.99, 45),
      makeVariant('4', 'Size: L / Color: Red', 'TEE-L-RED', 24.99, 20),
      makeVariant('5', 'Size: XL / Color: Red', 'TEE-XL-RED', 24.99, 0),
      makeVariant('6', 'Size: XS / Color: Blue', 'TEE-XS-BLU', 24.99, 72),
      makeVariant('7', 'Size: S / Color: Blue', 'TEE-S-BLU', 24.99, 55),
      makeVariant('8', 'Size: M / Color: Blue', 'TEE-M-BLU', 24.99, 38),
      makeVariant('9', 'Size: L / Color: Blue', 'TEE-L-BLU', 24.99, 22),
      makeVariant('10', 'Size: XL / Color: Blue', 'TEE-XL-BLU', 24.99, 10),
      makeVariant('11', 'Size: XS / Color: Green', 'TEE-XS-GRN', 24.99, 60),
      makeVariant('12', 'Size: S / Color: Green', 'TEE-S-GRN', 24.99, 48),
      makeVariant('13', 'Size: M / Color: Green', 'TEE-M-GRN', 24.99, 30),
      makeVariant('14', 'Size: L / Color: Green', 'TEE-L-GRN', 24.99, 15),
      makeVariant('15', 'Size: XL / Color: Green', 'TEE-XL-GRN', 24.99, 5),
    ],
  },
}
