import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2 } from 'lucide-react'
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
import type { ProductCategoryListItem } from '@/types/product'
import { toast } from 'sonner'

interface DeleteProductCategoryDialogProps {
  category: ProductCategoryListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export const DeleteProductCategoryDialog = ({
  category,
  open,
  onOpenChange,
  onConfirm,
}: DeleteProductCategoryDialogProps) => {
  const { t } = useTranslation('common')
  const [isDeleting, setIsDeleting] = useState(false)

  const handleConfirm = async () => {
    if (!category) return

    setIsDeleting(true)
    try {
      const result = await onConfirm(category.id)

      if (result.success) {
        toast.success(t('productCategories.categoryDeleted', { name: category.name }))
        onOpenChange(false)
      } else {
        toast.error(result.error || t('productCategories.deleteFailed'))
      }
    } finally {
      setIsDeleting(false)
    }
  }

  const hasProducts = category && category.productCount > 0
  const hasChildren = category && category.childCount > 0

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('productCategories.deleteCategory')}</CredenzaTitle>
              <CredenzaDescription className="pt-2 space-y-2">
                <span>
                  {t('productCategories.deleteCategoryConfirmName')}{' '}
                  <span className="font-semibold text-foreground">"{category?.name}"</span>?
                </span>
                {hasProducts && (
                  <span className="block p-2 rounded-lg bg-amber-500/10 border border-amber-500/20 text-amber-600 dark:text-amber-400 text-sm">
                    <strong>{t('productCategories.warning')}:</strong> {t('productCategories.hasProducts', { count: category?.productCount })}
                  </span>
                )}
                {hasChildren && (
                  <span className="block p-2 rounded-lg bg-red-500/10 border border-red-500/20 text-red-600 dark:text-red-400 text-sm">
                    <strong>{t('productCategories.blocked')}:</strong> {t('productCategories.hasChildren', { count: category?.childCount })}
                  </span>
                )}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>
        <CredenzaBody />
        <CredenzaFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isDeleting} className="cursor-pointer">
            {t('buttons.cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={isDeleting || !!hasChildren}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {isDeleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isDeleting ? t('buttons.deleting') : t('buttons.delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
