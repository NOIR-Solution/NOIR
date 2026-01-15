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
export async function getRoles(params: {
  search?: string
  page?: number
  pageSize?: number
}): Promise<PaginatedResponse<RoleListItem>> {
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
export async function getRoleById(id: string): Promise<Role> {
  return apiClient<Role>(`/roles/${id}`)
}

/**
 * Fetch role hierarchy (tree structure)
 */
export async function getRoleHierarchy(): Promise<RoleHierarchy[]> {
  return apiClient<RoleHierarchy[]>('/roles/hierarchy')
}

/**
 * Create a new role
 */
export async function createRole(request: CreateRoleRequest): Promise<Role> {
  return apiClient<Role>('/roles', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing role
 */
export async function updateRole(request: UpdateRoleRequest): Promise<Role> {
  return apiClient<Role>(`/roles/${request.roleId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a role
 */
export async function deleteRole(id: string): Promise<void> {
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
export async function getRolePermissions(roleId: string): Promise<string[]> {
  return apiClient<string[]>(`/roles/${roleId}/permissions`)
}

/**
 * Get effective permissions (including inherited from parent roles)
 */
export async function getEffectivePermissions(roleId: string): Promise<string[]> {
  return apiClient<string[]>(`/roles/${roleId}/effective-permissions`)
}

/**
 * Assign permissions to a role
 */
export async function assignPermissions(
  roleId: string,
  permissions: string[]
): Promise<string[]> {
  return apiClient<string[]>(`/roles/${roleId}/permissions`, {
    method: 'PUT',
    body: JSON.stringify({ roleId, permissions }),
  })
}

/**
 * Remove permissions from a role
 */
export async function removePermissions(
  roleId: string,
  permissions: string[]
): Promise<string[]> {
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
export async function getAllPermissions(): Promise<Permission[]> {
  return apiClient<Permission[]>('/permissions')
}

/**
 * Get permissions grouped by category
 */
export async function getPermissionsByCategory(): Promise<Record<string, Permission[]>> {
  const permissions = await getAllPermissions()
  return permissions.reduce((groups, permission) => {
    const category = permission.category || 'Uncategorized'
    if (!groups[category]) {
      groups[category] = []
    }
    groups[category].push(permission)
    return groups
  }, {} as Record<string, Permission[]>)
}

// ============================================================================
// Permission Templates
// ============================================================================

/**
 * Get all permission templates
 */
export async function getPermissionTemplates(): Promise<PermissionTemplate[]> {
  return apiClient<PermissionTemplate[]>('/permission-templates')
}

/**
 * Get a single permission template by ID
 */
export async function getPermissionTemplateById(id: string): Promise<PermissionTemplate> {
  return apiClient<PermissionTemplate>(`/permission-templates/${id}`)
}

/**
 * Apply a permission template to a role
 */
export async function applyTemplateToRole(
  roleId: string,
  templateId: string
): Promise<string[]> {
  return apiClient<string[]>(`/roles/${roleId}/apply-template/${templateId}`, {
    method: 'POST',
  })
}
