import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { AlertTriangle, Building2, Loader2 } from 'lucide-react'
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

  const handleConfirm = async () => {
    if (!tenant) return

    setLoading(true)
    try {
      const result = await onConfirm(tenant.id)

      if (result.success) {
        toast.success(t('messages.deleteSuccess'))
        onOpenChange(false)
      } else {
        toast.error(result.error || t('messages.operationFailed'))
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
              <AlertDialogTitle>{t('tenants.deleteTitle')}</AlertDialogTitle>
              <AlertDialogDescription>
                {t('tenants.deleteDescription', { name: tenant?.name || tenant?.identifier })}
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>

        {tenant && (
          <div className="my-4 p-4 bg-muted rounded-lg">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center">
                <Building2 className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="font-medium">{tenant.name || tenant.identifier}</p>
                {tenant.name && tenant.identifier !== tenant.name && (
                  <p className="text-sm text-muted-foreground">{tenant.identifier}</p>
                )}
              </div>
            </div>
            <div className="mt-3 p-2 bg-destructive/10 rounded text-sm text-destructive">
              {t('tenants.deleteWarning', { defaultValue: 'This will permanently delete all tenant data including users, settings, and content.' })}
            </div>
          </div>
        )}

        <AlertDialogFooter>
          <AlertDialogCancel disabled={loading} className="cursor-pointer">
            {t('buttons.cancel')}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={loading}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90 cursor-pointer"
          >
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {loading ? t('labels.deleting', { defaultValue: 'Deleting...' }) : t('buttons.delete')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
