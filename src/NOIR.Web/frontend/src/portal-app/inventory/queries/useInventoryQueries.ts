import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { getInventoryReceipts, getInventoryReceiptById } from '@/services/inventory'
import type { GetInventoryReceiptsParams } from '@/types/inventory'
import { inventoryKeys } from './queryKeys'

export const useInventoryReceiptsQuery = (params: GetInventoryReceiptsParams) =>
  useQuery({
    queryKey: inventoryKeys.receiptList(params),
    queryFn: () => getInventoryReceipts(params),
    placeholderData: keepPreviousData,
  })

export const useInventoryReceiptQuery = (id: string | undefined) =>
  useQuery({
    queryKey: inventoryKeys.receiptDetail(id!),
    queryFn: () => getInventoryReceiptById(id!),
    enabled: !!id,
  })
