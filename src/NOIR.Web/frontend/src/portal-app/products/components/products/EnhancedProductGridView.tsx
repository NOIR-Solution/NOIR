import type { ProductListItem } from '@/types/product'
import { EnhancedProductCard } from './EnhancedProductCard'

interface EnhancedProductGridViewProps {
  products: ProductListItem[]
  onDelete?: (product: ProductListItem) => void
  onPublish?: (product: ProductListItem) => void
  onArchive?: (product: ProductListItem) => void
  onDuplicate?: (product: ProductListItem) => void
  canEdit?: boolean
  canDelete?: boolean
  canPublish?: boolean
  canCreate?: boolean
}

export function EnhancedProductGridView({
  products,
  onDelete,
  onPublish,
  onArchive,
  onDuplicate,
  canEdit = true,
  canDelete = true,
  canPublish = true,
  canCreate = true,
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
          onDuplicate={canCreate ? onDuplicate : undefined}
          canEdit={canEdit}
          canDelete={canDelete}
          canPublish={canPublish}
          canCreate={canCreate}
        />
      ))}
    </div>
  )
}
