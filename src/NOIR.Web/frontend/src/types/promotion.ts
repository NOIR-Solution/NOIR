/**
 * Promotion-related TypeScript types
 * Mirrors backend DTOs from NOIR.Application.Features.Promotions.DTOs
 */

// ============================================================================
// Enums
// ============================================================================

export type PromotionStatus = 'Draft' | 'Active' | 'Scheduled' | 'Expired' | 'Cancelled'

export type PromotionType = 'VoucherCode' | 'FlashSale' | 'BundleDeal' | 'FreeShipping'

export type DiscountType = 'FixedAmount' | 'Percentage' | 'FreeShipping' | 'BuyXGetY'

export type PromotionApplyLevel = 'Cart' | 'Product' | 'Category'

// ============================================================================
// DTOs
// ============================================================================

/**
 * Full promotion details including related products, categories, and usage stats.
 * Mirrors PromotionDto from the backend.
 */
export interface PromotionDto {
  id: string
  name: string
  description?: string | null
  code: string
  promotionType: PromotionType
  discountType: DiscountType
  discountValue: number
  maxDiscountAmount?: number | null
  minOrderValue?: number | null
  minItemQuantity?: number | null
  usageLimitTotal?: number | null
  usageLimitPerUser?: number | null
  currentUsageCount: number
  startDate: string
  endDate: string
  isActive: boolean
  status: PromotionStatus
  applyLevel: PromotionApplyLevel
  productIds: string[]
  categoryIds: string[]
  recentUsages: PromotionUsageDto[]
  createdAt: string
  modifiedAt?: string | null
}

/**
 * DTO for promotion usage records.
 * Mirrors PromotionUsageDto from the backend.
 */
export interface PromotionUsageDto {
  id: string
  promotionId: string
  userId: string
  orderId: string
  discountAmount: number
  usedAt: string
}

/**
 * Result DTO for promo code validation.
 * Mirrors PromoCodeValidationDto from the backend.
 */
export interface PromoCodeValidationDto {
  isValid: boolean
  message?: string | null
  discountAmount: number
  code: string
  discountType: DiscountType
  discountValue: number
  maxDiscountAmount?: number | null
}

// ============================================================================
// Request Types
// ============================================================================

export interface CreatePromotionRequest {
  name: string
  code: string
  description?: string | null
  promotionType: PromotionType
  discountType: DiscountType
  discountValue: number
  startDate: string
  endDate: string
  applyLevel?: PromotionApplyLevel
  maxDiscountAmount?: number | null
  minOrderValue?: number | null
  minItemQuantity?: number | null
  usageLimitTotal?: number | null
  usageLimitPerUser?: number | null
  productIds?: string[] | null
  categoryIds?: string[] | null
}

export interface UpdatePromotionRequest {
  name: string
  code: string
  description?: string | null
  promotionType: PromotionType
  discountType: DiscountType
  discountValue: number
  startDate: string
  endDate: string
  applyLevel: PromotionApplyLevel
  maxDiscountAmount?: number | null
  minOrderValue?: number | null
  minItemQuantity?: number | null
  usageLimitTotal?: number | null
  usageLimitPerUser?: number | null
  productIds?: string[] | null
  categoryIds?: string[] | null
}

// ============================================================================
// Paged Result Types
// ============================================================================

export interface PromotionPagedResult {
  items: PromotionDto[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
