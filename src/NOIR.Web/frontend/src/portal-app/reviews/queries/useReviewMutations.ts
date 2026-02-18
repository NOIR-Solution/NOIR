import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  approveReview,
  rejectReview,
  addAdminResponse,
  bulkApproveReviews,
  bulkRejectReviews,
} from '@/services/reviews'
import { optimisticListPatch } from '@/hooks/useOptimisticMutation'
import { reviewKeys } from './queryKeys'

export const useApproveReview = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => approveReview(id),
    ...optimisticListPatch(queryClient, reviewKeys.lists(), reviewKeys.all, {
      status: 'Approved',
    }),
  })
}

export const useRejectReview = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) => rejectReview(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: reviewKeys.all })
    },
  })
}

export const useAddAdminResponse = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, response }: { id: string; response: string }) =>
      addAdminResponse(id, response),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: reviewKeys.all })
    },
  })
}

export const useBulkApprove = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (reviewIds: string[]) => bulkApproveReviews(reviewIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: reviewKeys.all })
    },
  })
}

export const useBulkReject = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ reviewIds, reason }: { reviewIds: string[]; reason?: string }) =>
      bulkRejectReviews(reviewIds, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: reviewKeys.all })
    },
  })
}
