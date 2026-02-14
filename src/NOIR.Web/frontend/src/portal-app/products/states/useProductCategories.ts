import { useState, useEffect, useCallback } from 'react'
import type { ProductCategoryListItem } from '@/types/product'
import {
  getProductCategories,
  deleteProductCategory,
  type GetProductCategoriesParams,
} from '@/services/products'
import { ApiError } from '@/services/apiClient'

// ============================================================================
// Product Categories Hook
// ============================================================================

interface UseProductCategoriesState {
  data: ProductCategoryListItem[]
  loading: boolean
  error: string | null
}

interface UseProductCategoriesReturn extends UseProductCategoriesState {
  refresh: () => Promise<void>
  setSearch: (search: string) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  params: GetProductCategoriesParams
}

export function useProductCategories(
  initialParams: GetProductCategoriesParams = {}
): UseProductCategoriesReturn {
  const [state, setState] = useState<UseProductCategoriesState>({
    data: [],
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<GetProductCategoriesParams>(initialParams)

  const fetchCategories = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getProductCategories(params)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load categories'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchCategories()
  }, [fetchCategories])

  const setSearch = useCallback((search: string) => {
    setParams((prev) => ({ ...prev, search: search || undefined }))
  }, [])

  const handleDelete = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      try {
        await deleteProductCategory(id)
        await fetchCategories()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to delete category'
        return { success: false, error: message }
      }
    },
    [fetchCategories]
  )

  return {
    ...state,
    refresh: fetchCategories,
    setSearch,
    handleDelete,
    params,
  }
}
