/**
 * Audit API Service
 * Provides REST API methods for audit features
 */
import { apiClient } from './apiClient'
import type {
  AuditStatsUpdate,
  AuditDetailedStats,
  AuditSearchResult,
  AuditRetentionPolicy,
  CompliancePreset,
  CreateRetentionPolicyRequest,
  UpdateRetentionPolicyRequest,
} from '@/types'

/**
 * Search audit logs using full-text search
 */
export async function searchAuditLogs(params: {
  query: string
  entityType?: string
  fromDate?: string
  toDate?: string
  pageNumber?: number
  pageSize?: number
}): Promise<AuditSearchResult> {
  const searchParams = new URLSearchParams()
  searchParams.set('query', params.query)
  if (params.entityType) searchParams.set('entityType', params.entityType)
  if (params.fromDate) searchParams.set('fromDate', params.fromDate)
  if (params.toDate) searchParams.set('toDate', params.toDate)
  if (params.pageNumber) searchParams.set('pageNumber', params.pageNumber.toString())
  if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString())

  return apiClient<AuditSearchResult>(`/audit/search?${searchParams.toString()}`)
}

/**
 * Get current audit statistics for dashboard
 */
export async function getAuditStats(): Promise<AuditStatsUpdate> {
  return apiClient<AuditStatsUpdate>('/audit/stats')
}

/**
 * Get detailed audit statistics for a date range
 */
export async function getDetailedAuditStats(
  fromDate: string,
  toDate: string
): Promise<AuditDetailedStats> {
  const params = new URLSearchParams()
  params.set('fromDate', fromDate)
  params.set('toDate', toDate)
  return apiClient<AuditDetailedStats>(`/audit/stats/detailed?${params.toString()}`)
}

/**
 * Get all retention policies
 */
export async function getRetentionPolicies(): Promise<AuditRetentionPolicy[]> {
  return apiClient<AuditRetentionPolicy[]>('/audit/policies')
}

/**
 * Get a single retention policy by ID
 */
export async function getRetentionPolicy(id: string): Promise<AuditRetentionPolicy> {
  return apiClient<AuditRetentionPolicy>(`/audit/policies/${id}`)
}

/**
 * Get available compliance presets
 */
export async function getCompliancePresets(): Promise<CompliancePreset[]> {
  return apiClient<CompliancePreset[]>('/audit/policies/presets')
}

/**
 * Create a new retention policy
 */
export async function createRetentionPolicy(
  data: CreateRetentionPolicyRequest
): Promise<AuditRetentionPolicy> {
  return apiClient<AuditRetentionPolicy>('/audit/policies', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

/**
 * Update an existing retention policy
 */
export async function updateRetentionPolicy(
  id: string,
  data: UpdateRetentionPolicyRequest
): Promise<AuditRetentionPolicy> {
  return apiClient<AuditRetentionPolicy>(`/audit/policies/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

/**
 * Delete a retention policy
 */
export async function deleteRetentionPolicy(id: string): Promise<void> {
  return apiClient<void>(`/audit/policies/${id}`, {
    method: 'DELETE',
  })
}
