import { useTranslation } from 'react-i18next'
import { Loader2, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
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
import { useDeleteCustomerMutation } from '@/portal-app/customers/queries'
import type { CustomerDto, CustomerSummaryDto } from '@/types/customer'

interface DeleteCustomerDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  customer: CustomerDto | CustomerSummaryDto | null
  onSuccess?: () => void
}

export const DeleteCustomerDialog = ({ open, onOpenChange, customer, onSuccess }: DeleteCustomerDialogProps) => {
  const { t } = useTranslation('common')
  const deleteMutation = useDeleteCustomerMutation()

  const handleDelete = async () => {
    if (!customer) return
    try {
      await deleteMutation.mutateAsync(customer.id)
      toast.success(t('customers.deleteSuccess', 'Customer deleted successfully'))
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('customers.deleteError', 'Failed to delete customer')
      toast.error(message)
    }
  }

  const fullName = customer ? `${customer.firstName} ${customer.lastName}` : ''

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <Trash2 className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('customers.deleteTitle', 'Delete Customer')}</CredenzaTitle>
              <CredenzaDescription>
                {t('customers.deleteDescription', {
                  name: fullName,
                  defaultValue: `Are you sure you want to delete "${fullName}"? This action cannot be undone.`,
                })}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>
        <CredenzaBody />
        <CredenzaFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={deleteMutation.isPending} className="cursor-pointer">{t('labels.cancel', 'Cancel')}</Button>
          <Button
            variant="destructive"
            onClick={handleDelete}
            disabled={deleteMutation.isPending}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {deleteMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {deleteMutation.isPending ? t('labels.deleting', 'Deleting...') : t('labels.delete', 'Delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
