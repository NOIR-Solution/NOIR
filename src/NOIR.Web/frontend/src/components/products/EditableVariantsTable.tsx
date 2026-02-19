import { useTranslation } from 'react-i18next'
import { EditableVariantRow } from './EditableVariantRow'
import type { ProductVariant } from '@/types/product'

interface EditableVariantsTableProps {
  /** Product ID for API calls */
  productId: string
  /** List of variants to display */
  variants: ProductVariant[]
  /** Whether the table is in read-only mode */
  isReadOnly?: boolean
  /** Callback when delete is requested */
  onDelete?: (variant: ProductVariant) => void
  /** Callback when a variant is successfully saved */
  onSaveSuccess?: (updatedVariant: ProductVariant) => void
}

export const EditableVariantsTable = ({
  productId,
  variants,
  isReadOnly = false,
  onDelete,
  onSaveSuccess,
}: EditableVariantsTableProps) => {
  const { t } = useTranslation('common')

  if (variants.length === 0) {
    return (
      <p className="text-center text-muted-foreground py-8">
        {t('products.variants.empty')}
      </p>
    )
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse">
        <thead>
          <tr className="border-b bg-muted/50">
            <th className="p-2 text-left text-sm font-medium text-muted-foreground">
              {t('products.variants.columnName')}
            </th>
            <th className="p-2 text-left text-sm font-medium text-muted-foreground">
              {t('products.variants.columnSku')}
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              {t('products.variants.columnPrice')}
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              {t('products.variants.columnComparePrice')}
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              {t('products.variants.columnCostPrice')}
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              {t('products.variants.columnStock')}
            </th>
            <th className="p-2 text-right text-sm font-medium text-muted-foreground">
              {t('products.variants.columnSort')}
            </th>
            <th className="p-2 w-10">
              {/* Actions column - no header */}
            </th>
            <th className="p-2 w-8">
              {/* Status column - no header */}
            </th>
          </tr>
        </thead>
        <tbody>
          {variants.map((variant) => (
            <EditableVariantRow
              key={variant.id}
              productId={productId}
              variant={variant}
              isReadOnly={isReadOnly}
              onDelete={onDelete}
              onSaveSuccess={onSaveSuccess}
            />
          ))}
        </tbody>
      </table>
    </div>
  )
}
