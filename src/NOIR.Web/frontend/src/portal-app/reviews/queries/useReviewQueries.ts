import { useQuery, keepPreviousData } from '@tanstack/react-query'
import {
  getReviews,
  getReviewById,
  getReviewStats,
  getProductReviews,
  type GetReviewsParams,
  type GetProductReviewsParams,
} from '@/services/reviews'
import { reviewKeys } from './queryKeys'

export const useReviewsQuery = (params: GetReviewsParams) =>
  useQuery({
    queryKey: reviewKeys.list(params),
    queryFn: () => getReviews(params),
    placeholderData: keepPreviousData,
  })

export const useReviewQuery = (id: string | undefined) =>
  useQuery({
    queryKey: reviewKeys.detail(id!),
    queryFn: () => getReviewById(id!),
    enabled: !!id,
  })

export const useReviewStatsQuery = (productId: string | undefined) =>
  useQuery({
    queryKey: reviewKeys.stats(productId!),
    queryFn: () => getReviewStats(productId!),
    enabled: !!productId,
  })

export const useProductReviewsQuery = (
  productId: string | undefined,
  params: GetProductReviewsParams,
) =>
  useQuery({
    queryKey: reviewKeys.productReviewsList(productId!, params),
    queryFn: () => getProductReviews(productId!, params),
    placeholderData: keepPreviousData,
    enabled: !!productId,
  })
