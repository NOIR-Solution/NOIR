export type WebhookSubscriptionStatus = 'Active' | 'Inactive' | 'Suspended'
export type WebhookDeliveryStatus = 'Pending' | 'Succeeded' | 'Failed' | 'Retrying' | 'Exhausted'

export interface WebhookSubscriptionDto {
  id: string
  name: string
  url: string
  secret: string
  eventPatterns: string
  isActive: boolean
  maxRetries: number
  timeoutSeconds: number
  description: string | null
  customHeaders: string | null
  lastDeliveryAt: string | null
  status: WebhookSubscriptionStatus
  totalDeliveries: number
  successfulDeliveries: number
  failedDeliveries: number
  createdAt: string
  modifiedAt: string | null
}

export interface WebhookSubscriptionSummaryDto {
  id: string
  name: string
  url: string
  eventPatterns: string
  isActive: boolean
  status: WebhookSubscriptionStatus
  lastDeliveryAt: string | null
  totalDeliveries: number
  successfulDeliveries: number
  failedDeliveries: number
}

export interface WebhookDeliveryLogDto {
  id: string
  eventType: string
  eventId: string
  requestUrl: string
  requestBody: string
  requestHeaders: string | null
  responseBody: string | null
  responseStatusCode: number | null
  responseHeaders: string | null
  status: WebhookDeliveryStatus
  attemptNumber: number
  nextRetryAt: string | null
  errorMessage: string | null
  durationMs: number | null
  createdAt: string
}

export interface WebhookEventTypeDto {
  eventType: string
  category: string
  description: string
}

export interface WebhookSecretDto {
  secret: string
}

export interface WebhookPagedResult {
  items: WebhookSubscriptionSummaryDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface WebhookDeliveryPagedResult {
  items: WebhookDeliveryLogDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface CreateWebhookRequest {
  name: string
  url: string
  eventPatterns: string
  description?: string
  customHeaders?: string
  maxRetries?: number
  timeoutSeconds?: number
}

export interface UpdateWebhookRequest extends CreateWebhookRequest {
  id: string
}
