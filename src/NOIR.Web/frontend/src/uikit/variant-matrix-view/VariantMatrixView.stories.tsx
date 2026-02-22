import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { Grid, List } from 'lucide-react'
import { cn } from '@/lib/utils'

// --- Visual Replica ---
// VariantMatrixView uses framer-motion and react-i18next.
// This self-contained demo replicates the visual matrix with identical layout
// without those dependencies.

interface DemoVariant {
  id: string
  name: string
  sku?: string
  price: number
  stockQuantity: number
  options: Record<string, string>
}

interface DemoOption {
  id: string
  name: string
  displayName?: string
  values: Array<{ id: string; value: string; displayValue: string }>
}

const formatCurrency = (price: number, currency: string) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(price)

const LOW_STOCK_THRESHOLD = 10

interface VariantMatrixViewDemoProps {
  variants: DemoVariant[]
  options: DemoOption[]
  currency?: string
}

const VariantMatrixViewDemo = ({
  variants,
  options,
  currency = 'USD',
}: VariantMatrixViewDemoProps) => {
  const [viewMode, setViewMode] = useState<'matrix' | 'list'>('matrix')

  const canShowMatrix = options.length === 2

  if (!canShowMatrix) {
    // List view fallback
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <List className="h-4 w-4" />
            Variants
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead className="text-right">Price</TableHead>
                <TableHead className="text-right">Stock</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {variants.map((variant) => (
                <TableRow key={variant.id}>
                  <TableCell className="font-medium">{variant.name}</TableCell>
                  <TableCell className="text-right">
                    {formatCurrency(variant.price, currency)}
                  </TableCell>
                  <TableCell className="text-right">
                    <Badge variant={variant.stockQuantity === 0 ? 'destructive' : 'outline'}>
                      {variant.stockQuantity}
                    </Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    )
  }

  const rowOption = options[0]
  const colOption = options[1]
  const rowValues = rowOption.values.map((v) => v.value)
  const colValues = colOption.values.map((v) => v.value)

  const variantMap = new Map<string, DemoVariant>()
  variants.forEach((v) => {
    const rowVal = v.options[rowOption.name]
    const colVal = v.options[colOption.name]
    if (rowVal && colVal) {
      variantMap.set(`${rowVal}|${colVal}`, v)
    }
  })

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <Grid className="h-4 w-4" />
            Variant Matrix
          </CardTitle>
          <div className="flex items-center gap-1 p-1 rounded-lg bg-muted">
            <Button
              variant={viewMode === 'matrix' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('matrix')}
              className="cursor-pointer h-7 px-2"
              aria-label="Matrix view"
            >
              <Grid className="h-3 w-3" />
            </Button>
            <Button
              variant={viewMode === 'list' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('list')}
              className="cursor-pointer h-7 px-2"
              aria-label="List view"
            >
              <List className="h-3 w-3" />
            </Button>
          </div>
        </div>
        <p className="text-sm text-muted-foreground">
          Click any cell to edit price and stock. Rows: {rowOption.name}, Columns: {colOption.name}
        </p>
      </CardHeader>
      <CardContent>
        {viewMode === 'matrix' ? (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[120px] bg-muted/50">
                    {rowOption.name} / {colOption.name}
                  </TableHead>
                  {colValues.map((colVal) => (
                    <TableHead key={colVal} className="text-center min-w-[100px] bg-muted/30">
                      {colVal}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {rowValues.map((rowVal) => (
                  <TableRow key={rowVal}>
                    <TableCell className="font-medium bg-muted/30">{rowVal}</TableCell>
                    {colValues.map((colVal) => {
                      const variant = variantMap.get(`${rowVal}|${colVal}`)
                      if (!variant) {
                        return (
                          <TableCell key={colVal} className="p-1">
                            <div className="text-center text-muted-foreground text-xs py-4">—</div>
                          </TableCell>
                        )
                      }
                      const isLowStock =
                        variant.stockQuantity > 0 && variant.stockQuantity < LOW_STOCK_THRESHOLD
                      const isOutOfStock = variant.stockQuantity === 0
                      return (
                        <TableCell key={colVal} className="p-1">
                          <div
                            className={cn(
                              'p-2 rounded-md cursor-pointer transition-colors hover:bg-muted/50 text-center space-y-1',
                              isOutOfStock && 'bg-destructive/5',
                              isLowStock && 'bg-amber-500/5'
                            )}
                          >
                            <div className="font-medium text-sm">
                              {formatCurrency(variant.price, currency)}
                            </div>
                            <Badge
                              variant={
                                isOutOfStock ? 'destructive' : isLowStock ? 'secondary' : 'outline'
                              }
                              className={cn(
                                'text-xs',
                                isLowStock &&
                                  'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/30'
                              )}
                            >
                              {variant.stockQuantity}
                            </Badge>
                          </div>
                        </TableCell>
                      )
                    })}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>{rowOption.name}</TableHead>
                <TableHead>{colOption.name}</TableHead>
                <TableHead className="text-right">Price</TableHead>
                <TableHead className="text-right">Stock</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {variants.map((variant) => (
                <TableRow key={variant.id}>
                  <TableCell className="font-medium">{variant.name}</TableCell>
                  <TableCell>{variant.options[rowOption.name] || '—'}</TableCell>
                  <TableCell>{variant.options[colOption.name] || '—'}</TableCell>
                  <TableCell className="text-right">
                    {formatCurrency(variant.price, currency)}
                  </TableCell>
                  <TableCell className="text-right">
                    <Badge variant={variant.stockQuantity === 0 ? 'destructive' : 'outline'}>
                      {variant.stockQuantity}
                    </Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>
    </Card>
  )
}

// --- Fixtures ---

const SIZE_OPTION: DemoOption = {
  id: 'opt-size',
  name: 'Size',
  values: [
    { id: 'v-xs', value: 'XS', displayValue: 'XS' },
    { id: 'v-s', value: 'S', displayValue: 'S' },
    { id: 'v-m', value: 'M', displayValue: 'M' },
    { id: 'v-l', value: 'L', displayValue: 'L' },
  ],
}

const COLOR_OPTION: DemoOption = {
  id: 'opt-color',
  name: 'Color',
  values: [
    { id: 'c-black', value: 'Black', displayValue: 'Black' },
    { id: 'c-white', value: 'White', displayValue: 'White' },
    { id: 'c-navy', value: 'Navy', displayValue: 'Navy' },
  ],
}

const makeVariants = (): DemoVariant[] => {
  const variants: DemoVariant[] = []
  const prices: Record<string, number> = {
    'XS-Black': 29.99,
    'XS-White': 29.99,
    'XS-Navy': 32.99,
    'S-Black': 29.99,
    'S-White': 29.99,
    'S-Navy': 32.99,
    'M-Black': 29.99,
    'M-White': 0, // Will skip this one
    'M-Navy': 32.99,
    'L-Black': 34.99,
    'L-White': 34.99,
    'L-Navy': 37.99,
  }
  const stock: Record<string, number> = {
    'XS-Black': 45,
    'XS-White': 32,
    'XS-Navy': 8,
    'S-Black': 60,
    'S-White': 0,
    'S-Navy': 5,
    'M-Black': 78,
    // M-White is missing — no variant
    'M-Navy': 12,
    'L-Black': 0,
    'L-White': 15,
    'L-Navy': 22,
  }

  let idx = 0
  for (const size of SIZE_OPTION.values) {
    for (const color of COLOR_OPTION.values) {
      const key = `${size.value}-${color.value}`
      if (key === 'M-White') continue // Missing variant
      variants.push({
        id: `var-${idx++}`,
        name: `${size.value} / ${color.value}`,
        sku: `TSHIRT-${size.value}-${color.value.toUpperCase()}`,
        price: prices[key] ?? 29.99,
        stockQuantity: stock[key] ?? 0,
        options: { Size: size.value, Color: color.value },
      })
    }
  }
  return variants
}

const SINGLE_OPTION: DemoOption = {
  id: 'opt-size',
  name: 'Size',
  values: [
    { id: 'v-s', value: 'S', displayValue: 'S' },
    { id: 'v-m', value: 'M', displayValue: 'M' },
    { id: 'v-l', value: 'L', displayValue: 'L' },
  ],
}

const singleOptionVariants: DemoVariant[] = [
  { id: 'v1', name: 'Small', price: 29.99, stockQuantity: 15, options: { Size: 'S' } },
  { id: 'v2', name: 'Medium', price: 29.99, stockQuantity: 0, options: { Size: 'M' } },
  { id: 'v3', name: 'Large', price: 34.99, stockQuantity: 8, options: { Size: 'L' } },
]

// --- Meta ---

const meta = {
  title: 'UIKit/VariantMatrixView',
  component: VariantMatrixViewDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Displays product variants in a 2D matrix grid for products with exactly 2 option types. ' +
          'Toggle between matrix and list view. Cells highlight out-of-stock (red) and low-stock (amber) states. ' +
          'Note: inline editing (framer-motion) is not replicated in this visual demo.',
      },
    },
  },
} satisfies Meta<typeof VariantMatrixViewDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  args: {
    variants: makeVariants(),
    options: [SIZE_OPTION, COLOR_OPTION],
    currency: 'USD',
  },
}

export const ListViewFallback: Story = {
  parameters: {
    docs: {
      description: {
        story: 'When there is only 1 option type, matrix view is not available — falls back to a simple list.',
      },
    },
  },
  args: {
    variants: singleOptionVariants,
    options: [SINGLE_OPTION],
    currency: 'USD',
  },
}

export const AllOutOfStock: Story = {
  parameters: {
    docs: {
      description: { story: 'All variants are out of stock — cells show red destructive badge.' },
    },
  },
  args: {
    variants: makeVariants().map((v) => ({ ...v, stockQuantity: 0 })),
    options: [SIZE_OPTION, COLOR_OPTION],
    currency: 'USD',
  },
}

export const VietnamDong: Story = {
  parameters: {
    docs: {
      description: { story: 'Matrix with Vietnamese Dong currency formatting.' },
    },
  },
  args: {
    variants: makeVariants().map((v) => ({ ...v, price: v.price * 25000 })),
    options: [SIZE_OPTION, COLOR_OPTION],
    currency: 'VND',
  },
}
