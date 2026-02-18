import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  createCustomer,
  updateCustomer,
  deleteCustomer,
  updateCustomerSegment,
  addLoyaltyPoints,
  redeemLoyaltyPoints,
  addCustomerAddress,
  updateCustomerAddress,
  deleteCustomerAddress,
} from '@/services/customers'
import type {
  CreateCustomerRequest,
  UpdateCustomerRequest,
  UpdateCustomerSegmentRequest,
  LoyaltyPointsRequest,
  CreateCustomerAddressRequest,
  UpdateCustomerAddressRequest,
  CustomerDto,
} from '@/types/customer'
import { customerKeys } from './queryKeys'
import { optimisticListDelete } from '@/hooks/useOptimisticMutation'

const onCustomerMutationSuccess = (queryClient: ReturnType<typeof useQueryClient>) =>
  (updatedCustomer: CustomerDto) => {
    queryClient.setQueryData(customerKeys.detail(updatedCustomer.id), updatedCustomer)
    queryClient.invalidateQueries({ queryKey: customerKeys.lists() })
    queryClient.invalidateQueries({ queryKey: customerKeys.stats() })
  }

export const useCreateCustomerMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateCustomerRequest) => createCustomer(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() })
      queryClient.invalidateQueries({ queryKey: customerKeys.stats() })
    },
  })
}

export const useUpdateCustomerMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCustomerRequest }) => updateCustomer(id, request),
    onSuccess: onCustomerMutationSuccess(queryClient),
  })
}

export const useDeleteCustomerMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteCustomer(id),
    ...optimisticListDelete(queryClient, customerKeys.lists(), customerKeys.all),
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: customerKeys.all })
      queryClient.invalidateQueries({ queryKey: customerKeys.stats() })
    },
  })
}

export const useUpdateCustomerSegmentMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCustomerSegmentRequest }) => updateCustomerSegment(id, request),
    onSuccess: onCustomerMutationSuccess(queryClient),
  })
}

export const useAddLoyaltyPointsMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: LoyaltyPointsRequest }) => addLoyaltyPoints(id, request),
    onSuccess: onCustomerMutationSuccess(queryClient),
  })
}

export const useRedeemLoyaltyPointsMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: LoyaltyPointsRequest }) => redeemLoyaltyPoints(id, request),
    onSuccess: onCustomerMutationSuccess(queryClient),
  })
}

export const useAddCustomerAddressMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ customerId, request }: { customerId: string; request: CreateCustomerAddressRequest }) =>
      addCustomerAddress(customerId, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.detail(variables.customerId) })
    },
  })
}

export const useUpdateCustomerAddressMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ customerId, addressId, request }: { customerId: string; addressId: string; request: UpdateCustomerAddressRequest }) =>
      updateCustomerAddress(customerId, addressId, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.detail(variables.customerId) })
    },
  })
}

export const useDeleteCustomerAddressMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ customerId, addressId }: { customerId: string; addressId: string }) =>
      deleteCustomerAddress(customerId, addressId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.detail(variables.customerId) })
    },
  })
}
