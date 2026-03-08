import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { FolderTree, Loader2 } from 'lucide-react'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
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

import { toast } from 'sonner'
import { useCreateBlogCategory, useUpdateBlogCategory, useBlogCategoriesQuery } from '@/portal-app/blogs/queries'
import type { PostCategoryListItem } from '@/types'

const createFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(2, t('validation.minLength', { count: 2 })).max(100, t('validation.maxLength', { count: 100 })),
    slug: z.string().min(2, t('validation.minLength', { count: 2 })).max(100, t('validation.maxLength', { count: 100 })).regex(/^[a-z0-9-]+$/, t('validation.identifierFormat')),
    description: z.string().max(500, t('validation.maxLength', { count: 500 })).optional(),
    sortOrder: z.preprocess(
      (val) => (val === '' || val === undefined || val === null ? 0 : Number(val)),
      z.number().int().min(0, t('validation.minValue', { value: 0 }))
    ),
    parentId: z.string().optional(),
  })

type FormValues = z.output<ReturnType<typeof createFormSchema>>

interface BlogCategoryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  category?: PostCategoryListItem | null
  parentId?: string | null
  onSuccess: () => void
}

export const BlogCategoryDialog = ({ open, onOpenChange, category, parentId, onSuccess }: BlogCategoryDialogProps) => {
  const { t } = useTranslation('common')
  const isEdit = !!category

  const createMutation = useCreateBlogCategory()
  const updateMutation = useUpdateBlogCategory()
  const isPending = createMutation.isPending || updateMutation.isPending

  const { data: allCategories = [] } = useBlogCategoriesQuery()
  const existingCategories = category
    ? allCategories.filter(c => c.id !== category.id && c.parentId !== category.id)
    : allCategories

  const form = useForm<FormValues>({
    resolver: zodResolver(createFormSchema(t)) as unknown as Resolver<FormValues>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      slug: '',
      description: '',
      sortOrder: 0,
      parentId: '',
    },
  })

  useEffect(() => {
    if (!open) return
    if (category) {
      form.reset({
        name: category.name,
        slug: category.slug,
        description: category.description || '',
        sortOrder: category.sortOrder,
        parentId: category.parentId || '',
      })
    } else {
      form.reset({ name: '', slug: '', description: '', sortOrder: 0, parentId: parentId || '' })
    }
  }, [open, category, parentId, form])

  // Auto-generate slug from name (create mode only)
  const watchName = form.watch('name')
  useEffect(() => {
    if (!isEdit && watchName) {
      const slug = watchName
        .toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-')
      form.setValue('slug', slug)
    }
  }, [watchName, isEdit, form])

  const onSubmit = async (values: FormValues) => {
    const request = {
      name: values.name,
      slug: values.slug,
      description: values.description || undefined,
      sortOrder: values.sortOrder,
      parentId: values.parentId || undefined,
    }

    try {
      if (isEdit && category) {
        await updateMutation.mutateAsync({ id: category.id, request })
        toast.success(t('blog.categoryUpdated'))
      } else {
        await createMutation.mutateAsync(request)
        toast.success(t('blog.categoryCreated'))
      }
      form.reset()
      onOpenChange(false)
      onSuccess()
    } catch {
      toast.error(isEdit ? t('blog.failedToUpdateCategory') : t('blog.failedToCreateCategory'))
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[500px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-xl border border-primary/20">
              <FolderTree className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>
                {isEdit ? t('blog.editCategory') : t('blog.createNewCategory')}
              </CredenzaTitle>
              <CredenzaDescription>
                {isEdit ? t('blog.editCategoryDescription') : t('blog.createCategoryDescription')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)}>
            <CredenzaBody>
              <div className="space-y-4">
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('blog.categoryName')}</FormLabel>
                      <FormControl>
                        <Input placeholder={t('blog.categoryNamePlaceholder')} {...field} />
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
                      <FormLabel>{t('labels.slug')}</FormLabel>
                      <FormControl>
                        <Input placeholder={t('blog.categorySlugPlaceholder')} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="description"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('blog.descriptionOptional')}</FormLabel>
                      <FormControl>
                        <Textarea placeholder={t('blog.categoryDescription')} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="sortOrder"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('labels.sortOrder')}</FormLabel>
                        <FormControl>
                          <Input type="number" min="0" {...field} />
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
                        <FormLabel>{t('blog.parentCategory')}</FormLabel>
                        <Select
                          onValueChange={(value) => field.onChange(value === '__none__' ? '' : value)}
                          value={field.value || '__none__'}
                        >
                          <FormControl>
                            <SelectTrigger className="cursor-pointer" aria-label={t('blog.selectParentCategoryOptional', 'Select parent category (optional)')}>
                              <SelectValue placeholder={t('blog.selectParentOptional')} />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="__none__" className="cursor-pointer">{t('blog.noParent')}</SelectItem>
                            {existingCategories.map((cat) => (
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
                </div>
              </div>
            </CredenzaBody>

            <CredenzaFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isPending} className="cursor-pointer">
                {t('labels.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isPending} className="cursor-pointer">
                {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isEdit ? t('labels.update', 'Update') : t('labels.create', 'Create')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
