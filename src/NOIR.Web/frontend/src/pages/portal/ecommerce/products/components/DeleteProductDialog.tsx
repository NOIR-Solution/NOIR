import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2 } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import type { ProductListItem } from '@/types/product'
import { toast } from 'sonner'

interface DeleteProductDialogProps {
  product: ProductListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export function DeleteProductDialog({
  product,
  open,
  onOpenChange,
  onConfirm,
}: DeleteProductDialogProps) {
  const { t } = useTranslation('common')
  const [isDeleting, setIsDeleting] = useState(false)

  const handleConfirm = async () => {
    if (!product) return

    setIsDeleting(true)
    const result = await onConfirm(product.id)
    setIsDeleting(false)

    if (result.success) {
      toast.success(t('products.deleteSuccess', { name: product.name, defaultValue: `Product "${product.name}" deleted successfully` }))
      onOpenChange(false)
    } else {
      toast.error(result.error || t('products.deleteFailed', 'Failed to delete product'))
    }
  }

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="border-destructive/30">
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <AlertDialogTitle>{t('products.deleteProduct', 'Delete Product')}</AlertDialogTitle>
          </div>
          <AlertDialogDescription className="pt-2">
            {t('products.deleteConfirmation', {
              name: product?.name,
              defaultValue: `Are you sure you want to delete "${product?.name}"? This action cannot be undone.`
            })}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isDeleting} className="cursor-pointer">
            {t('labels.cancel', 'Cancel')}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={isDeleting}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90 cursor-pointer"
          >
            {isDeleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isDeleting ? t('labels.deleting', 'Deleting...') : t('labels.delete', 'Delete')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
