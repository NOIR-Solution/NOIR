import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { useTranslation } from 'react-i18next'
import { Eye, Pencil, Trash2, Send, Archive, Copy } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@uikit'
import type { ProductListItem } from '@/types/product'

interface ProductActionsMenuProps {
  product: ProductListItem
  onDelete?: (product: ProductListItem) => void
  onPublish?: (product: ProductListItem) => void
  onArchive?: (product: ProductListItem) => void
  onDuplicate?: (product: ProductListItem) => void
  trigger: React.ReactNode
  align?: 'start' | 'center' | 'end'
  canEdit?: boolean
  canDelete?: boolean
  canPublish?: boolean
  canCreate?: boolean
}

/**
 * Shared dropdown menu component for product actions.
 * Used by both ProductGridView and EnhancedProductCard to ensure consistent behavior.
 */
export function ProductActionsMenu({
  product,
  onDelete,
  onPublish,
  onArchive,
  onDuplicate,
  trigger,
  align = 'center',
  canEdit = true,
  canDelete = true,
  canPublish = true,
  canCreate = true,
}: ProductActionsMenuProps) {
  const { t } = useTranslation('common')

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{trigger}</DropdownMenuTrigger>
      <DropdownMenuContent align={align} className="w-48">
        <DropdownMenuItem className="cursor-pointer" asChild>
          <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}`}>
            <Eye className="h-4 w-4 mr-2" />
            {t('labels.viewDetails', 'View Details')}
          </ViewTransitionLink>
        </DropdownMenuItem>
        {canEdit && (
          <DropdownMenuItem className="cursor-pointer" asChild>
            <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}/edit`}>
              <Pencil className="h-4 w-4 mr-2" />
              {t('products.editProduct', 'Edit Product')}
            </ViewTransitionLink>
          </DropdownMenuItem>
        )}
        {canCreate && onDuplicate && (
          <DropdownMenuItem
            className="cursor-pointer"
            onClick={() => onDuplicate(product)}
          >
            <Copy className="h-4 w-4 mr-2" />
            {t('products.duplicate', 'Duplicate')}
          </DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        {canPublish && product.status === 'Draft' && onPublish && (
          <DropdownMenuItem
            className="cursor-pointer text-emerald-600 dark:text-emerald-400"
            onClick={() => onPublish(product)}
          >
            <Send className="h-4 w-4 mr-2" />
            {t('labels.publish', 'Publish')}
          </DropdownMenuItem>
        )}
        {canEdit && product.status === 'Active' && onArchive && (
          <DropdownMenuItem
            className="cursor-pointer text-amber-600 dark:text-amber-400"
            onClick={() => onArchive(product)}
          >
            <Archive className="h-4 w-4 mr-2" />
            {t('labels.archive', 'Archive')}
          </DropdownMenuItem>
        )}
        {canDelete && onDelete && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="cursor-pointer text-destructive focus:text-destructive"
              onClick={() => onDelete(product)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete', 'Delete')}
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
