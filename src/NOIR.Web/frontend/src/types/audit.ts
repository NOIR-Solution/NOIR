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

// Audit statistics
export interface AuditStatsUpdate {
  totalEvents: number
  eventsLast24Hours: number
  eventsLastHour: number
  httpRequestCount: number
  handlerCount: number
  entityChangeCount: number
  topEntities: EntityCount[]
  topOperations: OperationCount[]
  lastUpdated: string
}

export interface EntityCount {
  entityType: string
  count: number
}

export interface OperationCount {
  operation: string
  count: number
}

// Detailed stats for charts
export interface AuditDetailedStats {
  hourlyBreakdown: HourlyStats[]
  entityTypeBreakdown: EntityTypeStats[]
  operationBreakdown: OperationStats[]
  topUsers: UserActivityStats[]
  periodStart: string
  periodEnd: string
}

export interface HourlyStats {
  hour: string
  httpRequests: number
  handlers: number
  entityChanges: number
  total: number
}

export interface EntityTypeStats {
  entityType: string
  createCount: number
  updateCount: number
  deleteCount: number
  readCount: number
  total: number
}

export interface OperationStats {
  operation: string
  count: number
  percentage: number
}

export interface UserActivityStats {
  userId: string
  userName: string | null
  actionCount: number
  lastActivity: string
}

// Search
export type AuditSearchScope = 'All' | 'HttpRequests' | 'Handlers' | 'EntityChanges'

export interface AuditSearchResult {
  items: AuditSearchItem[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface AuditSearchItem {
  id: string
  eventType: AuditEventType
  entityType: string
  entityId: string | null
  operation: string
  userId: string | null
  userName: string | null
  timestamp: string
  highlights: string[]
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
  groups: string[]
  initialStats: AuditStatsUpdate
}
