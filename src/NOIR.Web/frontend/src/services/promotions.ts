/**
 * Promotions API Service
 *
 * Provides methods for managing promotions and vouchers.
 */
import { apiClient } from './apiClient'
import type {
  PromotionDto,
  PromotionPagedResult,
  PromoCodeValidationDto,
  CreatePromotionRequest,
  UpdatePromotionRequest,
  PromotionStatus,
  PromotionType,
} from '@/types/promotion'

// ============================================================================
// Query Parameters
// ============================================================================

export interface GetPromotionsParams {
  page?: number
  pageSize?: number
  search?: string
  status?: PromotionStatus
  promotionType?: PromotionType
  fromDate?: string
  toDate?: string
}

// ============================================================================
// Promotions
// ============================================================================

/**
 * Fetch paginated list of promotions with optional filters
 */
export const getPromotions = async (params: GetPromotionsParams = {}): Promise<PromotionPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.search) queryParams.append('search', params.search)
  if (params.status) queryParams.append('status', params.status)
  if (params.promotionType) queryParams.append('promotionType', params.promotionType)
  if (params.fromDate) queryParams.append('fromDate', params.fromDate)
  if (params.toDate) queryParams.append('toDate', params.toDate)

  const query = queryParams.toString()
  return apiClient<PromotionPagedResult>(`/promotions${query ? `?${query}` : ''}`)
}

/**
 * Fetch a single promotion by ID
 */
export const getPromotionById = async (id: string): Promise<PromotionDto> => {
  return apiClient<PromotionDto>(`/promotions/${id}`)
}

/**
 * Create a new promotion
 */
export const createPromotion = async (request: CreatePromotionRequest): Promise<PromotionDto> => {
  return apiClient<PromotionDto>('/promotions', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing promotion
 */
export const updatePromotion = async (id: string, request: UpdatePromotionRequest): Promise<PromotionDto> => {
  return apiClient<PromotionDto>(`/promotions/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a promotion (soft delete)
 */
export const deletePromotion = async (id: string): Promise<void> => {
  return apiClient<void>(`/promotions/${id}`, {
    method: 'DELETE',
  })
}

/**
 * Activate a promotion
 */
export const activatePromotion = async (id: string): Promise<PromotionDto> => {
  return apiClient<PromotionDto>(`/promotions/${id}/activate`, {
    method: 'POST',
  })
}

/**
 * Deactivate a promotion
 */
export const deactivatePromotion = async (id: string): Promise<PromotionDto> => {
  return apiClient<PromotionDto>(`/promotions/${id}/deactivate`, {
    method: 'POST',
  })
}

/**
 * Validate a promo code against an order total
 */
export const validatePromoCode = async (code: string, orderTotal: number): Promise<PromoCodeValidationDto> => {
  return apiClient<PromoCodeValidationDto>(`/promotions/validate/${encodeURIComponent(code)}?orderTotal=${orderTotal}`)
}
