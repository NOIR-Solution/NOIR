/**
 * Wishlists API Service
 *
 * Provides methods for managing wishlists, wishlist items,
 * sharing, and analytics.
 */
import { apiClient } from './apiClient'
import type {
  WishlistDto,
  WishlistDetailDto,
  WishlistAnalyticsDto,
  AddToWishlistRequest,
  CreateWishlistRequest,
  UpdateWishlistRequest,
  WishlistItemPriority,
} from '@/types/wishlist'

// ============================================================================
// Queries
// ============================================================================

export const getWishlists = async (): Promise<WishlistDto[]> => {
  return apiClient<WishlistDto[]>('/wishlists')
}

export const getWishlistById = async (id: string): Promise<WishlistDetailDto> => {
  return apiClient<WishlistDetailDto>(`/wishlists/${id}`)
}

export const getSharedWishlist = async (token: string): Promise<WishlistDetailDto> => {
  return apiClient<WishlistDetailDto>(`/wishlists/shared/${token}`)
}

export interface GetWishlistAnalyticsParams {
  topCount?: number
}

export const getWishlistAnalytics = async (
  params: GetWishlistAnalyticsParams = {}
): Promise<WishlistAnalyticsDto> => {
  const queryParams = new URLSearchParams()
  if (params.topCount != null) queryParams.append('topCount', params.topCount.toString())

  const query = queryParams.toString()
  return apiClient<WishlistAnalyticsDto>(`/wishlists/analytics${query ? `?${query}` : ''}`)
}

// ============================================================================
// Mutations
// ============================================================================

export const createWishlist = async (request: CreateWishlistRequest): Promise<WishlistDto> => {
  return apiClient<WishlistDto>('/wishlists', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateWishlist = async (
  id: string,
  request: UpdateWishlistRequest
): Promise<WishlistDto> => {
  return apiClient<WishlistDto>(`/wishlists/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const deleteWishlist = async (id: string): Promise<WishlistDto> => {
  return apiClient<WishlistDto>(`/wishlists/${id}`, {
    method: 'DELETE',
  })
}

export const addToWishlist = async (
  request: AddToWishlistRequest
): Promise<WishlistDetailDto> => {
  return apiClient<WishlistDetailDto>('/wishlists/items', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const removeFromWishlist = async (itemId: string): Promise<WishlistDetailDto> => {
  return apiClient<WishlistDetailDto>(`/wishlists/items/${itemId}`, {
    method: 'DELETE',
  })
}

export const moveToCart = async (itemId: string): Promise<WishlistDetailDto> => {
  return apiClient<WishlistDetailDto>(`/wishlists/items/${itemId}/move-to-cart`, {
    method: 'POST',
  })
}

export const shareWishlist = async (id: string): Promise<WishlistDto> => {
  return apiClient<WishlistDto>(`/wishlists/${id}/share`, {
    method: 'POST',
  })
}

export const updateWishlistItemPriority = async (
  itemId: string,
  priority: WishlistItemPriority
): Promise<WishlistDetailDto> => {
  return apiClient<WishlistDetailDto>(`/wishlists/items/${itemId}/priority`, {
    method: 'PUT',
    body: JSON.stringify({ priority }),
  })
}
