import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { AlertTriangle, Building2, Loader2 } from 'lucide-react'
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
import type { TenantListItem } from '@/types'

interface DeleteTenantDialogProps {
  tenant: TenantListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export const DeleteTenantDialog = ({
  tenant,
  open,
  onOpenChange,
  onConfirm,
}: DeleteTenantDialogProps) => {
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
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[450px] border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('tenants.deleteTitle')}</CredenzaTitle>
              <CredenzaDescription>
                {t('tenants.deleteDescription', { name: tenant?.name || tenant?.identifier })}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <CredenzaBody>
          {tenant && (
            <div className="my-2 p-4 bg-muted rounded-lg">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center shrink-0">
                  <Building2 className="h-5 w-5 text-primary" />
                </div>
                <div className="min-w-0">
                  <p className="font-medium truncate">{tenant.name || tenant.identifier}</p>
                  {tenant.name && tenant.identifier !== tenant.name && (
                    <p className="text-sm text-muted-foreground truncate">{tenant.identifier}</p>
                  )}
                </div>
              </div>
              <div className="mt-3 p-2 bg-destructive/10 rounded text-sm text-destructive">
                {t('tenants.deleteWarning')}
              </div>
            </div>
          )}
        </CredenzaBody>

        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={loading}
            className="cursor-pointer"
          >
            {t('buttons.cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={loading}
            className="cursor-pointer"
          >
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {loading ? t('labels.deleting') : t('buttons.delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
