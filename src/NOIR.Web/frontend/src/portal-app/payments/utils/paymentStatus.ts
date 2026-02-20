import type { PaymentStatus } from '@/services/payments'

export const paymentStatusColors: Record<PaymentStatus, string> = {
  Pending: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
  Processing: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400',
  RequiresAction: 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400',
  Authorized: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-400',
  Paid: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
  Failed: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
  Cancelled: 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400',
  Expired: 'bg-gray-100 text-gray-500 dark:bg-gray-900/30 dark:text-gray-500',
  Refunded: 'bg-pink-100 text-pink-800 dark:bg-pink-900/30 dark:text-pink-400',
  PartialRefund: 'bg-pink-50 text-pink-600 dark:bg-pink-900/20 dark:text-pink-400',
  CodPending: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400',
  CodCollected: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-400',
}
