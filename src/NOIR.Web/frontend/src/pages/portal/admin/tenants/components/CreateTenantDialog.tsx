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
import { TenantFormValidated } from './TenantFormValidated'
import { createTenant } from '@/services/tenants'
import type { CreateTenantRequest } from '@/types'
import { Plus } from 'lucide-react'

interface CreateTenantDialogProps {
  onSuccess: () => void
}

export function CreateTenantDialog({ onSuccess }: CreateTenantDialogProps) {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)

  const handleSubmit = async (data: CreateTenantRequest) => {
    await createTenant(data)
    toast.success(t('messages.createSuccess'))
    setOpen(false)
    onSuccess()
  }

  return (
    <Credenza open={open} onOpenChange={setOpen}>
      <CredenzaTrigger asChild>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          {t('tenants.createNew')}
        </Button>
      </CredenzaTrigger>
      <CredenzaContent className="sm:max-w-[500px]">
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
