import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { RefreshCw, Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import {
  useShippingProvidersQuery,
  useShippingProviderSchemasQuery,
} from '@/portal-app/shipping/queries/useShippingQueries'
import {
  useConfigureProviderMutation,
  useUpdateProviderMutation,
  useActivateProviderMutation,
  useDeactivateProviderMutation,
} from '@/portal-app/shipping/queries/useShippingMutations'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Skeleton } from '@uikit'

import { toast } from 'sonner'
import { ProviderCard } from '../shipping-providers/ProviderCard'
import { ConfigureProviderDialog } from '../shipping-providers/ConfigureProviderDialog'
import type { ShippingProviderDto, ConfigureShippingProviderRequest, UpdateShippingProviderRequest } from '@/types/shipping'

export const ShippingProvidersTab = () => {
  const { t } = useTranslation('common')

  const { data: providers = [], isLoading: providersLoading, error: providersError, refetch: refreshProviders } = useShippingProvidersQuery()
  const { data: schemas, isLoading: schemasLoading, error: schemasError, refetch: refreshSchemas } = useShippingProviderSchemasQuery()
  const configureMutation = useConfigureProviderMutation()
  const updateMutation = useUpdateProviderMutation()
  const activateMutation = useActivateProviderMutation()
  const deactivateMutation = useDeactivateProviderMutation()

  const loading = providersLoading || schemasLoading
  const error = providersError?.message ?? schemasError?.message ?? null

  const getProviderByCode = (code: string) => providers.find((p) => p.providerCode === code)
  const availableProviders = schemas ? Object.keys(schemas.schemas) : []

  const refresh = async () => {
    await Promise.all([refreshProviders(), refreshSchemas()])
  }

  // Wrapper functions that maintain { success, error } return pattern for ConfigureProviderDialog
  const configure = async (request: ConfigureShippingProviderRequest): Promise<{ success: boolean; error?: string }> => {
    try {
      await configureMutation.mutateAsync(request)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to configure provider'
      return { success: false, error: message }
    }
  }

  const update = async (id: string, request: UpdateShippingProviderRequest): Promise<{ success: boolean; error?: string }> => {
    try {
      await updateMutation.mutateAsync({ id, request })
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to update provider'
      return { success: false, error: message }
    }
  }

  const [configureDialogOpen, setConfigureDialogOpen] = useState(false)
  const [selectedProviderCode, setSelectedProviderCode] = useState<string | null>(null)
  const [isRefreshing, setIsRefreshing] = useState(false)

  const selectedSchema = selectedProviderCode ? schemas?.schemas[selectedProviderCode] : null
  const selectedProvider = selectedProviderCode ? getProviderByCode(selectedProviderCode) : null

  const handleConfigure = (providerCode: string) => {
    setSelectedProviderCode(providerCode)
    setConfigureDialogOpen(true)
  }

  const handleToggle = async (provider: ShippingProviderDto, isActive: boolean) => {
    try {
      if (isActive) {
        await activateMutation.mutateAsync(provider.id)
      } else {
        await deactivateMutation.mutateAsync(provider.id)
      }
      toast.success(
        isActive
          ? t('shippingProviders.enabledSuccess', 'Provider enabled')
          : t('shippingProviders.disabledSuccess', 'Provider disabled')
      )
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to toggle provider')
    }
  }

  const handleRefresh = async () => {
    setIsRefreshing(true)
    try {
      await refresh()
      toast.success(t('shippingProviders.refreshed', 'Provider list refreshed'))
    } finally {
      setIsRefreshing(false)
    }
  }

  if (loading) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-64" />
        </CardHeader>
        <CardContent>
          <div className="grid gap-6 sm:grid-cols-2">
            {[1, 2, 3, 4].map(i => (
              <Skeleton key={i} className="h-48 rounded-lg" />
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <CardTitle className="text-lg">{t('shippingProviders.title', 'Shipping Providers')}</CardTitle>
          <CardDescription>
            {t('shippingProviders.description', 'Configure shipping providers for your store')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="p-6 bg-destructive/10 text-destructive rounded-lg">
            <p className="font-medium">{t('errors.loadFailed', 'Failed to load data')}</p>
            <p className="text-sm mt-1">{error}</p>
            <Button
              variant="outline"
              size="sm"
              onClick={refresh}
              className="mt-4 cursor-pointer group hover:shadow-md transition-all duration-300"
            >
              <RefreshCw className="h-4 w-4 mr-2 transition-transform duration-300 group-hover:rotate-180" />
              {t('buttons.retry', 'Retry')}
            </Button>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="text-lg">{t('shippingProviders.title', 'Shipping Providers')}</CardTitle>
            <CardDescription>
              {t('shippingProviders.description', 'Configure shipping providers for your store')}
            </CardDescription>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefresh}
            disabled={isRefreshing}
            className="cursor-pointer group hover:shadow-md transition-all duration-300"
          >
            {isRefreshing ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <RefreshCw className={cn('h-4 w-4 mr-2 transition-transform duration-300', !isRefreshing && 'group-hover:rotate-180')} />
            )}
            {t('buttons.refresh', 'Refresh')}
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {/* Provider Cards Grid */}
        <div className="grid gap-6 sm:grid-cols-2">
          {availableProviders.map(providerCode => {
            const schema = schemas?.schemas[providerCode]
            const provider = getProviderByCode(providerCode)

            if (!schema) return null

            return (
              <ProviderCard
                key={providerCode}
                provider={provider ?? null}
                schema={schema}
                onConfigure={() => handleConfigure(providerCode)}
                onToggle={async (isActive) => {
                  if (!provider) {
                    toast.error(t('shippingProviders.configureFirst', 'Configure this provider before enabling it'))
                    return
                  }
                  await handleToggle(provider, isActive)
                }}
              />
            )
          })}
        </div>

        {/* Configure Dialog */}
        {selectedSchema && (
          <ConfigureProviderDialog
            open={configureDialogOpen}
            onOpenChange={setConfigureDialogOpen}
            provider={selectedProvider ?? null}
            schema={selectedSchema}
            onConfigure={configure}
            onUpdate={update}
          />
        )}
      </CardContent>
    </Card>
  )
}
