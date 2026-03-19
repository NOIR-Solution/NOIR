import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  ColorPicker,
  DatePicker,
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormErrorBanner,
  FormMessage,
  Input,
  Textarea,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'
import { useCreateProject, useUpdateProject } from '@/portal-app/pm/queries'
import type { ProjectDto, ProjectVisibility, ProjectStatus } from '@/types/pm'
import { getRequiredFields, handleFormError } from '@/lib/form'

const PROJECT_COLORS = [
  '#6366f1', // indigo (default)
  '#8b5cf6', // violet
  '#ec4899', // pink
  '#ef4444', // red
  '#f97316', // orange
  '#eab308', // yellow
  '#22c55e', // green
  '#14b8a6', // teal
  '#06b6d4', // cyan
  '#3b82f6', // blue
  '#64748b', // slate
  '#78716c', // stone
]

const createProjectSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 })),
    description: z.string().max(2000, t('validation.maxLength', { count: 2000 })).optional().or(z.literal('')),
    startDate: z.string().optional().or(z.literal('')),
    endDate: z.string().optional().or(z.literal('')),
    dueDate: z.string().optional().or(z.literal('')),
    budget: z.coerce.number().min(0).optional().or(z.literal(0)),
    currency: z.string().max(3).optional().or(z.literal('')),
    color: z.string().optional().or(z.literal('')),
    visibility: z.enum(['Private', 'Internal', 'Public']).default('Private'),
    status: z.enum(['Active', 'Completed', 'OnHold', 'Archived']).optional(),
  })

type ProjectFormData = z.infer<ReturnType<typeof createProjectSchema>>

interface ProjectDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  project?: ProjectDto | null
}

export const ProjectDialog = ({ open, onOpenChange, project }: ProjectDialogProps) => {
  const { t } = useTranslation('common')
  const { formatDate } = useRegionalSettings()
  const isEdit = !!project
  const createMutation = useCreateProject()
  const updateMutation = useUpdateProject()
  const isSubmitting = createMutation.isPending || updateMutation.isPending
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const schema = useMemo(() => createProjectSchema(t), [t])
  const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

  const form = useForm<ProjectFormData>({
    resolver: zodResolver(schema) as unknown as Resolver<ProjectFormData>,
    mode: 'onBlur',
    reValidateMode: 'onChange',
    defaultValues: {
      name: '',
      description: '',
      startDate: '',
      endDate: '',
      dueDate: '',
      budget: 0,
      currency: '',
      color: '#6366f1',
      visibility: 'Private' as ProjectVisibility,
      status: 'Active' as ProjectStatus,
    },
  })

  useEffect(() => {
    if (open) {
      setServerErrors([])
      if (project) {
        form.reset({
          name: project.name,
          description: project.description ?? '',
          startDate: project.startDate?.split('T')[0] ?? '',
          endDate: project.endDate?.split('T')[0] ?? '',
          dueDate: project.dueDate?.split('T')[0] ?? '',
          budget: project.budget ?? 0,
          currency: project.currency ?? '',
          color: project.color ?? '#6366f1',
          visibility: project.visibility,
          status: project.status ?? 'Active',
        })
      } else {
        form.reset({
          name: '',
          description: '',
          startDate: '',
          endDate: '',
          dueDate: '',
          budget: 0,
          currency: '',
          color: '#6366f1',
          visibility: 'Private',
          status: 'Active',
        })
      }
    }
  }, [open, project, form])

  const onSubmit = async (data: ProjectFormData) => {
    const request = {
      name: data.name,
      description: data.description || undefined,
      startDate: data.startDate || undefined,
      endDate: data.endDate || undefined,
      dueDate: data.dueDate || undefined,
      budget: data.budget || undefined,
      currency: data.currency || undefined,
      color: data.color || undefined,
      visibility: data.visibility,
      ...(isEdit && data.status ? { status: data.status } : {}),
    }

    if (isEdit && project) {
      updateMutation.mutate(
        { id: project.id, request },
        {
          onSuccess: () => {
            onOpenChange(false)
          },
          onError: (err) => {
            handleFormError(err, form, setServerErrors, t)
          },
        },
      )
    } else {
      createMutation.mutate(request, {
        onSuccess: () => {
          onOpenChange(false)
        },
        onError: (err) => {
          handleFormError(err, form, setServerErrors, t)
        },
      })
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent>
        <CredenzaHeader>
          <CredenzaTitle>{isEdit ? t('pm.editProject') : t('pm.createProject')}</CredenzaTitle>
          <CredenzaDescription>
            {isEdit
              ? t('pm.editProjectDescription', { defaultValue: 'Update your project settings and details' })
              : t('pm.createProjectDescription', { defaultValue: 'Fill in the details to create a new project' })
            }
          </CredenzaDescription>
        </CredenzaHeader>
        <Form {...form} requiredFields={requiredFields}>
          <form onSubmit={form.handleSubmit(onSubmit)}>
            <CredenzaBody className="space-y-4">
              <FormErrorBanner
                errors={serverErrors}
                onDismiss={() => setServerErrors([])}
                title={t('validation.unableToSave', 'Unable to save')}
              />
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('pm.projectName')}</FormLabel>
                    <FormControl>
                      <Input {...field} placeholder={t('pm.projectName')} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {isEdit && (
                <FormField
                  control={form.control}
                  name="status"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('pm.status')}</FormLabel>
                      <Select onValueChange={field.onChange} value={field.value ?? 'Active'}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          <SelectItem value="Active" className="cursor-pointer">{t('statuses.active', { defaultValue: 'Active' })}</SelectItem>
                          <SelectItem value="Completed" className="cursor-pointer">{t('statuses.completed', { defaultValue: 'Completed' })}</SelectItem>
                          <SelectItem value="OnHold" className="cursor-pointer">{t('statuses.onHold', { defaultValue: 'On Hold' })}</SelectItem>
                          <SelectItem value="Archived" className="cursor-pointer">{t('statuses.archived', { defaultValue: 'Archived' })}</SelectItem>
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              )}

              <FormField
                control={form.control}
                name="description"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('pm.projectDescription')}</FormLabel>
                    <FormControl>
                      <Textarea {...field} placeholder={t('pm.projectDescription')} rows={3} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="startDate"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('pm.startDate')}</FormLabel>
                      <FormControl>
                        <DatePicker
                          value={field.value ? new Date(field.value) : undefined}
                          onChange={(date) => field.onChange(date ? `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}` : '')}
                          placeholder={t('pm.startDate')}
                          formatDate={formatDate}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="dueDate"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('pm.dueDate')}</FormLabel>
                      <FormControl>
                        <DatePicker
                          value={field.value ? new Date(field.value) : undefined}
                          onChange={(date) => field.onChange(date ? `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}` : '')}
                          placeholder={t('pm.dueDate')}
                          formatDate={formatDate}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="budget"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('pm.budget')}</FormLabel>
                      <FormControl>
                        <Input {...field} type="number" min={0} onChange={e => field.onChange(e.target.valueAsNumber || 0)} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="currency"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('pm.currency')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder="USD" maxLength={3} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="visibility"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('pm.visibility')}</FormLabel>
                    <Select onValueChange={field.onChange} value={field.value}>
                      <FormControl>
                        <SelectTrigger className="cursor-pointer">
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="Private" className="cursor-pointer">{t('pm.visibilityPrivate', { defaultValue: 'Private' })}</SelectItem>
                        <SelectItem value="Internal" className="cursor-pointer">{t('pm.visibilityInternal', { defaultValue: 'Internal' })}</SelectItem>
                        <SelectItem value="Public" className="cursor-pointer">{t('pm.visibilityPublic', { defaultValue: 'Public' })}</SelectItem>
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
                    <FormLabel>{t('pm.color')}</FormLabel>
                    <FormControl>
                      <ColorPicker
                        value={field.value || '#6366f1'}
                        onChange={field.onChange}
                        colors={PROJECT_COLORS}
                        showCustomInput
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </CredenzaBody>
            <CredenzaFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)} className="cursor-pointer">
                {t('buttons.cancel')}
              </Button>
              <Button type="submit" disabled={isSubmitting} className="cursor-pointer">
                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isEdit ? t('buttons.save') : t('buttons.create')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
