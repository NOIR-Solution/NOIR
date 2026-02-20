import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { Pencil, Save, X, Trash2, Check } from 'lucide-react'
import { Button, Input } from '@uikit'

// --- Visual Replica ---
// EditableVariantsTable depends on EditableVariantRow which uses TanStack Query,
// react-i18next, and API mutation hooks. This self-contained demo replicates
// the visual appearance and inline-edit interactions without those providers.
//
// Note: The real component uses InlineEditInput with auto-save (debounced API calls).
// This demo uses explicit Save/Cancel buttons to avoid needing the mutation hooks,
// so the interaction model differs slightly from production.

interface DemoVariant {
  id: string
  name: string
  sku: string
  price: number
  comparePrice?: number
  costPrice?: number
  stockQuantity: number
  sortOrder: number
}

// --- Editable Row ---

interface VariantRowProps {
  variant: DemoVariant
  isReadOnly: boolean
  showErrors?: boolean
  onDelete?: (id: string) => void
}

const VariantRow = ({
  variant: initial,
  isReadOnly,
  showErrors = false,
  onDelete,
}: VariantRowProps) => {
  // `committed` = last saved state; `draft` = currently editing state.
  // This keeps the row self-contained: saving updates what's displayed
  // and cancelling reverts to the last committed values, not the original prop.
  const [committed, setCommitted] = useState(initial)
  const [draft, setDraft] = useState(initial)
  const [isEditing, setIsEditing] = useState(false)
  const [saved, setSaved] = useState(false)

  const editSkuError = showErrors && !draft.sku ? 'SKU is required' : null
  const viewSkuError = showErrors && !committed.sku ? 'SKU is required' : null

  const handleEdit = () => {
    setDraft(committed) // always start from last committed state
    setIsEditing(true)
  }

  const handleSave = () => {
    if (editSkuError) return
    setCommitted(draft) // persist the edit — displayed values now reflect the save
    setIsEditing(false)
    setSaved(true)
    setTimeout(() => setSaved(false), 2000)
  }

  const handleCancel = () => {
    setDraft(committed) // revert to last committed, not original prop
    setIsEditing(false)
  }

  return (
    <tr className="border-b hover:bg-muted/30 transition-colors">
      {/* Name */}
      <td className="p-2 text-sm font-medium whitespace-nowrap">{initial.name}</td>

      {/* SKU */}
      <td className="p-2">
        {isEditing ? (
          <div>
            <Input
              value={draft.sku}
              onChange={(e) => setDraft((d) => ({ ...d, sku: e.target.value }))}
              className={`h-8 text-xs w-32 font-mono${editSkuError ? ' border-destructive' : ''}`}
              placeholder="e.g. SKU-001"
              autoFocus
            />
            {editSkuError && (
              <p className="text-xs text-destructive mt-0.5">{editSkuError}</p>
            )}
          </div>
        ) : (
          <div>
            <span
              className={`text-xs font-mono${viewSkuError ? ' text-destructive' : ' text-muted-foreground'}`}
            >
              {committed.sku || <span className="italic">missing</span>}
            </span>
            {viewSkuError && (
              <p className="text-xs text-destructive mt-0.5">{viewSkuError}</p>
            )}
          </div>
        )}
      </td>

      {/* Price */}
      <td className="p-2 text-right">
        {isEditing ? (
          <Input
            type="number"
            min={0}
            step={0.01}
            value={draft.price}
            onChange={(e) =>
              setDraft((d) => ({ ...d, price: parseFloat(e.target.value) || 0 }))
            }
            className="h-8 text-xs w-24 text-right"
          />
        ) : (
          <span className="text-sm">${committed.price.toFixed(2)}</span>
        )}
      </td>

      {/* Compare Price */}
      <td className="p-2 text-right">
        {isEditing ? (
          <Input
            type="number"
            min={0}
            step={0.01}
            value={draft.comparePrice ?? ''}
            onChange={(e) =>
              setDraft((d) => ({
                ...d,
                comparePrice: e.target.value ? parseFloat(e.target.value) : undefined,
              }))
            }
            className="h-8 text-xs w-24 text-right"
            placeholder="—"
          />
        ) : (
          <span className="text-sm text-muted-foreground">
            {committed.comparePrice ? `$${committed.comparePrice.toFixed(2)}` : '—'}
          </span>
        )}
      </td>

      {/* Cost Price */}
      <td className="p-2 text-right">
        {isEditing ? (
          <Input
            type="number"
            min={0}
            step={0.01}
            value={draft.costPrice ?? ''}
            onChange={(e) =>
              setDraft((d) => ({
                ...d,
                costPrice: e.target.value ? parseFloat(e.target.value) : undefined,
              }))
            }
            className="h-8 text-xs w-24 text-right"
            placeholder="—"
          />
        ) : (
          <span className="text-sm text-muted-foreground">
            {committed.costPrice ? `$${committed.costPrice.toFixed(2)}` : '—'}
          </span>
        )}
      </td>

      {/* Stock */}
      <td className="p-2 text-right">
        {isEditing ? (
          <Input
            type="number"
            min={0}
            step={1}
            value={draft.stockQuantity}
            onChange={(e) =>
              setDraft((d) => ({
                ...d,
                stockQuantity: parseInt(e.target.value) || 0,
              }))
            }
            className="h-8 text-xs w-20 text-right"
          />
        ) : (
          <span
            className={`text-sm${committed.stockQuantity === 0 ? ' text-destructive font-medium' : ''}`}
          >
            {committed.stockQuantity}
          </span>
        )}
      </td>

      {/* Sort order */}
      <td className="p-2 text-right">
        <span className="text-xs text-muted-foreground">{initial.sortOrder}</span>
      </td>

      {/* Actions */}
      <td className="p-2 w-20">
        {!isReadOnly && (
          <div className="flex gap-1 justify-end">
            {isEditing ? (
              <>
                <Button
                  size="icon"
                  variant="ghost"
                  className="h-7 w-7 cursor-pointer text-green-600 hover:text-green-700"
                  onClick={handleSave}
                  aria-label="Save changes"
                >
                  <Save className="h-3.5 w-3.5" />
                </Button>
                <Button
                  size="icon"
                  variant="ghost"
                  className="h-7 w-7 cursor-pointer"
                  onClick={handleCancel}
                  aria-label="Cancel editing"
                >
                  <X className="h-3.5 w-3.5" />
                </Button>
              </>
            ) : (
              <>
                <Button
                  size="icon"
                  variant="ghost"
                  className="h-7 w-7 cursor-pointer"
                  onClick={handleEdit}
                  aria-label="Edit variant"
                >
                  <Pencil className="h-3.5 w-3.5" />
                </Button>
                <Button
                  size="icon"
                  variant="ghost"
                  className="h-7 w-7 cursor-pointer text-destructive hover:text-destructive"
                  onClick={() => onDelete?.(initial.id)}
                  aria-label="Delete variant"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </>
            )}
          </div>
        )}
      </td>

      {/* Saved indicator */}
      <td className="p-2 w-8">
        {saved && <Check className="h-4 w-4 text-green-500" />}
      </td>
    </tr>
  )
}

// --- Demo Component ---

interface EditableVariantsTableDemoProps {
  variants: DemoVariant[]
  isReadOnly?: boolean
  showErrors?: boolean
}

const EditableVariantsTableDemo = ({
  variants: initialVariants,
  isReadOnly = false,
  showErrors = false,
}: EditableVariantsTableDemoProps) => {
  const [variants, setVariants] = useState(initialVariants)

  if (variants.length === 0) {
    return (
      <p className="text-center text-muted-foreground py-8">
        No variants yet. Add attributes to generate variants automatically.
      </p>
    )
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse">
        <thead>
          <tr className="border-b bg-muted/50">
            <th className="p-2 text-left text-sm font-medium text-muted-foreground">
              Name
            </th>
            <th className="p-2 text-left text-sm font-medium text-muted-foreground">
              SKU
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              Price
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              Compare Price
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              Cost Price
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              Stock
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              Sort
            </th>
            <th className="p-2 w-20">{/* Actions */}</th>
            <th className="p-2 w-8">{/* Status */}</th>
          </tr>
        </thead>
        <tbody>
          {variants.map((variant) => (
            <VariantRow
              key={variant.id}
              variant={variant}
              isReadOnly={isReadOnly}
              showErrors={showErrors}
              onDelete={(id) =>
                setVariants((vs) => vs.filter((v) => v.id !== id))
              }
            />
          ))}
        </tbody>
      </table>
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/EditableVariantsTable',
  component: EditableVariantsTableDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
  },
} satisfies Meta<typeof EditableVariantsTableDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Helpers ---

/** id, name, sku, price, stock, sortOrder, comparePrice?, costPrice? */
const makeVariant = (
  id: string,
  name: string,
  sku: string,
  price: number,
  stock: number,
  sort: number,
  comparePrice?: number,
  costPrice?: number
): DemoVariant => ({
  id,
  name,
  sku,
  price,
  comparePrice,
  costPrice,
  stockQuantity: stock,
  sortOrder: sort,
})

// --- Stories ---

export const Default: Story = {
  args: {
    variants: [
      makeVariant('1', 'Size: S / Color: Red', 'SHIRT-S-RED', 29.99, 50, 1, 39.99, 15.0),
      makeVariant('2', 'Size: M / Color: Red', 'SHIRT-M-RED', 29.99, 30, 2, 39.99, 15.0),
      makeVariant('3', 'Size: L / Color: Red', 'SHIRT-L-RED', 29.99, 10, 3, 39.99, 15.0),
    ],
  },
}

export const Empty: Story = {
  args: {
    variants: [],
  },
}

export const SingleVariant: Story = {
  args: {
    variants: [
      makeVariant('1', 'Default', 'PROD-DEFAULT', 99.99, 25, 1, 129.99, 45.0),
    ],
  },
}

export const WithErrors: Story = {
  name: 'WithErrors (missing SKUs)',
  args: {
    variants: [
      makeVariant('1', 'Size: S / Color: Blue', '', 19.99, 20, 1),
      makeVariant('2', 'Size: M / Color: Blue', 'SHIRT-M-BLUE', 19.99, 15, 2),
      makeVariant('3', 'Size: L / Color: Blue', '', 19.99, 0, 3),
    ],
    showErrors: true,
  },
}

export const ReadOnly: Story = {
  args: {
    variants: [
      makeVariant('1', 'Size: XS / Color: Black', 'JACKET-XS-BLK', 149.99, 100, 1, 199.99, 65.0),
      makeVariant('2', 'Size: S / Color: Black', 'JACKET-S-BLK', 149.99, 75, 2, 199.99, 65.0),
      makeVariant('3', 'Size: M / Color: Black', 'JACKET-M-BLK', 149.99, 50, 3, 199.99, 65.0),
      makeVariant('4', 'Size: L / Color: Black', 'JACKET-L-BLK', 149.99, 0, 4, 199.99, 65.0),
    ],
    isReadOnly: true,
  },
}

export const ManyVariants: Story = {
  args: {
    variants: [
      makeVariant('1', 'Size: XS / Color: Red', 'TEE-XS-RED', 24.99, 80, 1, 34.99, 10.0),
      makeVariant('2', 'Size: S / Color: Red', 'TEE-S-RED', 24.99, 65, 2, 34.99, 10.0),
      makeVariant('3', 'Size: M / Color: Red', 'TEE-M-RED', 24.99, 45, 3, 34.99, 10.0),
      makeVariant('4', 'Size: L / Color: Red', 'TEE-L-RED', 24.99, 20, 4, 34.99, 10.0),
      makeVariant('5', 'Size: XL / Color: Red', 'TEE-XL-RED', 24.99, 0, 5, 34.99, 10.0),
      makeVariant('6', 'Size: XS / Color: Blue', 'TEE-XS-BLU', 24.99, 72, 6, 34.99, 10.0),
      makeVariant('7', 'Size: S / Color: Blue', 'TEE-S-BLU', 24.99, 55, 7, 34.99, 10.0),
      makeVariant('8', 'Size: M / Color: Blue', 'TEE-M-BLU', 24.99, 38, 8, 34.99, 10.0),
    ],
  },
}
