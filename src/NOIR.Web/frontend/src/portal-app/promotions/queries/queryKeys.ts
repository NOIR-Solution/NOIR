import type { GetPromotionsParams } from '@/services/promotions'

export const promotionKeys = {
  all: ['promotions'] as const,
  lists: () => [...promotionKeys.all, 'list'] as const,
  list: (params: GetPromotionsParams) => [...promotionKeys.lists(), params] as const,
  details: () => [...promotionKeys.all, 'detail'] as const,
  detail: (id: string) => [...promotionKeys.details(), id] as const,
}
