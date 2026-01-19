import { useState } from 'react'
import { AlertTriangle, FolderTree } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { toast } from 'sonner'
import type { PostCategoryListItem } from '@/types'

interface DeleteCategoryDialogProps {
  category: PostCategoryListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export function DeleteCategoryDialog({ category, open, onOpenChange, onConfirm }: DeleteCategoryDialogProps) {
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!category) return

    setLoading(true)
    try {
      const result = await onConfirm(category.id)

      if (result.success) {
        toast.success('Category deleted')
        onOpenChange(false)
      } else {
        toast.error(result.error || 'Failed to delete category')
      }
    } finally {
      setLoading(false)
    }
  }

  const canDelete = category && category.postCount === 0 && category.childCount === 0

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-destructive/10 rounded-lg">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <AlertDialogTitle>Delete Category</AlertDialogTitle>
              <AlertDialogDescription>
                Are you sure you want to delete this category? This action cannot be undone.
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>

        {category && (
          <div className="my-4 p-4 bg-muted rounded-lg">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center">
                <FolderTree className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="font-medium">{category.name}</p>
                <p className="text-sm text-muted-foreground">/{category.slug}</p>
              </div>
            </div>
            {!canDelete && (
              <div className="mt-3 p-2 bg-destructive/10 rounded text-sm text-destructive">
                {category.postCount > 0 && (
                  <p>This category has {category.postCount} posts assigned to it.</p>
                )}
                {category.childCount > 0 && (
                  <p>This category has {category.childCount} child categories.</p>
                )}
                <p className="mt-1 font-medium">Please reassign or delete them first.</p>
              </div>
            )}
          </div>
        )}

        <AlertDialogFooter>
          <AlertDialogCancel disabled={loading}>
            Cancel
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={loading || !canDelete}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {loading ? 'Deleting...' : 'Delete'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
