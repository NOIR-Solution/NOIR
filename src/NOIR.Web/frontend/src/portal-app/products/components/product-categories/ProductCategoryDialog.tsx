import { useEffect, useState } from 'react'
import { useForm, type Resolver } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  Button,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '@uikit'

import {
  createProductCategory,
  updateProductCategory,
} from '@/services/products'
import { useProductCategoriesQuery } from '@/portal-app/products/queries'
import type { ProductCategoryListItem } from '@/types/product'
import { toast } from 'sonner'
import { FolderTree, Loader2 } from 'lucide-react'
import { ApiError } from '@/services/apiClient'
import { generateSlug } from '@/lib/utils/slug'

const createCategorySchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
    slug: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 }))
      .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, t('validation.identifierFormat')),
    description: z.string().optional().nullable(),
    metaTitle: z.string().optional().nullable(),
    metaDescription: z.string().optional().nullable(),
    imageUrl: z.string().optional().nullable(),
    sortOrder: z.coerce.number().default(0),
    parentId: z.string().optional().nullable(),
  })

type CategoryFormData = z.infer<ReturnType<typeof createCategorySchema>>

interface ProductCategoryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  category: ProductCategoryListItem | null
  categories?: ProductCategoryListItem[]  // Optional - will use hook if not provided
  onSuccess: () => void
}

export const ProductCategoryDialog = ({
  open,
  onOpenChange,
  category,
  categories: categoriesProp,
  onSuccess,
}: ProductCategoryDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!category
  const [isSaving, setIsSaving] = useState(false)

  // Fetch fresh categories when dialog opens - this fixes CAT-013 where newly created
  // categories weren't appearing in parent dropdown after page reload
  const { data: categoriesFromHook = [], isLoading: categoriesLoading, refetch: refreshCategories } = useProductCategoriesQuery({})

  // Use hook data (always fresh) as primary source once loaded
  // Fallback to prop only when hook hasn't loaded yet
  const categories = categoriesFromHook.length > 0
    ? categoriesFromHook
    : categoriesProp ?? []

  // Refresh categories when dialog opens to ensure fresh data
  useEffect(() => {
    if (open) {
      refreshCategories()
    }
  }, [open, refreshCategories])

  const form = useForm<CategoryFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createCategorySchema(t)) as unknown as Resolver<CategoryFormData>,
    mode: 'onBlur',
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
      // KNOWN LIMITATION: metaTitle, metaDescription, imageUrl are not included in
      // ProductCategoryListItem DTO. These fields will be empty when editing existing
      // categories - they are only set during category creation.
      form.reset({
        name: category.name,
        slug: category.slug,
        description: category.description || '',
        metaTitle: '',
        metaDescription: '',
        imageUrl: '',
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
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[500px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <FolderTree className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>{isEditing ? t('categories.editCategory', 'Edit Category') : t('categories.newCategory', 'New Category')}</CredenzaTitle>
              <CredenzaDescription>
                {isEditing ? t('categories.editDescription', 'Update category details') : t('categories.createDescription', 'Create a new product category')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <CredenzaBody className="space-y-4">
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
                      disabled={categoriesLoading}
                    >
                      <FormControl>
                        <SelectTrigger className="cursor-pointer" aria-label={t('categories.parentCategory', 'Parent Category')}>
                          <SelectValue placeholder={categoriesLoading ? t('labels.loading', 'Loading...') : t('categories.selectParent', 'Select parent category')} />
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
            </CredenzaBody>

            <CredenzaFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)} className="cursor-pointer">
                {t('labels.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isSaving} className="cursor-pointer">
                {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isEditing ? t('labels.update', 'Update') : t('labels.create', 'Create')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
