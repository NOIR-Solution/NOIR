import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import {
  Badge,
  Button,
  InlineEditInput,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { Trash2, Loader2, Check, AlertCircle } from 'lucide-react'
import { cn } from '@/lib/utils'

// --- Visual Replica ---
// EditableVariantRow uses useVariantAutoSave (TanStack Query + API calls) and react-i18next.
// This self-contained demo replicates the visual row in different auto-save states
// without those external dependencies.

type AutoSaveStatus = 'idle' | 'dirty' | 'saving' | 'saved' | 'error'

interface DemoVariant {
  id: string
  name: string
  sku: string
  price: number
  compareAtPrice?: number | null
  costPrice?: number | null
  stockQuantity: number
  inStock: boolean
  lowStock: boolean
  sortOrder: number
}

interface StatusDotProps {
  status: AutoSaveStatus
  error?: string
}

const StatusDot = ({ status, error }: StatusDotProps) => {
  switch (status) {
    case 'dirty':
      return (
        <div
          className="w-2 h-2 rounded-full bg-yellow-500 animate-pulse"
          title="Unsaved changes"
        />
      )
    case 'saving':
      return (
        <Loader2
          className="h-4 w-4 animate-spin text-muted-foreground"
          title="Saving..."
        />
      )
    case 'saved':
      return <Check className="h-4 w-4 text-green-500" title="Saved" />
    case 'error':
      return (
        <AlertCircle
          className="h-4 w-4 text-destructive"
          title={error || 'Save failed'}
        />
      )
    default:
      return <div className="w-4 h-4" aria-hidden="true" />
  }
}

interface EditableVariantRowDemoProps {
  variant: DemoVariant
  status?: AutoSaveStatus
  saveError?: string
  isReadOnly?: boolean
}

const EditableVariantRowDemo = ({
  variant: initialVariant,
  status: initialStatus = 'idle',
  saveError,
  isReadOnly = false,
}: EditableVariantRowDemoProps) => {
  const [variant, setVariant] = useState(initialVariant)
  const [status, setStatus] = useState<AutoSaveStatus>(initialStatus)

  const handleChange = (field: keyof DemoVariant, value: string | number | null) => {
    setVariant((prev) => ({ ...prev, [field]: value }))
    setStatus('dirty')
  }

  const handleSave = () => {
    setStatus('saving')
    setTimeout(() => {
      setStatus(saveError ? 'error' : 'saved')
      if (!saveError) {
        setTimeout(() => setStatus('idle'), 2000)
      }
    }, 800)
  }

  const showOutOfStock = !variant.inStock
  const showLowStock = variant.lowStock

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="min-w-[120px]">Name</TableHead>
          <TableHead className="min-w-[100px]">SKU</TableHead>
          <TableHead className="min-w-[80px] text-right">Price</TableHead>
          <TableHead className="min-w-[80px] text-right">Compare At</TableHead>
          <TableHead className="min-w-[80px] text-right">Cost</TableHead>
          <TableHead className="min-w-[120px] text-right">Stock</TableHead>
          <TableHead className="min-w-[50px] text-right">Sort</TableHead>
          <TableHead className="w-10" />
          <TableHead className="w-8" />
        </TableRow>
      </TableHeader>
      <TableBody>
        <tr
          className={cn(
            'group border-b transition-colors hover:bg-muted/50',
            status === 'error' && 'bg-destructive/5'
          )}
        >
          <td className="p-2">
            <InlineEditInput
              value={variant.name}
              onChange={(v) => handleChange('name', v)}
              onEnterPress={handleSave}
              onEscapePress={() => setStatus('idle')}
              placeholder="Variant name"
              disabled={isReadOnly}
              className="min-w-[120px]"
            />
          </td>
          <td className="p-2">
            <InlineEditInput
              value={variant.sku || ''}
              onChange={(v) => handleChange('sku', v)}
              onEnterPress={handleSave}
              onEscapePress={() => setStatus('idle')}
              placeholder="SKU-001"
              disabled={isReadOnly}
              className="min-w-[100px]"
            />
          </td>
          <td className="p-2">
            <InlineEditInput
              type="number"
              value={variant.price.toString()}
              onChange={(v) => handleChange('price', parseFloat(v) || 0)}
              onEnterPress={handleSave}
              onEscapePress={() => setStatus('idle')}
              placeholder="0"
              disabled={isReadOnly}
              align="right"
              className="min-w-[80px]"
              min={0}
              step="any"
            />
          </td>
          <td className="p-2">
            <InlineEditInput
              type="number"
              value={variant.compareAtPrice?.toString() || ''}
              onChange={(v) => handleChange('compareAtPrice', v ? parseFloat(v) : null)}
              onEnterPress={handleSave}
              onEscapePress={() => setStatus('idle')}
              placeholder="—"
              disabled={isReadOnly}
              align="right"
              className="min-w-[80px]"
              min={0}
              step="any"
            />
          </td>
          <td className="p-2">
            <InlineEditInput
              type="number"
              value={variant.costPrice?.toString() || ''}
              onChange={(v) => handleChange('costPrice', v ? parseFloat(v) : null)}
              onEnterPress={handleSave}
              onEscapePress={() => setStatus('idle')}
              placeholder="—"
              disabled={isReadOnly}
              align="right"
              className="min-w-[80px]"
              min={0}
              step="any"
            />
          </td>
          <td className="p-2">
            <div className="flex items-center gap-2">
              <InlineEditInput
                type="number"
                value={variant.stockQuantity.toString()}
                onChange={(v) => handleChange('stockQuantity', parseInt(v) || 0)}
                onEnterPress={handleSave}
                onEscapePress={() => setStatus('idle')}
                placeholder="0"
                disabled={isReadOnly}
                align="right"
                className="min-w-[60px]"
                min={0}
              />
              {showOutOfStock && (
                <Badge variant="secondary" className="text-xs whitespace-nowrap">
                  Out of Stock
                </Badge>
              )}
              {showLowStock && !showOutOfStock && (
                <Badge variant="destructive" className="text-xs whitespace-nowrap">
                  Low Stock
                </Badge>
              )}
            </div>
          </td>
          <td className="p-2">
            <InlineEditInput
              type="number"
              value={variant.sortOrder.toString()}
              onChange={(v) => handleChange('sortOrder', parseInt(v) || 0)}
              onEnterPress={handleSave}
              onEscapePress={() => setStatus('idle')}
              placeholder="0"
              disabled={isReadOnly}
              align="right"
              className="min-w-[50px]"
              min={0}
            />
          </td>
          <td className="p-2">
            {!isReadOnly && (
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8 cursor-pointer opacity-0 group-hover:opacity-100 transition-opacity"
                onClick={() => {}}
                aria-label={`Delete variant ${variant.name}`}
              >
                <Trash2 className="h-4 w-4 text-destructive" />
              </Button>
            )}
          </td>
          <td className="p-2 w-8">
            <StatusDot status={status} error={saveError} />
          </td>
        </tr>
      </TableBody>
    </Table>
  )
}

// --- Fixtures ---

const baseVariant: DemoVariant = {
  id: 'var-1',
  name: 'Classic Black / Size M',
  sku: 'TSHIRT-BLK-M',
  price: 29.99,
  compareAtPrice: 39.99,
  costPrice: 12.5,
  stockQuantity: 45,
  inStock: true,
  lowStock: false,
  sortOrder: 0,
}

// --- Meta ---

const meta = {
  title: 'UIKit/EditableVariantRow',
  component: EditableVariantRowDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'A table row for editing product variant fields inline with auto-save status indicators. ' +
          'Real component debounces saves via useVariantAutoSave; this demo simulates all save states. ' +
          'Edit any field and press Enter to trigger a simulated save.',
      },
    },
  },
} satisfies Meta<typeof EditableVariantRowDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: { story: 'Idle state. Edit any cell to see the "dirty" indicator.' },
    },
  },
  args: {
    variant: baseVariant,
    status: 'idle',
  },
}

export const DirtyState: Story = {
  parameters: {
    docs: {
      description: { story: 'Unsaved changes — pulsing yellow dot indicates pending save.' },
    },
  },
  args: {
    variant: baseVariant,
    status: 'dirty',
  },
}

export const SavingState: Story = {
  parameters: {
    docs: {
      description: { story: 'Save in progress — spinning loader.' },
    },
  },
  args: {
    variant: baseVariant,
    status: 'saving',
  },
}

export const SavedState: Story = {
  parameters: {
    docs: {
      description: { story: 'Save successful — green checkmark (auto-clears after 2s in production).' },
    },
  },
  args: {
    variant: baseVariant,
    status: 'saved',
  },
}

export const ErrorState: Story = {
  parameters: {
    docs: {
      description: { story: 'Save failed — red alert icon with error tooltip.' },
    },
  },
  args: {
    variant: baseVariant,
    status: 'error',
    saveError: 'SKU already exists on another variant',
  },
}

export const OutOfStock: Story = {
  parameters: {
    docs: {
      description: { story: 'Zero stock — row shows "Out of Stock" badge.' },
    },
  },
  args: {
    variant: {
      ...baseVariant,
      stockQuantity: 0,
      inStock: false,
      lowStock: false,
    },
  },
}

export const LowStock: Story = {
  parameters: {
    docs: {
      description: { story: 'Low stock warning badge when quantity is below threshold.' },
    },
  },
  args: {
    variant: {
      ...baseVariant,
      stockQuantity: 4,
      inStock: true,
      lowStock: true,
    },
  },
}

export const ReadOnly: Story = {
  parameters: {
    docs: {
      description: { story: 'Read-only view mode — inputs are disabled and delete button is hidden.' },
    },
  },
  args: {
    variant: baseVariant,
    isReadOnly: true,
  },
}
