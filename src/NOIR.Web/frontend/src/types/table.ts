/**
 * Generic paginated response shape — normalizes the inconsistent
 * per-domain types (page / pageNumber / pageIndex etc.)
 */
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage?: boolean
  hasNextPage?: boolean
}

/** Server-side table state passed to API query params */
export interface ServerTableState {
  page: number
  pageSize: number
  search?: string
  orderBy?: string
  isDescending?: boolean
}

/** Persisted column preferences (saved to localStorage) */
export interface TableColumnPreferences {
  columnVisibility: Record<string, boolean>
  columnOrder: string[]
  columnSizing: Record<string, number>
  /** Schema version — bump when preferences become incompatible */
  _v: number
}
