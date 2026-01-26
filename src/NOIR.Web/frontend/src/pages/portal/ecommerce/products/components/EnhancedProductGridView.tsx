import type { ProductListItem } from '@/types/product'
import { EnhancedProductCard } from './EnhancedProductCard'

interface EnhancedProductGridViewProps {
  products: ProductListItem[]
  onDelete?: (product: ProductListItem) => void
  onPublish?: (product: ProductListItem) => void
  onArchive?: (product: ProductListItem) => void
  canEdit?: boolean
  canDelete?: boolean
  canPublish?: boolean
}

export function EnhancedProductGridView({
  products,
  onDelete,
  onPublish,
  onArchive,
  canEdit = true,
  canDelete = true,
  canPublish = true,
}: EnhancedProductGridViewProps) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {products.map((product) => (
        <EnhancedProductCard
          key={product.id}
          product={product}
          onDelete={canDelete ? onDelete : undefined}
          onPublish={canPublish ? onPublish : undefined}
          onArchive={canEdit ? onArchive : undefined}
          canEdit={canEdit}
          canDelete={canDelete}
          canPublish={canPublish}
        />
      ))}
    </div>
  )
}
