/**
 * Common API types shared across the application
 * These mirror the backend DTOs for type safety
 */

/**
 * Standard API error response format
 * Matches RFC 7807 ProblemDetails from ASP.NET Core with NOIR extensions
 */
export interface ApiError {
  /** URI reference identifying the problem type */
  type?: string
  /** Short, human-readable summary of the problem type */
  title: string
  /** HTTP status code */
  status: number
  /** Human-readable explanation specific to this occurrence */
  detail?: string
  /** URI reference identifying the specific occurrence */
  instance?: string
  /** NOIR error code (format: NOIR-XXX-NNNN) */
  errorCode?: string
  /** Correlation ID for tracking/support */
  correlationId?: string
  /** ISO 8601 timestamp when the error occurred */
  timestamp?: string
  /** Validation errors by property name */
  errors?: Record<string, string[]>
}

/**
 * Generic paginated response wrapper
 */
export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}
