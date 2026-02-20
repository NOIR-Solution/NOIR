import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  CreditCard,
  Settings,
  Plug,
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
import type { PaymentGateway, GatewaySchema, GatewayHealthStatus } from '@/types'
import { formatLastHealthCheck } from '@/services/paymentGateways'

interface GatewayCardProps {
  gateway: PaymentGateway | null
  schema: GatewaySchema
  onConfigure: () => void
  onToggleActive: (isActive: boolean) => Promise<void>
  onTestConnection: () => Promise<void>
  isTestingConnection: boolean
}

const healthStatusConfig: Record<GatewayHealthStatus, {
  icon: React.ElementType
  color: string
  label: string
}> = {
  Healthy: { icon: CheckCircle2, color: 'text-green-500', label: 'Healthy' },
  Degraded: { icon: AlertCircle, color: 'text-yellow-500', label: 'Degraded' },
  Unhealthy: { icon: XCircle, color: 'text-red-500', label: 'Unhealthy' },
  Unknown: { icon: Clock, color: 'text-muted-foreground', label: 'Unknown' },
}

export const GatewayCard = ({
  gateway,
  schema,
  onConfigure,
  onToggleActive,
  onTestConnection,
  isTestingConnection,
}: GatewayCardProps) => {
  const { t } = useTranslation('common')
  const [isToggling, setIsToggling] = useState(false)

  const isConfigured = gateway?.hasCredentials ?? false
  const isActive = gateway?.isActive ?? false
  const healthStatus = gateway?.healthStatus ?? 'Unknown'
  const HealthIcon = healthStatusConfig[healthStatus].icon

  const handleToggle = async (checked: boolean) => {
    setIsToggling(true)
    try {
      await onToggleActive(checked)
    } finally {
      setIsToggling(false)
    }
  }

  const getStatusBadge = () => {
    if (!isConfigured) {
      return (
        <Badge variant="secondary" className="text-muted-foreground">
          {t('paymentGateways.status.notConfigured', 'Not Configured')}
        </Badge>
      )
    }
    if (isActive) {
      return (
        <Badge variant="outline" className={getStatusBadgeClasses('green')}>
          {t('paymentGateways.status.active', 'Active')}
        </Badge>
      )
    }
    return (
      <Badge variant="outline">
        {t('paymentGateways.status.configured', 'Configured')}
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
                    // Fallback to CreditCard icon if image fails to load
                    e.currentTarget.style.display = 'none'
                    e.currentTarget.nextElementSibling?.classList.remove('hidden')
                  }}
                />
              ) : null}
              <CreditCard className={cn(
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
                    title={t('paymentGateways.viewDocs', 'View documentation')}
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
                {healthStatusConfig[healthStatus].label}
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
            {gateway?.environment && (
              <p className="text-xs text-muted-foreground mt-1">
                {gateway.environment === 'Sandbox' ? '🧪 ' : '🚀 '}
                {gateway.environment}
              </p>
            )}
          </div>
          {isConfigured && (
            <p className="text-xs text-muted-foreground">
              {t('paymentGateways.lastCheck', 'Last check')}: {formatLastHealthCheck(gateway?.lastHealthCheck ?? null)}
            </p>
          )}
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
                ? t('paymentGateways.enabled', 'Enabled')
                : t('paymentGateways.disabled', 'Disabled')}
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

          {/* Test Connection Button */}
          {isConfigured && (
            <Button
              variant="outline"
              size="sm"
              onClick={onTestConnection}
              disabled={isTestingConnection}
              className="cursor-pointer"
            >
              {isTestingConnection ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Plug className="h-4 w-4" />
              )}
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
