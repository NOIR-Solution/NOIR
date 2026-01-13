import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import type { TenantListItem } from '@/types'

interface DeleteTenantDialogProps {
  tenant: TenantListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export function DeleteTenantDialog({
  tenant,
  open,
  onOpenChange,
  onConfirm,
}: DeleteTenantDialogProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)

  const handleConfirm = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!tenant) return

    setLoading(true)
    const result = await onConfirm(tenant.id)
    setLoading(false)

    if (result.success) {
      toast.success(t('messages.deleteSuccess'))
      onOpenChange(false)
    } else {
      toast.error(result.error || t('messages.operationFailed'))
    }
  }

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <form onSubmit={handleConfirm}>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('tenants.deleteTitle')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('tenants.deleteDescription', { name: tenant?.name || tenant?.identifier })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter className="mt-4">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={loading}>
              {t('buttons.cancel')}
            </Button>
            <Button
              type="submit"
              disabled={loading}
              className="border border-destructive bg-transparent text-destructive hover:bg-destructive hover:text-white"
            >
              {loading ? t('labels.loading') : t('buttons.delete')}
            </Button>
          </AlertDialogFooter>
        </form>
      </AlertDialogContent>
    </AlertDialog>
  )
}
