import { useTranslation } from 'react-i18next'
import {
  ArrowLeft,
  ExternalLink,
  Package,
  Truck,
} from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Skeleton,
} from '@uikit'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { useShippingTrackingQuery } from '@/portal-app/shipping/queries'
import type { ShippingOrderDto } from '@/types/shipping'
import { TrackingTimeline } from './TrackingTimeline'
import { getShippingStatusColor } from '../utils/shippingStatus'

interface ShipmentDetailProps {
  shipment: ShippingOrderDto
  onBack: () => void
}

export const ShipmentDetail = ({ shipment, onBack }: ShipmentDetailProps) => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()
  const { data: tracking, isLoading: trackingLoading } = useShippingTrackingQuery(shipment.trackingNumber)

  const trackingUrl = shipment.trackingUrl || tracking?.trackingUrl

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button
          variant="ghost"
          size="sm"
          onClick={onBack}
          className="cursor-pointer"
          aria-label={t('buttons.back', 'Back')}
        >
          <ArrowLeft className="h-4 w-4 mr-1" />
          {t('buttons.back', 'Back')}
        </Button>
      </div>

      {/* Shipment Summary */}
      <Card className="shadow-sm">
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
                <Truck className="h-5 w-5 text-primary" />
              </div>
              <div>
                <CardTitle className="flex items-center gap-2">
                  {shipment.trackingNumber}
                  {trackingUrl && (
                    <a
                      href={trackingUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-primary hover:text-primary/80"
                      aria-label={t('shipping.openTracking', 'Open tracking page')}
                    >
                      <ExternalLink className="h-4 w-4" />
                    </a>
                  )}
                </CardTitle>
                <CardDescription>
                  {shipment.providerName} &middot; {shipment.serviceTypeName}
                </CardDescription>
              </div>
            </div>
            <Badge variant="outline" className={getShippingStatusColor(shipment.status)}>
              {t(`shipping.status.${shipment.status.toLowerCase()}`, shipment.status)}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <p className="text-xs text-muted-foreground">{t('shipping.totalFee', 'Total Fee')}</p>
              <p className="text-sm font-medium">{formatCurrency(shipment.totalShippingFee, 'VND')}</p>
            </div>
            {shipment.codAmount != null && shipment.codAmount > 0 && (
              <div>
                <p className="text-xs text-muted-foreground">{t('shipping.codAmount', 'COD Amount')}</p>
                <p className="text-sm font-medium">{formatCurrency(shipment.codAmount, 'VND')}</p>
              </div>
            )}
            <div>
              <p className="text-xs text-muted-foreground">{t('shipping.createdAt', 'Created')}</p>
              <p className="text-sm">{formatDateTime(shipment.createdAt)}</p>
            </div>
            {shipment.estimatedDeliveryDate && (
              <div>
                <p className="text-xs text-muted-foreground">{t('shipping.estimatedDelivery', 'Est. Delivery')}</p>
                <p className="text-sm">{formatDateTime(shipment.estimatedDeliveryDate)}</p>
              </div>
            )}
            {shipment.actualDeliveryDate && (
              <div>
                <p className="text-xs text-muted-foreground">{t('shipping.actualDelivery', 'Delivered')}</p>
                <p className="text-sm">{formatDateTime(shipment.actualDeliveryDate)}</p>
              </div>
            )}
          </div>

          {/* Fee breakdown */}
          <div className="mt-4 pt-4 border-t">
            <div className="grid grid-cols-3 gap-4 text-sm">
              <div>
                <p className="text-xs text-muted-foreground">{t('shipping.baseFee', 'Base Rate')}</p>
                <p>{formatCurrency(shipment.baseRate, 'VND')}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">{t('shipping.codFee', 'COD Fee')}</p>
                <p>{formatCurrency(shipment.codFee, 'VND')}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">{t('shipping.insuranceFee', 'Insurance Fee')}</p>
                <p>{formatCurrency(shipment.insuranceFee, 'VND')}</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tracking Timeline */}
      <Card className="shadow-sm">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Package className="h-5 w-5" />
            {t('shipping.trackingTimeline', 'Tracking Timeline')}
          </CardTitle>
          <CardDescription>
            {tracking
              ? t('shipping.trackingStatus', { status: tracking.statusDescription, defaultValue: `Status: ${tracking.statusDescription}` })
              : t('shipping.loadingTracking', 'Loading tracking information...')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {trackingLoading ? (
            <div className="space-y-4">
              {[...Array(3)].map((_, i) => (
                <div key={i} className="flex gap-4">
                  <Skeleton className="h-8 w-8 rounded-full" />
                  <div className="space-y-2 flex-1">
                    <Skeleton className="h-4 w-3/4" />
                    <Skeleton className="h-3 w-1/2" />
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <TrackingTimeline events={tracking?.events ?? []} />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
