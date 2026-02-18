import { useQuery } from '@tanstack/react-query'
import {
  getWishlists,
  getWishlistById,
  getSharedWishlist,
  getWishlistAnalytics,
  type GetWishlistAnalyticsParams,
} from '@/services/wishlists'
import { wishlistKeys } from './queryKeys'

export const useWishlistsQuery = () =>
  useQuery({
    queryKey: wishlistKeys.lists(),
    queryFn: () => getWishlists(),
  })

export const useWishlistDetailQuery = (id: string | undefined) =>
  useQuery({
    queryKey: wishlistKeys.detail(id!),
    queryFn: () => getWishlistById(id!),
    enabled: !!id,
  })

export const useSharedWishlistQuery = (token: string | undefined) =>
  useQuery({
    queryKey: wishlistKeys.shared(token!),
    queryFn: () => getSharedWishlist(token!),
    enabled: !!token,
  })

export const useWishlistAnalyticsQuery = (params: GetWishlistAnalyticsParams = {}) =>
  useQuery({
    queryKey: wishlistKeys.analytics(params),
    queryFn: () => getWishlistAnalytics(params),
    staleTime: 30_000,
  })
