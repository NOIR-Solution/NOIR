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
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  // Fetch full tenant data when dialog opens
  useEffect(() => {
    if (open && tenant) {
      const fetchTenant = async () => {
        setLoading(true)
        setFieldErrors({}) // Clear errors when loading new tenant
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
      setFieldErrors({})
    }
  }, [open, tenant, onOpenChange])

  const handleUpdate = async (data: UpdateTenantRequest) => {
    if (!tenant) return
    setSaving(true)
    setFieldErrors({}) // Clear previous errors
    try {
      await updateTenant(tenant.id, data)
      toast.success(t('messages.updateSuccess'))
      onOpenChange(false)
      onSuccess()
    } catch (err) {
      if (err instanceof ApiError) {
        // Check if we have field-specific validation errors
        if (err.hasFieldErrors && err.errors) {
          setFieldErrors(err.errors)
          // Don't show toast for validation errors - they're displayed inline
        } else {
          // For non-validation errors, show toast
          toast.error(err.message)
        }
      } else {
        toast.error(t('messages.operationFailed'))
      }
    } finally {
      setSaving(false)
    }
  }

  const handleFieldChange = (field: string) => {
    // Clear error for the modified field
    setFieldErrors(prev => {
      const pascalCase = field.charAt(0).toUpperCase() + field.slice(1)
      const newErrors = { ...prev }
      delete newErrors[field]
      delete newErrors[pascalCase]
      return newErrors
    })
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
            <TenantForm
              tenant={fullTenant}
              onSubmit={handleUpdate}
              onCancel={() => onOpenChange(false)}
              loading={saving}
              errors={fieldErrors}
              onFieldChange={handleFieldChange}
            />
          ) : null}
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}
