import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
} from '@/components/ui/credenza'
import { TenantFormValidated, type CreateTenantFormData, type UpdateTenantFormData } from './TenantFormValidated'
import { getTenant, updateTenant } from '@/services/tenants'
import { ApiError } from '@/services/apiClient'
import type { TenantListItem, Tenant } from '@/types'

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

  const handleSubmit = async (data: CreateTenantFormData | UpdateTenantFormData) => {
    if (!tenant) return
    // When editing, data will have UpdateTenantFormData shape
    const updateData = data as UpdateTenantFormData
    await updateTenant(tenant.id, {
      identifier: updateData.identifier,
      name: updateData.name,
      isActive: updateData.isActive ?? true,
    })
    toast.success(t('messages.updateSuccess'))
    onOpenChange(false)
    onSuccess()
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[500px]">
        <CredenzaHeader>
          <CredenzaTitle>{t('tenants.editTitle')}</CredenzaTitle>
          <CredenzaDescription>{t('tenants.editDescription')}</CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          {loading ? (
            <div className="space-y-4">
              {/* Identifier field skeleton */}
              <div className="space-y-2">
                <div className="h-4 w-20 bg-muted animate-pulse rounded" />
                <div className="h-10 w-full bg-muted animate-pulse rounded-md" />
                <div className="h-3 w-48 bg-muted animate-pulse rounded" />
              </div>
              {/* Name field skeleton */}
              <div className="space-y-2">
                <div className="h-4 w-16 bg-muted animate-pulse rounded" />
                <div className="h-10 w-full bg-muted animate-pulse rounded-md" />
              </div>
              {/* Checkbox skeleton */}
              <div className="flex items-center space-x-2">
                <div className="h-4 w-4 bg-muted animate-pulse rounded" />
                <div className="h-4 w-12 bg-muted animate-pulse rounded" />
              </div>
              {/* Buttons skeleton */}
              <div className="flex justify-end space-x-2 pt-4">
                <div className="h-10 w-20 bg-muted animate-pulse rounded-md" />
                <div className="h-10 w-20 bg-muted animate-pulse rounded-md" />
              </div>
            </div>
          ) : fullTenant ? (
            <TenantFormValidated
              tenant={fullTenant}
              onSubmit={handleSubmit}
              onCancel={() => onOpenChange(false)}
            />
          ) : null}
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}
