import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { motion, AnimatePresence } from 'framer-motion'
import {
  Eye,
  Pencil,
  Package,
  AlertTriangle,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import type { ProductListItem } from '@/types/product'
import { formatCurrency } from '@/lib/utils/currency'
import { PRODUCT_STATUS_CONFIG, LOW_STOCK_THRESHOLD } from '@/lib/constants/product'
import { ProductActionsMenu } from './ProductActionsMenu'

interface EnhancedProductCardProps {
  product: ProductListItem
  onDelete?: (product: ProductListItem) => void
  onPublish?: (product: ProductListItem) => void
  onArchive?: (product: ProductListItem) => void
  canEdit?: boolean
  canDelete?: boolean
  canPublish?: boolean
}

export function EnhancedProductCard({
  product,
  onDelete,
  onPublish,
  onArchive,
  canEdit = true,
  canDelete = true,
  canPublish = true,
}: EnhancedProductCardProps) {
  const { t } = useTranslation('common')
  const [isHovered, setIsHovered] = useState(false)

  const status = PRODUCT_STATUS_CONFIG[product.status]
  const StatusIcon = status.icon

  const isLowStock = product.totalStock > 0 && product.totalStock < LOW_STOCK_THRESHOLD
  const hasDiscount = !!product.discountPercentage && product.discountPercentage > 0

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
      className="w-full"
    >
      <Card
        className="group relative overflow-hidden border-border/60 bg-background/50 backdrop-blur-xl shadow-sm hover:shadow-lg transition-all duration-300 p-0"
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        {/* Image Container */}
        <div className="relative aspect-square overflow-hidden bg-gradient-to-br from-muted to-muted/50">
          <AnimatePresence mode="wait">
            <motion.div
              key={product.id}
              className="h-full w-full"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.2, ease: 'easeInOut' }}
            >
              {product.primaryImageUrl ? (
                <img
                  src={product.primaryImageUrl}
                  alt={product.name}
                  className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-110"
                  loading="lazy"
                />
              ) : (
                <div className="h-full w-full flex items-center justify-center">
                  <Package className="h-16 w-16 text-muted-foreground/30" />
                </div>
              )}
            </motion.div>
          </AnimatePresence>

          {/* Glassmorphism Overlay on Hover */}
          <motion.div
            className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/20 to-transparent pointer-events-none"
            initial={{ opacity: 0 }}
            animate={{ opacity: isHovered ? 1 : 0 }}
            transition={{ duration: 0.3 }}
          />

          {/* Status Badge */}
          <Badge
            className={`absolute top-3 left-3 ${status.color} border transition-all duration-200 shadow-lg gap-1`}
            variant="secondary"
          >
            <StatusIcon className="h-3 w-3" />
            {status.label}
          </Badge>

          {/* Discount Badge (if applicable) */}
          {hasDiscount && product.discountPercentage && (
            <Badge className="absolute left-3 top-14 bg-green-500 text-white border-0 shadow-lg">
              -{product.discountPercentage}%
            </Badge>
          )}

          {/* Low Stock Warning */}
          {isLowStock && (
            <Badge className="absolute top-3 right-3 bg-orange-500/90 text-white border-0 shadow-lg gap-1 backdrop-blur-sm">
              <AlertTriangle className="h-3 w-3" />
              {t('products.lowStock', 'Low Stock')}
            </Badge>
          )}

          {/* Category Badge */}
          {product.categoryName && !isLowStock && (
            <Badge className="absolute top-3 right-3 bg-background/80 backdrop-blur-sm border-border/60">
              {product.categoryName}
            </Badge>
          )}

          {/* Quick Action Buttons - Appear on Hover */}
          <motion.div
            className="absolute bottom-3 right-3 flex gap-2"
            initial={{ opacity: 0, x: 20 }}
            animate={{ opacity: isHovered ? 1 : 0, x: isHovered ? 0 : 20 }}
            transition={{ duration: 0.3 }}
          >
            <Link to={`/portal/ecommerce/products/${product.id}`}>
              <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
                <Button
                  size="icon"
                  variant="secondary"
                  className="h-9 w-9 rounded-full bg-background/90 backdrop-blur-md border-border shadow-lg hover:bg-background cursor-pointer"
                  aria-label={`View ${product.name} details`}
                >
                  <Eye className="h-4 w-4" />
                </Button>
              </motion.div>
            </Link>
            {canEdit && (
              <Link to={`/portal/ecommerce/products/${product.id}/edit`}>
                <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
                  <Button
                    size="icon"
                    variant="secondary"
                    className="h-9 w-9 rounded-full bg-background/90 backdrop-blur-md border-border shadow-lg hover:bg-background cursor-pointer"
                    aria-label={`Edit ${product.name}`}
                  >
                    <Pencil className="h-4 w-4" />
                  </Button>
                </motion.div>
              </Link>
            )}
          </motion.div>

          {/* Out of Stock Overlay */}
          {!product.inStock && (
            <div className="absolute inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center">
              <Badge variant="secondary" className="text-lg px-6 py-2 shadow-lg">
                {t('products.outOfStock', 'Out of Stock')}
              </Badge>
            </div>
          )}
        </div>

        {/* Content */}
        <div className="p-4 space-y-3 bg-background/30 backdrop-blur-md">
          {/* Brand & SKU */}
          <div className="flex items-center justify-between gap-2">
            {product.brand && (
              <div className="text-xs text-muted-foreground uppercase tracking-wider font-medium">
                {product.brand}
              </div>
            )}
            {product.sku && (
              <div className="text-xs text-muted-foreground font-mono">
                SKU: {product.sku}
              </div>
            )}
          </div>

          {/* Product Name */}
          <h3 className="font-semibold text-base text-foreground line-clamp-2 leading-snug min-h-[2.5rem] group-hover:text-primary transition-colors duration-200">
            {product.name}
          </h3>

          {/* Stock Info */}
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">{t('labels.stock', 'Stock')}:</span>
            <Badge
              variant={product.inStock ? 'default' : 'destructive'}
              className="transition-all duration-200 hover:scale-105"
            >
              {product.totalStock}
            </Badge>
          </div>

          {/* Price */}
          <div className="flex items-baseline gap-2">
            <span className="text-2xl font-bold text-foreground">
              {formatCurrency(product.basePrice, product.currency)}
            </span>
            {hasDiscount && product.discountPercentage && (
              <span className="text-sm text-muted-foreground line-through">
                {formatCurrency(
                  product.basePrice / (1 - product.discountPercentage / 100),
                  product.currency
                )}
              </span>
            )}
          </div>

          {/* Actions Dropdown */}
          <ProductActionsMenu
            product={product}
            onDelete={onDelete}
            onPublish={onPublish}
            onArchive={onArchive}
            canEdit={canEdit}
            canDelete={canDelete}
            canPublish={canPublish}
            trigger={
              <motion.div whileHover={{ scale: 1.02 }} whileTap={{ scale: 0.98 }}>
                <Button
                  variant="outline"
                  className="w-full cursor-pointer bg-background/50 backdrop-blur-sm hover:bg-primary hover:text-primary-foreground transition-all duration-200"
                >
                  {t('labels.actions', 'Actions')}
                </Button>
              </motion.div>
            }
          />
        </div>

        {/* Glassmorphism Border Effect */}
        <motion.div
          className="absolute inset-0 rounded-xl border-2 border-primary/0 pointer-events-none"
          animate={{
            borderColor: isHovered ? 'hsl(var(--primary) / 0.3)' : 'hsl(var(--primary) / 0)',
          }}
          transition={{ duration: 0.3 }}
        />
      </Card>
    </motion.div>
  )
}
