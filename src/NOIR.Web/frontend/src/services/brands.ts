/**
 * Brands API Service
 *
 * Provides methods for managing product brands.
 */
import { apiClient } from './apiClient'
import type {
  Brand,
  BrandListItem,
  BrandPagedResult,
  CreateBrandRequest,
  UpdateBrandRequest,
} from '@/types/brand'

// ============================================================================
// Brands
// ============================================================================

export interface GetBrandsParams {
  search?: string
  isActive?: boolean
  isFeatured?: boolean
  page?: number
  pageSize?: number
}

/**
 * Fetch paginated list of brands
 */
export const getBrands = async (params: GetBrandsParams = {}): Promise<BrandPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)
  if (params.isActive !== undefined) queryParams.append('isActive', String(params.isActive))
  if (params.isFeatured !== undefined) queryParams.append('isFeatured', String(params.isFeatured))
  if (params.page) queryParams.append('pageNumber', params.page.toString())
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<BrandPagedResult>(`/brands${query ? `?${query}` : ''}`)
}

/**
 * Fetch all active brands (for dropdowns)
 */
export const getActiveBrands = async (): Promise<BrandListItem[]> => {
  const result = await getBrands({ isActive: true, pageSize: 1000 })
  return result.items
}

/**
 * Fetch a single brand by ID
 */
export const getBrandById = async (id: string): Promise<Brand> => {
  return apiClient<Brand>(`/brands/${id}`)
}

/**
 * Create a new brand
 */
export const createBrand = async (request: CreateBrandRequest): Promise<Brand> => {
  return apiClient<Brand>('/brands', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing brand
 */
export const updateBrand = async (id: string, request: UpdateBrandRequest): Promise<Brand> => {
  return apiClient<Brand>(`/brands/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a brand (soft delete)
 */
export const deleteBrand = async (id: string): Promise<void> => {
  return apiClient<void>(`/brands/${id}`, {
    method: 'DELETE',
  })
}
