import { Link } from 'react-router-dom'
import { Eye, Pencil, Trash2, Send, Archive } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { ProductListItem } from '@/types/product'

interface ProductActionsMenuProps {
  product: ProductListItem
  onDelete: (product: ProductListItem) => void
  onPublish: (product: ProductListItem) => void
  onArchive: (product: ProductListItem) => void
  trigger: React.ReactNode
  align?: 'start' | 'center' | 'end'
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
  trigger,
  align = 'center',
}: ProductActionsMenuProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{trigger}</DropdownMenuTrigger>
      <DropdownMenuContent align={align} className="w-48">
        <DropdownMenuItem className="cursor-pointer" asChild>
          <Link to={`/portal/ecommerce/products/${product.id}`}>
            <Eye className="h-4 w-4 mr-2" />
            View Details
          </Link>
        </DropdownMenuItem>
        <DropdownMenuItem className="cursor-pointer" asChild>
          <Link to={`/portal/ecommerce/products/${product.id}/edit`}>
            <Pencil className="h-4 w-4 mr-2" />
            Edit Product
          </Link>
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
  )
}
