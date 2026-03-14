import { useState, useDeferredValue, useTransition, useMemo, useEffect, useCallback } from 'react'
import type { SortingState, Updater } from '@tanstack/react-table'

const PAGE_SIZE_STORAGE_PREFIX = 'noir:table-page-size'

const loadSavedPageSize = (tableKey: string): number | undefined => {
  try {
    const raw = localStorage.getItem(`${PAGE_SIZE_STORAGE_PREFIX}:${tableKey}`)
    if (raw) {
      const num = parseInt(raw, 10)
      if (!isNaN(num) && num >= 1 && num <= 500) return num
    }
  } catch {
    // localStorage unavailable
  }
  return undefined
}

const savePageSize = (tableKey: string, size: number) => {
  try {
    localStorage.setItem(`${PAGE_SIZE_STORAGE_PREFIX}:${tableKey}`, String(size))
  } catch {
    // quota exceeded or unavailable
  }
}

const clearSavedPageSize = (tableKey: string) => {
  try {
    localStorage.removeItem(`${PAGE_SIZE_STORAGE_PREFIX}:${tableKey}`)
  } catch {
    // unavailable
  }
}

interface UseTableParamsOptions<TFilters extends Record<string, unknown>> {
  defaultPageSize?: number
  defaultFilters?: TFilters
  /** localStorage key for persisting page size. Must match useEnterpriseTable's tableKey. */
  tableKey?: string
}

interface TableParams<TFilters extends Record<string, unknown>> {
  page: number
  pageSize: number
  search?: string
  orderBy?: string
  isDescending?: boolean
  sorting: SortingState
  filters: TFilters
}

interface UseTableParamsReturn<TFilters extends Record<string, unknown>> {
  /** Full params object ready to pass to a query hook */
  params: TableParams<TFilters>
  /** Raw search input value (controlled) */
  searchInput: string
  setSearchInput: (value: string) => void
  /** True while the deferred search hasn't settled yet (show stale indicator) */
  isSearchStale: boolean
  /** True during a filter/page/sort transition */
  isFilterPending: boolean
  /** Update a single filter key. Resets page to 1. */
  setFilter: <K extends keyof TFilters>(key: K, value: TFilters[K]) => void
  /** Reset all filters to defaults */
  resetFilters: () => void
  setPage: (page: number) => void
  setPageSize: (size: number) => void
  /** Accepts both direct SortingState and TanStack's functional Updater<SortingState> */
  setSorting: (updaterOrValue: Updater<SortingState>) => void
  /** The page's configured default page size (pass to DataTablePagination) */
  defaultPageSize: number
}

export const useTableParams = <TFilters extends Record<string, unknown> = Record<string, never>>(
  options: UseTableParamsOptions<TFilters> = {},
): UseTableParamsReturn<TFilters> => {
  const { defaultPageSize = 20, defaultFilters = {} as TFilters, tableKey } = options

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch

  const [sorting, setSortingState] = useState<SortingState>([])
  const [page, setPageState] = useState(1)
  const [pageSize, setPageSizeState] = useState(() => {
    if (tableKey) {
      const saved = loadSavedPageSize(tableKey)
      if (saved != null) return saved
    }
    return defaultPageSize
  })
  const [filters, setFilters] = useState<TFilters>(defaultFilters)
  const [isFilterPending, startFilterTransition] = useTransition()

  // Reset to page 1 when search settles
  useEffect(() => {
    setPageState(1)
  }, [deferredSearch])

  const setPage = useCallback((p: number) => {
    startFilterTransition(() => setPageState(p))
  }, [])

  const setPageSize = useCallback((size: number) => {
    startFilterTransition(() => {
      setPageSizeState(size)
      setPageState(1)
    })
    // Persist to localStorage when tableKey is provided
    if (tableKey) {
      if (size === defaultPageSize) {
        // Clear saved value when resetting to default — avoids stale entries
        clearSavedPageSize(tableKey)
      } else {
        savePageSize(tableKey, size)
      }
    }
  }, [tableKey, defaultPageSize])

  const setSorting = useCallback((updaterOrValue: Updater<SortingState>) => {
    startFilterTransition(() => {
      setSortingState((prev) =>
        typeof updaterOrValue === 'function' ? updaterOrValue(prev) : updaterOrValue,
      )
      setPageState(1)
    })
  }, [])

  const setFilter = useCallback(<K extends keyof TFilters>(key: K, value: TFilters[K]) => {
    startFilterTransition(() => {
      setFilters(prev => ({ ...prev, [key]: value }))
      setPageState(1)
    })
  }, [])

  const resetFilters = useCallback(() => {
    startFilterTransition(() => {
      setFilters(defaultFilters)
      setPageState(1)
      setSortingState([])
      setSearchInput('')
    })
  }, [defaultFilters])

  const params = useMemo((): TableParams<TFilters> => ({
    page,
    pageSize,
    search: deferredSearch || undefined,
    orderBy: sorting[0]?.id,
    isDescending: sorting[0]?.desc,
    sorting,
    filters,
    ...filters,
  }), [page, pageSize, deferredSearch, sorting, filters])

  return {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    isFilterPending,
    setFilter,
    resetFilters,
    setPage,
    setPageSize,
    setSorting,
    defaultPageSize,
  }
}
