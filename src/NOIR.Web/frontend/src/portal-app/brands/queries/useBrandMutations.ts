import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createBrand, updateBrand, deleteBrand } from '@/services/brands'
import type { CreateBrandRequest, UpdateBrandRequest } from '@/types/brand'
import { brandKeys } from './queryKeys'
import { optimisticListDelete } from '@/hooks/useOptimisticMutation'

export const useCreateBrandMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateBrandRequest) => createBrand(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: brandKeys.all })
    },
  })
}

export const useUpdateBrandMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateBrandRequest }) => updateBrand(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: brandKeys.all })
    },
  })
}

export const useDeleteBrandMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteBrand(id),
    ...optimisticListDelete(queryClient, brandKeys.lists(), brandKeys.all),
  })
}
