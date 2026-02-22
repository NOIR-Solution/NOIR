import { useState } from 'react'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { useTranslation } from 'react-i18next'
import { motion, AnimatePresence } from 'framer-motion'
import {
  Eye,
  Pencil,
  Package,
  AlertTriangle,
  Send,
  Archive,
  MoreHorizontal,
  Tag,
} from 'lucide-react'
import { Badge, Button, Card, FilePreviewModal, TippyTooltip } from '@uikit'
import { cn } from '@/lib/utils'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

import type { ProductListItem, ProductAttributeDisplay } from '@/types/product'
import { formatCurrency } from '@/lib/utils/currency'
import { PRODUCT_STATUS_CONFIG, LOW_STOCK_THRESHOLD } from '@/lib/constants/product'
import { ProductActionsMenu } from './ProductActionsMenu'
import { AttributeBadges } from './AttributeBadges'

interface EnhancedProductCardProps {
  product: ProductListItem
  index?: number
  displayAttributes?: ProductAttributeDisplay[]
  onDelete?: (product: ProductListItem) => void
  onPublish?: (product: ProductListItem) => void
  onArchive?: (product: ProductListItem) => void
  onDuplicate?: (product: ProductListItem) => void
  canEdit?: boolean
  canDelete?: boolean
  canPublish?: boolean
  canCreate?: boolean
}

export const EnhancedProductCard = ({
  product,
  index = 0,
  displayAttributes,
  onDelete,
  onPublish,
  onArchive,
  onDuplicate,
  canEdit = true,
  canDelete = true,
  canPublish = true,
  canCreate = true,
}: EnhancedProductCardProps) => {
  const { t } = useTranslation('common')
  const [isHovered, setIsHovered] = useState(false)
  const [previewOpen, setPreviewOpen] = useState(false)

  const status = PRODUCT_STATUS_CONFIG[product.status]
  const StatusIcon = status.icon

  const isLowStock = product.totalStock > 0 && product.totalStock < LOW_STOCK_THRESHOLD
  const isOutOfStock = !product.inStock

  // Explicit prop overrides product's own attributes
  const attrs = displayAttributes?.length ? displayAttributes : product.displayAttributes

  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, delay: Math.min(index * 0.04, 0.4), ease: 'easeOut' }}
      className="w-full"
    >
      <Card
        className="group relative overflow-hidden border-border/60 bg-background shadow-sm hover:shadow-lg transition-all duration-300 p-0"
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        {/* Image Container */}
        <div
          className={cn(
            'relative aspect-square overflow-hidden bg-gradient-to-br from-muted to-muted/50',
            product.primaryImageUrl && 'cursor-pointer'
          )}
          style={{ viewTransitionName: `product-image-${product.id}` } as React.CSSProperties}
          onClick={() => { if (product.primaryImageUrl) setPreviewOpen(true) }}
        >
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
                  className={cn(
                    'h-full w-full object-cover transition-all duration-500 group-hover:scale-105',
                    isOutOfStock && 'grayscale-[60%] opacity-75'
                  )}
                  loading="lazy"
                />
              ) : (
                <div className={cn(
                  'h-full w-full flex items-center justify-center',
                  isOutOfStock && 'opacity-50'
                )}>
                  <Package className="h-16 w-16 text-muted-foreground/30" />
                </div>
              )}
            </motion.div>
          </AnimatePresence>

          {/* Hover gradient overlay */}
          <motion.div
            className="absolute inset-0 bg-gradient-to-t from-black/50 via-transparent to-transparent pointer-events-none"
            initial={{ opacity: 0 }}
            animate={{ opacity: isHovered ? 1 : 0 }}
            transition={{ duration: 0.3 }}
          />

          {/* Status Badge — top-left */}
          <Badge
            className={`absolute top-3 left-3 ${status.color} border transition-all duration-200 shadow-lg gap-1`}
            variant="outline"
          >
            <StatusIcon className="h-3 w-3" />
            {t(status.labelKey)}
          </Badge>

          {/* Low Stock Warning — top-right */}
          {isLowStock && (
            <Badge
              variant="outline"
              className={`absolute top-3 right-3 ${getStatusBadgeClasses('orange')} shadow-lg gap-1 backdrop-blur-sm`}
            >
              <AlertTriangle className="h-3 w-3" />
              {t('products.lowStock', 'Low Stock')}
            </Badge>
          )}

          {/* Category Badge — top-right (when not low stock) */}
          {product.categoryName && !isLowStock && (
            <Badge variant="secondary" className="absolute top-3 right-3 shadow-md text-xs gap-1 bg-background border-border">
              <Tag className="h-2.5 w-2.5" />
              {product.categoryName}
            </Badge>
          )}

          {/* Out of Stock badge — bottom-left, subtle */}
          {isOutOfStock && (
            <Badge
              variant="secondary"
              className="absolute bottom-3 left-3 shadow-md text-xs bg-background/90 backdrop-blur-sm text-muted-foreground"
            >
              {t('products.outOfStock', 'Out of Stock')}
            </Badge>
          )}

          {/* Quick Action Buttons — on hover, bottom-right */}
          <motion.div
            className="absolute bottom-3 right-3 flex gap-1.5"
            initial={{ opacity: 0, x: 16 }}
            animate={{ opacity: isHovered ? 1 : 0, x: isHovered ? 0 : 16 }}
            transition={{ duration: 0.25 }}
          >
            {canPublish && product.status === 'Draft' && onPublish && (
              <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
                <TippyTooltip content={t('labels.publish', 'Publish')} placement="top">
                  <Button
                    size="icon"
                    variant="secondary"
                    className="h-8 w-8 rounded-full bg-emerald-500/90 text-white backdrop-blur-md border-0 shadow-lg hover:bg-emerald-600 cursor-pointer"
                    aria-label={t('labels.publishItem', { name: product.name, defaultValue: `Publish ${product.name}` })}
                    onClick={(e) => {
                      e.preventDefault()
                      e.stopPropagation()
                      onPublish(product)
                    }}
                  >
                    <Send className="h-3.5 w-3.5" />
                  </Button>
                </TippyTooltip>
              </motion.div>
            )}
            {canEdit && product.status === 'Active' && onArchive && (
              <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
                <TippyTooltip content={t('labels.archive', 'Archive')} placement="top">
                  <Button
                    size="icon"
                    variant="secondary"
                    className="h-8 w-8 rounded-full bg-amber-500/90 text-white backdrop-blur-md border-0 shadow-lg hover:bg-amber-600 cursor-pointer"
                    aria-label={t('labels.archiveItem', { name: product.name, defaultValue: `Archive ${product.name}` })}
                    onClick={(e) => {
                      e.preventDefault()
                      e.stopPropagation()
                      onArchive(product)
                    }}
                  >
                    <Archive className="h-3.5 w-3.5" />
                  </Button>
                </TippyTooltip>
              </motion.div>
            )}
            <div onClick={(e) => e.stopPropagation()}>
              <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}`}>
                <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
                  <TippyTooltip content={t('labels.viewDetails', 'View Details')} placement="top">
                    <Button
                      size="icon"
                      variant="secondary"
                      className="h-8 w-8 rounded-full bg-background/90 backdrop-blur-md border-border shadow-lg hover:bg-background cursor-pointer"
                      aria-label={t('labels.viewDetailsFor', { name: product.name, defaultValue: `View ${product.name} details` })}
                    >
                      <Eye className="h-3.5 w-3.5" />
                    </Button>
                  </TippyTooltip>
                </motion.div>
              </ViewTransitionLink>
            </div>
            {canEdit && (
              <div onClick={(e) => e.stopPropagation()}>
                <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}/edit`}>
                  <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
                    <TippyTooltip content={t('products.editProduct', 'Edit Product')} placement="top">
                      <Button
                        size="icon"
                        variant="secondary"
                        className="h-8 w-8 rounded-full bg-background/90 backdrop-blur-md border-border shadow-lg hover:bg-background cursor-pointer"
                        aria-label={t('labels.editItem', { name: product.name, defaultValue: `Edit ${product.name}` })}
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                    </TippyTooltip>
                  </motion.div>
                </ViewTransitionLink>
              </div>
            )}
          </motion.div>
        </div>

        {/* Content */}
        <div className="p-3 space-y-2">
          {/* Brand + Actions row */}
          <div className="flex items-center justify-between gap-2">
            <span className="text-[11px] text-muted-foreground uppercase tracking-wider font-medium truncate">
              {product.brandName || product.brand || '\u00A0'}
            </span>
            <ProductActionsMenu
              product={product}
              onDelete={onDelete}
              onPublish={onPublish}
              onArchive={onArchive}
              onDuplicate={onDuplicate}
              canEdit={canEdit}
              canDelete={canDelete}
              canPublish={canPublish}
              canCreate={canCreate}
              align="end"
              trigger={
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 -mr-1.5 text-muted-foreground hover:text-foreground cursor-pointer shrink-0"
                  aria-label={t('labels.actionsFor', { name: product.name })}
                >
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              }
            />
          </div>

          {/* Product Name */}
          <h3 className="font-semibold text-sm text-foreground line-clamp-2 leading-snug min-h-[2.5rem] group-hover:text-primary transition-colors duration-200">
            {product.name}
          </h3>

          {/* Attribute Badges */}
          {attrs && attrs.length > 0 && (
            <AttributeBadges displayAttributes={attrs} maxColors={5} />
          )}

          {/* Price + Stock row */}
          <div className="flex items-center justify-between gap-2 pt-1">
            <span className="text-lg font-bold text-foreground">
              {formatCurrency(product.basePrice, product.currency)}
            </span>
            <Badge
              variant={product.inStock ? 'secondary' : 'destructive'}
              className={cn(
                'text-xs tabular-nums',
                product.inStock && 'text-muted-foreground'
              )}
            >
              {product.totalStock}
            </Badge>
          </div>
        </div>

        {/* Hover border accent */}
        <motion.div
          className="absolute inset-0 rounded-xl border-2 border-primary/0 pointer-events-none"
          animate={{
            borderColor: isHovered ? 'color-mix(in oklch, var(--primary) 30%, transparent)' : 'transparent',
          }}
          transition={{ duration: 0.3 }}
        />
      </Card>

      {/* Image Preview Lightbox */}
      {product.primaryImageUrl && (
        <FilePreviewModal
          open={previewOpen}
          onOpenChange={setPreviewOpen}
          files={[{ url: product.primaryImageUrl, name: product.name }]}
        />
      )}
    </motion.div>
  )
}
