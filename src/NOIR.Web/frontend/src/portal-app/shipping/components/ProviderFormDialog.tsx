import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, Truck } from 'lucide-react'
import { toast } from 'sonner'
import {
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
  Switch,
  Button,
} from '@uikit'
import type {
  ShippingProviderDto,
  ShippingProviderCode,
  GatewayEnvironment,
} from '@/types/shipping'
import {
  useConfigureProviderMutation,
  useUpdateProviderMutation,
} from '@/portal-app/shipping/queries'

const PROVIDER_CODES: ShippingProviderCode[] = [
  'GHTK',
  'GHN',
  'JTExpress',
  'ViettelPost',
  'NinjaVan',
  'VNPost',
  'BestExpress',
  'Custom',
]

const PROVIDER_NAMES: Record<ShippingProviderCode, string> = {
  GHTK: 'Giao Hang Tiet Kiem',
  GHN: 'Giao Hang Nhanh',
  JTExpress: 'J&T Express Vietnam',
  ViettelPost: 'Viettel Post',
  NinjaVan: 'Ninja Van Vietnam',
  VNPost: 'Vietnam Post',
  BestExpress: 'Best Express Vietnam',
  Custom: 'Custom Provider',
}

const createFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    providerCode: z.string().min(1, t('validation.required')),
    displayName: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { max: 100 })),
    environment: z.string().min(1, t('validation.required')),
    isActive: z.boolean().default(true),
    supportsCod: z.boolean().default(true),
    supportsInsurance: z.boolean().default(false),
    apiBaseUrl: z.string().optional().or(z.literal('')),
    trackingUrlTemplate: z.string().optional().or(z.literal('')),
    sortOrder: z.coerce.number().int().min(0).default(0),
  })

type FormData = z.infer<ReturnType<typeof createFormSchema>>

interface ProviderFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  provider?: ShippingProviderDto | null
}

export const ProviderFormDialog = ({ open, onOpenChange, provider }: ProviderFormDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!provider
  const configureMutation = useConfigureProviderMutation()
  const updateMutation = useUpdateProviderMutation()

  const schema = createFormSchema(t)

  const form = useForm<FormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(schema) as unknown as Resolver<FormData>,
    mode: 'onBlur',
    defaultValues: {
      providerCode: '',
      displayName: '',
      environment: 'Sandbox',
      isActive: true,
      supportsCod: true,
      supportsInsurance: false,
      apiBaseUrl: '',
      trackingUrlTemplate: '',
      sortOrder: 0,
    },
  })

  useEffect(() => {
    if (open && provider) {
      form.reset({
        providerCode: provider.providerCode,
        displayName: provider.displayName,
        environment: provider.environment,
        isActive: provider.isActive,
        supportsCod: provider.supportsCod,
        supportsInsurance: provider.supportsInsurance,
        apiBaseUrl: provider.apiBaseUrl ?? '',
        trackingUrlTemplate: provider.trackingUrlTemplate ?? '',
        sortOrder: provider.sortOrder,
      })
    } else if (open) {
      form.reset({
        providerCode: '',
        displayName: '',
        environment: 'Sandbox',
        isActive: true,
        supportsCod: true,
        supportsInsurance: false,
        apiBaseUrl: '',
        trackingUrlTemplate: '',
        sortOrder: 0,
      })
    }
  }, [open, provider, form])

  const onSubmit = async (data: FormData) => {
    try {
      if (isEditing) {
        await updateMutation.mutateAsync({
          id: provider!.id,
          request: {
            displayName: data.displayName,
            environment: data.environment as GatewayEnvironment,
            isActive: data.isActive,
            supportsCod: data.supportsCod,
            supportsInsurance: data.supportsInsurance,
            apiBaseUrl: data.apiBaseUrl || null,
            trackingUrlTemplate: data.trackingUrlTemplate || null,
            sortOrder: data.sortOrder,
          },
        })
        toast.success(t('shipping.providerUpdated', { name: data.displayName }))
      } else {
        await configureMutation.mutateAsync({
          providerCode: data.providerCode as ShippingProviderCode,
          displayName: data.displayName,
          environment: data.environment as GatewayEnvironment,
          credentials: {},
          supportedServices: [],
          sortOrder: data.sortOrder,
          isActive: data.isActive,
          supportsCod: data.supportsCod,
          supportsInsurance: data.supportsInsurance,
          apiBaseUrl: data.apiBaseUrl || null,
          trackingUrlTemplate: data.trackingUrlTemplate || null,
        })
        toast.success(t('shipping.providerCreated', { name: data.displayName }))
      }
      onOpenChange(false)
    } catch {
      toast.error(t('shipping.providerSaveFailed', 'Failed to save provider'))
    }
  }

  const isPending = configureMutation.isPending || updateMutation.isPending

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[500px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <Truck className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>
                {isEditing
                  ? t('shipping.editProvider', 'Edit Provider')
                  : t('shipping.addProvider', 'Add Provider')}
              </CredenzaTitle>
              <CredenzaDescription>
                {isEditing
                  ? t('shipping.editProviderDescription', 'Update shipping provider configuration.')
                  : t('shipping.addProviderDescription', 'Configure a new shipping provider for your store.')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <CredenzaBody>
              {!isEditing && (
                <FormField
                  control={form.control}
                  name="providerCode"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('shipping.providerCodeLabel', 'Provider')}</FormLabel>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue placeholder={t('shipping.selectProvider', 'Select a provider')} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {PROVIDER_CODES.map((code) => (
                            <SelectItem key={code} value={code} className="cursor-pointer">
                              {PROVIDER_NAMES[code]} ({code})
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              )}

              <FormField
                control={form.control}
                name="displayName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('shipping.displayName', 'Display Name')}</FormLabel>
                    <FormControl>
                      <Input {...field} placeholder={t('shipping.displayNamePlaceholder', 'e.g., GHN Express')} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="environment"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.environment', 'Environment')}</FormLabel>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          <SelectItem value="Sandbox" className="cursor-pointer">
                            {t('shipping.env.sandbox', 'Sandbox')}
                          </SelectItem>
                          <SelectItem value="Production" className="cursor-pointer">
                            {t('shipping.env.production', 'Production')}
                          </SelectItem>
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="sortOrder"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('shipping.sortOrder', 'Sort Order')}</FormLabel>
                      <FormControl>
                        <Input type="number" min={0} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="apiBaseUrl"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('shipping.apiBaseUrl', 'API Base URL')}</FormLabel>
                    <FormControl>
                      <Input {...field} placeholder="https://..." />
                    </FormControl>
                    <FormDescription>
                      {t('shipping.apiBaseUrlHint', 'Override the default API URL for this provider.')}
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="trackingUrlTemplate"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('shipping.trackingUrlTemplate', 'Tracking URL Template')}</FormLabel>
                    <FormControl>
                      <Input {...field} placeholder="https://tracking.example.com/{trackingNumber}" />
                    </FormControl>
                    <FormDescription>
                      {t('shipping.trackingUrlHint', 'Use {trackingNumber} as placeholder.')}
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="space-y-3 rounded-lg border p-4">
                <FormField
                  control={form.control}
                  name="isActive"
                  render={({ field }) => (
                    <FormItem className="flex items-center justify-between">
                      <div>
                        <FormLabel className="mb-0">{t('labels.active', 'Active')}</FormLabel>
                        <FormDescription className="mt-0.5">
                          {t('shipping.activeHint', 'Available for checkout when active.')}
                        </FormDescription>
                      </div>
                      <FormControl>
                        <Switch checked={field.value} onCheckedChange={field.onChange} className="cursor-pointer" />
                      </FormControl>
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="supportsCod"
                  render={({ field }) => (
                    <FormItem className="flex items-center justify-between">
                      <div>
                        <FormLabel className="mb-0">{t('shipping.cod', 'Cash on Delivery')}</FormLabel>
                        <FormDescription className="mt-0.5">
                          {t('shipping.codHint', 'Allow COD payments for this provider.')}
                        </FormDescription>
                      </div>
                      <FormControl>
                        <Switch checked={field.value} onCheckedChange={field.onChange} className="cursor-pointer" />
                      </FormControl>
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="supportsInsurance"
                  render={({ field }) => (
                    <FormItem className="flex items-center justify-between">
                      <div>
                        <FormLabel className="mb-0">{t('shipping.insuranceLabel', 'Insurance')}</FormLabel>
                        <FormDescription className="mt-0.5">
                          {t('shipping.insuranceHint', 'Offer package insurance option.')}
                        </FormDescription>
                      </div>
                      <FormControl>
                        <Switch checked={field.value} onCheckedChange={field.onChange} className="cursor-pointer" />
                      </FormControl>
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
                className="cursor-pointer"
              >
                {t('buttons.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isPending} className="cursor-pointer">
                {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isPending
                  ? t('buttons.saving', 'Saving...')
                  : isEditing
                    ? t('buttons.save', 'Save')
                    : t('buttons.create', 'Create')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
