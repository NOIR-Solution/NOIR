import { useState, useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type FieldValues } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import {
  CreditCard,
  Lock,
  AlertTriangle,
  Loader2,
  CheckCircle2,
  XCircle,
  Plug,
  ExternalLink,
} from 'lucide-react'
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
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { Combobox } from '@/components/ui/combobox'
import { toast } from 'sonner'
import type {
  PaymentGateway,
  GatewaySchema,
  GatewayEnvironment,
  ConfigureGatewayRequest,
  UpdateGatewayRequest,
  TestConnectionResult,
} from '@/types'

interface ConfigureGatewayDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  gateway: PaymentGateway | null
  schema: GatewaySchema
  onConfigure: (request: ConfigureGatewayRequest) => Promise<{ success: boolean; error?: string }>
  onUpdate: (id: string, request: UpdateGatewayRequest) => Promise<{ success: boolean; error?: string }>
  onTestConnection: (id: string) => Promise<TestConnectionResult>
}

export function ConfigureGatewayDialog({
  open,
  onOpenChange,
  gateway,
  schema,
  onConfigure,
  onUpdate,
  onTestConnection,
}: ConfigureGatewayDialogProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const [testResult, setTestResult] = useState<TestConnectionResult | null>(null)
  const [isTesting, setIsTesting] = useState(false)
  const [showProductionWarning, setShowProductionWarning] = useState(false)
  const [pendingEnvironment, setPendingEnvironment] = useState<GatewayEnvironment | null>(null)

  const isEditing = !!gateway

  // Build dynamic schema based on provider fields - memoized to prevent rebuilds
  const formSchema = useMemo(() => {
    const schemaFields: Record<string, z.ZodTypeAny> = {
      displayName: z.string().min(1, 'Display name is required').max(100),
      environment: z.enum(['Sandbox', 'Production']),
    }

    // Add credential fields
    schema.fields.forEach(field => {
      let fieldSchema: z.ZodTypeAny = z.string()

      if (field.type === 'url') {
        fieldSchema = z.string().url('Must be a valid URL').or(z.literal(''))
      } else if (field.type === 'number') {
        fieldSchema = z.string().regex(/^\d*$/, 'Must be a number').or(z.literal(''))
      } else if (field.type === 'select' && field.options && field.options.length > 0) {
        // Validate select fields - only allow values from options list
        const allowedValues = field.options.map(opt => opt.value) as [string, ...string[]]
        fieldSchema = z.enum(allowedValues, {
          errorMap: () => ({ message: `Please select a valid ${field.label.toLowerCase()}` })
        }).or(z.literal('')) // Allow empty string for optional fields
      }

      if (field.required && !isEditing) {
        fieldSchema = fieldSchema.refine((val: unknown) => typeof val === 'string' && val.length > 0, {
          message: `${field.label} is required`,
        })
      } else {
        fieldSchema = fieldSchema.optional()
      }

      schemaFields[`credential_${field.key}`] = fieldSchema
    })

    return z.object(schemaFields)
  }, [schema.fields, isEditing])

  // Build default values
  const buildDefaultValues = (): FieldValues => {
    const defaults: FieldValues = {
      displayName: gateway?.displayName ?? schema.displayName,
      environment: gateway?.environment ?? 'Sandbox',
    }

    schema.fields.forEach(field => {
      defaults[`credential_${field.key}`] = field.default ?? ''
    })

    return defaults
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const form = useForm<FieldValues>({
    resolver: zodResolver(formSchema) as any,
    defaultValues: buildDefaultValues(),
  })

  // Reset form when dialog opens/closes or gateway changes
  useEffect(() => {
    if (open) {
      form.reset(buildDefaultValues())
      setTestResult(null)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, gateway?.id])

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

  const handleTestConnection = async () => {
    if (!gateway) return

    setIsTesting(true)
    setTestResult(null)

    try {
      const result = await onTestConnection(gateway.id)
      setTestResult(result)

      if (result.success) {
        toast.success(result.message)
      } else {
        toast.error(result.message)
      }
    } finally {
      setIsTesting(false)
    }
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
        // Only include credentials if any were provided
        const updateRequest: UpdateGatewayRequest = {
          displayName: values.displayName as string,
          environment: values.environment as GatewayEnvironment,
        }

        if (Object.keys(credentials).length > 0) {
          updateRequest.credentials = credentials
        }

        result = await onUpdate(gateway.id, updateRequest)
      } else {
        const configureRequest: ConfigureGatewayRequest = {
          provider: schema.provider,
          displayName: values.displayName as string,
          environment: values.environment as GatewayEnvironment,
          credentials,
          supportedMethods: [], // Will be determined by provider
          sortOrder: 0,
          isActive: false,
        }

        result = await onConfigure(configureRequest)
      }

      if (result.success) {
        toast.success(
          isEditing
            ? t('paymentGateways.updateSuccess', 'Gateway updated successfully')
            : t('paymentGateways.configureSuccess', 'Gateway configured successfully')
        )
        onOpenChange(false)
      } else {
        toast.error(result.error ?? 'Operation failed')
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
                <CreditCard className="h-5 w-5 text-primary" />
              </div>
              <div>
                <DialogTitle>
                  {isEditing
                    ? t('paymentGateways.editTitle', 'Edit {{name}}', { name: schema.displayName })
                    : t('paymentGateways.configureTitle', 'Configure {{name}}', { name: schema.displayName })}
                </DialogTitle>
                <DialogDescription>
                  {isEditing
                    ? t('paymentGateways.editDescription', 'Update your merchant credentials')
                    : t('paymentGateways.configureDescription', 'Enter your merchant credentials to enable payments')}
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
              {t('paymentGateways.viewDocs', 'View documentation')}
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
                    <FormLabel>{t('paymentGateways.fields.displayName', 'Display Name')}</FormLabel>
                    <FormControl>
                      <Input placeholder={schema.displayName} {...field} value={field.value ?? ''} />
                    </FormControl>
                    <FormDescription>
                      {t('paymentGateways.fields.displayNameHelp', 'Shown to customers during checkout')}
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
                    <FormLabel>{t('paymentGateways.fields.environment', 'Environment')}</FormLabel>
                    <Select
                      onValueChange={handleEnvironmentChange}
                      value={field.value as string}
                    >
                      <FormControl>
                        <SelectTrigger className="cursor-pointer">
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="Sandbox" className="cursor-pointer">
                          Sandbox (Testing)
                        </SelectItem>
                        <SelectItem value="Production" className="cursor-pointer">
                          Production (Live)
                        </SelectItem>
                      </SelectContent>
                    </Select>
                    {environment === 'Production' && (
                      <p className="text-sm text-yellow-600 flex items-center gap-1">
                        <AlertTriangle className="h-3 w-3" />
                        {t('paymentGateways.productionWarning', 'Real transactions will be processed')}
                      </p>
                    )}
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Credentials/Settings Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2">
                  <Separator className="flex-1" />
                  <span className="text-xs text-muted-foreground uppercase tracking-wider">
                    {schema.supportsCod
                      ? t('paymentGateways.settings', 'Settings')
                      : t('paymentGateways.credentials', 'Credentials')}
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
                              placeholder={credField.placeholder ?? `Select ${credField.label.toLowerCase()}`}
                              searchPlaceholder={`Search ${credField.label.toLowerCase()}...`}
                              emptyText={`No ${credField.label.toLowerCase()} found.`}
                              countLabel={credField.label.toLowerCase() === 'bank' ? 'banks' : 'options'}
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
                  {t('paymentGateways.encryptedNote', 'Credentials are encrypted at rest')}
                </p>
              </div>

              {/* Test Connection */}
              {isEditing && (
                <div className="space-y-2">
                  <Button
                    type="button"
                    variant="outline"
                    className="w-full cursor-pointer"
                    onClick={handleTestConnection}
                    disabled={isTesting}
                  >
                    {isTesting ? (
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    ) : (
                      <Plug className="h-4 w-4 mr-2" />
                    )}
                    {t('paymentGateways.testConnection', 'Test Connection')}
                  </Button>

                  {testResult && (
                    <div
                      className={`text-sm flex items-center gap-2 p-2 rounded ${
                        testResult.success
                          ? 'text-green-600 bg-green-50'
                          : 'text-red-600 bg-red-50'
                      }`}
                    >
                      {testResult.success ? (
                        <CheckCircle2 className="h-4 w-4" />
                      ) : (
                        <XCircle className="h-4 w-4" />
                      )}
                      {testResult.message}
                      {testResult.responseTimeMs && (
                        <span className="text-xs text-muted-foreground ml-auto">
                          {testResult.responseTimeMs}ms
                        </span>
                      )}
                    </div>
                  )}
                </div>
              )}

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
              {t('paymentGateways.productionWarningTitle', 'Switch to Production Mode')}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {t(
                'paymentGateways.productionWarningMessage',
                'You are about to switch to production mode. Real transactions will be processed. Make sure your credentials are for the production environment.'
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">
              {t('buttons.cancel', 'Cancel')}
            </AlertDialogCancel>
            <AlertDialogAction onClick={confirmProductionSwitch} className="cursor-pointer">
              {t('paymentGateways.confirmSwitch', 'Confirm Switch')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
