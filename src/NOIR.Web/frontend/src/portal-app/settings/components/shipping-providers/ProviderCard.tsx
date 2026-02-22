import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Truck,
  Settings,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Clock,
  Loader2,
  ExternalLink,
} from 'lucide-react'
import { Badge, Button, Card, CardContent, CardHeader, Switch } from '@uikit'

import { cn } from '@/lib/utils'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import type { ShippingProviderDto, ShippingProviderHealthStatus, ShippingProviderSchema } from '@/types/shipping'
import { formatLastHealthCheck } from '@/services/paymentGateways'

interface ProviderCardProps {
  provider: ShippingProviderDto | null
  schema: ShippingProviderSchema
  onConfigure: () => void
  onToggle: (isActive: boolean) => Promise<void>
}

const healthStatusConfig: Record<ShippingProviderHealthStatus, {
  icon: React.ElementType
  color: string
  labelKey: string
}> = {
  Healthy: { icon: CheckCircle2, color: 'text-green-500', labelKey: 'shipping.healthStatus.healthy' },
  Degraded: { icon: AlertCircle, color: 'text-yellow-500', labelKey: 'shipping.healthStatus.degraded' },
  Unhealthy: { icon: XCircle, color: 'text-red-500', labelKey: 'shipping.healthStatus.unhealthy' },
  Unknown: { icon: Clock, color: 'text-muted-foreground', labelKey: 'shipping.healthStatus.unknown' },
}

export const ProviderCard = ({
  provider,
  schema,
  onConfigure,
  onToggle,
}: ProviderCardProps) => {
  const { t } = useTranslation('common')
  const [isToggling, setIsToggling] = useState(false)

  const isConfigured = provider?.hasCredentials ?? false
  const isActive = provider?.isActive ?? false
  const healthStatus = provider?.healthStatus ?? 'Unknown'
  const HealthIcon = healthStatusConfig[healthStatus].icon

  const handleToggle = async (checked: boolean) => {
    setIsToggling(true)
    try {
      await onToggle(checked)
    } finally {
      setIsToggling(false)
    }
  }

  const getStatusBadge = () => {
    if (!isConfigured) {
      return (
        <Badge variant="outline" className={getStatusBadgeClasses('gray')}>
          {t('shippingProviders.status.notConfigured', 'Not Configured')}
        </Badge>
      )
    }
    if (isActive) {
      return (
        <Badge variant="outline" className={getStatusBadgeClasses('green')}>
          {t('shippingProviders.status.active', 'Active')}
        </Badge>
      )
    }
    return (
      <Badge variant="outline" className={getStatusBadgeClasses('blue')}>
        {t('shippingProviders.status.configured', 'Configured')}
      </Badge>
    )
  }

  return (
    <Card
      className={cn(
        'transition-all duration-200',
        isActive && 'ring-2 ring-green-500/50 shadow-green-500/10 shadow-lg',
        !isConfigured && 'opacity-75'
      )}
    >
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div className={cn(
              'p-2 rounded-lg flex items-center justify-center',
              isActive ? 'bg-green-500/10' : 'bg-muted'
            )}>
              {schema.iconUrl ? (
                <img
                  src={schema.iconUrl}
                  alt={schema.displayName}
                  className="h-8 w-8 rounded object-contain"
                  onError={(e) => {
                    e.currentTarget.style.display = 'none'
                    e.currentTarget.nextElementSibling?.classList.remove('hidden')
                  }}
                />
              ) : null}
              <Truck className={cn(
                'h-6 w-6',
                isActive ? 'text-green-500' : 'text-muted-foreground',
                schema.iconUrl ? 'hidden' : ''
              )} />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="font-semibold text-lg">{schema.displayName}</h3>
                {schema.documentationUrl && (
                  <a
                    href={schema.documentationUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-muted-foreground hover:text-primary transition-colors"
                    title={t('shippingProviders.viewDocs', 'View documentation')}
                  >
                    <ExternalLink className="h-4 w-4" />
                  </a>
                )}
              </div>
              <p className="text-sm text-muted-foreground line-clamp-1">
                {schema.description}
              </p>
            </div>
          </div>
          {isConfigured && (
            <div className="flex items-center gap-1.5">
              <HealthIcon className={cn('h-4 w-4', healthStatusConfig[healthStatus].color)} />
              <span className={cn('text-xs', healthStatusConfig[healthStatus].color)}>
                {t(healthStatusConfig[healthStatus].labelKey)}
              </span>
            </div>
          )}
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Status Row */}
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            {getStatusBadge()}
            {provider?.environment && (
              <p className="text-xs text-muted-foreground mt-1">
                {provider.environment === 'Sandbox' ? '🧪 ' : '🚀 '}
                {t(`shippingProviders.${provider.environment.toLowerCase()}`, provider.environment)}
              </p>
            )}
          </div>
          <div className="flex flex-col items-end gap-1">
            {isConfigured && (
              <p className="text-xs text-muted-foreground">
                {t('shippingProviders.lastCheck', 'Last check')}: {formatLastHealthCheck(provider?.lastHealthCheck ?? null, t)}
              </p>
            )}
            {/* Feature badges */}
            <div className="flex items-center gap-1.5">
              {(provider?.supportsCod ?? schema.supportsCod) && (
                <Badge variant="outline" className={`${getStatusBadgeClasses('green')} text-xs`}>
                  {t('shippingProviders.cod', 'COD')}
                </Badge>
              )}
              {(provider?.supportsInsurance ?? schema.supportsInsurance) && (
                <Badge variant="outline" className={`${getStatusBadgeClasses('blue')} text-xs`}>
                  {t('shippingProviders.insurance', 'Insurance')}
                </Badge>
              )}
            </div>
          </div>
        </div>

        {/* Actions Row */}
        <div className="flex items-center gap-2 pt-2 border-t">
          {/* Enable/Disable Toggle */}
          <div className="flex items-center gap-2 flex-1">
            <Switch
              checked={isActive}
              onCheckedChange={handleToggle}
              disabled={!isConfigured || isToggling}
              className="cursor-pointer"
            />
            <span className="text-sm text-muted-foreground">
              {isActive
                ? t('shippingProviders.enabled', 'Enabled')
                : t('shippingProviders.disabled', 'Disabled')}
            </span>
            {isToggling && <Loader2 className="h-3 w-3 animate-spin" />}
          </div>

          {/* Configure Button */}
          <Button
            variant={isConfigured ? 'outline' : 'default'}
            size="sm"
            onClick={onConfigure}
            className="cursor-pointer"
          >
            <Settings className="h-4 w-4 mr-1" />
            {isConfigured
              ? t('buttons.edit', 'Edit')
              : t('buttons.configure', 'Configure')}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
