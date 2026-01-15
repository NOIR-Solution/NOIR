import { useState, useEffect, useCallback } from 'react'
import type { RoleListItem, PaginatedResponse, Permission, PermissionTemplate } from '@/types'
import {
  getRoles,
  deleteRole,
  getAllPermissions,
  getPermissionTemplates,
} from '@/services/roles'
import { ApiError } from '@/services/apiClient'

// ============================================================================
// Roles List Hook
// ============================================================================

interface RolesParams {
  page?: number
  pageSize?: number
  search?: string
}

interface UseRolesState {
  data: PaginatedResponse<RoleListItem> | null
  loading: boolean
  error: string | null
}

interface UseRolesReturn extends UseRolesState {
  refresh: () => Promise<void>
  setPage: (page: number) => void
  setPageSize: (size: number) => void
  setSearch: (search: string) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  params: RolesParams
}

export function useRoles(initialParams: RolesParams = {}): UseRolesReturn {
  const [state, setState] = useState<UseRolesState>({
    data: null,
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<RolesParams>({
    page: 1,
    pageSize: 10,
    ...initialParams,
  })

  const fetchRoles = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getRoles(params)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to load roles'
      setState(prev => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchRoles()
  }, [fetchRoles])

  const setPage = useCallback((page: number) => {
    setParams(prev => ({ ...prev, page }))
  }, [])

  const setPageSize = useCallback((size: number) => {
    setParams(prev => ({ ...prev, pageSize: size, page: 1 }))
  }, [])

  const setSearch = useCallback((search: string) => {
    setParams(prev => ({ ...prev, search, page: 1 }))
  }, [])

  const handleDelete = useCallback(async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteRole(id)
      await fetchRoles()
      return { success: true }
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to delete role'
      return { success: false, error: message }
    }
  }, [fetchRoles])

  return {
    ...state,
    refresh: fetchRoles,
    setPage,
    setPageSize,
    setSearch,
    handleDelete,
    params,
  }
}

// ============================================================================
// Permissions Hook
// ============================================================================

interface UsePermissionsState {
  permissions: Permission[]
  permissionsByCategory: Record<string, Permission[]>
  loading: boolean
  error: string | null
}

export function usePermissions() {
  const [state, setState] = useState<UsePermissionsState>({
    permissions: [],
    permissionsByCategory: {},
    loading: true,
    error: null,
  })

  const fetchPermissions = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }))

    try {
      const permissions = await getAllPermissions()

      // Group by category
      const permissionsByCategory = permissions.reduce((groups, permission) => {
        const category = permission.category || 'Uncategorized'
        if (!groups[category]) {
          groups[category] = []
        }
        groups[category].push(permission)
        return groups
      }, {} as Record<string, Permission[]>)

      setState({
        permissions,
        permissionsByCategory,
        loading: false,
        error: null,
      })
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to load permissions'
      setState(prev => ({ ...prev, loading: false, error: message }))
    }
  }, [])

  useEffect(() => {
    fetchPermissions()
  }, [fetchPermissions])

  return {
    ...state,
    refresh: fetchPermissions,
  }
}

// ============================================================================
// Permission Templates Hook
// ============================================================================

interface UsePermissionTemplatesState {
  templates: PermissionTemplate[]
  loading: boolean
  error: string | null
}

export function usePermissionTemplates() {
  const [state, setState] = useState<UsePermissionTemplatesState>({
    templates: [],
    loading: true,
    error: null,
  })

  const fetchTemplates = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }))

    try {
      const templates = await getPermissionTemplates()
      setState({
        templates,
        loading: false,
        error: null,
      })
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to load permission templates'
      setState(prev => ({ ...prev, loading: false, error: message }))
    }
  }, [])

  useEffect(() => {
    fetchTemplates()
  }, [fetchTemplates])

  return {
    ...state,
    refresh: fetchTemplates,
  }
}
