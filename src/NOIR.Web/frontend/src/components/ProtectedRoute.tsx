import { Navigate, useLocation } from 'react-router-dom'
import { useAuthContext } from '@/contexts/AuthContext'
import { usePermissions, type PermissionKey } from '@/hooks/usePermissions'
import type { ReactNode } from 'react'
import { PageSpinner } from '@/components/ui/loading'

interface ProtectedRouteProps {
  children: ReactNode
  /** Optional permission(s) required to access this route */
  permissions?: PermissionKey | PermissionKey[]
  /** If true, require ALL permissions. Default: false (ANY permission) */
  requireAll?: boolean
  /** Where to redirect if permissions check fails. Default: /portal (home) */
  redirectTo?: string
}

/**
 * Protects routes requiring authentication and optionally specific permissions.
 *
 * @example
 * // Auth only
 * <ProtectedRoute><Dashboard /></ProtectedRoute>
 *
 * @example
 * // With single permission
 * <ProtectedRoute permissions="users:read"><UsersPage /></ProtectedRoute>
 *
 * @example
 * // With multiple permissions (any)
 * <ProtectedRoute permissions={["roles:read", "roles:update"]}>
 *   <RolesPage />
 * </ProtectedRoute>
 *
 * @example
 * // With multiple permissions (all required)
 * <ProtectedRoute permissions={["admin:read", "admin:write"]} requireAll>
 *   <AdminPage />
 * </ProtectedRoute>
 */
export function ProtectedRoute({
  children,
  permissions,
  requireAll = false,
  redirectTo = '/portal',
}: ProtectedRouteProps) {
  const { isAuthenticated, isLoading: authLoading } = useAuthContext()
  const {
    hasPermission,
    hasAllPermissions,
    hasAnyPermission,
    isLoading: permissionsLoading,
  } = usePermissions()
  const location = useLocation()

  // Show loading while checking auth
  if (authLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <PageSpinner />
      </div>
    )
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    const returnUrl = encodeURIComponent(location.pathname + location.search)
    return <Navigate to={`/login?returnUrl=${returnUrl}`} replace />
  }

  // If no permissions required, just check auth
  if (!permissions) {
    return <>{children}</>
  }

  // Show loading while fetching permissions
  if (permissionsLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <PageSpinner />
      </div>
    )
  }

  // Check permissions
  const permissionArray = Array.isArray(permissions) ? permissions : [permissions]
  const hasAccess = requireAll
    ? hasAllPermissions(permissionArray)
    : permissionArray.length === 1
      ? hasPermission(permissionArray[0])
      : hasAnyPermission(permissionArray)

  // Redirect if no permission
  if (!hasAccess) {
    return <Navigate to={redirectTo} replace />
  }

  return <>{children}</>
}
