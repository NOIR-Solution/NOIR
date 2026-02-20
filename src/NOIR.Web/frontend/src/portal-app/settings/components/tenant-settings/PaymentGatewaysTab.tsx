import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { RefreshCw, Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import {
  usePaymentGatewaysListQuery,
  useGatewaySchemasQuery,
  useConfigureGatewayMutation,
  useUpdateGatewayMutation,
  useToggleGatewayActiveMutation,
  useTestGatewayConnectionMutation,
} from '@/portal-app/settings/queries'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Skeleton } from '@uikit'

import { toast } from 'sonner'
import { GatewayCard } from '../payment-gateways/GatewayCard'
import { ConfigureGatewayDialog } from '../payment-gateways/ConfigureGatewayDialog'
import type { PaymentGateway } from '@/types'

export const PaymentGatewaysTab = () => {
  const { t } = useTranslation('common')

  const { data: gateways = [], isLoading: gatewaysLoading, error: gatewaysError, refetch: refreshGateways } = usePaymentGatewaysListQuery()
  const { data: schemas, isLoading: schemasLoading, error: schemasError, refetch: refreshSchemas } = useGatewaySchemasQuery()
  const configureMutation = useConfigureGatewayMutation()
  const updateMutation = useUpdateGatewayMutation()
  const toggleActiveMutation = useToggleGatewayActiveMutation()
  const testConnectionMutation = useTestGatewayConnectionMutation()

  const loading = gatewaysLoading || schemasLoading
  const error = gatewaysError?.message ?? schemasError?.message ?? null

  const getGatewayByProvider = (provider: string) => gateways.find((g) => g.provider === provider)
  const availableProviders = schemas ? Object.keys(schemas.schemas) : []

  const refresh = async () => {
    await Promise.all([refreshGateways(), refreshSchemas()])
  }

  // Wrapper functions that maintain { success, error } return pattern for ConfigureGatewayDialog
  const configure = async (request: import('@/types').ConfigureGatewayRequest): Promise<{ success: boolean; error?: string }> => {
    try {
      await configureMutation.mutateAsync(request)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to configure gateway'
      return { success: false, error: message }
    }
  }

  const update = async (id: string, request: import('@/types').UpdateGatewayRequest): Promise<{ success: boolean; error?: string }> => {
    try {
      await updateMutation.mutateAsync({ id, request })
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to update gateway'
      return { success: false, error: message }
    }
  }

  const testConnection = async (id: string) => {
    return testConnectionMutation.mutateAsync(id)
  }

  const [configureDialogOpen, setConfigureDialogOpen] = useState(false)
  const [selectedProvider, setSelectedProvider] = useState<string | null>(null)
  const [testingGatewayId, setTestingGatewayId] = useState<string | null>(null)
  const [isRefreshing, setIsRefreshing] = useState(false)

  const selectedSchema = selectedProvider ? schemas?.schemas[selectedProvider] : null
  const selectedGateway = selectedProvider ? getGatewayByProvider(selectedProvider) : null

  const handleConfigure = (provider: string) => {
    setSelectedProvider(provider)
    setConfigureDialogOpen(true)
  }

  const handleToggleActive = async (gateway: PaymentGateway, isActive: boolean) => {
    try {
      await toggleActiveMutation.mutateAsync({ id: gateway.id, isActive })
      toast.success(
        isActive
          ? t('paymentGateways.enabled', 'Gateway enabled')
          : t('paymentGateways.disabled', 'Gateway disabled')
      )
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to toggle gateway')
    }
  }

  const handleTestConnection = async (gatewayId: string) => {
    setTestingGatewayId(gatewayId)
    try {
      const result = await testConnection(gatewayId)
      if (result.success) {
        toast.success(result.message)
      } else {
        toast.error(result.message)
      }
    } finally {
      setTestingGatewayId(null)
    }
  }

  const handleRefresh = async () => {
    setIsRefreshing(true)
    try {
      await refresh()
      toast.success(t('paymentGateways.refreshed', 'Gateway list refreshed'))
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
          <CardTitle className="text-lg">{t('paymentGateways.title', 'Payment Gateways')}</CardTitle>
          <CardDescription>
            {t('paymentGateways.description', 'Configure payment methods for your store')}
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
            <CardTitle className="text-lg">{t('paymentGateways.title', 'Payment Gateways')}</CardTitle>
            <CardDescription>
              {t('paymentGateways.description', 'Configure payment methods for your store')}
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
        {/* Gateway Cards Grid */}
        <div className="grid gap-6 sm:grid-cols-2">
          {availableProviders.map(provider => {
            const schema = schemas?.schemas[provider]
            const gateway = getGatewayByProvider(provider)

            if (!schema) return null

            return (
              <GatewayCard
                key={provider}
                gateway={gateway ?? null}
                schema={schema}
                onConfigure={() => handleConfigure(provider)}
                onToggleActive={async (isActive) => {
                  if (gateway) {
                    await handleToggleActive(gateway, isActive)
                  }
                }}
                onTestConnection={async () => {
                  if (gateway) {
                    await handleTestConnection(gateway.id)
                  }
                }}
                isTestingConnection={gateway?.id === testingGatewayId}
              />
            )
          })}
        </div>

        {/* Configure Dialog */}
        {selectedSchema && (
          <ConfigureGatewayDialog
            open={configureDialogOpen}
            onOpenChange={setConfigureDialogOpen}
            gateway={selectedGateway ?? null}
            schema={selectedSchema}
            onConfigure={configure}
            onUpdate={update}
            onTestConnection={testConnection}
          />
        )}
      </CardContent>
    </Card>
  )
}
