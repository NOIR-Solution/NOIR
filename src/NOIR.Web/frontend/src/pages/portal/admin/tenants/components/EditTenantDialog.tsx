import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { TenantForm } from './TenantForm'
import { getTenant, updateTenant } from '@/services/tenants'
import { ApiError } from '@/services/apiClient'
import type { TenantListItem, Tenant, UpdateTenantRequest } from '@/types'

interface EditTenantDialogProps {
  tenant: TenantListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export function EditTenantDialog({ tenant, open, onOpenChange, onSuccess }: EditTenantDialogProps) {
  const { t } = useTranslation('common')
  const [fullTenant, setFullTenant] = useState<Tenant | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)

  // Fetch full tenant data when dialog opens
  useEffect(() => {
    if (open && tenant) {
      const fetchTenant = async () => {
        setLoading(true)
        try {
          const data = await getTenant(tenant.id)
          setFullTenant(data)
        } catch (err) {
          const message = err instanceof ApiError ? err.message : 'Failed to load tenant'
          toast.error(message)
          onOpenChange(false)
        } finally {
          setLoading(false)
        }
      }
      fetchTenant()
    } else {
      setFullTenant(null)
    }
  }, [open, tenant, onOpenChange])

  const handleUpdate = async (data: UpdateTenantRequest) => {
    if (!tenant) return
    setSaving(true)
    try {
      await updateTenant(tenant.id, data)
      toast.success(t('messages.updateSuccess'))
      onOpenChange(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t('messages.operationFailed')
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>{t('tenants.editTitle')}</DialogTitle>
          <DialogDescription>{t('tenants.editDescription')}</DialogDescription>
        </DialogHeader>
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <p className="text-muted-foreground">{t('labels.loading')}</p>
          </div>
        ) : fullTenant ? (
          <TenantForm
            tenant={fullTenant}
            onSubmit={handleUpdate}
            onCancel={() => onOpenChange(false)}
            loading={saving}
          />
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
