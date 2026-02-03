import { useState } from 'react'
import { AlertTriangle, Tag } from 'lucide-react'
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
import type { PostTagListItem } from '@/types'

interface DeleteTagDialogProps {
  tag: PostTagListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export function DeleteTagDialog({ tag, open, onOpenChange, onConfirm }: DeleteTagDialogProps) {
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!tag) return

    setLoading(true)
    try {
      const result = await onConfirm(tag.id)

      if (result.success) {
        toast.success('Tag deleted')
        onOpenChange(false)
      } else {
        toast.error(result.error || 'Failed to delete tag')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="border-destructive/30">
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <AlertDialogTitle>Delete Tag</AlertDialogTitle>
              <AlertDialogDescription>
                Are you sure you want to delete this tag? This action cannot be undone.
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>

        {tag && (
          <div className="my-4 p-4 bg-muted rounded-lg">
            <div className="flex items-center gap-3">
              <div
                className="w-10 h-10 rounded-full flex items-center justify-center"
                style={{ backgroundColor: tag.color || '#6b7280' }}
              >
                <Tag className="h-5 w-5 text-white" />
              </div>
              <div>
                <p className="font-medium">{tag.name}</p>
                <p className="text-sm text-muted-foreground">/{tag.slug}</p>
              </div>
            </div>
            {tag.postCount > 0 && (
              <div className="mt-3 p-2 bg-yellow-100 dark:bg-yellow-900/20 rounded text-sm text-yellow-800 dark:text-yellow-200">
                This tag is used in {tag.postCount} posts. Deleting it will remove the tag from those posts.
              </div>
            )}
          </div>
        )}

        <AlertDialogFooter>
          <AlertDialogCancel disabled={loading} className="cursor-pointer">
            Cancel
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={loading}
            className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
          >
            {loading ? 'Deleting...' : 'Delete'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
