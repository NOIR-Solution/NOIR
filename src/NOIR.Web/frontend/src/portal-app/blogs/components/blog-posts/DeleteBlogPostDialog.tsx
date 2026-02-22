import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, FileText, Loader2 } from 'lucide-react'
import {
  Badge,
  Button,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
} from '@uikit'

import { toast } from 'sonner'
import type { PostListItem, PostStatus } from '@/types'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

const statusColors: Record<PostStatus, string> = {
  Draft: getStatusBadgeClasses('gray'),
  Published: getStatusBadgeClasses('green'),
  Scheduled: getStatusBadgeClasses('blue'),
  Archived: getStatusBadgeClasses('yellow'),
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
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('blog.deletePost')}</CredenzaTitle>
              <CredenzaDescription>
                {t('blog.deletePostConfirm')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <CredenzaBody>
          {post && (
            <div className="my-4 p-4 bg-muted rounded-lg">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center">
                  <FileText className="h-5 w-5 text-primary" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="font-medium truncate">{post.title}</p>
                  <div className="flex items-center gap-2 mt-1">
                    <Badge className={statusColors[post.status]} variant="outline">
                      {t(`blog.status.${post.status.toLowerCase()}`, post.status)}
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
        </CredenzaBody>

        <CredenzaFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={loading} className="cursor-pointer">
            {t('buttons.cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={loading}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {loading ? t('buttons.deleting') : t('buttons.delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
