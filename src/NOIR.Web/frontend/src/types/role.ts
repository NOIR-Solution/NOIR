/**
 * Role and Permission-related types
 * These mirror the backend Role DTOs exactly
 */

/**
 * Full role details with permissions
 * Matches backend RoleDto from NOIR.Application.Features.Roles.DTOs
 */
export interface Role {
  id: string
  name: string
  normalizedName: string | null
  description: string | null
  parentRoleId: string | null
  parentRoleName: string | null
  tenantId: string | null
  isSystemRole: boolean
  sortOrder: number
  iconName: string | null
  color: string | null
  userCount: number
  permissions: string[]
  effectivePermissions: string[]
}

/**
 * Role list item (lighter version for table display)
 * Matches backend RoleListDto
 */
export interface RoleListItem {
  id: string
  name: string
  description: string | null
  parentRoleId: string | null
  isSystemRole: boolean
  sortOrder: number
  iconName: string | null
  color: string | null
  userCount: number
}

/**
 * Role hierarchy item showing parent-child relationships
 * Matches backend RoleHierarchyDto
 */
export interface RoleHierarchy {
  id: string
  name: string
  description: string | null
  level: number
  children: RoleHierarchy[]
}

/**
 * Create role request
 * Matches backend CreateRoleCommand
 */
export interface CreateRoleRequest {
  name: string
  description?: string
  parentRoleId?: string
  tenantId?: string
  sortOrder?: number
  iconName?: string
  color?: string
  permissions?: string[]
}

/**
 * Update role request
 * Matches backend UpdateRoleCommand
 */
export interface UpdateRoleRequest {
  roleId: string
  name: string
  description?: string
  parentRoleId?: string
  sortOrder?: number
  iconName?: string
  color?: string
}

/**
 * Permission entity
 * Matches backend Permission entity
 */
export interface Permission {
  id: string
  resource: string
  action: string
  scope: string | null
  displayName: string
  description: string | null
  category: string | null
  isSystem: boolean
  sortOrder: number
  name: string // resource:action:scope
  isTenantAllowed: boolean // Whether this permission can be assigned to tenant-specific roles
}

/**
 * Permission template for quick role creation
 * Matches backend PermissionTemplate entity
 */
export interface PermissionTemplate {
  id: string
  name: string
  description: string | null
  tenantId: string | null
  isSystem: boolean
  iconName: string | null
  color: string | null
  sortOrder: number
  permissions: string[]
}

/**
 * Grouped permissions for UI display
 */
export interface PermissionGroup {
  category: string
  permissions: Permission[]
}
