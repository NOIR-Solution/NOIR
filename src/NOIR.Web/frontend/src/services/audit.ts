/**
 * Audit API Service
 *
 * Provides API calls for Activity Timeline features.
 */
import { apiClient } from './apiClient'
import type { PaginatedResponse } from '@/types'

// ============================================================================
// Types
// ============================================================================

export interface FieldChange {
  fieldName: string
  oldValue: unknown
  newValue: unknown
  operation: 'Added' | 'Modified' | 'Removed'
}

// Activity Timeline Types
export interface ActivityTimelineEntry {
  id: string
  timestamp: string
  userEmail: string | null
  userId: string | null
  displayContext: string
  operationType: string
  actionDescription: string | null
  targetDisplayName: string | null
  targetDtoType: string | null
  targetDtoId: string | null
  isSuccess: boolean
  durationMs: number | null
  entityChangeCount: number
  correlationId: string | null
  handlerName: string | null
}

export interface HttpRequestDetails {
  id: string
  method: string
  path: string
  statusCode: number
  queryString: string | null
  clientIpAddress: string | null
  userAgent: string | null
  requestTime: string
  durationMs: number | null
}

export interface EntityChange {
  id: string
  entityType: string
  entityId: string
  operation: string
  version: number
  timestamp: string
  changes: FieldChange[]
}

export interface ActivityDetails {
  entry: ActivityTimelineEntry
  inputParameters: string | null
  outputResult: string | null
  dtoDiff: string | null
  errorMessage: string | null
  httpRequest: HttpRequestDetails | null
  entityChanges: EntityChange[]
}

// ============================================================================
// Activity Timeline API Functions
// ============================================================================

/**
 * Search the activity timeline with filtering and pagination
 */
export async function searchActivityTimeline(params: {
  pageContext?: string
  operationType?: string
  userId?: string
  targetId?: string
  correlationId?: string
  searchTerm?: string
  fromDate?: string
  toDate?: string
  onlyFailed?: boolean
  page?: number
  pageSize?: number
}): Promise<PaginatedResponse<ActivityTimelineEntry>> {
  const searchParams = new URLSearchParams()

  if (params.pageContext) searchParams.set('pageContext', params.pageContext)
  if (params.operationType) searchParams.set('operationType', params.operationType)
  if (params.userId) searchParams.set('userId', params.userId)
  if (params.targetId) searchParams.set('targetId', params.targetId)
  if (params.correlationId) searchParams.set('correlationId', params.correlationId)
  if (params.searchTerm) searchParams.set('searchTerm', params.searchTerm)
  if (params.fromDate) searchParams.set('fromDate', params.fromDate)
  if (params.toDate) searchParams.set('toDate', params.toDate)
  if (params.onlyFailed !== undefined) searchParams.set('onlyFailed', params.onlyFailed.toString())
  if (params.page) searchParams.set('page', params.page.toString())
  if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString())

  const query = searchParams.toString()
  return apiClient<PaginatedResponse<ActivityTimelineEntry>>(
    `/audit/activity-timeline${query ? `?${query}` : ''}`
  )
}

/**
 * Get detailed information about a specific activity entry
 */
export async function getActivityDetails(id: string): Promise<ActivityDetails> {
  return apiClient<ActivityDetails>(`/audit/activity-timeline/${id}`)
}

/**
 * Get list of distinct page contexts for filtering
 */
export async function getPageContexts(): Promise<string[]> {
  return apiClient<string[]>('/audit/page-contexts')
}
