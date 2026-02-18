import type { ShippingStatus } from '@/types/shipping'

export const SHIPPING_STATUSES: ShippingStatus[] = [
  'Draft',
  'AwaitingPickup',
  'PickedUp',
  'InTransit',
  'OutForDelivery',
  'Delivered',
  'DeliveryFailed',
  'Cancelled',
  'Returning',
  'Returned',
]

export const getShippingStatusColor = (status: ShippingStatus): string => {
  switch (status) {
    case 'Delivered':
      return 'bg-emerald-100 text-emerald-700 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-400 dark:border-emerald-800'
    case 'InTransit':
    case 'OutForDelivery':
      return 'bg-blue-100 text-blue-700 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400 dark:border-blue-800'
    case 'PickedUp':
    case 'AwaitingPickup':
      return 'bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400 dark:border-amber-800'
    case 'Draft':
      return 'bg-slate-100 text-slate-700 border-slate-200 dark:bg-slate-900/30 dark:text-slate-400 dark:border-slate-800'
    case 'Cancelled':
    case 'DeliveryFailed':
      return 'bg-red-100 text-red-700 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800'
    case 'Returning':
    case 'Returned':
      return 'bg-orange-100 text-orange-700 border-orange-200 dark:bg-orange-900/30 dark:text-orange-400 dark:border-orange-800'
    default:
      return ''
  }
}
