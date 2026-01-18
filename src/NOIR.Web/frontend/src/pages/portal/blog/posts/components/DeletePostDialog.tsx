import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, FileText } from 'lucide-react'
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
import { Badge } from '@/components/ui/badge'
import { toast } from 'sonner'
import type { PostListItem, PostStatus } from '@/types'

const statusColors: Record<PostStatus, string> = {
  Draft: 'bg-gray-100 text-gray-800',
  Published: 'bg-green-100 text-green-800',
  Scheduled: 'bg-blue-100 text-blue-800',
  Archived: 'bg-yellow-100 text-yellow-800',
}

interface DeletePostDialogProps {
  post: PostListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export function DeletePostDialog({ post, open, onOpenChange, onConfirm }: DeletePostDialogProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!post) return

    setLoading(true)
    try {
      const result = await onConfirm(post.id)

      if (result.success) {
        toast.success('Post deleted')
        onOpenChange(false)
      } else {
        toast.error(result.error || 'Failed to delete post')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-destructive/10 rounded-lg">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <AlertDialogTitle>Delete Post</AlertDialogTitle>
              <AlertDialogDescription>
                Are you sure you want to delete this post? This action cannot be undone.
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>

        {post && (
          <div className="my-4 p-4 bg-muted rounded-lg">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center">
                <FileText className="h-5 w-5 text-primary" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="font-medium truncate">{post.title}</p>
                <div className="flex items-center gap-2 mt-1">
                  <Badge className={statusColors[post.status]} variant="secondary">
                    {post.status}
                  </Badge>
                  {post.categoryName && (
                    <span className="text-sm text-muted-foreground">{post.categoryName}</span>
                  )}
                </div>
              </div>
            </div>
            {post.status === 'Published' && (
              <div className="mt-3 p-2 bg-destructive/10 rounded text-sm text-destructive">
                This post is currently published and visible to readers. Deleting it will remove it permanently.
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
            disabled={loading}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {loading ? 'Deleting...' : 'Delete'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
