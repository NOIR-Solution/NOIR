import { useState, useEffect, useCallback } from 'react'
import type {
  PostListItem,
  PostCategoryListItem,
  PostTagListItem,
  BlogPagedResult,
  PostStatus,
} from '@/types'
import {
  getPosts,
  deletePost,
  getCategories,
  deleteCategory,
  getTags,
  deleteTag,
  type GetPostsParams,
  type GetCategoriesParams,
  type GetTagsParams,
} from '@/services/blog'
import { ApiError } from '@/services/apiClient'

// ============================================================================
// Posts Hook
// ============================================================================

interface UsePostsState {
  data: BlogPagedResult<PostListItem> | null
  loading: boolean
  error: string | null
}

interface UsePostsReturn extends UsePostsState {
  refresh: () => Promise<void>
  setPage: (page: number) => void
  setPageSize: (size: number) => void
  setSearch: (search: string) => void
  setStatus: (status: PostStatus | undefined) => void
  setCategoryId: (categoryId: string | undefined) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  params: GetPostsParams
}

export function usePosts(initialParams: GetPostsParams = {}): UsePostsReturn {
  const [state, setState] = useState<UsePostsState>({
    data: null,
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<GetPostsParams>({
    page: 1,
    pageSize: 10,
    ...initialParams,
  })

  const fetchPosts = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getPosts(params)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load posts'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchPosts()
  }, [fetchPosts])

  const setPage = useCallback((page: number) => {
    setParams((prev) => ({ ...prev, page }))
  }, [])

  const setPageSize = useCallback((size: number) => {
    setParams((prev) => ({ ...prev, pageSize: size, page: 1 }))
  }, [])

  const setSearch = useCallback((search: string) => {
    setParams((prev) => ({ ...prev, search: search || undefined, page: 1 }))
  }, [])

  const setStatus = useCallback((status: PostStatus | undefined) => {
    setParams((prev) => ({ ...prev, status, page: 1 }))
  }, [])

  const setCategoryId = useCallback((categoryId: string | undefined) => {
    setParams((prev) => ({ ...prev, categoryId, page: 1 }))
  }, [])

  const handleDelete = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      try {
        await deletePost(id)
        await fetchPosts()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to delete post'
        return { success: false, error: message }
      }
    },
    [fetchPosts]
  )

  return {
    ...state,
    refresh: fetchPosts,
    setPage,
    setPageSize,
    setSearch,
    setStatus,
    setCategoryId,
    handleDelete,
    params,
  }
}

// ============================================================================
// Categories Hook
// ============================================================================

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

export function useCategories(
  initialParams: GetCategoriesParams = {}
): UseCategoriesReturn {
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

// ============================================================================
// Tags Hook
// ============================================================================

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

export function useTags(initialParams: GetTagsParams = {}): UseTagsReturn {
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
