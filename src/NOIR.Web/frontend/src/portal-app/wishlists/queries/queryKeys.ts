import type { GetWishlistAnalyticsParams } from '@/services/wishlists'

export const wishlistKeys = {
  all: ['wishlists'] as const,
  lists: () => [...wishlistKeys.all, 'list'] as const,
  details: () => [...wishlistKeys.all, 'detail'] as const,
  detail: (id: string) => [...wishlistKeys.details(), id] as const,
  shared: (token: string) => [...wishlistKeys.all, 'shared', token] as const,
  analytics: (params?: GetWishlistAnalyticsParams) => [...wishlistKeys.all, 'analytics', params] as const,
}
