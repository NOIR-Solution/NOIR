/**
 * Reviews API Service
 *
 * Provides methods for review moderation, admin responses, and bulk actions.
 */
import { apiClient } from './apiClient'
import type {
  ReviewDto,
  ReviewDetailDto,
  ReviewStatsDto,
  ReviewPagedResult,
  ReviewStatus,
} from '@/types/review'

export interface GetReviewsParams {
  page?: number
  pageSize?: number
  status?: ReviewStatus
  productId?: string
  rating?: number
  search?: string
}

export interface GetProductReviewsParams {
  sort?: string
  page?: number
  pageSize?: number
}

export const getReviews = async (params: GetReviewsParams = {}): Promise<ReviewPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.status) queryParams.append('status', params.status)
  if (params.productId) queryParams.append('productId', params.productId)
  if (params.rating != null) queryParams.append('rating', params.rating.toString())
  if (params.search) queryParams.append('search', params.search)

  const query = queryParams.toString()
  return apiClient<ReviewPagedResult>(`/reviews${query ? `?${query}` : ''}`)
}

export const getReviewById = async (id: string): Promise<ReviewDetailDto> => {
  return apiClient<ReviewDetailDto>(`/reviews/${id}`)
}

export const approveReview = async (id: string): Promise<ReviewDto> => {
  return apiClient<ReviewDto>(`/reviews/${id}/approve`, { method: 'POST' })
}

export const rejectReview = async (id: string, reason?: string): Promise<ReviewDto> => {
  return apiClient<ReviewDto>(`/reviews/${id}/reject`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
}

export const addAdminResponse = async (id: string, response: string): Promise<ReviewDto> => {
  return apiClient<ReviewDto>(`/reviews/${id}/respond`, {
    method: 'POST',
    body: JSON.stringify({ response }),
  })
}

export const bulkApproveReviews = async (reviewIds: string[]): Promise<void> => {
  return apiClient<void>('/reviews/bulk-approve', {
    method: 'POST',
    body: JSON.stringify({ reviewIds }),
  })
}

export const bulkRejectReviews = async (reviewIds: string[], reason?: string): Promise<void> => {
  return apiClient<void>('/reviews/bulk-reject', {
    method: 'POST',
    body: JSON.stringify({ reviewIds, reason }),
  })
}

export const getProductReviews = async (
  productId: string,
  params: GetProductReviewsParams = {},
): Promise<ReviewPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.sort) queryParams.append('sort', params.sort)
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<ReviewPagedResult>(`/products/${productId}/reviews${query ? `?${query}` : ''}`)
}

export const getReviewStats = async (productId: string): Promise<ReviewStatsDto> => {
  return apiClient<ReviewStatsDto>(`/products/${productId}/reviews/stats`)
}
