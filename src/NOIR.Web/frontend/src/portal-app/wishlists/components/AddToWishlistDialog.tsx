import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Heart, Loader2, Plus } from 'lucide-react'
import { toast } from 'sonner'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '@uikit'
import { useWishlistsQuery, useAddToWishlist } from '@/portal-app/wishlists/queries'

interface AddToWishlistDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  productId: string
  productVariantId?: string | null
  productName?: string
  onSuccess?: () => void
}

export const AddToWishlistDialog = ({
  open,
  onOpenChange,
  productId,
  productVariantId,
  productName,
  onSuccess,
}: AddToWishlistDialogProps) => {
  const { t } = useTranslation('common')
  const [selectedWishlistId, setSelectedWishlistId] = useState<string>('default')
  const [note, setNote] = useState('')

  const { data: wishlists, isLoading: loadingWishlists } = useWishlistsQuery()
  const addToWishlistMutation = useAddToWishlist()

  const handleSubmit = async () => {
    try {
      await addToWishlistMutation.mutateAsync({
        wishlistId: selectedWishlistId === 'default' ? null : selectedWishlistId,
        productId,
        productVariantId: productVariantId ?? null,
        note: note.trim() || null,
      })
      toast.success(
        t('wishlists.addedToWishlist', 'Product added to wishlist')
      )
      onSuccess?.()
      onOpenChange(false)
      // Reset state
      setSelectedWishlistId('default')
      setNote('')
    } catch (err) {
      const message = err instanceof Error
        ? err.message
        : t('wishlists.addFailed', 'Failed to add product to wishlist')
      toast.error(message)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[425px]">
        <CredenzaHeader>
          <CredenzaTitle className="flex items-center gap-2">
            <Heart className="h-5 w-5 text-rose-500" />
            {t('wishlists.addToWishlist', 'Add to Wishlist')}
          </CredenzaTitle>
          <CredenzaDescription>
            {productName
              ? t('wishlists.addToWishlistDescription', 'Save "{{name}}" to one of your wishlists.', { name: productName })
              : t('wishlists.addToWishlistGeneric', 'Choose a wishlist to save this product.')}
          </CredenzaDescription>
        </CredenzaHeader>

        <CredenzaBody>
          <div className="space-y-4 py-2">
            {/* Wishlist Selector */}
            <div className="space-y-2">
              <Label>{t('wishlists.selectWishlist', 'Wishlist')}</Label>
              <Select
                value={selectedWishlistId}
                onValueChange={setSelectedWishlistId}
                disabled={loadingWishlists}
              >
                <SelectTrigger className="cursor-pointer">
                  <SelectValue placeholder={t('wishlists.selectWishlistPlaceholder', 'Choose a wishlist...')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="default" className="cursor-pointer">
                    {t('wishlists.defaultWishlist', 'Default Wishlist')}
                  </SelectItem>
                  {wishlists?.filter((w) => !w.isDefault).map((wishlist) => (
                    <SelectItem key={wishlist.id} value={wishlist.id} className="cursor-pointer">
                      {wishlist.name} ({wishlist.itemCount})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Optional Note */}
            <div className="space-y-2">
              <Label>{t('wishlists.note', 'Note')} <span className="text-muted-foreground text-xs">({t('labels.optional', 'Optional')})</span></Label>
              <Textarea
                value={note}
                onChange={(e) => setNote(e.target.value)}
                placeholder={t('wishlists.notePlaceholder', 'Add a note about this item...')}
                rows={3}
                maxLength={500}
              />
            </div>
          </div>
        </CredenzaBody>

        <CredenzaFooter>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            className="cursor-pointer"
          >
            {t('buttons.cancel', 'Cancel')}
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={addToWishlistMutation.isPending}
            className="cursor-pointer"
          >
            {addToWishlistMutation.isPending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Plus className="mr-2 h-4 w-4" />
            )}
            {t('wishlists.addItem', 'Add to Wishlist')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
