import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { FolderTree, Pencil } from 'lucide-react'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { toast } from 'sonner'
import { createCategory, updateCategory, getCategories } from '@/services/blog'
import { ApiError } from '@/services/apiClient'
import type { PostCategoryListItem, CreateCategoryRequest } from '@/types'

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

interface CategoryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  category?: PostCategoryListItem | null
  onSuccess: () => void
}

export function CategoryDialog({ open, onOpenChange, category, onSuccess }: CategoryDialogProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const [existingCategories, setExistingCategories] = useState<PostCategoryListItem[]>([])
  const isEdit = !!category

  const form = useForm<FormValues>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
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
    if (open) {
      // Fetch existing categories for parent selection
      getCategories({ topLevelOnly: false })
        .then(result => {
          // Filter out the current category and its children to prevent circular references
          const filtered = category
            ? result.filter(c => c.id !== category.id && c.parentId !== category.id)
            : result
          setExistingCategories(filtered)
        })
        .catch(() => setExistingCategories([]))

      // Reset form with category data if editing
      if (category) {
        form.reset({
          name: category.name,
          slug: category.slug,
          description: category.description || '',
          sortOrder: category.sortOrder,
          parentId: category.parentId || '',
        })
      } else {
        form.reset({
          name: '',
          slug: '',
          description: '',
          sortOrder: 0,
          parentId: '',
        })
      }
    }
  }, [open, category, form])

  // Auto-generate slug from name
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
    setLoading(true)
    try {
      const request: CreateCategoryRequest = {
        name: values.name,
        slug: values.slug,
        description: values.description || undefined,
        sortOrder: values.sortOrder,
        parentId: values.parentId || undefined,
      }

      if (isEdit && category) {
        await updateCategory(category.id, request)
        toast.success('Category updated')
      } else {
        await createCategory(request)
        toast.success('Category created')
      }

      form.reset()
      onOpenChange(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : isEdit ? 'Failed to update category' : 'Failed to create category'
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              {isEdit ? (
                <Pencil className="h-5 w-5 text-primary" />
              ) : (
                <FolderTree className="h-5 w-5 text-primary" />
              )}
            </div>
            <div>
              <DialogTitle>
                {isEdit ? 'Edit Category' : 'Create New Category'}
              </DialogTitle>
              <DialogDescription>
                {isEdit
                  ? 'Update the category details below.'
                  : 'Add a new category to organize your blog posts.'}
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
                  <FormLabel>Category Name</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g., Technology, News" {...field} />
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
                  <FormLabel>Slug</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g., technology, news" {...field} />
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
                  <FormLabel>Description (optional)</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder="Describe what this category is about..."
                      {...field}
                    />
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
                    <FormLabel>Sort Order</FormLabel>
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
                    <FormLabel>Parent Category</FormLabel>
                    <Select
                      onValueChange={(value) => field.onChange(value === '__none__' ? '' : value)}
                      value={field.value || '__none__'}
                    >
                      <FormControl>
                        <SelectTrigger className="cursor-pointer" aria-label={t('blog.selectParentCategoryOptional', 'Select parent category (optional)')}>
                          <SelectValue placeholder="Select parent (optional)" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="__none__" className="cursor-pointer">No parent</SelectItem>
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

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={loading}>
                {loading
                  ? (isEdit ? 'Updating...' : 'Creating...')
                  : (isEdit ? 'Update' : 'Create')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
