import { useState, useEffect, useCallback } from 'react'
import type {
  PostTagListItem,
} from '@/types'
import {
  getTags,
  deleteTag,
  type GetTagsParams,
} from '@/services/blog'
import { ApiError } from '@/services/apiClient'

interface UseTagsState {
  data: PostTagListItem[]
  loading: boolean
  error: string | null
}

interface UseTagsReturn extends UseTagsState {
  refresh: () => Promise<void>
  setSearch: (search: string) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  params: GetTagsParams
}

export const useTags = (initialParams: GetTagsParams = {}): UseTagsReturn => {
  const [state, setState] = useState<UseTagsState>({
    data: [],
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<GetTagsParams>(initialParams)

  const fetchTags = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getTags(params)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load tags'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchTags()
  }, [fetchTags])

  const setSearch = useCallback((search: string) => {
    setParams((prev) => ({ ...prev, search: search || undefined }))
  }, [])

  const handleDelete = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      try {
        await deleteTag(id)
        await fetchTags()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to delete tag'
        return { success: false, error: message }
      }
    },
    [fetchTags]
  )

  return {
    ...state,
    refresh: fetchTags,
    setSearch,
    handleDelete,
    params,
  }
}
