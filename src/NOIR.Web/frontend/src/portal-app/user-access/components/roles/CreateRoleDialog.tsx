import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2, Shield } from 'lucide-react'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import {
  Alert,
  AlertDescription,
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
import { createRole, getRoles } from '@/services/roles'
import { ApiError } from '@/services/apiClient'
import type { RoleListItem } from '@/types'

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
  const [existingRoles, setExistingRoles] = useState<RoleListItem[]>([])
  const [apiError, setApiError] = useState<string | null>(null)

  const form = useForm<FormValues>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createFormSchema(t)) as unknown as Resolver<FormValues>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      description: '',
      parentRoleId: '',
      color: '#6b7280',
      iconName: '',
    },
  })

  useEffect(() => {
    if (open) {
      setApiError(null)
      // Fetch existing roles for parent selection
      getRoles({ pageSize: 100 })
        .then(result => setExistingRoles(result.items))
        .catch(() => setExistingRoles([]))
    }
  }, [open])

  const onSubmit = async (values: FormValues) => {
    setLoading(true)
    setApiError(null)
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
      if (err instanceof ApiError) {
        // Extract field-level errors from API response and set them on the form
        const fieldErrors = err.response?.errors
        if (fieldErrors) {
          Object.entries(fieldErrors).forEach(([field, messages]) => {
            const fieldName = field.charAt(0).toLowerCase() + field.slice(1)
            if (fieldName in form.getValues()) {
              form.setError(fieldName as keyof FormValues, { message: messages[0] })
            }
          })
        }
        setApiError(err.message)
      } else {
        setApiError(t('roles.createError', 'Failed to create role'))
      }
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

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <CredenzaBody className="space-y-4">
              {apiError && (
                <Alert variant="destructive" className="border-destructive/30">
                  <AlertTriangle className="h-4 w-4" />
                  <AlertDescription>{apiError}</AlertDescription>
                </Alert>
              )}
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
