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
import type { ContactDto, ContactListDto, ContactSource } from '@/types/crm'
import { useCreateContact, useUpdateContact } from '@/portal-app/crm/queries'
import { getRequiredFields, handleFormError } from '@/lib/form'

const CONTACT_SOURCES: ContactSource[] = ['Web', 'Referral', 'Social', 'Cold', 'Event', 'Other']

const createSchema = (t: (key: string) => string) => z.object({
  firstName: z.string().min(1, t('validation.required')).max(100),
  lastName: z.string().min(1, t('validation.required')).max(100),
  email: z.string().min(1, t('validation.required')).email(t('validation.invalidEmail')),
  phone: z.string().max(20).optional().or(z.literal('')),
  jobTitle: z.string().max(100).optional().or(z.literal('')),
  companyId: z.string().optional().or(z.literal('')),
  ownerId: z.string().optional().or(z.literal('')),
  source: z.enum(['Web', 'Referral', 'Social', 'Cold', 'Event', 'Other']),
  notes: z.string().max(2000).optional().or(z.literal('')),
})

type ContactFormData = z.infer<ReturnType<typeof createSchema>>

interface ContactDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  contact?: ContactDto | ContactListDto | null
}

export const ContactDialog = ({ open, onOpenChange, contact }: ContactDialogProps) => {
  const { t } = useTranslation('common')
  const isEdit = !!contact
  const createMutation = useCreateContact()
  const updateMutation = useUpdateContact()
  const isPending = createMutation.isPending || updateMutation.isPending
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const schema = useMemo(() => createSchema(t), [t])
  const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

  const form = useForm<ContactFormData>({
    resolver: zodResolver(schema) as never,
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      jobTitle: '',
      companyId: '',
      ownerId: '',
      source: 'Web' as ContactSource,
      notes: '',
    },
    mode: 'onBlur',
    reValidateMode: 'onChange',
  })

  useEffect(() => {
    if (open && contact) {
      setServerErrors([])
      form.reset({
        firstName: contact.firstName,
        lastName: contact.lastName,
        email: contact.email,
        phone: ('phone' in contact ? contact.phone : undefined) ?? '',
        jobTitle: ('jobTitle' in contact ? contact.jobTitle : undefined) ?? '',
        companyId: ('companyId' in contact ? contact.companyId : undefined) ?? '',
        ownerId: ('ownerId' in contact ? contact.ownerId : undefined) ?? '',
        source: contact.source,
        notes: ('notes' in contact ? contact.notes : undefined) ?? '',
      })
    } else if (open) {
      setServerErrors([])
      form.reset({
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        jobTitle: '',
        companyId: '',
        ownerId: '',
        source: 'Web',
        notes: '',
      })
    }
  }, [open, contact, form])

  const onSubmit = async (data: ContactFormData) => {
    const request = {
      ...data,
      phone: data.phone || undefined,
      jobTitle: data.jobTitle || undefined,
      companyId: data.companyId || undefined,
      ownerId: data.ownerId || undefined,
      notes: data.notes || undefined,
    }

    try {
      if (isEdit && 'id' in contact!) {
        await updateMutation.mutateAsync({ id: contact!.id, request })
        toast.success(t('labels.savedSuccessfully'))
      } else {
        await createMutation.mutateAsync(request)
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
          <CredenzaTitle>{isEdit ? t('crm.contacts.edit') : t('crm.contacts.create')}</CredenzaTitle>
          <CredenzaDescription>
            {isEdit ? t('crm.contacts.edit') : t('crm.contacts.create')}
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
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="firstName">{t('crm.contacts.firstName')}</Label>
                  <Input
                    id="firstName"
                    {...form.register('firstName')}
                    placeholder={t('crm.contacts.firstName')}
                  />
                  {form.formState.errors.firstName && (
                    <p className="text-sm text-destructive">{form.formState.errors.firstName.message}</p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="lastName">{t('crm.contacts.lastName')}</Label>
                  <Input
                    id="lastName"
                    {...form.register('lastName')}
                    placeholder={t('crm.contacts.lastName')}
                  />
                  {form.formState.errors.lastName && (
                    <p className="text-sm text-destructive">{form.formState.errors.lastName.message}</p>
                  )}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="email">{t('crm.contacts.email')}</Label>
                <Input
                  id="email"
                  type="email"
                  {...form.register('email')}
                  placeholder={t('crm.contacts.email')}
                />
                {form.formState.errors.email && (
                  <p className="text-sm text-destructive">{form.formState.errors.email.message}</p>
                )}
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="phone">{t('crm.contacts.phone')}</Label>
                  <Input
                    id="phone"
                    {...form.register('phone')}
                    placeholder={t('crm.contacts.phone')}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="jobTitle">{t('crm.contacts.jobTitle')}</Label>
                  <Input
                    id="jobTitle"
                    {...form.register('jobTitle')}
                    placeholder={t('crm.contacts.jobTitle')}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="source">{t('crm.contacts.source')}</Label>
                <Select
                  value={form.watch('source')}
                  onValueChange={(value) => form.setValue('source', value as ContactSource)}
                >
                  <SelectTrigger className="cursor-pointer" aria-label={t('crm.contacts.source')}>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {CONTACT_SOURCES.map((source) => (
                      <SelectItem key={source} value={source} className="cursor-pointer">
                        {t(`crm.sources.${source}`)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="notes">{t('crm.contacts.notes')}</Label>
                <Textarea
                  id="notes"
                  {...form.register('notes')}
                  placeholder={t('crm.contacts.notes')}
                  rows={3}
                />
              </div>
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
