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
import { TenantFormValidated, type ProvisionTenantFormData, type UpdateTenantFormData } from './TenantFormValidated'
import { provisionTenant } from '@/services/tenants'
import type { ProvisionTenantRequest } from '@/types'
import { Plus } from 'lucide-react'

interface CreateTenantDialogProps {
  onSuccess: () => void
}

export function CreateTenantDialog({ onSuccess }: CreateTenantDialogProps) {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)

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
        defaultValue: 'Tenant created successfully with admin user {{email}}',
        email: result.adminEmail,
      }))
    } else if (result.adminCreationError) {
      // Partial success - tenant created but admin user failed
      toast.warning(t('tenants.messages.provisionPartial', {
        defaultValue: 'Tenant created, but admin user creation failed: {{error}}',
        error: result.adminCreationError,
      }))
    } else {
      // Generic success (no admin requested)
      toast.success(t('messages.createSuccess'))
    }

    setOpen(false)
    onSuccess()
  }

  return (
    <Credenza open={open} onOpenChange={setOpen}>
      <CredenzaTrigger asChild>
        <Button className="group shadow-lg hover:shadow-xl transition-all duration-300">
          <Plus className="mr-2 h-4 w-4 transition-transform group-hover:rotate-90 duration-300" />
          {t('tenants.createNew')}
        </Button>
      </CredenzaTrigger>
      <CredenzaContent className="sm:max-w-[550px]">
        <CredenzaHeader>
          <CredenzaTitle>{t('tenants.createTitle')}</CredenzaTitle>
          <CredenzaDescription>{t('tenants.createDescription')}</CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          <TenantFormValidated
            onSubmit={handleSubmit}
            onCancel={() => setOpen(false)}
          />
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}
