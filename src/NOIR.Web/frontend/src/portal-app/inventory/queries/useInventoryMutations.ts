import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { InventoryReceiptDto } from '@/types/inventory'
import {
  createInventoryReceipt,
  confirmInventoryReceipt,
  cancelInventoryReceipt,
  createStockMovement,
} from '@/services/inventory'
import { inventoryKeys } from './queryKeys'

/**
 * Targeted cache invalidation for inventory receipt mutations.
 * Updates the detail cache immediately with server response,
 * then invalidates only the list queries for status updates.
 */
const onReceiptMutationSuccess = (queryClient: ReturnType<typeof useQueryClient>) =>
  (updatedReceipt: InventoryReceiptDto) => {
    queryClient.setQueryData(inventoryKeys.receiptDetail(updatedReceipt.id), updatedReceipt)
    queryClient.invalidateQueries({ queryKey: inventoryKeys.receiptLists() })
  }

export const useCreateInventoryReceiptMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createInventoryReceipt,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: inventoryKeys.receiptLists() })
    },
  })
}

export const useConfirmInventoryReceiptMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => confirmInventoryReceipt(id),
    onSuccess: onReceiptMutationSuccess(queryClient),
  })
}

export const useCancelInventoryReceiptMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) => cancelInventoryReceipt(id, reason),
    onSuccess: onReceiptMutationSuccess(queryClient),
  })
}

export const useCreateStockMovementMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createStockMovement,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: inventoryKeys.all })
    },
  })
}
