import type { GetPaymentsParams } from '@/services/payments'

export const paymentKeys = {
  all: ['payments'] as const,
  lists: () => [...paymentKeys.all, 'list'] as const,
  list: (params: GetPaymentsParams) => [...paymentKeys.lists(), params] as const,
  details: () => [...paymentKeys.all, 'detail'] as const,
  detail: (id: string) => [...paymentKeys.details(), id] as const,
  timeline: (id: string) => [...paymentKeys.all, 'timeline', id] as const,
  orderPayments: (orderId: string) => [...paymentKeys.all, 'order', orderId] as const,
}
