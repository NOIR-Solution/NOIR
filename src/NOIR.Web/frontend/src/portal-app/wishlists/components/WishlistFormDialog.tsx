import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Switch,
} from '@uikit'
import { useCreateWishlist, useUpdateWishlist } from '@/portal-app/wishlists/queries'
import type { WishlistDto } from '@/types/wishlist'

const createWishlistSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string()
      .min(1, t('validation.required'))
      .max(100, t('validation.maxLength', { count: 100 })),
    isPublic: z.boolean().default(false),
  })

type WishlistFormData = z.infer<ReturnType<typeof createWishlistSchema>>

interface WishlistFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  wishlist?: WishlistDto | null
  onSuccess?: () => void
}

export const WishlistFormDialog = ({
  open,
  onOpenChange,
  wishlist,
  onSuccess,
}: WishlistFormDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!wishlist
  const createMutation = useCreateWishlist()
  const updateMutation = useUpdateWishlist()

  const form = useForm<WishlistFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createWishlistSchema(t)) as unknown as Resolver<WishlistFormData>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      isPublic: false,
    },
  })

  // Reset form when dialog opens/closes or wishlist changes
  useEffect(() => {
    if (open) {
      if (wishlist) {
        form.reset({
          name: wishlist.name,
          isPublic: wishlist.isPublic,
        })
      } else {
        form.reset({
          name: '',
          isPublic: false,
        })
      }
    }
  }, [open, wishlist, form])

  const onSubmit = async (data: WishlistFormData) => {
    try {
      if (isEditing && wishlist) {
        await updateMutation.mutateAsync({
          id: wishlist.id,
          request: { name: data.name, isPublic: data.isPublic },
        })
        toast.success(t('wishlists.updateSuccess', 'Wishlist updated successfully'))
      } else {
        await createMutation.mutateAsync({
          name: data.name,
          isPublic: data.isPublic,
        })
        toast.success(t('wishlists.createSuccess', 'Wishlist created successfully'))
      }
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : isEditing
        ? t('wishlists.updateFailed', 'Failed to update wishlist')
        : t('wishlists.createFailed', 'Failed to create wishlist')
      toast.error(message)
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>
            {isEditing
              ? t('wishlists.editWishlist', 'Edit Wishlist')
              : t('wishlists.createWishlist', 'Create Wishlist')}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? t('wishlists.editWishlistDescription', 'Update the wishlist details below.')
              : t('wishlists.createWishlistDescription', 'Fill in the details to create a new wishlist.')}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('labels.name', 'Name')}</FormLabel>
                  <FormControl>
                    <Input
                      {...field}
                      placeholder={t('wishlists.namePlaceholder', 'e.g., Holiday Gift Ideas')}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="isPublic"
              render={({ field }) => (
                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                  <div className="space-y-0.5">
                    <FormLabel>{t('wishlists.public', 'Public')}</FormLabel>
                    <FormDescription className="text-xs">
                      {t('wishlists.publicDescription', 'Allow others to view this wishlist via a shared link')}
                    </FormDescription>
                  </div>
                  <FormControl>
                    <Switch
                      checked={field.value}
                      onCheckedChange={field.onChange}
                      className="cursor-pointer"
                    />
                  </FormControl>
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                className="cursor-pointer"
              >
                {t('buttons.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isSubmitting} className="cursor-pointer">
                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isEditing ? t('buttons.save', 'Save') : t('buttons.create', 'Create')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
