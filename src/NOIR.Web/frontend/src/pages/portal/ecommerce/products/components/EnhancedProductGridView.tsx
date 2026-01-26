import type { ProductListItem } from '@/types/product'
import { EnhancedProductCard } from './EnhancedProductCard'

interface EnhancedProductGridViewProps {
  products: ProductListItem[]
  onDelete: (product: ProductListItem) => void
  onPublish: (product: ProductListItem) => void
  onArchive: (product: ProductListItem) => void
}

export function EnhancedProductGridView({
  products,
  onDelete,
  onPublish,
  onArchive,
}: EnhancedProductGridViewProps) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {products.map((product, index) => (
        <EnhancedProductCard
          key={product.id}
          product={product}
          onDelete={onDelete}
          onPublish={onPublish}
          onArchive={onArchive}
        />
      ))}
    </div>
  )
}
