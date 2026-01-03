/**
 * Common API types shared across the application
 * These mirror the backend DTOs for type safety
 */

/**
 * Standard API error response format
 * Matches ProblemDetails from ASP.NET Core
 */
export interface ApiError {
  title: string
  status: number
  detail?: string
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
