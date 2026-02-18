/**
 * Inventory API Service
 *
 * Provides methods for inventory receipts and stock movements.
 */
import { apiClient } from './apiClient'
import type {
  InventoryReceiptDto,
  InventoryReceiptPagedResult,
  InventoryMovement,
  StockHistoryPagedResult,
  CreateInventoryReceiptRequest,
  GetInventoryReceiptsParams,
} from '@/types/inventory'

export const getInventoryReceipts = async (
  params: GetInventoryReceiptsParams = {}
): Promise<InventoryReceiptPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.type) queryParams.append('type', params.type)
  if (params.status) queryParams.append('status', params.status)

  const query = queryParams.toString()
  return apiClient<InventoryReceiptPagedResult>(`/inventory/receipts${query ? `?${query}` : ''}`)
}

export const getInventoryReceiptById = async (id: string): Promise<InventoryReceiptDto> => {
  return apiClient<InventoryReceiptDto>(`/inventory/receipts/${id}`)
}

export const createInventoryReceipt = async (
  request: CreateInventoryReceiptRequest
): Promise<InventoryReceiptDto> => {
  return apiClient<InventoryReceiptDto>('/inventory/receipts', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const confirmInventoryReceipt = async (id: string): Promise<InventoryReceiptDto> => {
  return apiClient<InventoryReceiptDto>(`/inventory/receipts/${id}/confirm`, { method: 'POST' })
}

export const cancelInventoryReceipt = async (
  id: string,
  reason?: string
): Promise<InventoryReceiptDto> => {
  return apiClient<InventoryReceiptDto>(`/inventory/receipts/${id}/cancel`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
}

export const createStockMovement = async (request: {
  productVariantId: string
  productId: string
  movementType: string
  quantity: number
  notes?: string
}): Promise<InventoryMovement> => {
  return apiClient<InventoryMovement>('/inventory/movements', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const getStockHistory = async (
  productId: string,
  variantId: string,
  page = 1,
  pageSize = 20
): Promise<StockHistoryPagedResult> => {
  const queryParams = new URLSearchParams()
  queryParams.append('page', page.toString())
  queryParams.append('pageSize', pageSize.toString())

  return apiClient<StockHistoryPagedResult>(
    `/inventory/products/${productId}/variants/${variantId}/history?${queryParams.toString()}`
  )
}
