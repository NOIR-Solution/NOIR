import type { GetReviewsParams, GetProductReviewsParams } from '@/services/reviews'

export const reviewKeys = {
  all: ['reviews'] as const,
  lists: () => [...reviewKeys.all, 'list'] as const,
  list: (params: GetReviewsParams) => [...reviewKeys.lists(), params] as const,
  details: () => [...reviewKeys.all, 'detail'] as const,
  detail: (id: string) => [...reviewKeys.details(), id] as const,
  productReviews: (productId: string) => [...reviewKeys.all, 'product', productId] as const,
  productReviewsList: (productId: string, params: GetProductReviewsParams) =>
    [...reviewKeys.productReviews(productId), 'list', params] as const,
  stats: (productId: string) => [...reviewKeys.all, 'stats', productId] as const,
}
