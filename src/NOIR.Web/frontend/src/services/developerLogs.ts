/**
 * Developer Logs API Service
 *
 * Provides API client functions for the Developer Log system:
 * - Log level control
 * - Buffer management
 * - Historical log access
 */
import { apiClient } from './apiClient'

// ==================== Types ====================

export type DevLogLevel = 'Verbose' | 'Debug' | 'Information' | 'Warning' | 'Error' | 'Fatal'

export interface LogEntryDto {
  id: number
  timestamp: string
  level: DevLogLevel
  message: string
  messageTemplate?: string
  sourceContext?: string
  exception?: ExceptionDto
  properties?: Record<string, unknown>
  requestId?: string
  traceId?: string
  userId?: string
  tenantId?: string
}

export interface ExceptionDto {
  type: string
  message: string
  stackTrace?: string
  innerException?: ExceptionDto
}

export interface LogBufferStatsDto {
  totalEntries: number
  maxCapacity: number
  entriesByLevel: Record<string, number>
  memoryUsageBytes: number
  oldestEntry?: string
  newestEntry?: string
}

export interface ErrorClusterDto {
  id: string
  pattern: string
  count: number
  firstSeen: string
  lastSeen: string
  samples: LogEntryDto[]
  severity: string
}

export interface LogLevelResponse {
  level: string
  availableLevels: string[]
}

export interface LogLevelOverridesResponse {
  globalLevel: string
  overrides: LogLevelOverrideDto[]
}

export interface LogLevelOverrideDto {
  sourcePrefix: string
  level: string
}

export interface LogEntriesPagedResponse {
  items: LogEntryDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export type LogSortOrder = 'newest' | 'oldest'

export interface LogSearchParams {
  search?: string
  minLevel?: DevLogLevel
  levels?: DevLogLevel[]
  sources?: string[]
  hasException?: boolean
  requestId?: string
  page?: number
  pageSize?: number
  sortOrder?: LogSortOrder
}

// ==================== Log Level Control ====================

/**
 * Get the current global minimum log level
 */
export const getLogLevel = async (): Promise<LogLevelResponse> => {
  return apiClient<LogLevelResponse>('/admin/developer-logs/level')
}

/**
 * Set the global minimum log level dynamically
 */
export const setLogLevel = async (level: string): Promise<LogLevelResponse> => {
  return apiClient<LogLevelResponse>('/admin/developer-logs/level', {
    method: 'PUT',
    body: JSON.stringify({ level }),
  })
}

/**
 * Get all source-specific log level overrides
 */
export const getLogLevelOverrides = async (): Promise<LogLevelOverridesResponse> => {
  return apiClient<LogLevelOverridesResponse>('/admin/developer-logs/level/overrides')
}

/**
 * Set a log level override for a specific source namespace
 */
export const setLogLevelOverride = async (sourcePrefix: string, level: string): Promise<{ source: string; level: string }> => {
  return apiClient<{ source: string; level: string }>(`/admin/developer-logs/level/overrides/${encodeURIComponent(sourcePrefix)}`, {
    method: 'PUT',
    body: JSON.stringify({ level }),
  })
}

/**
 * Remove a source-specific log level override
 */
export const removeLogLevelOverride = async (sourcePrefix: string): Promise<void> => {
  return apiClient<void>(`/admin/developer-logs/level/overrides/${encodeURIComponent(sourcePrefix)}`, {
    method: 'DELETE',
  })
}

// ==================== Live Buffer ====================

/**
 * Get statistics about the in-memory log buffer
 */
export const getBufferStats = async (): Promise<LogBufferStatsDto> => {
  return apiClient<LogBufferStatsDto>('/admin/developer-logs/buffer/stats')
}

/**
 * Get filtered log entries from the in-memory buffer
 */
export const getBufferEntries = async (params: {
  count?: number
  minLevel?: string
  sources?: string
  search?: string
  exceptionsOnly?: boolean
}): Promise<LogEntryDto[]> => {
  const searchParams = new URLSearchParams()
  searchParams.set('count', String(params.count ?? 100))
  if (params.minLevel) searchParams.set('minLevel', params.minLevel)
  if (params.sources) searchParams.set('sources', params.sources)
  if (params.search) searchParams.set('search', params.search)
  if (params.exceptionsOnly) searchParams.set('exceptionsOnly', 'true')

  return apiClient<LogEntryDto[]>(`/admin/developer-logs/buffer/entries?${searchParams.toString()}`)
}

/**
 * Get error patterns grouped by similarity
 */
export const getErrorClusters = async (maxClusters = 10): Promise<ErrorClusterDto[]> => {
  return apiClient<ErrorClusterDto[]>(`/admin/developer-logs/buffer/errors?maxClusters=${maxClusters}`)
}

/**
 * Clear all entries from the in-memory log buffer
 */
export const clearBuffer = async (): Promise<void> => {
  return apiClient<void>('/admin/developer-logs/buffer', {
    method: 'DELETE',
  })
}

// ==================== Historical Logs ====================

/**
 * Get list of dates that have log files available
 */
export const getAvailableLogDates = async (): Promise<string[]> => {
  return apiClient<string[]>('/admin/developer-logs/history/dates')
}

/**
 * Get paginated log entries for a specific date
 */
export const getHistoricalLogs = async (
  date: string,
  params: LogSearchParams = {}
): Promise<LogEntriesPagedResponse> => {
  const searchParams = new URLSearchParams()
  if (params.search) searchParams.set('search', params.search)
  if (params.minLevel) searchParams.set('minLevel', params.minLevel)
  if (params.levels?.length) searchParams.set('levels', params.levels.join(','))
  if (params.sources?.length) searchParams.set('sources', params.sources.join(','))
  if (params.hasException !== undefined) searchParams.set('hasException', String(params.hasException))
  if (params.requestId) searchParams.set('requestId', params.requestId)
  if (params.sortOrder) searchParams.set('sortOrder', params.sortOrder)
  searchParams.set('page', String(params.page ?? 1))
  searchParams.set('pageSize', String(params.pageSize ?? 100))

  const query = searchParams.toString()
  return apiClient<LogEntriesPagedResponse>(`/admin/developer-logs/history/${date}${query ? `?${query}` : ''}`)
}

/**
 * Search log entries across a date range (max 30 days)
 */
export const searchHistoricalLogs = async (
  fromDate: string,
  toDate: string,
  params: LogSearchParams = {}
): Promise<LogEntriesPagedResponse> => {
  const searchParams = new URLSearchParams()
  searchParams.set('fromDate', fromDate)
  searchParams.set('toDate', toDate)
  if (params.search) searchParams.set('search', params.search)
  if (params.minLevel) searchParams.set('minLevel', params.minLevel)
  if (params.levels?.length) searchParams.set('levels', params.levels.join(','))
  if (params.sources?.length) searchParams.set('sources', params.sources.join(','))
  if (params.hasException !== undefined) searchParams.set('hasException', String(params.hasException))
  if (params.requestId) searchParams.set('requestId', params.requestId)
  searchParams.set('page', String(params.page ?? 1))
  searchParams.set('pageSize', String(params.pageSize ?? 100))

  return apiClient<LogEntriesPagedResponse>(`/admin/developer-logs/history/search?${searchParams.toString()}`)
}

/**
 * Get total file size of logs for a date range
 */
export const getLogFileSize = async (fromDate: string, toDate: string): Promise<{ sizeBytes: number; sizeFormatted: string }> => {
  return apiClient<{ sizeBytes: number; sizeFormatted: string }>(
    `/admin/developer-logs/history/size?fromDate=${fromDate}&toDate=${toDate}`
  )
}
