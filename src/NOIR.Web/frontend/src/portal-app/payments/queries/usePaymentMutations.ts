import { useMutation, useQueryClient } from '@tanstack/react-query'
import { refreshPaymentStatus, recordManualPayment, confirmCodCollection, requestRefund, approveRefund, rejectRefund, type RecordManualPaymentRequest, type RequestRefundRequest, type ApproveRefundRequest, type RejectRefundRequest } from '@/services/payments'
import { paymentKeys } from './queryKeys'

export const useRefreshPaymentMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => refreshPaymentStatus(id),
    onSuccess: (updatedPayment) => {
      queryClient.invalidateQueries({ queryKey: paymentKeys.detail(updatedPayment.id) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.timeline(updatedPayment.id) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.lists() })
    },
  })
}

export const useRecordManualPaymentMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: RecordManualPaymentRequest) => recordManualPayment(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: paymentKeys.lists() })
    },
  })
}

export const useConfirmCodCollectionMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string }) => confirmCodCollection(id, notes),
    onSuccess: (updatedPayment) => {
      queryClient.invalidateQueries({ queryKey: paymentKeys.detail(updatedPayment.id) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.timeline(updatedPayment.id) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.lists() })
    },
  })
}

export const useRequestRefundMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: RequestRefundRequest) => requestRefund(request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: paymentKeys.detail(variables.paymentTransactionId) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.timeline(variables.paymentTransactionId) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.lists() })
    },
  })
}

export const useApproveRefundMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request?: ApproveRefundRequest }) => approveRefund(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: paymentKeys.detail(data.paymentTransactionId) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.timeline(data.paymentTransactionId) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.lists() })
    },
  })
}

export const useRejectRefundMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: RejectRefundRequest }) => rejectRefund(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: paymentKeys.detail(data.paymentTransactionId) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.timeline(data.paymentTransactionId) })
      queryClient.invalidateQueries({ queryKey: paymentKeys.lists() })
    },
  })
}
