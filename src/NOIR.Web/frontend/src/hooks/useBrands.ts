/**
 * React hooks for Brand management
 */
import { useState, useEffect, useCallback } from 'react'
import {
  getBrands,
  getActiveBrands,
  getBrandById,
  createBrand,
  updateBrand,
  deleteBrand,
  type GetBrandsParams,
} from '@/services/brands'
import type { Brand, BrandListItem, BrandPagedResult, CreateBrandRequest, UpdateBrandRequest } from '@/types/brand'
import { ApiError } from '@/services/apiClient'

// ============================================================================
// Brands List Hook
// ============================================================================

interface UseBrandsState {
  data: BrandPagedResult | null
  loading: boolean
  error: string | null
}

interface UseBrandsReturn extends UseBrandsState {
  refresh: () => Promise<void>
  setPage: (page: number) => void
  setPageSize: (size: number) => void
  setSearch: (search: string) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  params: GetBrandsParams
}

export function useBrands(initialParams: GetBrandsParams = {}): UseBrandsReturn {
  const [state, setState] = useState<UseBrandsState>({
    data: null,
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<GetBrandsParams>({
    page: 1,
    pageSize: 20,
    ...initialParams,
  })

  const fetchBrands = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getBrands(params)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load brands'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchBrands()
  }, [fetchBrands])

  const setPage = useCallback((page: number) => {
    setParams((prev) => ({ ...prev, page }))
  }, [])

  const setPageSize = useCallback((size: number) => {
    setParams((prev) => ({ ...prev, pageSize: size, page: 1 }))
  }, [])

  const setSearch = useCallback((search: string) => {
    setParams((prev) => ({ ...prev, search: search || undefined, page: 1 }))
  }, [])

  const handleDelete = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      try {
        await deleteBrand(id)
        await fetchBrands()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to delete brand'
        return { success: false, error: message }
      }
    },
    [fetchBrands]
  )

  return {
    ...state,
    refresh: fetchBrands,
    setPage,
    setPageSize,
    setSearch,
    handleDelete,
    params,
  }
}

// ============================================================================
// Active Brands Hook (for dropdowns)
// ============================================================================

interface UseActiveBrandsState {
  data: BrandListItem[]
  loading: boolean
  error: string | null
}

interface UseActiveBrandsReturn extends UseActiveBrandsState {
  refresh: () => Promise<void>
}

export function useActiveBrands(): UseActiveBrandsReturn {
  const [state, setState] = useState<UseActiveBrandsState>({
    data: [],
    loading: true,
    error: null,
  })

  const fetchActiveBrands = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getActiveBrands()
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load brands'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [])

  useEffect(() => {
    fetchActiveBrands()
  }, [fetchActiveBrands])

  return {
    ...state,
    refresh: fetchActiveBrands,
  }
}

// ============================================================================
// Single Brand Hook
// ============================================================================

interface UseBrandState {
  data: Brand | null
  loading: boolean
  error: string | null
}

interface UseBrandReturn extends UseBrandState {
  refresh: () => Promise<void>
}

export function useBrand(id: string | undefined): UseBrandReturn {
  const [state, setState] = useState<UseBrandState>({
    data: null,
    loading: !!id,
    error: null,
  })

  const fetchBrand = useCallback(async () => {
    if (!id) {
      setState({ data: null, loading: false, error: null })
      return
    }

    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getBrandById(id)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load brand'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [id])

  useEffect(() => {
    fetchBrand()
  }, [fetchBrand])

  return {
    ...state,
    refresh: fetchBrand,
  }
}

// ============================================================================
// Create Brand Hook
// ============================================================================

interface UseCreateBrandReturn {
  createBrand: (request: CreateBrandRequest) => Promise<{ success: boolean; data?: Brand; error?: string }>
  isPending: boolean
}

export function useCreateBrand(): UseCreateBrandReturn {
  const [isPending, setIsPending] = useState(false)

  const create = useCallback(
    async (request: CreateBrandRequest): Promise<{ success: boolean; data?: Brand; error?: string }> => {
      setIsPending(true)
      try {
        const data = await createBrand(request)
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to create brand'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    createBrand: create,
    isPending,
  }
}

// ============================================================================
// Update Brand Hook
// ============================================================================

interface UseUpdateBrandReturn {
  updateBrand: (id: string, request: UpdateBrandRequest) => Promise<{ success: boolean; data?: Brand; error?: string }>
  isPending: boolean
}

export function useUpdateBrand(): UseUpdateBrandReturn {
  const [isPending, setIsPending] = useState(false)

  const update = useCallback(
    async (id: string, request: UpdateBrandRequest): Promise<{ success: boolean; data?: Brand; error?: string }> => {
      setIsPending(true)
      try {
        const data = await updateBrand(id, request)
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to update brand'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    updateBrand: update,
    isPending,
  }
}

// ============================================================================
// Delete Brand Hook
// ============================================================================

interface UseDeleteBrandReturn {
  deleteBrand: (id: string) => Promise<{ success: boolean; error?: string }>
  isPending: boolean
}

export function useDeleteBrand(): UseDeleteBrandReturn {
  const [isPending, setIsPending] = useState(false)

  const remove = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      setIsPending(true)
      try {
        await deleteBrand(id)
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to delete brand'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    deleteBrand: remove,
    isPending,
  }
}

// Re-export types for convenience
export type { Brand, BrandListItem, CreateBrandRequest, UpdateBrandRequest }
