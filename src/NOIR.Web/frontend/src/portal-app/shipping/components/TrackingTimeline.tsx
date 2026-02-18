import { useTranslation } from 'react-i18next'
import {
  CheckCircle2,
  Circle,
  Clock,
  MapPin,
  Package,
  Truck,
  XCircle,
  RotateCcw,
  AlertTriangle,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import type { TrackingEventDto, ShippingStatus } from '@/types/shipping'

const getStatusIcon = (status: ShippingStatus) => {
  switch (status) {
    case 'Delivered':
      return CheckCircle2
    case 'InTransit':
    case 'OutForDelivery':
      return Truck
    case 'PickedUp':
    case 'AwaitingPickup':
      return Package
    case 'Cancelled':
      return XCircle
    case 'DeliveryFailed':
      return AlertTriangle
    case 'Returning':
    case 'Returned':
      return RotateCcw
    default:
      return Circle
  }
}

const getStatusColor = (status: ShippingStatus) => {
  switch (status) {
    case 'Delivered':
      return 'text-emerald-600 bg-emerald-100 border-emerald-200 dark:text-emerald-400 dark:bg-emerald-900/30 dark:border-emerald-800'
    case 'InTransit':
    case 'OutForDelivery':
      return 'text-blue-600 bg-blue-100 border-blue-200 dark:text-blue-400 dark:bg-blue-900/30 dark:border-blue-800'
    case 'PickedUp':
    case 'AwaitingPickup':
      return 'text-amber-600 bg-amber-100 border-amber-200 dark:text-amber-400 dark:bg-amber-900/30 dark:border-amber-800'
    case 'Cancelled':
    case 'DeliveryFailed':
      return 'text-red-600 bg-red-100 border-red-200 dark:text-red-400 dark:bg-red-900/30 dark:border-red-800'
    case 'Returning':
    case 'Returned':
      return 'text-orange-600 bg-orange-100 border-orange-200 dark:text-orange-400 dark:bg-orange-900/30 dark:border-orange-800'
    default:
      return 'text-muted-foreground bg-muted border-border'
  }
}

const getLineColor = (status: ShippingStatus) => {
  switch (status) {
    case 'Delivered':
      return 'bg-emerald-300 dark:bg-emerald-700'
    case 'InTransit':
    case 'OutForDelivery':
      return 'bg-blue-300 dark:bg-blue-700'
    case 'PickedUp':
    case 'AwaitingPickup':
      return 'bg-amber-300 dark:bg-amber-700'
    case 'Cancelled':
    case 'DeliveryFailed':
      return 'bg-red-300 dark:bg-red-700'
    default:
      return 'bg-border'
  }
}

interface TrackingTimelineProps {
  events: TrackingEventDto[]
  className?: string
}

export const TrackingTimeline = ({ events, className }: TrackingTimelineProps) => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()

  if (events.length === 0) {
    return (
      <div className={cn('flex flex-col items-center justify-center py-8 text-muted-foreground', className)}>
        <Clock className="h-8 w-8 mb-2" />
        <p className="text-sm">{t('shipping.noTrackingEvents', 'No tracking events yet')}</p>
      </div>
    )
  }

  // Events are displayed newest-first
  const sortedEvents = [...events].sort(
    (a, b) => new Date(b.eventDate).getTime() - new Date(a.eventDate).getTime()
  )

  return (
    <div className={cn('relative', className)}>
      {sortedEvents.map((event, index) => {
        const Icon = getStatusIcon(event.status)
        const isFirst = index === 0
        const isLast = index === sortedEvents.length - 1

        return (
          <div key={`${event.eventDate}-${index}`} className="flex gap-4 pb-6 last:pb-0">
            {/* Timeline line + icon */}
            <div className="flex flex-col items-center">
              <div className={cn(
                'flex h-8 w-8 items-center justify-center rounded-full border-2 flex-shrink-0',
                getStatusColor(event.status),
                isFirst && 'ring-2 ring-offset-2 ring-offset-background ring-primary/20'
              )}>
                <Icon className="h-4 w-4" />
              </div>
              {!isLast && (
                <div className={cn('w-0.5 flex-1 mt-1', getLineColor(event.status))} />
              )}
            </div>

            {/* Content */}
            <div className="flex-1 pt-0.5 pb-2">
              <div className="flex items-start justify-between gap-2">
                <div>
                  <p className={cn('text-sm font-medium', isFirst && 'text-foreground')}>
                    {event.description}
                  </p>
                  {event.location && (
                    <div className="flex items-center gap-1 mt-1 text-xs text-muted-foreground">
                      <MapPin className="h-3 w-3" />
                      <span>{event.location}</span>
                    </div>
                  )}
                </div>
                <span className="text-xs text-muted-foreground whitespace-nowrap">
                  {formatDateTime(event.eventDate)}
                </span>
              </div>
            </div>
          </div>
        )
      })}
    </div>
  )
}
