import { useTranslation } from 'react-i18next'
import { Loader2, Trash2 } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
} from '@uikit'
import { useDeletePromotionMutation } from '@/portal-app/promotions/queries'
import type { PromotionDto } from '@/types/promotion'
import { toast } from 'sonner'

interface DeletePromotionDialogProps {
  promotion: PromotionDto | null
  onOpenChange: (open: boolean) => void
  onSuccess?: () => void
}

export const DeletePromotionDialog = ({ promotion, onOpenChange, onSuccess }: DeletePromotionDialogProps) => {
  const { t } = useTranslation('common')
  const deleteMutation = useDeletePromotionMutation()

  const handleDelete = async () => {
    if (!promotion) return
    try {
      await deleteMutation.mutateAsync(promotion.id)
      toast.success(t('promotions.deleteSuccess', 'Promotion deleted successfully'))
      onOpenChange(false)
      onSuccess?.()
    } catch (err) {
      const message = err instanceof Error ? err.message : t('promotions.deleteError', 'Failed to delete promotion')
      toast.error(message)
    }
  }

  return (
    <Credenza open={!!promotion} onOpenChange={onOpenChange}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <Trash2 className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('promotions.deleteTitle', 'Delete Promotion')}</CredenzaTitle>
              <CredenzaDescription>
                {t('promotions.deleteDescription', {
                  name: promotion?.name,
                  defaultValue: `Are you sure you want to delete "${promotion?.name}"? This action cannot be undone.`
                })}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>
        <CredenzaBody />
        <CredenzaFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} className="cursor-pointer">{t('labels.cancel', 'Cancel')}</Button>
          <Button
            variant="destructive"
            onClick={handleDelete}
            disabled={deleteMutation.isPending}
            className="cursor-pointer"
          >
            {deleteMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {deleteMutation.isPending ? t('labels.deleting', 'Deleting...') : t('labels.delete', 'Delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
