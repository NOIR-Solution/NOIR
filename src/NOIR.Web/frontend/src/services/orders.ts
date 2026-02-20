/**
 * Orders API Service
 *
 * Provides methods for managing orders and order lifecycle transitions.
 */
import { apiClient } from './apiClient'
import type {
  OrderDto,
  OrderNoteDto,
  OrderPagedResult,
  OrderStatus,
  CreateOrderRequest,
} from '@/types/order'

export interface GetOrdersParams {
  page?: number
  pageSize?: number
  status?: OrderStatus
  customerEmail?: string
  fromDate?: string
  toDate?: string
}

export const getOrders = async (params: GetOrdersParams = {}): Promise<OrderPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.status) queryParams.append('status', params.status)
  if (params.customerEmail) queryParams.append('customerEmail', params.customerEmail)
  if (params.fromDate) queryParams.append('fromDate', params.fromDate)
  if (params.toDate) queryParams.append('toDate', params.toDate)

  const query = queryParams.toString()
  return apiClient<OrderPagedResult>(`/orders${query ? `?${query}` : ''}`)
}

export const getOrderById = async (id: string): Promise<OrderDto> => {
  return apiClient<OrderDto>(`/orders/${id}`)
}

export const createOrder = async (request: CreateOrderRequest): Promise<OrderDto> => {
  return apiClient<OrderDto>('/orders', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const confirmOrder = async (id: string): Promise<OrderDto> => {
  return apiClient<OrderDto>(`/orders/${id}/confirm`, { method: 'POST' })
}

export const shipOrder = async (id: string, trackingNumber?: string, carrier?: string): Promise<OrderDto> => {
  return apiClient<OrderDto>(`/orders/${id}/ship`, {
    method: 'POST',
    body: JSON.stringify({ trackingNumber, shippingCarrier: carrier }),
  })
}

export const deliverOrder = async (id: string): Promise<OrderDto> => {
  return apiClient<OrderDto>(`/orders/${id}/deliver`, { method: 'POST' })
}

export const completeOrder = async (id: string): Promise<OrderDto> => {
  return apiClient<OrderDto>(`/orders/${id}/complete`, { method: 'POST' })
}

export const cancelOrder = async (id: string, reason?: string): Promise<OrderDto> => {
  return apiClient<OrderDto>(`/orders/${id}/cancel`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
}

export const returnOrder = async (id: string, reason: string): Promise<OrderDto> => {
  return apiClient<OrderDto>(`/orders/${id}/return`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
}

// --- Order Notes ---

export const getOrderNotes = async (orderId: string): Promise<OrderNoteDto[]> => {
  return apiClient<OrderNoteDto[]>(`/orders/${orderId}/notes`)
}

export const addOrderNote = async (orderId: string, content: string): Promise<OrderNoteDto> => {
  return apiClient<OrderNoteDto>(`/orders/${orderId}/notes`, {
    method: 'POST',
    body: JSON.stringify({ content }),
  })
}

export const deleteOrderNote = async (orderId: string, noteId: string): Promise<OrderNoteDto> => {
  return apiClient<OrderNoteDto>(`/orders/${orderId}/notes/${noteId}`, { method: 'DELETE' })
}

// --- Manual Create Order ---

export interface ManualOrderItemRequest {
  productVariantId: string
  quantity: number
  unitPrice?: number
  discountAmount?: number
}

export interface ManualCreateOrderRequest {
  customerEmail: string
  customerName?: string
  customerPhone?: string
  customerId?: string
  items: ManualOrderItemRequest[]
  shippingAddress?: ManualCreateAddressRequest
  billingAddress?: ManualCreateAddressRequest
  shippingMethod?: string
  shippingAmount?: number
  couponCode?: string
  discountAmount?: number
  taxAmount?: number
  customerNotes?: string
  internalNotes?: string
  paymentMethod?: string
  initialPaymentStatus?: string
  currency?: string
}

export interface ManualCreateAddressRequest {
  fullName: string
  phone: string
  addressLine1: string
  addressLine2?: string
  ward: string
  district: string
  province: string
  country?: string
  postalCode?: string
}

export const manualCreateOrder = async (request: ManualCreateOrderRequest): Promise<OrderDto> => {
  return apiClient<OrderDto>('/orders/manual', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

// --- Product Variant Search (for manual order creation) ---

export interface ProductVariantLookupDto {
  id: string
  productId: string
  productName: string
  variantName: string
  sku?: string
  price: number
  stockQuantity: number
  imageUrl?: string
}

export interface ProductVariantSearchResult {
  items: ProductVariantLookupDto[]
  totalCount: number
  pageIndex: number
  pageSize: number
}

export const searchProductVariants = async (params: { search?: string; categoryId?: string; page?: number; pageSize?: number }): Promise<ProductVariantSearchResult> => {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)
  if (params.categoryId) queryParams.append('categoryId', params.categoryId)
  if (params.page) queryParams.append('page', params.page.toString())
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString())
  const query = queryParams.toString()
  return apiClient<ProductVariantSearchResult>(`/products/variants/search${query ? `?${query}` : ''}`)
}
