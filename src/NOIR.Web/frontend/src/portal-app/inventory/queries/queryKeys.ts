import type { GetInventoryReceiptsParams } from '@/types/inventory'

export const inventoryKeys = {
  all: ['inventory'] as const,
  receipts: () => [...inventoryKeys.all, 'receipts'] as const,
  receiptLists: () => [...inventoryKeys.receipts(), 'list'] as const,
  receiptList: (params: GetInventoryReceiptsParams) => [...inventoryKeys.receiptLists(), params] as const,
  receiptDetails: () => [...inventoryKeys.receipts(), 'detail'] as const,
  receiptDetail: (id: string) => [...inventoryKeys.receiptDetails(), id] as const,
}
