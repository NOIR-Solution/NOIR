import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { OrderDto, OrderNoteDto } from '@/types/order'
import {
  createOrder,
  confirmOrder,
  shipOrder,
  deliverOrder,
  completeOrder,
  cancelOrder,
  returnOrder,
  addOrderNote,
  deleteOrderNote,
  manualCreateOrder,
  type ManualCreateOrderRequest,
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

export const useAddOrderNoteMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ orderId, content }: { orderId: string; content: string }) =>
      addOrderNote(orderId, content),
    onSuccess: (newNote, variables) => {
      queryClient.setQueryData<OrderNoteDto[]>(
        orderKeys.notes(variables.orderId),
        (old) => [newNote, ...(old ?? [])],
      )
    },
  })
}

export const useManualCreateOrderMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: ManualCreateOrderRequest) => manualCreateOrder(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() })
    },
  })
}

export const useDeleteOrderNoteMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ orderId, noteId }: { orderId: string; noteId: string }) =>
      deleteOrderNote(orderId, noteId),
    onMutate: async ({ orderId, noteId }) => {
      const key = orderKeys.notes(orderId)
      await queryClient.cancelQueries({ queryKey: key })
      const prev = queryClient.getQueryData<OrderNoteDto[]>(key)
      queryClient.setQueryData<OrderNoteDto[]>(key, (old) => old?.filter((n) => n.id !== noteId) ?? [])
      return { prev, key }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.prev !== undefined) queryClient.setQueryData(ctx.key, ctx.prev)
    },
    onSettled: (_data, _err, variables) => {
      queryClient.invalidateQueries({ queryKey: orderKeys.notes(variables.orderId) })
    },
  })
}
