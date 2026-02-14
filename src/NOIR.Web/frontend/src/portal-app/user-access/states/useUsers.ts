import { useState, useEffect, useCallback } from 'react'
import type { UserListItem, PaginatedResponse, RoleListItem } from '@/types'
import { getUsers, deleteUser, lockUser, unlockUser } from '@/services/users'
import { getRoles } from '@/services/roles'
import { ApiError } from '@/services/apiClient'

// ============================================================================
// Users List Hook
// ============================================================================

interface UsersParams {
  page?: number
  pageSize?: number
  search?: string
  role?: string
  isLocked?: boolean
}

interface UseUsersState {
  data: PaginatedResponse<UserListItem> | null
  loading: boolean
  error: string | null
}

interface UseUsersReturn extends UseUsersState {
  refresh: () => Promise<void>
  setPage: (page: number) => void
  setPageSize: (size: number) => void
  setSearch: (search: string) => void
  setRoleFilter: (role: string) => void
  setLockedFilter: (isLocked: boolean | undefined) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  handleLock: (id: string) => Promise<{ success: boolean; error?: string }>
  handleUnlock: (id: string) => Promise<{ success: boolean; error?: string }>
  params: UsersParams
}

export const useUsers = (initialParams: UsersParams = {}): UseUsersReturn => {
  const [state, setState] = useState<UseUsersState>({
    data: null,
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<UsersParams>({
    page: 1,
    pageSize: 10,
    ...initialParams,
  })

  const fetchUsers = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getUsers(params)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to load users'
      setState(prev => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchUsers()
  }, [fetchUsers])

  const setPage = useCallback((page: number) => {
    setParams(prev => ({ ...prev, page }))
  }, [])

  const setPageSize = useCallback((size: number) => {
    setParams(prev => ({ ...prev, pageSize: size, page: 1 }))
  }, [])

  const setSearch = useCallback((search: string) => {
    setParams(prev => ({ ...prev, search, page: 1 }))
  }, [])

  const setRoleFilter = useCallback((role: string) => {
    setParams(prev => ({ ...prev, role: role || undefined, page: 1 }))
  }, [])

  const setLockedFilter = useCallback((isLocked: boolean | undefined) => {
    setParams(prev => ({ ...prev, isLocked, page: 1 }))
  }, [])

  const handleDelete = useCallback(async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteUser(id)
      await fetchUsers()
      return { success: true }
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to delete user'
      return { success: false, error: message }
    }
  }, [fetchUsers])

  const handleLock = useCallback(async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await lockUser(id)
      await fetchUsers()
      return { success: true }
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to lock user'
      return { success: false, error: message }
    }
  }, [fetchUsers])

  const handleUnlock = useCallback(async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await unlockUser(id)
      await fetchUsers()
      return { success: true }
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to unlock user'
      return { success: false, error: message }
    }
  }, [fetchUsers])

  return {
    ...state,
    refresh: fetchUsers,
    setPage,
    setPageSize,
    setSearch,
    setRoleFilter,
    setLockedFilter,
    handleDelete,
    handleLock,
    handleUnlock,
    params,
  }
}

// ============================================================================
// Available Roles Hook (for filtering and assignment)
// ============================================================================

interface UseAvailableRolesState {
  roles: RoleListItem[]
  loading: boolean
  error: string | null
}

export const useAvailableRoles = () => {
  const [state, setState] = useState<UseAvailableRolesState>({
    roles: [],
    loading: true,
    error: null,
  })

  const fetchRoles = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }))

    try {
      // Fetch all roles (no pagination for dropdown)
      const data = await getRoles({ pageSize: 100 })
      setState({
        roles: data.items,
        loading: false,
        error: null,
      })
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to load roles'
      setState(prev => ({ ...prev, loading: false, error: message }))
    }
  }, [])

  useEffect(() => {
    fetchRoles()
  }, [fetchRoles])

  return {
    ...state,
    refresh: fetchRoles,
  }
}
