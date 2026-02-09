/**
 * BulkVariantEditor Component
 *
 * Provides a spreadsheet-like interface for editing multiple variants at once.
 * Supports bulk price and stock updates.
 */
import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Save, Edit2, Percent } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { ScrollArea } from '@/components/ui/scroll-area'
import type { ProductVariant } from '@/types/product'

interface BulkVariantEditorProps {
  variants: ProductVariant[]
  onSave: (updates: VariantUpdate[]) => Promise<void>
  disabled?: boolean
}

export interface VariantUpdate {
  id: string
  price?: number
  stockQuantity?: number
  sku?: string
}

type BulkAction = 'setPrice' | 'adjustPrice' | 'percentPrice' | 'setStock' | 'adjustStock'

export function BulkVariantEditor({
  variants,
  onSave,
  disabled = false,
}: BulkVariantEditorProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [editedVariants, setEditedVariants] = useState<Map<string, Partial<ProductVariant>>>(
    new Map()
  )
  const [bulkAction, setBulkAction] = useState<BulkAction>('setPrice')
  const [bulkValue, setBulkValue] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  // Merge original variants with edits
  const displayVariants = useMemo(() => {
    return variants.map((v) => ({
      ...v,
      ...editedVariants.get(v.id),
    }))
  }, [variants, editedVariants])

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

  const handleCellChange = (id: string, field: keyof ProductVariant, value: string) => {
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
          updates.set(id, { ...current, price: Math.max(0, newValue) })
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

  const handleSave = async () => {
    if (editedVariants.size === 0) {
      setIsOpen(false)
      return
    }

    setIsSaving(true)
    try {
      const updates: VariantUpdate[] = []

      editedVariants.forEach((edits, id) => {
        const original = variants.find((v) => v.id === id)
        if (!original) return

        const update: VariantUpdate = { id }
        if (edits.price !== undefined && edits.price !== original.price) {
          update.price = edits.price
        }
        if (edits.stockQuantity !== undefined && edits.stockQuantity !== original.stockQuantity) {
          update.stockQuantity = edits.stockQuantity
        }
        if (edits.sku !== undefined && edits.sku !== original.sku) {
          update.sku = edits.sku || undefined
        }

        if (update.price !== undefined || update.stockQuantity !== undefined || update.sku !== undefined) {
          updates.push(update)
        }
      })

      if (updates.length > 0) {
        await onSave(updates)
      }
      setIsOpen(false)
    } finally {
      setIsSaving(false)
    }
  }

  const hasChanges = editedVariants.size > 0

  return (
    <>
      <Button
        variant="outline"
        onClick={handleOpen}
        disabled={disabled || variants.length === 0}
        className="cursor-pointer"
      >
        <Edit2 className="h-4 w-4 mr-2" />
        {t('products.variants.bulkEdit')}
      </Button>

      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="sm:max-w-[900px]">
          <DialogHeader>
            <DialogTitle>{t('products.variants.bulkEditTitle')}</DialogTitle>
            <DialogDescription>
              {t('products.variants.bulkEditDescription')}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {/* Bulk action bar */}
            <div className="flex items-end gap-2 p-3 rounded-lg bg-muted/50">
              <div className="space-y-1">
                <Label className="text-xs">{t('products.variants.bulkAction')}</Label>
                <Select
                  value={bulkAction}
                  onValueChange={(v) => setBulkAction(v as BulkAction)}
                >
                  <SelectTrigger className="w-[180px] cursor-pointer">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="setPrice" className="cursor-pointer">
                      {t('products.variants.actions.setPrice')}
                    </SelectItem>
                    <SelectItem value="adjustPrice" className="cursor-pointer">
                      {t('products.variants.actions.adjustPrice')}
                    </SelectItem>
                    <SelectItem value="percentPrice" className="cursor-pointer">
                      {t('products.variants.actions.percentPrice')}
                    </SelectItem>
                    <SelectItem value="setStock" className="cursor-pointer">
                      {t('products.variants.actions.setStock')}
                    </SelectItem>
                    <SelectItem value="adjustStock" className="cursor-pointer">
                      {t('products.variants.actions.adjustStock')}
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1">
                <Label className="text-xs">{t('products.variants.value')}</Label>
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
                {t('products.variants.applyToSelected', { count: selectedIds.size })}
              </Button>

              <div className="flex-1" />

              <Button variant="ghost" size="sm" onClick={handleSelectAll} className="cursor-pointer">
                {t('buttons.selectAll')}
              </Button>
              <Button variant="ghost" size="sm" onClick={handleSelectNone} className="cursor-pointer">
                {t('buttons.selectNone')}
              </Button>
            </div>

            {/* Variants table */}
            <ScrollArea className="h-[400px] rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[40px]" />
                    <TableHead>{t('products.variantName')}</TableHead>
                    <TableHead className="w-[100px]">{t('products.variantSku')}</TableHead>
                    <TableHead className="w-[120px]">{t('products.variantPrice')}</TableHead>
                    <TableHead className="w-[100px]">{t('products.variantStock')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {displayVariants.map((variant) => {
                    const isEdited = editedVariants.has(variant.id)
                    return (
                      <TableRow key={variant.id} className={isEdited ? 'bg-yellow-50 dark:bg-yellow-950/20' : ''}>
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
              {t('buttons.cancel')}
            </Button>
            <Button
              onClick={handleSave}
              disabled={isSaving || !hasChanges}
              className="cursor-pointer"
            >
              <Save className="h-4 w-4 mr-2" />
              {isSaving ? t('buttons.saving') : t('buttons.saveChanges')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
