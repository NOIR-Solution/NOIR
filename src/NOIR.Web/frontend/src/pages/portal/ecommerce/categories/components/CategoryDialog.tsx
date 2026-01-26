import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  createProductCategory,
  updateProductCategory,
} from '@/services/products'
import type { ProductCategoryListItem } from '@/types/product'
import { toast } from 'sonner'
import { ApiError } from '@/services/apiClient'
import { generateSlug } from '@/lib/utils/slug'

const categorySchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must be less than 100 characters'),
  slug: z.string().min(1, 'Slug is required').max(100, 'Slug must be less than 100 characters')
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Slug must be lowercase letters, numbers, and hyphens only'),
  description: z.string().optional().nullable(),
  metaTitle: z.string().optional().nullable(),
  metaDescription: z.string().optional().nullable(),
  imageUrl: z.string().optional().nullable(),
  sortOrder: z.coerce.number().default(0),
  parentId: z.string().optional().nullable(),
})

type CategoryFormData = z.infer<typeof categorySchema>

interface CategoryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  category: ProductCategoryListItem | null
  categories: ProductCategoryListItem[]
  onSuccess: () => void
}

export function CategoryDialog({
  open,
  onOpenChange,
  category,
  categories,
  onSuccess,
}: CategoryDialogProps) {
  const { t } = useTranslation('common')
  const isEditing = !!category
  const [isSaving, setIsSaving] = useState(false)

  const form = useForm<CategoryFormData>({
    resolver: zodResolver(categorySchema),
    defaultValues: {
      name: '',
      slug: '',
      description: '',
      metaTitle: '',
      metaDescription: '',
      imageUrl: '',
      sortOrder: 0,
      parentId: null,
    },
  })

  useEffect(() => {
    if (category) {
      // TODO: ProductCategoryListItem doesn't include metaTitle, metaDescription, imageUrl
      // To preserve these on edit, extend the type and backend DTO to include these fields
      // For now, these will reset to empty when editing
      form.reset({
        name: category.name,
        slug: category.slug,
        description: category.description || '',
        metaTitle: (category as unknown as Record<string, string>).metaTitle || '',
        metaDescription: (category as unknown as Record<string, string>).metaDescription || '',
        imageUrl: (category as unknown as Record<string, string>).imageUrl || '',
        sortOrder: category.sortOrder,
        parentId: category.parentId || null,
      })
    } else {
      form.reset({
        name: '',
        slug: '',
        description: '',
        metaTitle: '',
        metaDescription: '',
        imageUrl: '',
        sortOrder: 0,
        parentId: null,
      })
    }
  }, [category, form, open])

  const handleNameChange = (name: string) => {
    form.setValue('name', name)
    if (!isEditing || !form.getValues('slug')) {
      form.setValue('slug', generateSlug(name))
    }
  }

  const onSubmit = async (data: CategoryFormData) => {
    setIsSaving(true)
    try {
      if (isEditing && category) {
        await updateProductCategory(category.id, {
          ...data,
          parentId: data.parentId || null,
        })
        toast.success(t('categories.updateSuccess', 'Category updated successfully'))
      } else {
        await createProductCategory({
          ...data,
          parentId: data.parentId || null,
        })
        toast.success(t('categories.createSuccess', 'Category created successfully'))
      }
      onOpenChange(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t('categories.saveFailed', 'Failed to save category')
      toast.error(message)
    } finally {
      setIsSaving(false)
    }
  }

  // Filter out current category from parent options (to prevent circular reference)
  const parentOptions = categories.filter((c) => c.id !== category?.id)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>{isEditing ? t('categories.editCategory', 'Edit Category') : t('categories.newCategory', 'New Category')}</DialogTitle>
          <DialogDescription>
            {isEditing ? t('categories.editDescription', 'Update category details') : t('categories.createDescription', 'Create a new product category')}
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
                      onChange={(e) => handleNameChange(e.target.value)}
                      placeholder={t('categories.namePlaceholder', 'Category name')}
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
                    <Input {...field} placeholder={t('categories.slugPlaceholder', 'category-slug')} />
                  </FormControl>
                  <FormDescription>{t('categories.slugDescription', 'URL-friendly identifier')}</FormDescription>
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
                      value={field.value || ''}
                      placeholder={t('categories.descriptionPlaceholder', 'Category description')}
                      rows={3}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="parentId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('categories.parentCategory', 'Parent Category')}</FormLabel>
                  <Select
                    onValueChange={(value) => field.onChange(value === 'none' ? null : value)}
                    value={field.value || 'none'}
                  >
                    <FormControl>
                      <SelectTrigger className="cursor-pointer">
                        <SelectValue placeholder={t('categories.selectParent', 'Select parent category')} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="none" className="cursor-pointer">
                        {t('categories.noParent', 'No parent (top level)')}
                      </SelectItem>
                      {parentOptions.map((cat) => (
                        <SelectItem key={cat.id} value={cat.id} className="cursor-pointer">
                          {cat.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
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
                    <Input {...field} type="number" min="0" />
                  </FormControl>
                  <FormDescription>{t('categories.sortOrderDescription', 'Lower numbers appear first')}</FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)} className="cursor-pointer">
                {t('labels.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isSaving} className="cursor-pointer">
                {isSaving ? t('labels.saving', 'Saving...') : isEditing ? t('labels.update', 'Update') : t('labels.create', 'Create')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
