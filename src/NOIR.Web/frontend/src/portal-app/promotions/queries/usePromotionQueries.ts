import { useQuery } from '@tanstack/react-query'
import { getPromotions, getPromotionById, type GetPromotionsParams } from '@/services/promotions'
import { promotionKeys } from './queryKeys'

export const usePromotionsQuery = (params: GetPromotionsParams = {}) =>
  useQuery({
    queryKey: promotionKeys.list(params),
    queryFn: () => getPromotions(params),
    placeholderData: (previousData) => previousData,
  })

export const usePromotionQuery = (id: string | undefined) =>
  useQuery({
    queryKey: promotionKeys.detail(id!),
    queryFn: () => getPromotionById(id!),
    enabled: !!id,
  })
