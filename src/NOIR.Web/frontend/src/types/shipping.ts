/**
 * Shipping types matching backend DTOs.
 * Maps to: src/NOIR.Application/Features/Shipping/DTOs/
 */

// Enums (serialized as strings from backend)

export type ShippingProviderCode =
  | 'GHTK'
  | 'GHN'
  | 'JTExpress'
  | 'ViettelPost'
  | 'NinjaVan'
  | 'VNPost'
  | 'BestExpress'
  | 'Custom'

export type ShippingStatus =
  | 'Draft'
  | 'AwaitingPickup'
  | 'PickedUp'
  | 'InTransit'
  | 'OutForDelivery'
  | 'Delivered'
  | 'DeliveryFailed'
  | 'Cancelled'
  | 'Returning'
  | 'Returned'

export type GatewayEnvironment = 'Sandbox' | 'Production'

export type ShippingProviderHealthStatus = 'Unknown' | 'Healthy' | 'Degraded' | 'Unhealthy'

// Provider DTOs

export interface ShippingProviderDto {
  id: string
  providerCode: ShippingProviderCode
  providerName: string
  displayName: string
  isActive: boolean
  sortOrder: number
  environment: GatewayEnvironment
  hasCredentials: boolean
  webhookUrl?: string | null
  apiBaseUrl?: string | null
  trackingUrlTemplate?: string | null
  supportedServices: string
  minWeightGrams?: number | null
  maxWeightGrams?: number | null
  minCodAmount?: number | null
  maxCodAmount?: number | null
  supportsCod: boolean
  supportsInsurance: boolean
  lastHealthCheck?: string | null
  healthStatus: ShippingProviderHealthStatus
  createdAt: string
  modifiedAt?: string | null
}

export interface CheckoutShippingProviderDto {
  id: string
  providerCode: ShippingProviderCode
  providerName: string
  displayName: string
  sortOrder: number
  supportedServices: string
  supportsCod: boolean
  supportsInsurance: boolean
}

// Shipping Order DTOs

export interface ShippingOrderDto {
  id: string
  orderId: string
  providerCode: ShippingProviderCode
  providerName: string
  providerOrderId?: string | null
  trackingNumber: string
  serviceTypeCode: string
  serviceTypeName: string
  status: ShippingStatus
  baseRate: number
  codFee: number
  insuranceFee: number
  totalShippingFee: number
  codAmount?: number | null
  pickupAddressJson: string
  deliveryAddressJson: string
  labelUrl?: string | null
  trackingUrl?: string | null
  estimatedDeliveryDate?: string | null
  actualDeliveryDate?: string | null
  createdAt: string
  modifiedAt?: string | null
}

export interface ShippingOrderSummaryDto {
  id: string
  orderId: string
  trackingNumber: string
  providerCode: ShippingProviderCode
  providerName: string
  status: ShippingStatus
  totalShippingFee: number
  estimatedDeliveryDate?: string | null
  createdAt: string
}

// Tracking DTOs

export interface TrackingEventDto {
  eventType: string
  status: ShippingStatus
  description: string
  location?: string | null
  eventDate: string
}

export interface TrackingInfoDto {
  trackingNumber: string
  providerCode: ShippingProviderCode
  providerName: string
  currentStatus: ShippingStatus
  statusDescription: string
  currentLocation?: string | null
  estimatedDeliveryDate?: string | null
  actualDeliveryDate?: string | null
  events: TrackingEventDto[]
  trackingUrl?: string | null
}

// Rate DTOs

export interface ShippingRateDto {
  providerCode: ShippingProviderCode
  providerName: string
  serviceTypeCode: string
  serviceTypeName: string
  baseRate: number
  codFee: number
  insuranceFee: number
  totalRate: number
  estimatedDaysMin: number
  estimatedDaysMax: number
  currency: string
  notes?: string | null
}

export interface ShippingRatesResponse {
  rates: ShippingRateDto[]
  recommendedRate?: ShippingRateDto | null
  message?: string | null
}

// Request types

export interface ConfigureShippingProviderRequest {
  providerCode: ShippingProviderCode
  displayName: string
  environment: GatewayEnvironment
  credentials: Record<string, string>
  supportedServices: string[]
  sortOrder: number
  isActive: boolean
  supportsCod?: boolean
  supportsInsurance?: boolean
  apiBaseUrl?: string | null
  trackingUrlTemplate?: string | null
}

export interface UpdateShippingProviderRequest {
  displayName?: string | null
  environment?: GatewayEnvironment | null
  credentials?: Record<string, string> | null
  supportedServices?: string[] | null
  sortOrder?: number | null
  isActive?: boolean | null
  supportsCod?: boolean | null
  supportsInsurance?: boolean | null
  apiBaseUrl?: string | null
  trackingUrlTemplate?: string | null
  minWeightGrams?: number | null
  maxWeightGrams?: number | null
  minCodAmount?: number | null
  maxCodAmount?: number | null
}
