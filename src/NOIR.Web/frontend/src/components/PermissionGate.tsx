import { type ReactNode } from 'react'
import { usePermissions, type PermissionKey } from '@/hooks/usePermissions'

interface PermissionGateProps {
  /** Single permission or array of permissions required */
  permissions: PermissionKey | PermissionKey[]
  /** If true, require ALL permissions. If false (default), require ANY permission */
  requireAll?: boolean
  /** Content to render if user has permission */
  children: ReactNode
  /** Optional content to render if user lacks permission */
  fallback?: ReactNode
}

/**
 * Conditionally renders children based on user permissions
 *
 * @example
 * // Single permission
 * <PermissionGate permissions="users:create">
 *   <CreateUserButton />
 * </PermissionGate>
 *
 * @example
 * // Any of multiple permissions
 * <PermissionGate permissions={["users:update", "users:delete"]}>
 *   <UserActions />
 * </PermissionGate>
 *
 * @example
 * // All permissions required
 * <PermissionGate permissions={["users:read", "users:update"]} requireAll>
 *   <EditUserForm />
 * </PermissionGate>
 *
 * @example
 * // With fallback content
 * <PermissionGate permissions="admin:access" fallback={<AccessDenied />}>
 *   <AdminPanel />
 * </PermissionGate>
 */
export function PermissionGate({
  permissions,
  requireAll = false,
  children,
  fallback = null,
}: PermissionGateProps): ReactNode {
  const { hasPermission, hasAllPermissions, hasAnyPermission, isLoading } = usePermissions()

  // While loading, show nothing (or could show skeleton)
  if (isLoading) {
    return null
  }

  const permissionArray = Array.isArray(permissions) ? permissions : [permissions]

  const hasAccess = requireAll
    ? hasAllPermissions(permissionArray)
    : permissionArray.length === 1
      ? hasPermission(permissionArray[0])
      : hasAnyPermission(permissionArray)

  return hasAccess ? children : fallback
}

interface RoleGateProps {
  /** Single role or array of roles required */
  roles: string | string[]
  /** If true, require ALL roles. If false (default), require ANY role */
  requireAll?: boolean
  /** Content to render if user has role */
  children: ReactNode
  /** Optional content to render if user lacks role */
  fallback?: ReactNode
}

/**
 * Conditionally renders children based on user roles
 *
 * @example
 * // Admin-only content
 * <RoleGate roles="Admin">
 *   <AdminPanel />
 * </RoleGate>
 *
 * @example
 * // Multiple roles (any)
 * <RoleGate roles={["Admin", "Manager"]}>
 *   <ManageTeam />
 * </RoleGate>
 */
export function RoleGate({
  roles,
  requireAll = false,
  children,
  fallback = null,
}: RoleGateProps): ReactNode {
  const { hasRole, isLoading } = usePermissions()

  if (isLoading) {
    return null
  }

  const roleArray = Array.isArray(roles) ? roles : [roles]

  const hasAccess = requireAll
    ? roleArray.every(r => hasRole(r))
    : roleArray.some(r => hasRole(r))

  return hasAccess ? children : fallback
}
