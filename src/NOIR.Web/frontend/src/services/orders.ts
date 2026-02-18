/**
 * Orders API Service
 *
 * Provides methods for managing orders and order lifecycle transitions.
 */
import { apiClient } from './apiClient'
import type {
  OrderDto,
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
