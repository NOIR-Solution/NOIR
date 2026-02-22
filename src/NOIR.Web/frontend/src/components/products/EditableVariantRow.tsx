import { useTranslation } from 'react-i18next'
import { Trash2, Loader2, Check, AlertCircle } from 'lucide-react'
import { cn } from '@/lib/utils'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import {
  Badge,
  Button,
  InlineEditInput,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@uikit'

import {
  useVariantAutoSave,
  type AutoSaveStatus,
} from '@/hooks/useVariantAutoSave'
import type { ProductVariant } from '@/types/product'

interface EditableVariantRowProps {
  /** Product ID for API calls */
  productId: string
  /** Variant data */
  variant: ProductVariant
  /** Whether the row is in read-only mode */
  isReadOnly?: boolean
  /** Callback when delete is requested */
  onDelete?: (variant: ProductVariant) => void
  /** Callback when variant is successfully saved */
  onSaveSuccess?: (updatedVariant: ProductVariant) => void
}

const StatusIndicator = ({ status, error }: { status: AutoSaveStatus; error: string | null }) => {
  const { t } = useTranslation('common')

  switch (status) {
    case 'dirty':
      return (
        <TooltipProvider delayDuration={0}>
          <Tooltip>
            <TooltipTrigger asChild>
              <div
                className="w-2 h-2 rounded-full bg-yellow-500 animate-pulse"
                role="status"
                aria-label={t('products.variants.statusDirty')}
              />
            </TooltipTrigger>
            <TooltipContent side="left" className="text-xs">
              {t('products.variants.statusDirty')}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      )
    case 'saving':
      return (
        <TooltipProvider delayDuration={0}>
          <Tooltip>
            <TooltipTrigger asChild>
              <Loader2
                className="h-4 w-4 animate-spin text-muted-foreground"
                role="status"
                aria-label={t('products.variants.statusSaving')}
              />
            </TooltipTrigger>
            <TooltipContent side="left" className="text-xs">
              {t('products.variants.statusSaving')}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      )
    case 'saved':
      return (
        <TooltipProvider delayDuration={0}>
          <Tooltip>
            <TooltipTrigger asChild>
              <Check
                className="h-4 w-4 text-green-500"
                role="status"
                aria-label={t('products.variants.statusSaved')}
              />
            </TooltipTrigger>
            <TooltipContent side="left" className="text-xs">
              {t('products.variants.statusSaved')}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      )
    case 'error':
      return (
        <TooltipProvider delayDuration={0}>
          <Tooltip>
            <TooltipTrigger asChild>
              <AlertCircle
                className="h-4 w-4 text-destructive"
                role="alert"
                aria-label={error || t('products.variants.statusError')}
              />
            </TooltipTrigger>
            <TooltipContent side="left" className="text-xs bg-destructive text-destructive-foreground">
              {error || t('products.variants.statusError')}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      )
    default:
      return <div className="w-4 h-4" aria-hidden="true" /> // Placeholder for layout consistency
  }
}

export const EditableVariantRow = ({
  productId,
  variant,
  isReadOnly = false,
  onDelete,
  onSaveSuccess,
}: EditableVariantRowProps) => {
  const { t } = useTranslation('common')

  const {
    values,
    setFieldValue,
    status,
    error,
    saveNow,
    revert,
    hasFieldError,
    getFieldError,
  } = useVariantAutoSave({
    productId,
    variant,
    debounceMs: 1500,
    onSaveSuccess,
  })

  // Stock status badges
  const showLowStock = variant.lowStock
  const showOutOfStock = !variant.inStock

  return (
    <tr className={cn(
      'group border-b transition-colors hover:bg-muted/50',
      status === 'error' && 'bg-destructive/5',
    )}>
      {/* Name */}
      <td className="p-2">
        <InlineEditInput
          value={values.name}
          onChange={(value) => setFieldValue('name', value)}
          onEnterPress={saveNow}
          onEscapePress={revert}
          error={getFieldError('name')}
          hasError={hasFieldError('name')}
          placeholder={t('products.variants.namePlaceholder')}
          disabled={isReadOnly}
          className="min-w-[120px]"
        />
      </td>

      {/* SKU */}
      <td className="p-2">
        <InlineEditInput
          value={values.sku || ''}
          onChange={(value) => setFieldValue('sku', value || null)}
          onEnterPress={saveNow}
          onEscapePress={revert}
          error={getFieldError('sku')}
          hasError={hasFieldError('sku')}
          placeholder={t('products.variants.skuPlaceholder')}
          disabled={isReadOnly}
          className="min-w-[100px]"
        />
      </td>

      {/* Price */}
      <td className="p-2">
        <InlineEditInput
          type="number"
          value={values.price.toString()}
          onChange={(value) => setFieldValue('price', parseFloat(value) || 0)}
          onEnterPress={saveNow}
          onEscapePress={revert}
          error={getFieldError('price')}
          hasError={hasFieldError('price')}
          placeholder="0"
          disabled={isReadOnly}
          align="right"
          className="min-w-[80px]"
          min={0}
          step="any"
        />
      </td>

      {/* Compare At Price */}
      <td className="p-2">
        <InlineEditInput
          type="number"
          value={values.compareAtPrice?.toString() || ''}
          onChange={(value) => setFieldValue('compareAtPrice', value ? parseFloat(value) : null)}
          onEnterPress={saveNow}
          onEscapePress={revert}
          error={getFieldError('compareAtPrice')}
          hasError={hasFieldError('compareAtPrice')}
          placeholder="—"
          disabled={isReadOnly}
          align="right"
          className="min-w-[80px]"
          min={0}
          step="any"
        />
      </td>

      {/* Cost Price */}
      <td className="p-2">
        <InlineEditInput
          type="number"
          value={values.costPrice?.toString() || ''}
          onChange={(value) => setFieldValue('costPrice', value ? parseFloat(value) : null)}
          onEnterPress={saveNow}
          onEscapePress={revert}
          error={getFieldError('costPrice')}
          hasError={hasFieldError('costPrice')}
          placeholder="—"
          disabled={isReadOnly}
          align="right"
          className="min-w-[80px]"
          min={0}
          step="any"
        />
      </td>

      {/* Stock */}
      <td className="p-2">
        <div className="flex items-center gap-2">
          <InlineEditInput
            type="number"
            value={values.stockQuantity.toString()}
            onChange={(value) => setFieldValue('stockQuantity', parseInt(value) || 0)}
            onEnterPress={saveNow}
            onEscapePress={revert}
            error={getFieldError('stockQuantity')}
            hasError={hasFieldError('stockQuantity')}
            placeholder="0"
            disabled={isReadOnly}
            align="right"
            className="min-w-[60px]"
            min={0}
          />
          {showOutOfStock && (
            <Badge variant="outline" className={`${getStatusBadgeClasses('red')} text-xs whitespace-nowrap`}>
              {t('products.variants.outOfStock')}
            </Badge>
          )}
          {showLowStock && !showOutOfStock && (
            <Badge variant="outline" className={`${getStatusBadgeClasses('yellow')} text-xs whitespace-nowrap`}>
              {t('products.variants.lowStock')}
            </Badge>
          )}
        </div>
      </td>

      {/* Sort Order */}
      <td className="p-2">
        <InlineEditInput
          type="number"
          value={values.sortOrder.toString()}
          onChange={(value) => setFieldValue('sortOrder', parseInt(value) || 0)}
          onEnterPress={saveNow}
          onEscapePress={revert}
          error={getFieldError('sortOrder')}
          hasError={hasFieldError('sortOrder')}
          placeholder="0"
          disabled={isReadOnly}
          align="right"
          className="min-w-[50px]"
          min={0}
        />
      </td>

      {/* Actions */}
      <td className="p-2">
        {!isReadOnly && (
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 cursor-pointer opacity-0 group-hover:opacity-100 transition-opacity"
            onClick={() => onDelete?.(variant)}
            aria-label={t('products.variants.deleteAriaLabel', { name: values.name })}
          >
            <Trash2 className="h-4 w-4 text-destructive" />
          </Button>
        )}
      </td>

      {/* Status */}
      <td className="p-2 w-8">
        <StatusIndicator status={status} error={error} />
      </td>
    </tr>
  )
}
