import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
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
  FormErrorBanner,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '@uikit'
import type { ActivityDto, ActivityType } from '@/types/crm'
import { useCreateActivity, useUpdateActivity } from '@/portal-app/crm/queries'
import { getRequiredFields, handleFormError } from '@/lib/form'

const ACTIVITY_TYPES: ActivityType[] = ['Call', 'Email', 'Meeting', 'Note']

const createSchema = (t: (key: string) => string) => z.object({
  type: z.enum(['Call', 'Email', 'Meeting', 'Note']),
  subject: z.string().min(1, t('validation.required')).max(200),
  description: z.string().max(2000).optional().or(z.literal('')),
  performedAt: z.string().min(1, t('validation.required')),
  durationMinutes: z.coerce.number().int().positive().optional().or(z.literal('')),
})

type ActivityFormData = z.infer<ReturnType<typeof createSchema>>

interface ActivityDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  activity?: ActivityDto | null
  contactId?: string
  leadId?: string
}

export const ActivityDialog = ({ open, onOpenChange, activity, contactId, leadId }: ActivityDialogProps) => {
  const { t } = useTranslation('common')
  const isEdit = !!activity
  const createMutation = useCreateActivity()
  const updateMutation = useUpdateActivity()
  const isPending = createMutation.isPending || updateMutation.isPending
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const schema = useMemo(() => createSchema(t), [t])
  const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

  const form = useForm<ActivityFormData>({
    resolver: zodResolver(schema) as never,
    defaultValues: {
      type: 'Call' as ActivityType,
      subject: '',
      description: '',
      performedAt: new Date().toISOString().slice(0, 16),
      durationMinutes: '' as unknown as number,
    },
    mode: 'onBlur',
    reValidateMode: 'onChange',
  })

  const selectedType = form.watch('type')
  const showDuration = selectedType === 'Call' || selectedType === 'Meeting'

  useEffect(() => {
    if (open && activity) {
      setServerErrors([])
      form.reset({
        type: activity.type,
        subject: activity.subject,
        description: activity.description ?? '',
        performedAt: new Date(activity.performedAt).toISOString().slice(0, 16),
        durationMinutes: activity.durationMinutes ?? ('' as unknown as number),
      })
    } else if (open) {
      setServerErrors([])
      form.reset({
        type: 'Call',
        subject: '',
        description: '',
        performedAt: new Date().toISOString().slice(0, 16),
        durationMinutes: '' as unknown as number,
      })
    }
  }, [open, activity, form])

  const onSubmit = async (data: ActivityFormData) => {
    try {
      if (isEdit) {
        await updateMutation.mutateAsync({
          id: activity!.id,
          request: {
            type: data.type,
            subject: data.subject,
            description: data.description || undefined,
            performedAt: new Date(data.performedAt).toISOString(),
            durationMinutes: showDuration && data.durationMinutes ? Number(data.durationMinutes) : undefined,
          },
        })
        toast.success(t('labels.savedSuccessfully'))
      } else {
        await createMutation.mutateAsync({
          type: data.type,
          subject: data.subject,
          description: data.description || undefined,
          contactId,
          leadId,
          performedAt: new Date(data.performedAt).toISOString(),
          durationMinutes: showDuration && data.durationMinutes ? Number(data.durationMinutes) : undefined,
        })
        toast.success(t('labels.createdSuccessfully'))
      }
      onOpenChange(false)
    } catch (err) {
      handleFormError(err, form, setServerErrors, t)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent>
        <CredenzaHeader>
          <CredenzaTitle>{isEdit ? t('crm.activities.edit') : t('crm.activities.log')}</CredenzaTitle>
          <CredenzaDescription>
            {isEdit ? t('crm.activities.edit') : t('crm.activities.log')}
          </CredenzaDescription>
        </CredenzaHeader>
        <Form {...form} requiredFields={requiredFields}>
          <form onSubmit={form.handleSubmit(onSubmit)}>
            <CredenzaBody>
              <div className="space-y-4">
                <FormErrorBanner
                  errors={serverErrors}
                  onDismiss={() => setServerErrors([])}
                  title={t('validation.unableToSave', 'Unable to save')}
                />
              <div className="space-y-2">
                <Label htmlFor="type">{t('crm.activities.type')}</Label>
                <Select
                  value={form.watch('type')}
                  onValueChange={(value) => form.setValue('type', value as ActivityType)}
                >
                  <SelectTrigger className="cursor-pointer">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {ACTIVITY_TYPES.map((type) => (
                      <SelectItem key={type} value={type} className="cursor-pointer">
                        {t(`crm.activities.types.${type}`)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="subject">{t('crm.activities.subject')}</Label>
                <Input
                  id="subject"
                  {...form.register('subject')}
                  placeholder={t('crm.activities.subject')}
                />
                {form.formState.errors.subject && (
                  <p className="text-sm text-destructive">{form.formState.errors.subject.message}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">{t('crm.activities.description')}</Label>
                <Textarea
                  id="description"
                  {...form.register('description')}
                  placeholder={t('crm.activities.description')}
                  rows={3}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="performedAt">{t('crm.activities.performedAt')}</Label>
                  <Input
                    id="performedAt"
                    type="datetime-local"
                    {...form.register('performedAt')}
                    className="cursor-pointer"
                  />
                  {form.formState.errors.performedAt && (
                    <p className="text-sm text-destructive">{form.formState.errors.performedAt.message}</p>
                  )}
                </div>
                {showDuration && (
                  <div className="space-y-2">
                    <Label htmlFor="durationMinutes">{t('crm.activities.duration')}</Label>
                    <Input
                      id="durationMinutes"
                      type="number"
                      {...form.register('durationMinutes')}
                      placeholder="30"
                    />
                  </div>
                )}
              </div>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isPending} className="cursor-pointer">
              {t('labels.cancel')}
            </Button>
            <Button type="submit" disabled={isPending} className="cursor-pointer">
              {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isEdit ? t('labels.save') : t('labels.create')}
            </Button>
          </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
