import type { OrderStatus } from '@/types/order'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

const orderStatusColorMap: Record<OrderStatus, Parameters<typeof getStatusBadgeClasses>[0]> = {
  Pending: 'amber',
  Confirmed: 'blue',
  Processing: 'indigo',
  Shipped: 'purple',
  Delivered: 'emerald',
  Completed: 'green',
  Cancelled: 'red',
  Refunded: 'orange',
  Returned: 'rose',
}

export const getOrderStatusColor = (status: OrderStatus): string => {
  return getStatusBadgeClasses(orderStatusColorMap[status] ?? 'gray')
}

export const getOrderStatusIconColor = (status: OrderStatus): string => {
  switch (status) {
    case 'Pending':
      return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400'
    case 'Confirmed':
      return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
    case 'Processing':
      return 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-400'
    case 'Shipped':
      return 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400'
    case 'Delivered':
      return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
    case 'Completed':
      return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
    case 'Cancelled':
      return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
    case 'Refunded':
      return 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400'
    case 'Returned':
      return 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400'
    default:
      return ''
  }
}

export const ORDER_STATUSES: OrderStatus[] = [
  'Pending',
  'Confirmed',
  'Processing',
  'Shipped',
  'Delivered',
  'Completed',
  'Cancelled',
  'Refunded',
  'Returned',
]
