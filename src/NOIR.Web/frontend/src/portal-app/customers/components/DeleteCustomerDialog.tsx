import { useTranslation } from 'react-i18next'
import { Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
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
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <Trash2 className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <AlertDialogTitle>{t('customers.deleteTitle', 'Delete Customer')}</AlertDialogTitle>
              <AlertDialogDescription>
                {t('customers.deleteDescription', {
                  name: fullName,
                  defaultValue: `Are you sure you want to delete "${fullName}"? This action cannot be undone.`,
                })}
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel className="cursor-pointer">{t('labels.cancel', 'Cancel')}</AlertDialogCancel>
          <AlertDialogAction
            onClick={handleDelete}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {t('labels.delete', 'Delete')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
