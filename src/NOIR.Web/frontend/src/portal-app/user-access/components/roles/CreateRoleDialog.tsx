import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Loader2, Shield } from 'lucide-react'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import {
  Button,
  ColorPicker,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormErrorBanner,
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
import { createRole } from '@/services/roles'
import { useAvailableRolesQuery } from '@/portal-app/user-access/queries'
import { getRequiredFields, handleFormError } from '@/lib/form'

const createFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string()
      .min(2, t('validation.minLength', { count: 2 }))
      .max(50, t('validation.maxLength', { count: 50 }))
      .regex(/^[a-zA-Z][a-zA-Z0-9_-]*$/, t('roles.namePattern')),
    description: z.string().max(500, t('validation.maxLength', { count: 500 })).optional(),
    parentRoleId: z.string().optional(),
    color: z.string().optional(),
    iconName: z.string().optional(),
  })

type FormValues = z.infer<ReturnType<typeof createFormSchema>>

interface CreateRoleDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export const CreateRoleDialog = ({ open, onOpenChange, onSuccess }: CreateRoleDialogProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const { data: existingRoles = [] } = useAvailableRolesQuery()
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const schema = useMemo(() => createFormSchema(t), [t])
  const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

  const form = useForm<FormValues>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(schema) as unknown as Resolver<FormValues>,
    mode: 'onBlur',
    reValidateMode: 'onChange',
    defaultValues: {
      name: '',
      description: '',
      parentRoleId: '',
      color: '#6b7280',
      iconName: '',
    },
  })

  const onSubmit = async (values: FormValues) => {
    setLoading(true)
    setServerErrors([])
    try {
      await createRole({
        name: values.name,
        description: values.description || undefined,
        parentRoleId: values.parentRoleId || undefined,
        color: values.color || undefined,
        iconName: values.iconName || undefined,
      })

      toast.success(t('roles.createSuccess', 'Role created'))

      form.reset()
      onOpenChange(false)
      onSuccess()
    } catch (err) {
      handleFormError(err, form, setServerErrors, t)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[500px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <Shield className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>{t('roles.createTitle', 'Create New Role')}</CredenzaTitle>
              <CredenzaDescription>
                {t('roles.createDescription', 'Add a new role with custom permissions.')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <Form {...form} requiredFields={requiredFields}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
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
                    <FormLabel>{t('roles.fields.name', 'Role Name')}</FormLabel>
                    <FormControl>
                      <Input placeholder={t('roles.fields.namePlaceholder', 'e.g., Editor, Viewer')} {...field} />
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
                    <FormLabel>{t('roles.fields.description', 'Description')}</FormLabel>
                    <FormControl>
                      <Textarea
                        placeholder={t('roles.fields.descriptionPlaceholder', 'Describe what this role can do...')}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="parentRoleId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('roles.fields.parentRole', 'Parent Role')}</FormLabel>
                    <Select
                      onValueChange={(value) => field.onChange(value === '__none__' ? '' : value)}
                      value={field.value || '__none__'}
                    >
                      <FormControl>
                        <SelectTrigger className="cursor-pointer">
                          <SelectValue placeholder={t('roles.fields.parentRolePlaceholder', 'Select parent role (optional)')} />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="__none__">{t('roles.fields.noParent', 'No parent role')}</SelectItem>
                        {existingRoles.map((role) => (
                          <SelectItem key={role.id} value={role.id}>
                            <div className="flex items-center gap-2">
                              <div
                                className="w-4 h-4 rounded-full shrink-0"
                                style={{ backgroundColor: role.color || '#6b7280' }}
                              />
                              {role.name}
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormDescription>
                      {t('roles.fields.parentRoleDescription', 'Child roles inherit permissions from their parent.')}
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="color"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('roles.fields.color', 'Color')}</FormLabel>
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
            </CredenzaBody>

            <CredenzaFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)} className="cursor-pointer">
                {t('buttons.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={loading} className="cursor-pointer">
                {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {loading ? t('labels.creating', 'Creating...') : t('buttons.create', 'Create')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
