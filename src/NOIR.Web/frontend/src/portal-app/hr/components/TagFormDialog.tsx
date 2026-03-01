import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, Tags } from 'lucide-react'
import { toast } from 'sonner'
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
import { useCreateTag, useUpdateTag } from '@/portal-app/hr/queries'
import type { EmployeeTagDto, EmployeeTagCategory } from '@/types/hr'

const TAG_CATEGORIES: EmployeeTagCategory[] = ['Team', 'Skill', 'Project', 'Location', 'Seniority', 'Employment', 'Custom']

const PRESET_COLORS = [
  '#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6',
  '#EC4899', '#06B6D4', '#F97316', '#6366F1', '#14B8A6',
  '#84CC16', '#A855F7',
]

const createTagSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
    category: z.string().min(1, t('validation.required')),
    color: z.string().min(1, t('validation.required')),
    description: z.string().max(500, t('validation.maxLength', { count: 500 })).optional().nullable(),
    sortOrder: z.coerce.number().int().min(0).default(0),
  })

type TagFormData = z.infer<ReturnType<typeof createTagSchema>>

interface TagFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  tag?: EmployeeTagDto | null
}

export const TagFormDialog = ({ open, onOpenChange, tag }: TagFormDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!tag
  const createMutation = useCreateTag()
  const updateMutation = useUpdateTag()

  const form = useForm<TagFormData>({
    resolver: zodResolver(createTagSchema(t)) as unknown as Resolver<TagFormData>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      category: 'Custom',
      color: PRESET_COLORS[0],
      description: '',
      sortOrder: 0,
    },
  })

  useEffect(() => {
    if (open) {
      if (tag) {
        form.reset({
          name: tag.name,
          category: tag.category,
          color: tag.color,
          description: tag.description || '',
          sortOrder: tag.sortOrder,
        })
      } else {
        form.reset({
          name: '',
          category: 'Custom',
          color: PRESET_COLORS[0],
          description: '',
          sortOrder: 0,
        })
      }
    }
  }, [open, tag, form])

  const onSubmit = async (data: TagFormData) => {
    try {
      if (isEditing && tag) {
        await updateMutation.mutateAsync({
          id: tag.id,
          data: {
            name: data.name,
            category: data.category as EmployeeTagCategory,
            color: data.color || null,
            description: data.description || null,
            sortOrder: data.sortOrder,
          },
        })
        toast.success(t('hr.tags.tagUpdated'))
      } else {
        await createMutation.mutateAsync({
          name: data.name,
          category: data.category as EmployeeTagCategory,
          color: data.color || null,
          description: data.description || null,
          sortOrder: data.sortOrder,
        })
        toast.success(t('hr.tags.tagCreated'))
      }
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('errors.generic', 'An error occurred')
      toast.error(message)
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending
  const selectedColor = form.watch('color')

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[480px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
              <Tags className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>{isEditing ? t('hr.tags.editTag') : t('hr.tags.createTag')}</CredenzaTitle>
              <CredenzaDescription>
                {t('hr.tags.description')}
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
                      <FormLabel>{t('hr.tags.name')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('hr.tags.namePlaceholder')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="category"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.tags.category')}</FormLabel>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {TAG_CATEGORIES.map((cat) => (
                            <SelectItem key={cat} value={cat} className="cursor-pointer">
                              {t(`hr.tags.categories.${cat}`)}
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
                  name="color"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.tags.color')}</FormLabel>
                      <div className="flex flex-wrap gap-2">
                        {PRESET_COLORS.map((color) => (
                          <button
                            key={color}
                            type="button"
                            className={`h-7 w-7 rounded-full cursor-pointer border-2 transition-all ${
                              selectedColor === color ? 'border-foreground scale-110' : 'border-transparent hover:scale-105'
                            }`}
                            style={{ backgroundColor: color }}
                            onClick={() => field.onChange(color)}
                            aria-label={color}
                          />
                        ))}
                      </div>
                      <FormControl>
                        <div className="flex items-center gap-2 mt-2">
                          <Input
                            {...field}
                            type="color"
                            className="h-9 w-14 cursor-pointer p-1"
                          />
                          <Input
                            value={field.value}
                            onChange={field.onChange}
                            placeholder="#000000"
                            className="flex-1"
                          />
                        </div>
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
                      <FormLabel>{t('hr.tags.descriptionLabel')}</FormLabel>
                      <FormControl>
                        <Textarea {...field} value={field.value || ''} placeholder={t('hr.tags.descriptionPlaceholder')} rows={2} />
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
                      <FormLabel>{t('hr.tags.sortOrder')}</FormLabel>
                      <FormControl>
                        <Input {...field} type="number" min={0} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            </CredenzaBody>
            <CredenzaFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={isPending}
                className="cursor-pointer"
              >
                {t('labels.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isPending} className="cursor-pointer">
                {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isEditing ? t('labels.save', 'Save') : t('labels.create', 'Create')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
