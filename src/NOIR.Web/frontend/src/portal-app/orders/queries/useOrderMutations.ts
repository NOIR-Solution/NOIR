import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { OrderDto } from '@/types/order'
import {
  createOrder,
  confirmOrder,
  shipOrder,
  deliverOrder,
  completeOrder,
  cancelOrder,
  returnOrder,
} from '@/services/orders'
import { orderKeys } from './queryKeys'

/**
 * Targeted cache invalidation for order state transitions.
 * Updates the detail cache immediately with server response,
 * then invalidates only the list queries for status badge updates.
 */
const onOrderMutationSuccess = (queryClient: ReturnType<typeof useQueryClient>) =>
  (updatedOrder: OrderDto) => {
    queryClient.setQueryData(orderKeys.detail(updatedOrder.id), updatedOrder)
    queryClient.invalidateQueries({ queryKey: orderKeys.lists() })
  }

export const useCreateOrderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() })
    },
  })
}

export const useConfirmOrderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => confirmOrder(id),
    onSuccess: onOrderMutationSuccess(queryClient),
  })
}

export const useShipOrderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, trackingNumber, carrier }: { id: string; trackingNumber?: string; carrier?: string }) =>
      shipOrder(id, trackingNumber, carrier),
    onSuccess: onOrderMutationSuccess(queryClient),
  })
}

export const useDeliverOrderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deliverOrder(id),
    onSuccess: onOrderMutationSuccess(queryClient),
  })
}

export const useCompleteOrderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => completeOrder(id),
    onSuccess: onOrderMutationSuccess(queryClient),
  })
}

export const useCancelOrderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) => cancelOrder(id, reason),
    onSuccess: onOrderMutationSuccess(queryClient),
  })
}

export const useReturnOrderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => returnOrder(id, reason),
    onSuccess: onOrderMutationSuccess(queryClient),
  })
}
