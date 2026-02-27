/**
 * Webhooks API Service
 *
 * Provides methods for managing webhook subscriptions, delivery logs, and event types.
 */
import { apiClient } from './apiClient'
import type {
  WebhookSubscriptionDto,
  WebhookSubscriptionSummaryDto,
  WebhookEventTypeDto,
  WebhookSecretDto,
  WebhookPagedResult,
  WebhookDeliveryPagedResult,
  CreateWebhookRequest,
  UpdateWebhookRequest,
  WebhookSubscriptionStatus,
  WebhookDeliveryStatus,
} from '@/types/webhook'

// ============================================================================
// Query Parameters
// ============================================================================

export interface GetWebhooksParams {
  page?: number
  pageSize?: number
  search?: string
  status?: WebhookSubscriptionStatus
}

export interface GetWebhookDeliveriesParams {
  page?: number
  pageSize?: number
  status?: WebhookDeliveryStatus
}

// ============================================================================
// Webhook Subscriptions
// ============================================================================

/**
 * Fetch paginated list of webhook subscriptions with optional filters
 */
export const getWebhooks = async (params: GetWebhooksParams = {}): Promise<WebhookPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.search) queryParams.append('search', params.search)
  if (params.status) queryParams.append('status', params.status)

  const query = queryParams.toString()
  return apiClient<WebhookPagedResult>(`/webhooks${query ? `?${query}` : ''}`)
}

/**
 * Fetch a single webhook subscription by ID
 */
export const getWebhookById = async (id: string): Promise<WebhookSubscriptionDto> => {
  return apiClient<WebhookSubscriptionDto>(`/webhooks/${id}`)
}

/**
 * Create a new webhook subscription
 */
export const createWebhook = async (request: CreateWebhookRequest): Promise<WebhookSubscriptionDto> => {
  return apiClient<WebhookSubscriptionDto>('/webhooks', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing webhook subscription
 */
export const updateWebhook = async (id: string, request: UpdateWebhookRequest): Promise<WebhookSubscriptionDto> => {
  return apiClient<WebhookSubscriptionDto>(`/webhooks/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a webhook subscription (soft delete)
 */
export const deleteWebhook = async (id: string): Promise<void> => {
  return apiClient<void>(`/webhooks/${id}`, {
    method: 'DELETE',
  })
}

/**
 * Activate a webhook subscription
 */
export const activateWebhook = async (id: string): Promise<WebhookSubscriptionSummaryDto> => {
  return apiClient<WebhookSubscriptionSummaryDto>(`/webhooks/${id}/activate`, {
    method: 'POST',
  })
}

/**
 * Deactivate a webhook subscription
 */
export const deactivateWebhook = async (id: string): Promise<WebhookSubscriptionSummaryDto> => {
  return apiClient<WebhookSubscriptionSummaryDto>(`/webhooks/${id}/deactivate`, {
    method: 'POST',
  })
}

/**
 * Send a test delivery to a webhook subscription
 */
export const testWebhook = async (id: string): Promise<void> => {
  return apiClient<void>(`/webhooks/${id}/test`, {
    method: 'POST',
  })
}

/**
 * Rotate the HMAC secret for a webhook subscription
 */
export const rotateWebhookSecret = async (id: string): Promise<WebhookSecretDto> => {
  return apiClient<WebhookSecretDto>(`/webhooks/${id}/rotate-secret`, {
    method: 'POST',
  })
}

// ============================================================================
// Delivery Logs
// ============================================================================

/**
 * Fetch paginated delivery logs for a webhook subscription
 */
export const getWebhookDeliveries = async (
  id: string,
  params: GetWebhookDeliveriesParams = {}
): Promise<WebhookDeliveryPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.status) queryParams.append('status', params.status)

  const query = queryParams.toString()
  return apiClient<WebhookDeliveryPagedResult>(`/webhooks/${id}/deliveries${query ? `?${query}` : ''}`)
}

// ============================================================================
// Event Types
// ============================================================================

/**
 * Fetch all available webhook event types
 */
export const getWebhookEventTypes = async (): Promise<WebhookEventTypeDto[]> => {
  return apiClient<WebhookEventTypeDto[]>('/webhooks/event-types')
}
