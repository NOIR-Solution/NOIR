import { useState, useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type FieldValues, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import {
  Truck,
  Lock,
  AlertTriangle,
  Loader2,
  ExternalLink,
} from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  Combobox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
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
  Separator,
  Switch,
} from '@uikit'

import { toast } from 'sonner'
import type {
  ShippingProviderDto,
  ShippingProviderSchema,
  GatewayEnvironment,
  ConfigureShippingProviderRequest,
  UpdateShippingProviderRequest,
} from '@/types/shipping'

interface ConfigureProviderDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  provider: ShippingProviderDto | null
  schema: ShippingProviderSchema
  onConfigure: (request: ConfigureShippingProviderRequest) => Promise<{ success: boolean; error?: string }>
  onUpdate: (id: string, request: UpdateShippingProviderRequest) => Promise<{ success: boolean; error?: string }>
}

export const ConfigureProviderDialog = ({
  open,
  onOpenChange,
  provider,
  schema,
  onConfigure,
  onUpdate,
}: ConfigureProviderDialogProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const [showProductionWarning, setShowProductionWarning] = useState(false)
  const [pendingEnvironment, setPendingEnvironment] = useState<GatewayEnvironment | null>(null)

  const isEditing = !!provider

  // Build dynamic schema based on provider fields - memoized to prevent rebuilds
  const formSchema = useMemo(() => {
    const schemaFields: Record<string, z.ZodTypeAny> = {
      displayName: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
      environment: z.enum(['Sandbox', 'Production']),
      supportsCod: z.boolean(),
      supportsInsurance: z.boolean(),
      trackingUrlTemplate: z.string().optional(),
    }

    // Add credential fields
    schema.fields.forEach(field => {
      let fieldSchema: z.ZodTypeAny = z.string()

      if (field.type === 'url') {
        fieldSchema = z.string().url(t('validation.invalidFormat')).or(z.literal(''))
      } else if (field.type === 'number') {
        fieldSchema = z.string().regex(/^\d*$/, t('validation.invalidFormat')).or(z.literal(''))
      } else if (field.type === 'select' && field.options && field.options.length > 0) {
        const allowedValues = field.options.map(opt => opt.value) as [string, ...string[]]
        fieldSchema = z.enum(allowedValues).or(z.literal(''))
      }

      if (field.required && !isEditing) {
        fieldSchema = fieldSchema.refine((val: unknown) => typeof val === 'string' && val.length > 0, {
          message: t('validation.fieldRequired', { field: field.label }),
        })
      } else {
        fieldSchema = fieldSchema.optional()
      }

      schemaFields[`credential_${field.key}`] = fieldSchema
    })

    return z.object(schemaFields)
  }, [schema.fields, isEditing, t])

  // Build default values
  const buildDefaultValues = (): FieldValues => {
    const defaults: FieldValues = {
      displayName: provider?.displayName ?? schema.displayName,
      environment: provider?.environment ?? 'Sandbox',
      supportsCod: provider?.supportsCod ?? schema.supportsCod,
      supportsInsurance: provider?.supportsInsurance ?? schema.supportsInsurance,
      trackingUrlTemplate: provider?.trackingUrlTemplate ?? schema.defaultTrackingUrlTemplate ?? '',
    }

    schema.fields.forEach(field => {
      defaults[`credential_${field.key}`] = field.default ?? ''
    })

    return defaults
  }

  const form = useForm<FieldValues>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(formSchema) as unknown as Resolver<FieldValues>,
    mode: 'onBlur',
    defaultValues: buildDefaultValues(),
  })

  // Reset form when dialog opens/closes or provider changes
  useEffect(() => {
    if (open) {
      form.reset(buildDefaultValues())
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, provider?.id])

  const environment = form.watch('environment') as string

  const handleEnvironmentChange = (value: string) => {
    if (value === 'Production' && environment === 'Sandbox') {
      setPendingEnvironment(value as GatewayEnvironment)
      setShowProductionWarning(true)
    } else {
      form.setValue('environment', value)
    }
  }

  const confirmProductionSwitch = () => {
    if (pendingEnvironment) {
      form.setValue('environment', pendingEnvironment)
    }
    setShowProductionWarning(false)
    setPendingEnvironment(null)
  }

  const onSubmit = async (values: FieldValues) => {
    setLoading(true)

    try {
      // Extract credentials from form values
      const credentials: Record<string, string> = {}
      schema.fields.forEach(field => {
        const value = values[`credential_${field.key}`] as string | undefined
        if (value && value.length > 0) {
          credentials[field.key] = value
        }
      })

      let result: { success: boolean; error?: string }

      if (isEditing) {
        const updateRequest: UpdateShippingProviderRequest = {
          displayName: values.displayName as string,
          environment: values.environment as GatewayEnvironment,
          supportsCod: values.supportsCod as boolean,
          supportsInsurance: values.supportsInsurance as boolean,
          trackingUrlTemplate: (values.trackingUrlTemplate as string) || null,
        }

        if (Object.keys(credentials).length > 0) {
          updateRequest.credentials = credentials
        }

        result = await onUpdate(provider.id, updateRequest)
      } else {
        const configureRequest: ConfigureShippingProviderRequest = {
          providerCode: schema.providerCode,
          displayName: values.displayName as string,
          environment: values.environment as GatewayEnvironment,
          credentials,
          supportedServices: [],
          sortOrder: 0,
          isActive: false,
          supportsCod: values.supportsCod as boolean,
          supportsInsurance: values.supportsInsurance as boolean,
          trackingUrlTemplate: (values.trackingUrlTemplate as string) || null,
        }

        result = await onConfigure(configureRequest)
      }

      if (result.success) {
        toast.success(
          isEditing
            ? t('shippingProviders.updateSuccess', 'Provider updated successfully')
            : t('shippingProviders.configureSuccess', 'Provider configured successfully')
        )
        onOpenChange(false)
      } else {
        toast.error(result.error ?? t('errors.operationFailed'))
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-[550px]">
          <DialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 bg-primary/10 rounded-lg">
                <Truck className="h-5 w-5 text-primary" />
              </div>
              <div>
                <DialogTitle>
                  {isEditing
                    ? t('shippingProviders.editTitle', 'Edit {{name}}', { name: schema.displayName })
                    : t('shippingProviders.configureTitle', 'Configure {{name}}', { name: schema.displayName })}
                </DialogTitle>
                <DialogDescription>
                  {isEditing
                    ? t('shippingProviders.editDescription', 'Update your provider credentials')
                    : t('shippingProviders.configureDescription', 'Enter credentials to enable shipping')}
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          {/* Documentation Link */}
          {schema.documentationUrl && (
            <a
              href={schema.documentationUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-2 text-sm text-muted-foreground hover:text-primary transition-colors"
            >
              <ExternalLink className="h-4 w-4" />
              {t('shippingProviders.viewDocs', 'View documentation')}
            </a>
          )}

          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              {/* Display Name */}
              <FormField
                control={form.control}
                name="displayName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('shippingProviders.fields.displayName', 'Display Name')}</FormLabel>
                    <FormControl>
                      <Input placeholder={schema.displayName} {...field} value={field.value ?? ''} />
                    </FormControl>
                    <FormDescription>
                      {t('shippingProviders.fields.displayNameHelp', 'Shown to customers during checkout')}
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Environment */}
              <FormField
                control={form.control}
                name="environment"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('shippingProviders.fields.environment', 'Environment')}</FormLabel>
                    <Select
                      onValueChange={handleEnvironmentChange}
                      value={field.value as string}
                    >
                      <FormControl>
                        <SelectTrigger className="cursor-pointer" aria-label={t('shippingProviders.fields.environment', 'Environment')}>
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="Sandbox" className="cursor-pointer">
                          {t('shipping.environment.sandbox')}
                        </SelectItem>
                        <SelectItem value="Production" className="cursor-pointer">
                          {t('shipping.environment.production')}
                        </SelectItem>
                      </SelectContent>
                    </Select>
                    {environment === 'Production' && (
                      <p className="text-sm text-yellow-600 flex items-center gap-1">
                        <AlertTriangle className="h-3 w-3" />
                        {t('shippingProviders.productionWarning', 'Real shipments will be processed')}
                      </p>
                    )}
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Credentials Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2">
                  <Separator className="flex-1" />
                  <span className="text-xs text-muted-foreground uppercase tracking-wider">
                    {t('shippingProviders.credentials', 'Credentials')}
                  </span>
                  <Separator className="flex-1" />
                </div>

                {schema.fields.map(credField => (
                  <FormField
                    key={credField.key}
                    control={form.control}
                    name={`credential_${credField.key}`}
                    render={({ field: formField }) => (
                      <FormItem>
                        <FormLabel>
                          {credField.label}
                          {credField.required && !isEditing && (
                            <span className="text-destructive ml-1">*</span>
                          )}
                        </FormLabel>
                        <FormControl>
                          {credField.type === 'select' && credField.options && credField.options.length > 0 ? (
                            <Combobox
                              options={credField.options}
                              value={(formField.value as string) ?? ''}
                              onValueChange={formField.onChange}
                              placeholder={credField.placeholder ?? t('labels.selectField', { field: credField.label.toLowerCase() })}
                              searchPlaceholder={t('labels.searchField', { field: credField.label.toLowerCase() })}
                              emptyText={t('labels.noFieldFound', { field: credField.label.toLowerCase() })}
                              countLabel={credField.label.toLowerCase()}
                            />
                          ) : (
                            <Input
                              type={credField.type === 'password' ? 'password' : 'text'}
                              placeholder={
                                isEditing && credField.type === 'password'
                                  ? '••••••••••••'
                                  : credField.placeholder
                              }
                              {...formField}
                              value={(formField.value as string) ?? ''}
                            />
                          )}
                        </FormControl>
                        {credField.helpText && (
                          <FormDescription>{credField.helpText}</FormDescription>
                        )}
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                ))}

                {/* Security Note */}
                <p className="text-xs text-muted-foreground flex items-center gap-1">
                  <Lock className="h-3 w-3" />
                  {t('shippingProviders.encryptedNote', 'Credentials are encrypted at rest')}
                </p>
              </div>

              {/* Shipping Options Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2">
                  <Separator className="flex-1" />
                  <span className="text-xs text-muted-foreground uppercase tracking-wider">
                    {t('shippingProviders.options', 'Options')}
                  </span>
                  <Separator className="flex-1" />
                </div>

                {/* COD Toggle */}
                <FormField
                  control={form.control}
                  name="supportsCod"
                  render={({ field }) => (
                    <FormItem className="flex items-center justify-between rounded-lg border p-3">
                      <div className="space-y-0.5">
                        <FormLabel>{t('shippingProviders.codLabel', 'Cash on Delivery')}</FormLabel>
                        <FormDescription>
                          {t('shippingProviders.codHint', 'Allow COD payments for this provider')}
                        </FormDescription>
                      </div>
                      <FormControl>
                        <Switch
                          checked={field.value as boolean}
                          onCheckedChange={field.onChange}
                          className="cursor-pointer"
                        />
                      </FormControl>
                    </FormItem>
                  )}
                />

                {/* Insurance Toggle */}
                <FormField
                  control={form.control}
                  name="supportsInsurance"
                  render={({ field }) => (
                    <FormItem className="flex items-center justify-between rounded-lg border p-3">
                      <div className="space-y-0.5">
                        <FormLabel>{t('shippingProviders.insuranceLabel', 'Insurance')}</FormLabel>
                        <FormDescription>
                          {t('shippingProviders.insuranceHint', 'Offer package insurance option')}
                        </FormDescription>
                      </div>
                      <FormControl>
                        <Switch
                          checked={field.value as boolean}
                          onCheckedChange={field.onChange}
                          className="cursor-pointer"
                        />
                      </FormControl>
                    </FormItem>
                  )}
                />

                {/* Tracking URL Template */}
                <FormField
                  control={form.control}
                  name="trackingUrlTemplate"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('shippingProviders.trackingUrlTemplate', 'Tracking URL Template')}</FormLabel>
                      <FormControl>
                        <Input
                          placeholder="https://tracking.example.com/{trackingNumber}"
                          {...field}
                          value={field.value ?? ''}
                        />
                      </FormControl>
                      <FormDescription>
                        {t('shippingProviders.trackingUrlHint', 'Use {trackingNumber} as placeholder')}
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => onOpenChange(false)}
                  className="cursor-pointer"
                >
                  {t('buttons.cancel', 'Cancel')}
                </Button>
                <Button type="submit" disabled={loading} className="cursor-pointer">
                  {loading ? (
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  ) : null}
                  {isEditing
                    ? t('buttons.save', 'Save')
                    : t('buttons.configure', 'Configure')}
                </Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>

      {/* Production Warning Dialog */}
      <AlertDialog open={showProductionWarning} onOpenChange={setShowProductionWarning}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-yellow-500" />
              {t('shippingProviders.productionWarningTitle', 'Switch to Production Mode')}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {t(
                'shippingProviders.productionWarningMessage',
                'You are about to switch to production mode. Real shipments will be created. Make sure your credentials are for the production environment.'
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">
              {t('buttons.cancel', 'Cancel')}
            </AlertDialogCancel>
            <AlertDialogAction onClick={confirmProductionSwitch} className="cursor-pointer">
              {t('shippingProviders.confirmSwitch', 'Confirm Switch')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
