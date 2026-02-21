import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2 } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
} from '@uikit'
import type { ProductListItem } from '@/types/product'
import { toast } from 'sonner'

interface DeleteProductDialogProps {
  product: ProductListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export const DeleteProductDialog = ({
  product,
  open,
  onOpenChange,
  onConfirm,
}: DeleteProductDialogProps) => {
  const { t } = useTranslation('common')
  const [isDeleting, setIsDeleting] = useState(false)

  const handleConfirm = async () => {
    if (!product) return

    setIsDeleting(true)
    try {
      const result = await onConfirm(product.id)

      if (result.success) {
        toast.success(t('products.deleteSuccess', { name: product.name, defaultValue: `Product "${product.name}" deleted successfully` }))
        onOpenChange(false)
      } else {
        toast.error(result.error || t('products.deleteFailed', 'Failed to delete product'))
      }
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <CredenzaTitle>{t('products.deleteProduct', 'Delete Product')}</CredenzaTitle>
          </div>
          <CredenzaDescription className="pt-2">
            {t('products.deleteConfirmation', {
              name: product?.name,
              defaultValue: `Are you sure you want to delete "${product?.name}"? This action cannot be undone.`
            })}
          </CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody />
        <CredenzaFooter>
          <Button variant="outline" disabled={isDeleting} onClick={() => onOpenChange(false)} className="cursor-pointer">
            {t('labels.cancel', 'Cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={isDeleting}
            className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
          >
            {isDeleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isDeleting ? t('labels.deleting', 'Deleting...') : t('labels.delete', 'Delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
