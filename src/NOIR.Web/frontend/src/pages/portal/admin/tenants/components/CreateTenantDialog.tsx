import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaTrigger,
  CredenzaBody,
} from '@/components/ui/credenza'
import { Button } from '@/components/ui/button'
import { TenantForm } from './TenantForm'
import { createTenant } from '@/services/tenants'
import { ApiError } from '@/services/apiClient'
import type { CreateTenantRequest } from '@/types'
import { Plus } from 'lucide-react'

interface CreateTenantDialogProps {
  onSuccess: () => void
}

export function CreateTenantDialog({ onSuccess }: CreateTenantDialogProps) {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  const handleSubmit = async (data: CreateTenantRequest) => {
    setLoading(true)
    setFieldErrors({}) // Clear previous errors
    try {
      await createTenant(data)
      toast.success(t('messages.createSuccess'))
      setOpen(false)
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
      setLoading(false)
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

  const handleOpenChange = (newOpen: boolean) => {
    setOpen(newOpen)
    if (!newOpen) {
      // Clear errors when dialog closes
      setFieldErrors({})
    }
  }

  return (
    <Credenza open={open} onOpenChange={handleOpenChange}>
      <CredenzaTrigger asChild>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          {t('tenants.createNew')}
        </Button>
      </CredenzaTrigger>
      <CredenzaContent className="sm:max-w-[500px]">
        <CredenzaHeader>
          <CredenzaTitle>{t('tenants.createNew')}</CredenzaTitle>
          <CredenzaDescription>{t('tenants.createDescription')}</CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          <TenantForm
            onSubmit={handleSubmit}
            onCancel={() => setOpen(false)}
            loading={loading}
            errors={fieldErrors}
            onFieldChange={handleFieldChange}
          />
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}
