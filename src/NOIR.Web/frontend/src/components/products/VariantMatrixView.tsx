/**
 * VariantMatrixView Component
 *
 * Displays product variants in a 2D matrix grid for products with 2 option types.
 * Rows represent the first option (e.g., Size), columns represent the second (e.g., Color).
 * Each cell shows price and stock, with inline editing capability.
 *
 * For products with 1 option type, falls back to a simple list view.
 * For products with 3+ option types, shows a warning and falls back to list view.
 */
import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { motion, AnimatePresence } from 'framer-motion'
import { Save, X, AlertTriangle, Grid, List } from 'lucide-react'
import {
  Alert,
  AlertDescription,
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Input,
  Label,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@uikit'

import type { ProductVariant, ProductOption } from '@/types/product'
import { formatCurrency } from '@/lib/utils/currency'
import { cn } from '@/lib/utils'
import { LOW_STOCK_THRESHOLD } from '@/lib/constants/product'

interface VariantMatrixViewProps {
  variants: ProductVariant[]
  options: ProductOption[]
  currency: string
  onUpdateVariant?: (variantId: string, updates: { price?: number; stockQuantity?: number }) => Promise<void>
  disabled?: boolean
}

interface MatrixCell {
  variant: ProductVariant | null
  rowValue: string
  colValue: string
}

export const VariantMatrixView = ({
  variants,
  options,
  currency,
  onUpdateVariant,
  disabled = false,
}: VariantMatrixViewProps) => {
  const { t } = useTranslation('common')
  const [editingCell, setEditingCell] = useState<string | null>(null)
  const [editPrice, setEditPrice] = useState('')
  const [editStock, setEditStock] = useState('')
  const [saving, setSaving] = useState(false)
  const [viewMode, setViewMode] = useState<'matrix' | 'list'>('matrix')

  // Only show matrix for 2 option types
  const canShowMatrix = options.length === 2

  // Build matrix structure
  const { rowOption, colOption, matrix, rowValues, colValues } = useMemo(() => {
    if (!canShowMatrix || options.length < 2) {
      return { rowOption: null, colOption: null, matrix: [], rowValues: [], colValues: [] }
    }

    const rowOpt = options[0]
    const colOpt = options[1]
    const rowVals = rowOpt.values.map(v => v.value)
    const colVals = colOpt.values.map(v => v.value)

    // Create variant lookup
    const variantMap = new Map<string, ProductVariant>()
    variants.forEach(v => {
      if (v.options) {
        const rowVal = v.options[rowOpt.name]
        const colVal = v.options[colOpt.name]
        if (rowVal && colVal) {
          variantMap.set(`${rowVal}|${colVal}`, v)
        }
      }
    })

    // Build matrix
    const matrixData: MatrixCell[][] = rowVals.map(rowVal =>
      colVals.map(colVal => ({
        variant: variantMap.get(`${rowVal}|${colVal}`) || null,
        rowValue: rowVal,
        colValue: colVal,
      }))
    )

    return {
      rowOption: rowOpt,
      colOption: colOpt,
      matrix: matrixData,
      rowValues: rowVals,
      colValues: colVals,
    }
  }, [variants, options, canShowMatrix])

  const handleStartEdit = (variant: ProductVariant) => {
    if (disabled || !onUpdateVariant) return
    setEditingCell(variant.id)
    setEditPrice(variant.price.toString())
    setEditStock(variant.stockQuantity.toString())
  }

  const handleCancelEdit = () => {
    setEditingCell(null)
    setEditPrice('')
    setEditStock('')
  }

  const handleSaveEdit = async (variantId: string) => {
    if (!onUpdateVariant) return

    setSaving(true)
    try {
      await onUpdateVariant(variantId, {
        price: parseFloat(editPrice) || 0,
        stockQuantity: parseInt(editStock) || 0,
      })
      setEditingCell(null)
    } finally {
      setSaving(false)
    }
  }

  const renderCellContent = (cell: MatrixCell) => {
    if (!cell.variant) {
      return (
        <div className="text-center text-muted-foreground text-xs py-4">
          —
        </div>
      )
    }

    const isEditing = editingCell === cell.variant.id
    const isLowStock = cell.variant.stockQuantity > 0 && cell.variant.stockQuantity < LOW_STOCK_THRESHOLD
    const isOutOfStock = cell.variant.stockQuantity === 0

    if (isEditing) {
      return (
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          className="space-y-2 p-2"
        >
          <div className="space-y-1">
            <Label className="text-xs">{t('products.price', 'Price')}</Label>
            <Input
              type="number"
              step="0.01"
              min="0"
              value={editPrice}
              onChange={(e) => setEditPrice(e.target.value)}
              className="h-8 text-sm"
              autoFocus
            />
          </div>
          <div className="space-y-1">
            <Label className="text-xs">{t('labels.stock', 'Stock')}</Label>
            <Input
              type="number"
              step="1"
              min="0"
              value={editStock}
              onChange={(e) => setEditStock(e.target.value)}
              className="h-8 text-sm"
            />
          </div>
          <div className="flex gap-1">
            <Button
              size="sm"
              onClick={() => handleSaveEdit(cell.variant!.id)}
              disabled={saving}
              className="cursor-pointer flex-1 h-7 text-xs"
            >
              <Save className="h-3 w-3 mr-1" />
              {saving ? '...' : t('buttons.save', 'Save')}
            </Button>
            <Button
              size="sm"
              variant="ghost"
              onClick={handleCancelEdit}
              disabled={saving}
              className="cursor-pointer h-7 text-xs"
            >
              <X className="h-3 w-3" />
            </Button>
          </div>
        </motion.div>
      )
    }

    return (
      <Tooltip>
        <TooltipTrigger asChild>
          <motion.div
            className={cn(
              'p-2 rounded-md cursor-pointer transition-colors',
              'hover:bg-muted/50',
              isOutOfStock && 'bg-destructive/5',
              isLowStock && 'bg-amber-500/5'
            )}
            onClick={() => handleStartEdit(cell.variant!)}
            whileHover={{ scale: 1.02 }}
            whileTap={{ scale: 0.98 }}
          >
            <div className="text-center space-y-1">
              <div className="font-medium text-sm">
                {formatCurrency(cell.variant.price, currency)}
              </div>
              <Badge
                variant={isOutOfStock ? 'destructive' : isLowStock ? 'secondary' : 'outline'}
                className={cn(
                  'text-xs',
                  isLowStock && 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/30'
                )}
              >
                {cell.variant.stockQuantity}
              </Badge>
            </div>
          </motion.div>
        </TooltipTrigger>
        <TooltipContent>
          <div className="text-xs space-y-1">
            <p><strong>{cell.variant.name}</strong></p>
            {cell.variant.sku && <p>{t('products.variantSku', 'SKU')}: {cell.variant.sku}</p>}
            <p>{t('labels.clickToEdit', 'Click to edit')}</p>
          </div>
        </TooltipContent>
      </Tooltip>
    )
  }

  // If only 1 option type or 3+ option types, show list view
  if (!canShowMatrix || options.length !== 2) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <List className="h-4 w-4" />
            {t('products.variants.title', 'Variants')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {options.length > 2 && (
            <Alert className="mb-4">
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>
                {t('products.variants.matrixNotAvailable', 'Matrix view is only available for products with exactly 2 option types. Use the bulk editor for this product.')}
              </AlertDescription>
            </Alert>
          )}
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('products.variantName', 'Name')}</TableHead>
                <TableHead className="text-right">{t('products.price', 'Price')}</TableHead>
                <TableHead className="text-right">{t('labels.stock', 'Stock')}</TableHead>
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

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <Grid className="h-4 w-4" />
            {t('products.variants.matrixTitle', 'Variant Matrix')}
          </CardTitle>
          <div className="flex items-center gap-1 p-1 rounded-lg bg-muted">
            <Button
              variant={viewMode === 'matrix' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('matrix')}
              className="cursor-pointer h-7 px-2"
            >
              <Grid className="h-3 w-3" />
            </Button>
            <Button
              variant={viewMode === 'list' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('list')}
              className="cursor-pointer h-7 px-2"
            >
              <List className="h-3 w-3" />
            </Button>
          </div>
        </div>
        <p className="text-sm text-muted-foreground">
          {t('products.variants.matrixDescription', 'Click any cell to edit price and stock. Rows: {{row}}, Columns: {{col}}', {
            row: rowOption?.name || '',
            col: colOption?.name || '',
          })}
        </p>
      </CardHeader>
      <CardContent>
        <AnimatePresence mode="wait">
          {viewMode === 'matrix' ? (
            <motion.div
              key="matrix"
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -10 }}
              className="overflow-x-auto"
            >
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[120px] bg-muted/50">
                      {rowOption?.name} / {colOption?.name}
                    </TableHead>
                    {colValues.map((colVal) => (
                      <TableHead key={colVal} className="text-center min-w-[100px] bg-muted/30">
                        {colVal}
                      </TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {matrix.map((row, rowIndex) => (
                    <TableRow key={rowValues[rowIndex]}>
                      <TableCell className="font-medium bg-muted/30">
                        {rowValues[rowIndex]}
                      </TableCell>
                      {row.map((cell, colIndex) => (
                        <TableCell key={`${rowIndex}-${colIndex}`} className="p-1">
                          {renderCellContent(cell)}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </motion.div>
          ) : (
            <motion.div
              key="list"
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -10 }}
            >
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('products.variantName', 'Name')}</TableHead>
                    <TableHead>{rowOption?.name}</TableHead>
                    <TableHead>{colOption?.name}</TableHead>
                    <TableHead className="text-right">{t('products.price', 'Price')}</TableHead>
                    <TableHead className="text-right">{t('labels.stock', 'Stock')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {variants.map((variant) => (
                    <TableRow key={variant.id}>
                      <TableCell className="font-medium">{variant.name}</TableCell>
                      <TableCell>{variant.options?.[rowOption?.name || ''] || '—'}</TableCell>
                      <TableCell>{variant.options?.[colOption?.name || ''] || '—'}</TableCell>
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
            </motion.div>
          )}
        </AnimatePresence>
      </CardContent>
    </Card>
  )
}
