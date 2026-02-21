/**
 * Role and Permission API Service
 *
 * Provides methods for managing roles, permissions, and permission templates.
 */
import { apiClient } from './apiClient'
import type {
  Role,
  RoleListItem,
  RoleHierarchy,
  CreateRoleRequest,
  UpdateRoleRequest,
  Permission,
  PermissionTemplate,
  PaginatedResponse,
} from '@/types'

// ============================================================================
// Roles
// ============================================================================

/**
 * Fetch paginated list of roles
 */
export const getRoles = async (params: {
  search?: string
  page?: number
  pageSize?: number
}): Promise<PaginatedResponse<RoleListItem>> => {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)
  if (params.page) queryParams.append('page', params.page.toString())
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<PaginatedResponse<RoleListItem>>(`/roles${query ? `?${query}` : ''}`)
}

/**
 * Fetch a single role by ID with full details
 */
export const getRoleById = async (id: string): Promise<Role> => {
  return apiClient<Role>(`/roles/${id}`)
}

/**
 * Fetch role hierarchy (tree structure)
 */
export const getRoleHierarchy = async (): Promise<RoleHierarchy[]> => {
  return apiClient<RoleHierarchy[]>('/roles/hierarchy')
}

/**
 * Create a new role
 */
export const createRole = async (request: CreateRoleRequest): Promise<Role> => {
  return apiClient<Role>('/roles', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing role
 */
export const updateRole = async (request: UpdateRoleRequest): Promise<Role> => {
  return apiClient<Role>(`/roles/${request.roleId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a role
 */
export const deleteRole = async (id: string): Promise<void> => {
  return apiClient<void>(`/roles/${id}`, {
    method: 'DELETE',
  })
}

// ============================================================================
// Role Permissions
// ============================================================================

/**
 * Get permissions assigned to a role
 */
export const getRolePermissions = async (roleId: string): Promise<string[]> => {
  return apiClient<string[]>(`/roles/${roleId}/permissions`)
}

/**
 * Get effective permissions (including inherited from parent roles)
 */
export const getEffectivePermissions = async (roleId: string): Promise<string[]> => {
  return apiClient<string[]>(`/roles/${roleId}/effective-permissions`)
}

/**
 * Assign permissions to a role
 */
export const assignPermissions = async (
  roleId: string,
  permissions: string[]
): Promise<string[]> => {
  return apiClient<string[]>(`/roles/${roleId}/permissions`, {
    method: 'PUT',
    body: JSON.stringify({ roleId, permissions }),
  })
}

/**
 * Remove permissions from a role
 */
export const removePermissions = async (
  roleId: string,
  permissions: string[]
): Promise<string[]> => {
  return apiClient<string[]>(`/roles/${roleId}/permissions`, {
    method: 'DELETE',
    body: JSON.stringify({ roleId, permissions }),
  })
}

// ============================================================================
// Permissions
// ============================================================================

/**
 * Get all available permissions
 */
export const getAllPermissions = async (): Promise<Permission[]> => {
  return apiClient<Permission[]>('/permissions')
}


// ============================================================================
// Permission Templates
// ============================================================================

/**
 * Get all permission templates
 */
export const getPermissionTemplates = async (): Promise<PermissionTemplate[]> => {
  return apiClient<PermissionTemplate[]>('/permission-templates')
}

/**
 * Get a single permission template by ID
 */
export const getPermissionTemplateById = async (id: string): Promise<PermissionTemplate> => {
  return apiClient<PermissionTemplate>(`/permission-templates/${id}`)
}

/**
 * Apply a permission template to a role
 */
export const applyTemplateToRole = async (
  roleId: string,
  templateId: string
): Promise<string[]> => {
  return apiClient<string[]>(`/roles/${roleId}/apply-template/${templateId}`, {
    method: 'POST',
  })
}
