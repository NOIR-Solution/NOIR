import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  configureShippingProvider,
  updateShippingProvider,
  activateShippingProvider,
  deactivateShippingProvider,
  cancelShippingOrder,
} from '@/services/shipping'
import type {
  ConfigureShippingProviderRequest,
  UpdateShippingProviderRequest,
} from '@/types/shipping'
import { shippingKeys } from './queryKeys'

export const useConfigureProviderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: ConfigureShippingProviderRequest) => configureShippingProvider(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: shippingKeys.providers() })
    },
  })
}

export const useUpdateProviderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateShippingProviderRequest }) =>
      updateShippingProvider(id, request),
    onSuccess: (updatedProvider) => {
      queryClient.setQueryData(shippingKeys.provider(updatedProvider.id), updatedProvider)
      queryClient.invalidateQueries({ queryKey: shippingKeys.providers() })
    },
  })
}

export const useActivateProviderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => activateShippingProvider(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: shippingKeys.providers() })
    },
  })
}

export const useDeactivateProviderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deactivateShippingProvider(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: shippingKeys.providers() })
    },
  })
}

export const useCancelShippingOrderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ trackingNumber, reason }: { trackingNumber: string; reason?: string }) =>
      cancelShippingOrder(trackingNumber, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: shippingKeys.orders() })
    },
  })
}
