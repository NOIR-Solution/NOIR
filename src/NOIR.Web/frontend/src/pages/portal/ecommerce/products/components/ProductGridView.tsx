/**
 * @deprecated This component has been replaced by EnhancedProductGridView
 * which provides better UX with glassmorphism design, smooth animations,
 * and improved interactions. This file is kept temporarily for reference.
 *
 * Use EnhancedProductGridView instead:
 * import { EnhancedProductGridView } from './components/EnhancedProductGridView'
 *
 * TODO: Remove this file after verifying EnhancedProductGridView works correctly
 * in all scenarios.
 */

import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { Package, Eye, Pencil, Trash2, Send, Archive } from 'lucide-react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { ProductListItem } from '@/types/product'
import { formatCurrency } from '@/lib/utils/currency'
import { PRODUCT_STATUS_CONFIG } from '@/lib/constants/product'

interface ProductGridViewProps {
  products: ProductListItem[]
  onDelete: (product: ProductListItem) => void
  onPublish: (product: ProductListItem) => void
  onArchive: (product: ProductListItem) => void
}

export function ProductGridView({ products, onDelete, onPublish, onArchive }: ProductGridViewProps) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {products.map((product) => {
        const status = PRODUCT_STATUS_CONFIG[product.status]
        const StatusIcon = status.icon

        return (
          <Card
            key={product.id}
            className="group relative overflow-hidden border-border/60 bg-card/50 backdrop-blur-xl hover:border-border shadow-sm hover:shadow-lg transition-all duration-300"
          >
            {/* Image Container */}
            <div className="relative aspect-square overflow-hidden bg-gradient-to-br from-muted to-muted/50">
              {product.primaryImageUrl ? (
                <img
                  src={product.primaryImageUrl}
                  alt={product.name}
                  className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-110"
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center">
                  <Package className="h-16 w-16 text-muted-foreground/30" />
                </div>
              )}

              {/* Glassmorphism Overlay on Hover */}
              <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />

              {/* Status Badge */}
              <div className="absolute top-3 left-3">
                <Badge className={`${status.color} border transition-all duration-200`} variant="secondary">
                  <StatusIcon className="h-3 w-3 mr-1.5" />
                  {status.label}
                </Badge>
              </div>

              {/* Category Badge */}
              {product.categoryName && (
                <div className="absolute top-3 right-3">
                  <Badge variant="secondary" className="bg-background/80 backdrop-blur-sm border-border/60">
                    {product.categoryName}
                  </Badge>
                </div>
              )}

              {/* Action Buttons */}
              <div className="absolute bottom-3 right-3 flex gap-2 opacity-0 group-hover:opacity-100 transition-all duration-300 translate-x-4 group-hover:translate-x-0">
                <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}`}>
                  <Button
                    size="icon"
                    variant="secondary"
                    className="rounded-full bg-background/90 backdrop-blur-sm hover:bg-background h-8 w-8 cursor-pointer"
                  >
                    <Eye className="w-4 h-4" />
                  </Button>
                </ViewTransitionLink>
                <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}/edit`}>
                  <Button
                    size="icon"
                    variant="secondary"
                    className="rounded-full bg-background/90 backdrop-blur-sm hover:bg-background h-8 w-8 cursor-pointer"
                  >
                    <Pencil className="w-4 h-4" />
                  </Button>
                </ViewTransitionLink>
              </div>
            </div>

            {/* Content */}
            <div className="p-4 space-y-3">
              {/* Title */}
              <h3 className="font-semibold text-foreground line-clamp-2 min-h-[2.5rem] group-hover:text-primary transition-colors">
                {product.name}
              </h3>

              {/* SKU */}
              {product.sku && (
                <div className="text-xs text-muted-foreground font-mono">
                  SKU: {product.sku}
                </div>
              )}

              {/* Stock Info */}
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Stock:</span>
                <Badge variant={product.inStock ? 'default' : 'destructive'} className="text-xs">
                  {product.totalStock}
                </Badge>
              </div>

              {/* Price */}
              <div className="flex items-baseline gap-2">
                <span className="text-2xl font-bold text-foreground">
                  {formatCurrency(product.basePrice, product.currency)}
                </span>
              </div>

              {/* Actions Dropdown */}
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" className="w-full cursor-pointer">
                    Actions
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="center" className="w-48">
                  <DropdownMenuItem className="cursor-pointer" asChild>
                    <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}`}>
                      <Eye className="h-4 w-4 mr-2" />
                      View Details
                    </ViewTransitionLink>
                  </DropdownMenuItem>
                  <DropdownMenuItem className="cursor-pointer" asChild>
                    <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}/edit`}>
                      <Pencil className="h-4 w-4 mr-2" />
                      Edit Product
                    </ViewTransitionLink>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  {product.status === 'Draft' && (
                    <DropdownMenuItem
                      className="cursor-pointer text-emerald-600 dark:text-emerald-400"
                      onClick={() => onPublish(product)}
                    >
                      <Send className="h-4 w-4 mr-2" />
                      Publish
                    </DropdownMenuItem>
                  )}
                  {product.status === 'Active' && (
                    <DropdownMenuItem
                      className="cursor-pointer text-amber-600 dark:text-amber-400"
                      onClick={() => onArchive(product)}
                    >
                      <Archive className="h-4 w-4 mr-2" />
                      Archive
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="cursor-pointer text-destructive focus:text-destructive"
                    onClick={() => onDelete(product)}
                  >
                    <Trash2 className="h-4 w-4 mr-2" />
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </Card>
        )
      })}
    </div>
  )
}
