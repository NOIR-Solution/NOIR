/**
 * Shipping API Service
 *
 * Provides methods for managing shipping providers, orders, and tracking.
 */
import { apiClient } from './apiClient'
import type {
  ShippingProviderDto,
  ShippingProviderSchemas,
  ShippingOrderDto,
  TrackingInfoDto,
  ConfigureShippingProviderRequest,
  UpdateShippingProviderRequest,
} from '@/types/shipping'

// ============ Shipping Providers ============

export const getShippingProviders = async (): Promise<ShippingProviderDto[]> => {
  return apiClient<ShippingProviderDto[]>('/shipping-providers')
}

export const getShippingProviderSchemas = async (): Promise<ShippingProviderSchemas> => {
  return apiClient<ShippingProviderSchemas>('/shipping-providers/schemas')
}

export const getShippingProviderById = async (id: string): Promise<ShippingProviderDto> => {
  return apiClient<ShippingProviderDto>(`/shipping-providers/${id}`)
}

export const configureShippingProvider = async (
  request: ConfigureShippingProviderRequest
): Promise<ShippingProviderDto> => {
  return apiClient<ShippingProviderDto>('/shipping-providers', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateShippingProvider = async (
  id: string,
  request: UpdateShippingProviderRequest
): Promise<ShippingProviderDto> => {
  return apiClient<ShippingProviderDto>(`/shipping-providers/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const activateShippingProvider = async (id: string): Promise<ShippingProviderDto> => {
  return apiClient<ShippingProviderDto>(`/shipping-providers/${id}/activate`, { method: 'POST' })
}

export const deactivateShippingProvider = async (id: string): Promise<ShippingProviderDto> => {
  return apiClient<ShippingProviderDto>(`/shipping-providers/${id}/deactivate`, { method: 'POST' })
}

// ============ Shipping Orders ============

export const getShippingOrderByTracking = async (trackingNumber: string): Promise<ShippingOrderDto> => {
  return apiClient<ShippingOrderDto>(`/shipping/orders/${trackingNumber}`)
}

export const getShippingOrderByOrderId = async (orderId: string): Promise<ShippingOrderDto> => {
  return apiClient<ShippingOrderDto>(`/shipping/orders/by-order/${orderId}`)
}

export const cancelShippingOrder = async (trackingNumber: string, reason?: string): Promise<void> => {
  const params = reason ? `?reason=${encodeURIComponent(reason)}` : ''
  return apiClient<void>(`/shipping/orders/${trackingNumber}${params}`, { method: 'DELETE' })
}

// ============ Tracking ============

export const getShippingTracking = async (trackingNumber: string): Promise<TrackingInfoDto> => {
  return apiClient<TrackingInfoDto>(`/shipping/tracking/${trackingNumber}`)
}
