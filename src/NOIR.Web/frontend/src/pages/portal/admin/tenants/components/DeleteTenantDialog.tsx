import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
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
import type { TenantListItem } from '@/types'

interface DeleteTenantDialogProps {
  tenant: TenantListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<boolean>
}

export function DeleteTenantDialog({
  tenant,
  open,
  onOpenChange,
  onConfirm,
}: DeleteTenantDialogProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!tenant) return

    setLoading(true)
    const success = await onConfirm(tenant.id)
    setLoading(false)

    if (success) {
      toast.success(t('messages.deleteSuccess'))
      onOpenChange(false)
    } else {
      toast.error(t('messages.operationFailed'))
    }
  }

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t('tenants.deleteTitle')}</AlertDialogTitle>
          <AlertDialogDescription>
            {t('tenants.deleteDescription', { name: tenant?.name || tenant?.identifier })}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={loading}>{t('buttons.cancel')}</AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={loading}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {loading ? t('labels.loading') : t('buttons.delete')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
