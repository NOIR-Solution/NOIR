/**
 * Wishlist types matching backend DTOs.
 */

export type WishlistItemPriority = 'None' | 'Low' | 'Medium' | 'High'

export interface WishlistDto {
  id: string
  name: string
  itemCount: number
  isDefault: boolean
  isPublic: boolean
  shareUrl?: string | null
  createdAt: string
}

export interface WishlistDetailDto {
  id: string
  name: string
  itemCount: number
  isDefault: boolean
  isPublic: boolean
  shareUrl?: string | null
  createdAt: string
  items: WishlistItemDto[]
}

export interface WishlistItemDto {
  id: string
  productId: string
  productName: string
  productImage?: string | null
  price: number
  productVariantId?: string | null
  variantName?: string | null
  addedAt: string
  note?: string | null
  priority: WishlistItemPriority
  isInStock: boolean
}

export interface AddToWishlistRequest {
  wishlistId?: string | null
  productId: string
  productVariantId?: string | null
  note?: string | null
}

export interface UpdateWishlistItemPriorityRequest {
  priority: WishlistItemPriority
}

export interface CreateWishlistRequest {
  name: string
  isPublic?: boolean
}

export interface UpdateWishlistRequest {
  name: string
  isPublic: boolean
}

export interface WishlistAnalyticsDto {
  totalWishlists: number
  totalWishlistItems: number
  topProducts: TopWishlistedProductDto[]
}

export interface TopWishlistedProductDto {
  productId: string
  productName: string
  productImage?: string | null
  wishlistCount: number
}
