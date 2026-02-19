import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createCustomerGroup, updateCustomerGroup, deleteCustomerGroup } from '@/services/customerGroups'
import type { CreateCustomerGroupRequest, UpdateCustomerGroupRequest } from '@/types/customerGroup'
import { customerGroupKeys } from './queryKeys'
import { optimisticListDelete } from '@/hooks/useOptimisticMutation'

export const useCreateCustomerGroupMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateCustomerGroupRequest) => createCustomerGroup(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customerGroupKeys.all })
    },
  })
}

export const useUpdateCustomerGroupMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCustomerGroupRequest }) => updateCustomerGroup(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customerGroupKeys.all })
    },
  })
}

export const useDeleteCustomerGroupMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteCustomerGroup(id),
    ...optimisticListDelete(queryClient, customerGroupKeys.lists(), customerGroupKeys.all),
  })
}
