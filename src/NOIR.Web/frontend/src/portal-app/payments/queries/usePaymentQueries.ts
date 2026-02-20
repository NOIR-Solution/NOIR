import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { getPayments, getPaymentDetails, getPaymentTimeline, type GetPaymentsParams } from '@/services/payments'
import { paymentKeys } from './queryKeys'

export const usePaymentsQuery = (params: GetPaymentsParams) =>
  useQuery({
    queryKey: paymentKeys.list(params),
    queryFn: () => getPayments(params),
    placeholderData: keepPreviousData,
  })

export const usePaymentDetailsQuery = (id: string | undefined) =>
  useQuery({
    queryKey: paymentKeys.detail(id!),
    queryFn: () => getPaymentDetails(id!),
    enabled: !!id,
  })

export const usePaymentTimelineQuery = (id: string | undefined) =>
  useQuery({
    queryKey: paymentKeys.timeline(id!),
    queryFn: () => getPaymentTimeline(id!),
    enabled: !!id,
  })
