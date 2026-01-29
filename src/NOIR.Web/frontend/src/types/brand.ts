/**
 * Brand-related TypeScript types
 * Mirrors backend DTOs from NOIR.Application.Features.Brands.DTOs
 */

// ============================================================================
// Brand Types
// ============================================================================

/**
 * Full brand details for editing
 */
export interface Brand {
  id: string
  name: string
  slug: string
  logoUrl?: string | null
  bannerUrl?: string | null
  description?: string | null
  website?: string | null
  metaTitle?: string | null
  metaDescription?: string | null
  isActive: boolean
  isFeatured: boolean
  sortOrder: number
  productCount: number
  createdAt: string
  modifiedAt?: string | null
}

/**
 * Simplified brand for list views and dropdowns
 */
export interface BrandListItem {
  id: string
  name: string
  slug: string
  logoUrl?: string | null
  bannerUrl?: string | null
  description?: string | null
  websiteUrl?: string | null
  isActive: boolean
  isFeatured: boolean
  sortOrder: number
  productCount: number
}

// ============================================================================
// Request Types
// ============================================================================

export interface CreateBrandRequest {
  name: string
  slug: string
  logoUrl?: string | null
  bannerUrl?: string | null
  description?: string | null
  website?: string | null
  metaTitle?: string | null
  metaDescription?: string | null
  isFeatured?: boolean
}

export interface UpdateBrandRequest {
  name: string
  slug: string
  description?: string | null
  website?: string | null
  logoUrl?: string | null
  bannerUrl?: string | null
  metaTitle?: string | null
  metaDescription?: string | null
  isActive?: boolean
  isFeatured?: boolean
  sortOrder?: number
}

// ============================================================================
// Paged Result Types
// ============================================================================

export interface BrandPagedResult {
  items: BrandListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
