/**
 * Audit-related types for the frontend
 */

// Audit event types
export type AuditEventType = 'HttpRequest' | 'Handler' | 'EntityChange'

// Unified audit event from SignalR
export interface UnifiedAuditEvent {
  eventType: AuditEventType
  entityType: string
  entityId: string | null
  operation: string
  userId: string | null
  userName: string | null
  tenantId: string | null
  timestamp: string
  correlationId: string | null
  details: Record<string, unknown>
}

// Audit statistics (matches backend AuditStatsUpdate record)
export interface AuditStatsUpdate {
  timestamp: string
  todayHttpRequests: number
  todayHandlerExecutions: number
  todayEntityChanges: number
  todayErrors: number
  activeUsers: number
  avgResponseTimeMs: number
  hourlyActivity: HourlyActivityPoint[]
}

// Single data point for hourly activity chart
export interface HourlyActivityPoint {
  hour: number
  httpRequests: number
  entityChanges: number
  errors: number
}

// Detailed stats for charts (matches backend AuditDetailedStats record)
export interface AuditDetailedStats {
  fromDate: string
  toDate: string
  tenantId: string | null
  totalHttpRequests: number
  totalHandlerExecutions: number
  totalEntityChanges: number
  totalErrors: number
  avgResponseTimeMs: number
  dailyActivity: DailyActivitySummary[]
  entityTypeBreakdown: EntityTypeBreakdown[]
  topUsers: UserActivitySummary[]
  topHandlers: HandlerBreakdown[]
}

export interface DailyActivitySummary {
  date: string
  httpRequests: number
  entityChanges: number
  errors: number
  avgResponseTimeMs: number
}

export interface EntityTypeBreakdown {
  entityType: string
  created: number
  updated: number
  deleted: number
  total: number
}

export interface UserActivitySummary {
  userId: string | null
  userEmail: string | null
  requestCount: number
  changeCount: number
}

export interface HandlerBreakdown {
  handlerName: string
  executionCount: number
  successCount: number
  errorCount: number
  avgDurationMs: number
}

// Search
export type AuditSearchScope = 'All' | 'HttpRequests' | 'Handlers' | 'EntityChanges'

// Matches backend AuditSearchHitType enum
export type AuditSearchHitType = 'HttpRequest' | 'Handler' | 'Entity'

// Matches backend AuditSearchResult record
export interface AuditSearchResult {
  searchTerm: string
  totalCount: number
  pageNumber: number
  pageSize: number
  hits: AuditSearchHit[]
}

// Matches backend AuditSearchHit record
export interface AuditSearchHit {
  id: string
  type: AuditSearchHitType
  correlationId: string
  title: string
  snippet: string | null
  timestamp: string
  userId: string | null
  userEmail: string | null
  rank: number
}

// Retention Policies
export interface AuditRetentionPolicy {
  id: string
  tenantId: string | null
  name: string
  description: string | null
  hotStorageDays: number
  warmStorageDays: number
  coldStorageDays: number
  deleteAfterDays: number
  entityTypes: string[] | null
  compliancePreset: string | null
  exportBeforeArchive: boolean
  exportBeforeDelete: boolean
  isActive: boolean
  priority: number
  createdAt: string
  createdBy: string | null
  lastModifiedAt: string | null
  lastModifiedBy: string | null
}

export interface CompliancePreset {
  code: string
  name: string
  hotStorageDays: number
  warmStorageDays: number
  coldStorageDays: number
  deleteAfterDays: number
  description: string
}

export interface CreateRetentionPolicyRequest {
  name: string
  description?: string
  hotStorageDays: number
  warmStorageDays: number
  coldStorageDays: number
  deleteAfterDays: number
  entityTypes?: string[]
  compliancePreset?: string
  exportBeforeArchive: boolean
  exportBeforeDelete: boolean
  priority: number
}

export interface UpdateRetentionPolicyRequest {
  name: string
  description?: string
  hotStorageDays: number
  warmStorageDays: number
  coldStorageDays: number
  deleteAfterDays: number
  entityTypes?: string[]
  compliancePreset?: string
  exportBeforeArchive: boolean
  exportBeforeDelete: boolean
  isActive: boolean
  priority: number
}

// SignalR connection info
export interface AuditConnectionInfo {
  connectionId: string
  tenantId: string | null
  subscribedGroups: string[]
  initialStats: AuditStatsUpdate
}

// Backend SignalR event types (match C# records exactly)
export interface HttpRequestAuditEvent {
  id: string
  correlationId: string
  httpMethod: string
  url: string
  statusCode: number | null
  userId: string | null
  userEmail: string | null
  tenantId: string | null
  ipAddress: string | null
  timestamp: string
  durationMs: number | null
  handlerCount: number
  entityChangeCount: number
}

export interface HandlerAuditEvent {
  id: string
  correlationId: string
  handlerName: string
  operationType: string
  targetDtoType: string | null
  targetDtoId: string | null
  isSuccess: boolean
  errorMessage: string | null
  timestamp: string
  durationMs: number | null
  entityChangeCount: number
}

export interface EntityAuditEvent {
  id: string
  correlationId: string
  entityType: string
  entityId: string
  operation: string
  timestamp: string
  version: number
  changeSummary: string | null
}
