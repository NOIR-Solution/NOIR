import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  createWishlist,
  updateWishlist,
  deleteWishlist,
  addToWishlist,
  removeFromWishlist,
  moveToCart,
  shareWishlist,
  updateWishlistItemPriority,
} from '@/services/wishlists'
import type {
  CreateWishlistRequest,
  UpdateWishlistRequest,
  AddToWishlistRequest,
  WishlistItemPriority,
} from '@/types/wishlist'
import { optimisticArrayDelete } from '@/hooks/useOptimisticMutation'
import { wishlistKeys } from './queryKeys'

export const useCreateWishlist = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateWishlistRequest) => createWishlist(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wishlistKeys.all })
    },
  })
}

export const useUpdateWishlist = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateWishlistRequest }) =>
      updateWishlist(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wishlistKeys.all })
    },
  })
}

export const useDeleteWishlist = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteWishlist(id),
    ...optimisticArrayDelete(queryClient, wishlistKeys.lists(), wishlistKeys.all),
  })
}

export const useAddToWishlist = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: AddToWishlistRequest) => addToWishlist(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wishlistKeys.all })
    },
  })
}

export const useRemoveFromWishlist = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemId: string) => removeFromWishlist(itemId),
    onMutate: async (itemId: string) => {
      // Cancel outstanding queries
      await queryClient.cancelQueries({ queryKey: wishlistKeys.details() })
      // Snapshot all detail caches
      const previousDetails = queryClient.getQueriesData({ queryKey: wishlistKeys.details() })
      // Optimistically remove item from all detail caches
      queryClient.setQueriesData<{ items: { id: string }[] }>(
        { queryKey: wishlistKeys.details() },
        (old) => {
          if (!old?.items) return old
          return {
            ...old,
            items: old.items.filter((item) => item.id !== itemId),
            itemCount: old.items.filter((item) => item.id !== itemId).length,
          }
        }
      )
      return { previousDetails }
    },
    onError: (_err, _itemId, context) => {
      // Rollback on error
      if (context?.previousDetails) {
        for (const [key, data] of context.previousDetails) {
          queryClient.setQueryData(key, data)
        }
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: wishlistKeys.all })
    },
  })
}

export const useMoveToCart = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemId: string) => moveToCart(itemId),
    onMutate: async (itemId: string) => {
      await queryClient.cancelQueries({ queryKey: wishlistKeys.details() })
      const previousDetails = queryClient.getQueriesData({ queryKey: wishlistKeys.details() })
      // Optimistically remove item (it moves to cart)
      queryClient.setQueriesData<{ items: { id: string }[] }>(
        { queryKey: wishlistKeys.details() },
        (old) => {
          if (!old?.items) return old
          return {
            ...old,
            items: old.items.filter((item) => item.id !== itemId),
            itemCount: old.items.filter((item) => item.id !== itemId).length,
          }
        }
      )
      return { previousDetails }
    },
    onError: (_err, _itemId, context) => {
      if (context?.previousDetails) {
        for (const [key, data] of context.previousDetails) {
          queryClient.setQueryData(key, data)
        }
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: wishlistKeys.all })
    },
  })
}

export const useShareWishlist = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => shareWishlist(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wishlistKeys.all })
    },
  })
}

export const useUpdateWishlistItemPriority = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ itemId, priority }: { itemId: string; priority: WishlistItemPriority }) =>
      updateWishlistItemPriority(itemId, priority),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wishlistKeys.details() })
    },
  })
}
