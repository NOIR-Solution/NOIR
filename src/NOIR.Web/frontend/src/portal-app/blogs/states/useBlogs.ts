import { useState, useEffect, useCallback } from 'react'
import type {
  PostListItem,
  BlogPagedResult,
  PostStatus,
} from '@/types'
import {
  getPosts,
  deletePost,
  type GetPostsParams,
} from '@/services/blog'
import { ApiError } from '@/services/apiClient'

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
