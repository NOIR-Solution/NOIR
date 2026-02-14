import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { RefreshCw, Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { usePaymentGateways } from '../../states/usePaymentGateways'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Skeleton } from '@uikit'

import { toast } from 'sonner'
import { GatewayCard } from '../payment-gateways/GatewayCard'
import { ConfigureGatewayDialog } from '../payment-gateways/ConfigureGatewayDialog'
import type { PaymentGateway } from '@/types'

export function PaymentGatewaysTab() {
  const { t } = useTranslation('common')

  const {
    schemas,
    loading,
    error,
    refresh,
    configure,
    update,
    toggleActive,
    testConnection,
    getGatewayByProvider,
    availableProviders,
  } = usePaymentGateways()

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
    const result = await toggleActive(gateway.id, isActive)
    if (result.success) {
      toast.success(
        isActive
          ? t('paymentGateways.enabled', 'Gateway enabled')
          : t('paymentGateways.disabled', 'Gateway disabled')
      )
    } else {
      toast.error(result.error ?? 'Failed to toggle gateway')
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
        <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-64" />
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2">
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
        <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
          <CardTitle>{t('paymentGateways.title', 'Payment Gateways')}</CardTitle>
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
      <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
        <div className="flex items-center justify-between">
          <div>
            <CardTitle>{t('paymentGateways.title', 'Payment Gateways')}</CardTitle>
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
        <div className="grid gap-4 md:grid-cols-2">
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
