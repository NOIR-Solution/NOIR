import { useState, useEffect, useCallback } from 'react'
import type { TenantListItem, PaginatedResponse } from '@/types'
import { getTenants, deleteTenant, type GetTenantsParams } from '@/services/tenants'
import { ApiError } from '@/services/apiClient'

interface UseTenantsState {
  data: PaginatedResponse<TenantListItem> | null
  loading: boolean
  error: string | null
}

interface UseTenantsReturn extends UseTenantsState {
  refresh: () => Promise<void>
  setPage: (page: number) => void
  setPageSize: (size: number) => void
  setSearch: (search: string) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  params: GetTenantsParams
}

export const useTenants = (initialParams: GetTenantsParams = {}): UseTenantsReturn => {
  const [state, setState] = useState<UseTenantsState>({
    data: null,
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<GetTenantsParams>({
    pageNumber: 1,
    pageSize: 10,
    ...initialParams,
  })

  const fetchTenants = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getTenants(params)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to load tenants'
      setState(prev => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchTenants()
  }, [fetchTenants])

  const setPage = useCallback((page: number) => {
    setParams(prev => ({ ...prev, pageNumber: page }))
  }, [])

  const setPageSize = useCallback((size: number) => {
    setParams(prev => ({ ...prev, pageSize: size, pageNumber: 1 }))
  }, [])

  const setSearch = useCallback((search: string) => {
    setParams(prev => ({ ...prev, search, pageNumber: 1 }))
  }, [])

  const handleDelete = useCallback(async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteTenant(id)
      await fetchTenants()
      return { success: true }
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to delete tenant'
      return { success: false, error: message }
    }
  }, [fetchTenants])

  return {
    ...state,
    refresh: fetchTenants,
    setPage,
    setPageSize,
    setSearch,
    handleDelete,
    params,
  }
}
