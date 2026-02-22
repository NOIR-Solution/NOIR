import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2, Tag } from 'lucide-react'
import {
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
import type { PostTagListItem } from '@/types'

interface DeleteBlogTagDialogProps {
  tag: PostTagListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export const DeleteBlogTagDialog = ({ tag, open, onOpenChange, onConfirm }: DeleteBlogTagDialogProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!tag) return

    setLoading(true)
    try {
      const result = await onConfirm(tag.id)

      if (result.success) {
        toast.success(t('blog.tagDeleted'))
        onOpenChange(false)
      } else {
        toast.error(result.error || t('blog.deleteTagFailed'))
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
              <CredenzaTitle>{t('blog.deleteTag')}</CredenzaTitle>
              <CredenzaDescription>
                {t('blog.deleteTagConfirm')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <CredenzaBody>
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
                  {t('blog.tagUsedInPosts', { count: tag.postCount })}
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
