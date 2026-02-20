import { useTranslation } from 'react-i18next'
import { Loader2, Trash2 } from 'lucide-react'
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
    <AlertDialog open={!!promotion} onOpenChange={onOpenChange}>
      <AlertDialogContent className="border-destructive/30">
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <Trash2 className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <AlertDialogTitle>{t('promotions.deleteTitle', 'Delete Promotion')}</AlertDialogTitle>
              <AlertDialogDescription>
                {t('promotions.deleteDescription', {
                  name: promotion?.name,
                  defaultValue: `Are you sure you want to delete "${promotion?.name}"? This action cannot be undone.`
                })}
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={deleteMutation.isPending} className="cursor-pointer">{t('labels.cancel', 'Cancel')}</AlertDialogCancel>
          <AlertDialogAction
            onClick={handleDelete}
            disabled={deleteMutation.isPending}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {deleteMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {deleteMutation.isPending ? t('labels.deleting', 'Deleting...') : t('labels.delete', 'Delete')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
