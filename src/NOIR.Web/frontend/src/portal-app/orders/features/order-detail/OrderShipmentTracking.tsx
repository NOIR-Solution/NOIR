import { useTranslation } from 'react-i18next'
import { ExternalLink, Package, Truck } from 'lucide-react'
import {
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Skeleton,
} from '@uikit'
import { useShippingOrderByOrderIdQuery, useShippingTrackingQuery } from '@/portal-app/shipping/queries'
import { TrackingTimeline } from '@/portal-app/shipping/components/TrackingTimeline'
import { getShippingStatusColor } from '@/portal-app/shipping/utils/shippingStatus'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'

interface OrderShipmentTrackingProps {
  orderId: string
  trackingNumber?: string | null
  shippingCarrier?: string | null
  currency: string
}

export const OrderShipmentTracking = ({ orderId, trackingNumber, shippingCarrier, currency }: OrderShipmentTrackingProps) => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()

  const { data: shippingOrder, isLoading: shippingLoading, isError: shippingError } = useShippingOrderByOrderIdQuery(orderId)
  const { data: tracking, isLoading: trackingLoading } = useShippingTrackingQuery(shippingOrder?.trackingNumber)

  const trackingUrl = shippingOrder?.trackingUrl || tracking?.trackingUrl

  // Loading state
  if (shippingLoading) {
    return (
      <Card className="shadow-sm gap-4 py-5">
        <CardHeader>
          <CardTitle className="text-sm flex items-center gap-2">
            <Truck className="h-4 w-4" />
            {t('orders.shipmentTracking', 'Shipment Tracking')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="space-y-1">
                <Skeleton className="h-3 w-16" />
                <Skeleton className="h-4 w-24" />
              </div>
            ))}
          </div>
          <div className="space-y-4 pt-4 border-t">
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
        </CardContent>
      </Card>
    )
  }

  // Fallback: No shipping integration, just manual tracking number
  if (shippingError || !shippingOrder) {
    if (!trackingNumber) return null

    return (
      <Card className="shadow-sm gap-4 py-5">
        <CardHeader>
          <CardTitle className="text-sm flex items-center gap-2">
            <Truck className="h-4 w-4" />
            {t('orders.shipmentTracking', 'Shipment Tracking')}
          </CardTitle>
          <CardDescription>{t('orders.manualTrackingOnly', 'Tracking via carrier website')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <div>
            <p className="text-muted-foreground text-xs">{t('orders.trackingNumber', 'Tracking Number')}</p>
            <code className="text-sm bg-muted px-1.5 py-0.5 rounded">{trackingNumber}</code>
          </div>
          {shippingCarrier && (
            <div>
              <p className="text-muted-foreground text-xs">{t('orders.carrier', 'Carrier')}</p>
              <p className="font-medium">{shippingCarrier}</p>
            </div>
          )}
        </CardContent>
      </Card>
    )
  }

  // Full shipping integration view
  return (
    <Card className="shadow-sm gap-4 py-5">
      <CardHeader>
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
              <Truck className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CardTitle className="text-sm flex items-center gap-2">
                {shippingOrder.trackingNumber}
                {trackingUrl && (
                  <a
                    href={trackingUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-primary hover:text-primary/80"
                    aria-label={t('shipping.openTracking', 'Open tracking page')}
                  >
                    <ExternalLink className="h-3.5 w-3.5" />
                  </a>
                )}
              </CardTitle>
              <CardDescription>
                {shippingOrder.providerName} &middot; {shippingOrder.serviceTypeName}
              </CardDescription>
            </div>
          </div>
          <Badge variant="outline" className={getShippingStatusColor(shippingOrder.status)}>
            {t(`shipping.status.${shippingOrder.status.toLowerCase()}`, shippingOrder.status)}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Summary grid */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div>
            <p className="text-xs text-muted-foreground">{t('shipping.totalFee', 'Total Fee')}</p>
            <p className="text-sm font-medium">{formatCurrency(shippingOrder.totalShippingFee, currency)}</p>
          </div>
          {shippingOrder.codAmount != null && shippingOrder.codAmount > 0 && (
            <div>
              <p className="text-xs text-muted-foreground">{t('shipping.codAmount', 'COD Amount')}</p>
              <p className="text-sm font-medium">{formatCurrency(shippingOrder.codAmount, currency)}</p>
            </div>
          )}
          {shippingOrder.estimatedDeliveryDate && (
            <div>
              <p className="text-xs text-muted-foreground">{t('shipping.estimatedDelivery', 'Est. Delivery')}</p>
              <p className="text-sm">{formatDateTime(shippingOrder.estimatedDeliveryDate)}</p>
            </div>
          )}
          {shippingOrder.actualDeliveryDate && (
            <div>
              <p className="text-xs text-muted-foreground">{t('shipping.actualDelivery', 'Delivered')}</p>
              <p className="text-sm">{formatDateTime(shippingOrder.actualDeliveryDate)}</p>
            </div>
          )}
        </div>

        {/* Tracking Timeline */}
        <div className="pt-4 border-t">
          <div className="flex items-center gap-2 mb-4">
            <Package className="h-4 w-4 text-muted-foreground" />
            <p className="text-sm font-medium">{t('shipping.trackingTimeline', 'Tracking Timeline')}</p>
          </div>
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
        </div>
      </CardContent>
    </Card>
  )
}
