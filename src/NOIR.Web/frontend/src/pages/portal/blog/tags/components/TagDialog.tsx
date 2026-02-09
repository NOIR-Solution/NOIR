import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Tag, Pencil } from 'lucide-react'
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
import { ColorPicker } from '@/components/ui/color-picker'
import { toast } from 'sonner'
import { createTag, updateTag } from '@/services/blog'
import { ApiError } from '@/services/apiClient'
import type { PostTagListItem, CreateTagRequest } from '@/types'

const createFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(2, t('validation.minLength', { count: 2 })).max(50, t('validation.maxLength', { count: 50 })),
    slug: z.string().min(2, t('validation.minLength', { count: 2 })).max(50, t('validation.maxLength', { count: 50 })).regex(/^[a-z0-9-]+$/, t('validation.identifierFormat')),
    description: z.string().max(500, t('validation.maxLength', { count: 500 })).optional(),
    color: z.string().optional(),
  })

type FormValues = z.infer<ReturnType<typeof createFormSchema>>

interface TagDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  tag?: PostTagListItem | null
  onSuccess: () => void
}

export function TagDialog({ open, onOpenChange, tag, onSuccess }: TagDialogProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const isEdit = !!tag

  const form = useForm<FormValues>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createFormSchema(t)) as unknown as Resolver<FormValues>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      slug: '',
      description: '',
      color: '#6b7280',
    },
  })

  useEffect(() => {
    if (open) {
      // Reset form with tag data if editing
      if (tag) {
        form.reset({
          name: tag.name,
          slug: tag.slug,
          description: tag.description || '',
          color: tag.color || '#6b7280',
        })
      } else {
        form.reset({
          name: '',
          slug: '',
          description: '',
          color: '#6b7280',
        })
      }
    }
  }, [open, tag, form])

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
      const request: CreateTagRequest = {
        name: values.name,
        slug: values.slug,
        description: values.description || undefined,
        color: values.color || undefined,
      }

      if (isEdit && tag) {
        await updateTag(tag.id, request)
        toast.success('Tag updated')
      } else {
        await createTag(request)
        toast.success('Tag created')
      }

      form.reset()
      onOpenChange(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : isEdit ? 'Failed to update tag' : 'Failed to create tag'
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
                <Tag className="h-5 w-5 text-primary" />
              )}
            </div>
            <div>
              <DialogTitle>
                {isEdit ? 'Edit Tag' : 'Create New Tag'}
              </DialogTitle>
              <DialogDescription>
                {isEdit
                  ? 'Update the tag details below.'
                  : 'Add a new tag to label your blog posts.'}
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
                  <FormLabel>Tag Name</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g., JavaScript, Tutorial" {...field} />
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
                    <Input placeholder="e.g., javascript, tutorial" {...field} />
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
                      placeholder="Describe what this tag is about..."
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="color"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Color</FormLabel>
                  <FormControl>
                    <ColorPicker
                      value={field.value}
                      onChange={field.onChange}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

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
