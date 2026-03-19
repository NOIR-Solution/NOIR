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
  Textarea,
} from '@uikit'
import type { CompanyDto, CompanyListDto } from '@/types/crm'
import { useCreateCompany, useUpdateCompany } from '@/portal-app/crm/queries'
import { getRequiredFields, handleFormError } from '@/lib/form'

const createSchema = (t: (key: string) => string) => z.object({
  name: z.string().min(1, t('validation.required')).max(200),
  domain: z.string().max(100).optional().or(z.literal('')),
  industry: z.string().max(100).optional().or(z.literal('')),
  address: z.string().max(500).optional().or(z.literal('')),
  phone: z.string().max(20).optional().or(z.literal('')),
  website: z.string().max(256).optional().or(z.literal('')),
  taxId: z.string().max(50).optional().or(z.literal('')),
  employeeCount: z.coerce.number().int().positive().optional().or(z.literal('')),
  notes: z.string().max(2000).optional().or(z.literal('')),
})

type CompanyFormData = z.infer<ReturnType<typeof createSchema>>

interface CompanyDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  company?: CompanyDto | CompanyListDto | null
}

export const CompanyDialog = ({ open, onOpenChange, company }: CompanyDialogProps) => {
  const { t } = useTranslation('common')
  const isEdit = !!company
  const createMutation = useCreateCompany()
  const updateMutation = useUpdateCompany()
  const isPending = createMutation.isPending || updateMutation.isPending
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const schema = useMemo(() => createSchema(t), [t])
  const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

  const form = useForm<CompanyFormData>({
    resolver: zodResolver(schema) as never,
    defaultValues: {
      name: '',
      domain: '',
      industry: '',
      address: '',
      phone: '',
      website: '',
      taxId: '',
      employeeCount: '' as unknown as number,
      notes: '',
    },
    mode: 'onBlur',
    reValidateMode: 'onChange',
  })

  useEffect(() => {
    if (open && company) {
      setServerErrors([])
      form.reset({
        name: company.name,
        domain: ('domain' in company ? company.domain : undefined) ?? '',
        industry: ('industry' in company ? company.industry : undefined) ?? '',
        address: ('address' in company && company.address) || '',
        phone: ('phone' in company && company.phone) || '',
        website: ('website' in company && company.website) || '',
        taxId: ('taxId' in company && company.taxId) || '',
        employeeCount: ('employeeCount' in company && company.employeeCount) || ('' as unknown as number),
        notes: ('notes' in company && company.notes) || '',
      })
    } else if (open) {
      setServerErrors([])
      form.reset({
        name: '',
        domain: '',
        industry: '',
        address: '',
        phone: '',
        website: '',
        taxId: '',
        employeeCount: '' as unknown as number,
        notes: '',
      })
    }
  }, [open, company, form])

  const onSubmit = async (data: CompanyFormData) => {
    const request = {
      name: data.name,
      domain: data.domain || undefined,
      industry: data.industry || undefined,
      address: data.address || undefined,
      phone: data.phone || undefined,
      website: data.website || undefined,
      taxId: data.taxId || undefined,
      employeeCount: data.employeeCount ? Number(data.employeeCount) : undefined,
      notes: data.notes || undefined,
    }

    try {
      if (isEdit && 'id' in company!) {
        await updateMutation.mutateAsync({ id: company!.id, request })
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
          <CredenzaTitle>{isEdit ? t('crm.companies.edit') : t('crm.companies.create')}</CredenzaTitle>
          <CredenzaDescription>
            {isEdit ? t('crm.companies.edit') : t('crm.companies.create')}
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
                <Label htmlFor="name">{t('crm.companies.name')}</Label>
                <Input
                  id="name"
                  {...form.register('name')}
                  placeholder={t('crm.companies.name')}
                />
                {form.formState.errors.name && (
                  <p className="text-sm text-destructive">{form.formState.errors.name.message}</p>
                )}
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="domain">{t('crm.companies.domain')}</Label>
                  <Input id="domain" {...form.register('domain')} placeholder="acme.com" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="industry">{t('crm.companies.industry')}</Label>
                  <Input id="industry" {...form.register('industry')} placeholder={t('crm.companies.industry')} />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="address">{t('crm.companies.address')}</Label>
                <Input id="address" {...form.register('address')} placeholder={t('crm.companies.address')} />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="phone">{t('crm.companies.phone')}</Label>
                  <Input id="phone" {...form.register('phone')} placeholder={t('crm.companies.phone')} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="website">{t('crm.companies.website')}</Label>
                  <Input id="website" {...form.register('website')} placeholder="https://..." />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="taxId">{t('crm.companies.taxId')}</Label>
                  <Input id="taxId" {...form.register('taxId')} placeholder={t('crm.companies.taxId')} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="employeeCount">{t('crm.companies.employeeCount')}</Label>
                  <Input id="employeeCount" type="number" {...form.register('employeeCount')} placeholder="0" />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="notes">{t('crm.companies.notes')}</Label>
                <Textarea id="notes" {...form.register('notes')} placeholder={t('crm.companies.notes')} rows={3} />
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
