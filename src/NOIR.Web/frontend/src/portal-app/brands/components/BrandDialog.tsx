import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
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
  LogoUploadField,
  Switch,
  Textarea,
} from '@uikit'

import { useCreateBrandMutation, useUpdateBrandMutation } from '@/portal-app/brands/queries'
import { uploadMedia } from '@/services/media'
import type { BrandListItem } from '@/types/brand'
import { toast } from 'sonner'
import { Loader2, Tag } from 'lucide-react'

const createBrandSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
    slug: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 }))
      .regex(/^[a-z0-9-]+$/, t('validation.identifierFormat')),
    description: z.string().max(500, t('validation.maxLength', { count: 500 })).optional().nullable(),
    logoUrl: z.string().optional().nullable(),
    bannerUrl: z.string().url(t('validation.invalidFormat')).optional().nullable().or(z.literal('')),
    websiteUrl: z.string().url(t('validation.invalidFormat')).optional().nullable().or(z.literal('')),
    isActive: z.boolean(),
    sortOrder: z.number().int().min(0).default(0),
  })

type BrandFormData = z.infer<ReturnType<typeof createBrandSchema>>

interface BrandDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  brand?: BrandListItem | null
  onSuccess?: () => void
}

export const BrandDialog = ({ open, onOpenChange, brand, onSuccess }: BrandDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!brand
  const createMutation = useCreateBrandMutation()
  const updateMutation = useUpdateBrandMutation()

  const form = useForm<BrandFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createBrandSchema(t)) as unknown as Resolver<BrandFormData>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      slug: '',
      description: '',
      logoUrl: '',
      bannerUrl: '',
      websiteUrl: '',
      isActive: true,
      sortOrder: 0,
    },
  })

  // Reset form when dialog opens/closes or brand changes
  useEffect(() => {
    if (open) {
      if (brand) {
        form.reset({
          name: brand.name,
          slug: brand.slug,
          description: brand.description ?? '',
          logoUrl: brand.logoUrl ?? '',
          bannerUrl: brand.bannerUrl ?? '',
          websiteUrl: brand.websiteUrl ?? '',
          isActive: brand.isActive,
          sortOrder: brand.sortOrder,
        })
      } else {
        form.reset({
          name: '',
          slug: '',
          description: '',
          logoUrl: '',
          bannerUrl: '',
          websiteUrl: '',
          isActive: true,
          sortOrder: 0,
        })
      }
    }
  }, [open, brand, form])

  // Auto-generate slug from name
  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const name = e.target.value
    form.setValue('name', name)
    // Only auto-generate slug if it's empty or matches the previous auto-generated value
    const currentSlug = form.getValues('slug')
    const previousName = form.getValues('name')
    const autoSlug = previousName.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
    if (!currentSlug || currentSlug === autoSlug) {
      const newSlug = name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
      form.setValue('slug', newSlug)
    }
  }

  const onSubmit = async (data: BrandFormData) => {
    // Clean up empty strings to null
    const cleanedData = {
      ...data,
      description: data.description || null,
      logoUrl: data.logoUrl || null,
      bannerUrl: data.bannerUrl || null,
      websiteUrl: data.websiteUrl || null,
    }

    try {
      if (isEditing && brand) {
        await updateMutation.mutateAsync({ id: brand.id, request: cleanedData })
        toast.success(t('brands.updateSuccess', 'Brand updated successfully'))
      } else {
        await createMutation.mutateAsync(cleanedData)
        toast.success(t('brands.createSuccess', 'Brand created successfully'))
      }
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : isEditing
        ? t('brands.updateError', 'Failed to update brand')
        : t('brands.createError', 'Failed to create brand')
      toast.error(message)
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <Tag className="h-5 w-5 text-primary" />
            </div>
            <div>
              <DialogTitle>
                {isEditing ? t('brands.editBrand', 'Edit Brand') : t('brands.createBrand', 'Create Brand')}
              </DialogTitle>
              <DialogDescription>
                {isEditing
                  ? t('brands.editBrandDescription', 'Update the brand details below.')
                  : t('brands.createBrandDescription', 'Fill in the details to create a new brand.')}
              </DialogDescription>
            </div>
          </div>
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
                      onChange={handleNameChange}
                      placeholder={t('brands.namePlaceholder', 'Enter brand name')}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="slug"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('labels.slug', 'Slug')}</FormLabel>
                  <FormControl>
                    <Input
                      {...field}
                      placeholder={t('brands.slugPlaceholder', 'brand-slug')}
                    />
                  </FormControl>
                  <FormDescription>
                    {t('brands.slugDescription', 'URL-friendly identifier (auto-generated from name)')}
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('labels.description', 'Description')}</FormLabel>
                  <FormControl>
                    <Textarea
                      {...field}
                      value={field.value ?? ''}
                      placeholder={t('brands.descriptionPlaceholder', 'Brief description of the brand')}
                      rows={3}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="logoUrl"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('labels.logo', 'Logo')}</FormLabel>
                  <FormControl>
                    <LogoUploadField
                      value={field.value}
                      onChange={field.onChange}
                      onUpload={async (file) => {
                        const result = await uploadMedia(file, 'branding')
                        return result.defaultUrl || ''
                      }}
                      placeholder={t('brands.uploadLogo', 'Upload brand logo')}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="websiteUrl"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('labels.websiteUrl', 'Website URL')}</FormLabel>
                  <FormControl>
                    <Input
                      {...field}
                      value={field.value ?? ''}
                      placeholder={t('brands.websiteUrlPlaceholder', 'https://brand-website.com')}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="sortOrder"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('labels.sortOrder', 'Sort Order')}</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      {...field}
                      onChange={(e) => field.onChange(parseInt(e.target.value) || 0)}
                      min={0}
                    />
                  </FormControl>
                  <FormDescription>
                    {t('brands.sortOrderDescription', 'Lower numbers appear first')}
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="isActive"
              render={({ field }) => (
                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                  <div className="space-y-0.5">
                    <FormLabel>{t('labels.active', 'Active')}</FormLabel>
                    <FormDescription className="text-xs">
                      {t('brands.activeDescription', 'Show this brand on the storefront')}
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
                {t('labels.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isSubmitting} className="cursor-pointer">
                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isEditing ? t('labels.save', 'Save') : t('labels.create', 'Create')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
