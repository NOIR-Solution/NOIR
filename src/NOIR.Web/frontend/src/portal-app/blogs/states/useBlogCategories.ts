import { useState, useEffect, useCallback } from 'react'
import type {
  PostCategoryListItem,
} from '@/types'
import {
  getCategories,
  deleteCategory,
  type GetCategoriesParams,
} from '@/services/blog'
import { ApiError } from '@/services/apiClient'

interface UseCategoriesState {
  data: PostCategoryListItem[]
  loading: boolean
  error: string | null
}

interface UseCategoriesReturn extends UseCategoriesState {
  refresh: () => Promise<void>
  setSearch: (search: string) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  params: GetCategoriesParams
}

export const useCategories = (
  initialParams: GetCategoriesParams = {}
): UseCategoriesReturn => {
  const [state, setState] = useState<UseCategoriesState>({
    data: [],
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<GetCategoriesParams>(initialParams)

  const fetchCategories = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getCategories(params)
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
        await deleteCategory(id)
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
