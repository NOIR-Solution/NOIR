import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaHeader,
  CredenzaTitle,
} from '@uikit'

import { TenantFormValidated, type ProvisionTenantFormData, type UpdateTenantFormData } from './TenantFormValidated'
import { provisionTenant } from '@/services/tenants'
import type { ProvisionTenantRequest } from '@/types'

interface CreateTenantDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export const CreateTenantDialog = ({ open, onOpenChange, onSuccess }: CreateTenantDialogProps) => {
  const { t } = useTranslation('common')

  const handleSubmit = async (data: ProvisionTenantFormData | UpdateTenantFormData) => {
    // This is always ProvisionTenantFormData in create mode
    const formData = data as ProvisionTenantFormData
    // Map form data to provision request
    // Admin user is always required for new tenants
    const request: ProvisionTenantRequest = {
      identifier: formData.identifier,
      name: formData.name,
      description: formData.description || undefined,
      note: formData.note || undefined,
      createAdminUser: true,  // Always create admin user
      adminEmail: formData.adminEmail,
      adminPassword: formData.adminPassword,
      adminFirstName: formData.adminFirstName || undefined,
      adminLastName: formData.adminLastName || undefined,
    }

    const result = await provisionTenant(request)

    // Show appropriate message based on admin user creation result
    if (result.adminUserCreated && result.adminEmail) {
      // Full success - tenant and admin user created
      toast.success(t('tenants.messages.provisionSuccess', {
        email: result.adminEmail,
      }))
    } else if (result.adminCreationError) {
      // Partial success - tenant created but admin user failed
      toast.warning(t('tenants.messages.provisionPartial', {
        error: result.adminCreationError,
      }))
    } else {
      // Generic success (no admin requested)
      toast.success(t('messages.createSuccess'))
    }

    onOpenChange(false)
    onSuccess()
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[550px]">
        <CredenzaHeader>
          <CredenzaTitle>{t('tenants.createTitle')}</CredenzaTitle>
          <CredenzaDescription>{t('tenants.createDescription')}</CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          <TenantFormValidated
            onSubmit={handleSubmit}
            onCancel={() => onOpenChange(false)}
          />
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}
