import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  createPromotion,
  updatePromotion,
  deletePromotion,
  activatePromotion,
  deactivatePromotion,
} from '@/services/promotions'
import type { CreatePromotionRequest, UpdatePromotionRequest } from '@/types/promotion'
import { promotionKeys } from './queryKeys'
import { optimisticListDelete, optimisticListPatch } from '@/hooks/useOptimisticMutation'

export const useCreatePromotionMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreatePromotionRequest) => createPromotion(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: promotionKeys.all })
    },
  })
}

export const useUpdatePromotionMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdatePromotionRequest }) => updatePromotion(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: promotionKeys.all })
    },
  })
}

export const useDeletePromotionMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deletePromotion(id),
    ...optimisticListDelete(queryClient, promotionKeys.lists(), promotionKeys.all),
  })
}

export const useActivatePromotionMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => activatePromotion(id),
    ...optimisticListPatch(queryClient, promotionKeys.lists(), promotionKeys.all, {
      status: 'Active',
      isActive: true,
    }),
  })
}

export const useDeactivatePromotionMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deactivatePromotion(id),
    ...optimisticListPatch(queryClient, promotionKeys.lists(), promotionKeys.all, {
      status: 'Draft',
      isActive: false,
    }),
  })
}
