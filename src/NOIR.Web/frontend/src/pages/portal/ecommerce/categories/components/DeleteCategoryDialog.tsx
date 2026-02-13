import { useState } from 'react'
import { AlertTriangle, Loader2 } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@uikit'
import type { ProductCategoryListItem } from '@/types/product'
import { toast } from 'sonner'

interface DeleteCategoryDialogProps {
  category: ProductCategoryListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export function DeleteCategoryDialog({
  category,
  open,
  onOpenChange,
  onConfirm,
}: DeleteCategoryDialogProps) {
  const [isDeleting, setIsDeleting] = useState(false)

  const handleConfirm = async () => {
    if (!category) return

    setIsDeleting(true)
    try {
      const result = await onConfirm(category.id)

      if (result.success) {
        toast.success(`Category "${category.name}" deleted successfully`)
        onOpenChange(false)
      } else {
        toast.error(result.error || 'Failed to delete category')
      }
    } finally {
      setIsDeleting(false)
    }
  }

  const hasProducts = category && category.productCount > 0
  const hasChildren = category && category.childCount > 0

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="border-destructive/30">
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <AlertDialogTitle>Delete Category</AlertDialogTitle>
          </div>
          <AlertDialogDescription className="pt-2 space-y-2">
            <span>
              Are you sure you want to delete{' '}
              <span className="font-semibold text-foreground">"{category?.name}"</span>?
            </span>
            {hasProducts && (
              <span className="block p-2 rounded-lg bg-amber-500/10 border border-amber-500/20 text-amber-600 dark:text-amber-400 text-sm">
                <strong>Warning:</strong> This category has {category?.productCount} products that will be uncategorized.
              </span>
            )}
            {hasChildren && (
              <span className="block p-2 rounded-lg bg-red-500/10 border border-red-500/20 text-red-600 dark:text-red-400 text-sm">
                <strong>Blocked:</strong> This category has {category?.childCount} child categories that must be moved or deleted first.
              </span>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isDeleting} className="cursor-pointer">
            Cancel
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={isDeleting || !!hasChildren}
            className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
          >
            {isDeleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isDeleting ? 'Deleting...' : 'Delete'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
