import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
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

  const handleSubmit = async (data: CreateTenantRequest) => {
    setLoading(true)
    try {
      await createTenant(data)
      toast.success(t('messages.createSuccess'))
      setOpen(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : t('messages.operationFailed')
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          {t('tenants.createNew')}
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>{t('tenants.createNew')}</DialogTitle>
          <DialogDescription>{t('tenants.createDescription')}</DialogDescription>
        </DialogHeader>
        <TenantForm
          onSubmit={handleSubmit}
          onCancel={() => setOpen(false)}
          loading={loading}
        />
      </DialogContent>
    </Dialog>
  )
}
