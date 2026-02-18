import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Tag, Pencil } from 'lucide-react'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import {
  Button,
  ColorPicker,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Textarea,
} from '@uikit'

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

interface BlogTagDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  tag?: PostTagListItem | null
  onSuccess: () => void
}

export const BlogTagDialog = ({ open, onOpenChange, tag, onSuccess }: BlogTagDialogProps) => {
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
        toast.success(t('blog.tagUpdated'))
      } else {
        await createTag(request)
        toast.success(t('blog.tagCreated'))
      }

      form.reset()
      onOpenChange(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : isEdit ? t('blog.failedToUpdateTag') : t('blog.failedToCreateTag')
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
                {isEdit ? t('blog.editTag') : t('blog.createNewTag')}
              </DialogTitle>
              <DialogDescription>
                {isEdit
                  ? t('blog.editTagDescription')
                  : t('blog.createTagDescription')}
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
                  <FormLabel>{t('blog.tagName')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('blog.tagNamePlaceholder')} {...field} />
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
                    <Input placeholder={t('blog.tagSlugPlaceholder')} {...field} />
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
                    <Textarea
                      placeholder={t('blog.tagDescription')}
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
                  <FormLabel>{t('blog.color')}</FormLabel>
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
                {t('buttons.cancel')}
              </Button>
              <Button type="submit" disabled={loading}>
                {loading
                  ? t('buttons.saving')
                  : (isEdit ? t('buttons.update') : t('buttons.create'))}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
