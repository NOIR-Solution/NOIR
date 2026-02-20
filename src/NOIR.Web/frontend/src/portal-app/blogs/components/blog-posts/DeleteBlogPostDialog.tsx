import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, FileText, Loader2 } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Badge,
} from '@uikit'

import { toast } from 'sonner'
import type { PostListItem, PostStatus } from '@/types'

const statusColors: Record<PostStatus, string> = {
  Draft: 'bg-gray-100 text-gray-800',
  Published: 'bg-green-100 text-green-800',
  Scheduled: 'bg-blue-100 text-blue-800',
  Archived: 'bg-yellow-100 text-yellow-800',
}

interface DeleteBlogPostDialogProps {
  post: PostListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export const DeleteBlogPostDialog = ({ post, open, onOpenChange, onConfirm }: DeleteBlogPostDialogProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!post) return

    setLoading(true)
    try {
      const result = await onConfirm(post.id)

      if (result.success) {
        toast.success(t('blog.postDeleted'))
        onOpenChange(false)
      } else {
        toast.error(result.error || t('blog.deletePostFailed'))
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
              <AlertDialogTitle>{t('blog.deletePost')}</AlertDialogTitle>
              <AlertDialogDescription>
                {t('blog.deletePostConfirm')}
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
                {t('blog.postPublishedWarning')}
              </div>
            )}
          </div>
        )}

        <AlertDialogFooter>
          <AlertDialogCancel disabled={loading} className="cursor-pointer">
            {t('buttons.cancel')}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={loading}
            className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
          >
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {loading ? t('buttons.deleting') : t('buttons.delete')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
