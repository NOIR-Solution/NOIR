import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, FolderTree, Loader2 } from 'lucide-react'
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
import type { PostCategoryListItem } from '@/types'

interface DeleteBlogCategoryDialogProps {
  category: PostCategoryListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export const DeleteBlogCategoryDialog = ({ category, open, onOpenChange, onConfirm }: DeleteBlogCategoryDialogProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!category) return

    setLoading(true)
    try {
      const result = await onConfirm(category.id)

      if (result.success) {
        toast.success(t('blog.categoryDeleted'))
        onOpenChange(false)
      } else {
        toast.error(result.error || t('blog.deleteCategoryFailed'))
      }
    } finally {
      setLoading(false)
    }
  }

  const canDelete = category && category.postCount === 0 && category.childCount === 0

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('blog.deleteCategory')}</CredenzaTitle>
              <CredenzaDescription>
                {t('blog.deleteCategoryConfirm')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <CredenzaBody>
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
                    <p>{t('blog.categoryHasPosts', { count: category.postCount })}</p>
                  )}
                  {category.childCount > 0 && (
                    <p>{t('blog.categoryHasChildren', { count: category.childCount })}</p>
                  )}
                  <p className="mt-1 font-medium">{t('blog.reassignFirst')}</p>
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
            disabled={loading || !canDelete}
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
