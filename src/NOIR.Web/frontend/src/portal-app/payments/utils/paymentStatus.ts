import type { PaymentStatus } from '@/services/payments'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

export const paymentStatusColors: Record<PaymentStatus, string> = {
  Pending: getStatusBadgeClasses('yellow'),
  Processing: getStatusBadgeClasses('blue'),
  RequiresAction: getStatusBadgeClasses('orange'),
  Authorized: getStatusBadgeClasses('purple'),
  Paid: getStatusBadgeClasses('green'),
  Failed: getStatusBadgeClasses('red'),
  Cancelled: getStatusBadgeClasses('gray'),
  Expired: getStatusBadgeClasses('gray'),
  Refunded: getStatusBadgeClasses('pink'),
  PartialRefund: getStatusBadgeClasses('pink'),
  CodPending: getStatusBadgeClasses('amber'),
  CodCollected: getStatusBadgeClasses('emerald'),
}
