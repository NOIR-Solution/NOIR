/**
 * Payment Gateway API Service
 *
 * Provides methods for managing payment gateway configurations.
 */
import { apiClient } from './apiClient'
import type {
  PaymentGateway,
  GatewaySchemas,
  ConfigureGatewayRequest,
  UpdateGatewayRequest,
  TestConnectionResult,
} from '@/types'

// ============================================================================
// Gateway Management
// ============================================================================

/**
 * Fetch all payment gateways for the tenant
 */
export async function getPaymentGateways(): Promise<PaymentGateway[]> {
  return apiClient<PaymentGateway[]>('/payment-gateways')
}

/**
 * Fetch a single payment gateway by ID
 */
export async function getPaymentGateway(id: string): Promise<PaymentGateway> {
  return apiClient<PaymentGateway>(`/payment-gateways/${id}`)
}

/**
 * Fetch gateway credential schemas for all providers
 */
export async function getGatewaySchemas(): Promise<GatewaySchemas> {
  return apiClient<GatewaySchemas>('/payment-gateways/schemas')
}

/**
 * Configure a new payment gateway
 */
export async function configureGateway(
  request: ConfigureGatewayRequest
): Promise<PaymentGateway> {
  return apiClient<PaymentGateway>('/payment-gateways', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing payment gateway
 */
export async function updateGateway(
  id: string,
  request: UpdateGatewayRequest
): Promise<PaymentGateway> {
  return apiClient<PaymentGateway>(`/payment-gateways/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Toggle gateway active status
 */
export async function toggleGatewayActive(
  id: string,
  isActive: boolean
): Promise<PaymentGateway> {
  return updateGateway(id, { isActive })
}

/**
 * Test gateway connection
 */
export async function testGatewayConnection(
  id: string
): Promise<TestConnectionResult> {
  return apiClient<TestConnectionResult>(`/payment-gateways/${id}/test`, {
    method: 'POST',
  })
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Get provider display name from schemas
 */
export function getProviderDisplayName(
  schemas: GatewaySchemas | null,
  provider: string
): string {
  if (!schemas?.schemas[provider]) {
    // Fallback to capitalized provider name
    return provider.charAt(0).toUpperCase() + provider.slice(1)
  }
  return schemas.schemas[provider].displayName
}

/**
 * Get provider icon URL
 */
export function getProviderIconUrl(
  schemas: GatewaySchemas | null,
  provider: string
): string {
  if (!schemas?.schemas[provider]) {
    return '/images/gateways/default.svg'
  }
  return schemas.schemas[provider].iconUrl
}

/**
 * Check if a gateway is fully configured (has credentials)
 */
export function isGatewayConfigured(gateway: PaymentGateway | null): boolean {
  return gateway?.hasCredentials ?? false
}

/**
 * Get gateway status label
 */
export function getGatewayStatusLabel(
  gateway: PaymentGateway | null
): 'not-configured' | 'configured' | 'active' {
  if (!gateway || !gateway.hasCredentials) {
    return 'not-configured'
  }
  return gateway.isActive ? 'active' : 'configured'
}

/**
 * Format health check time
 */
export function formatLastHealthCheck(lastHealthCheck: string | null): string {
  if (!lastHealthCheck) return 'Never'

  const date = new Date(lastHealthCheck)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`

  const diffHours = Math.floor(diffMins / 60)
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`

  const diffDays = Math.floor(diffHours / 24)
  return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`
}
