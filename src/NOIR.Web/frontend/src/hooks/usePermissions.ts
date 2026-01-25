import { useState, useEffect, useCallback, useMemo } from 'react'
import { getUserPermissions, type UserPermissions } from '@/services/auth'
import { useAuthContext } from '@/contexts/AuthContext'

interface UsePermissionsResult {
  /** Raw permissions array from the server */
  permissions: string[]
  /** User's roles */
  roles: string[]
  /** Whether permissions are currently loading */
  isLoading: boolean
  /** Error if permissions failed to load */
  error: Error | null
  /** Check if user has a specific permission */
  hasPermission: (permission: string) => boolean
  /** Check if user has ALL specified permissions */
  hasAllPermissions: (permissions: string[]) => boolean
  /** Check if user has ANY of the specified permissions */
  hasAnyPermission: (permissions: string[]) => boolean
  /** Check if user has a specific role */
  hasRole: (role: string) => boolean
  /** Manually refresh permissions */
  refreshPermissions: () => Promise<void>
}

/**
 * Hook to access the current user's permissions
 * Automatically fetches permissions when authenticated and caches them
 * Provides utility functions for permission checking
 */
export function usePermissions(): UsePermissionsResult {
  const { isAuthenticated, user } = useAuthContext()
  const [permissionsData, setPermissionsData] = useState<UserPermissions | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)

  const fetchPermissions = useCallback(async () => {
    if (!isAuthenticated) {
      setPermissionsData(null)
      return
    }

    setIsLoading(true)
    setError(null)
    try {
      const data = await getUserPermissions()
      setPermissionsData(data)
    } catch (err) {
      setError(err instanceof Error ? err : new Error('Failed to fetch permissions'))
      setPermissionsData(null)
    } finally {
      setIsLoading(false)
    }
  }, [isAuthenticated])

  // Fetch permissions when authentication state changes
  useEffect(() => {
    if (isAuthenticated && user) {
      fetchPermissions()
    } else {
      setPermissionsData(null)
    }
  }, [isAuthenticated, user?.id, fetchPermissions])

  // Create a Set for O(1) permission lookup
  const permissionSet = useMemo(
    () => new Set(permissionsData?.permissions ?? []),
    [permissionsData?.permissions]
  )

  const roleSet = useMemo(
    () => new Set(permissionsData?.roles ?? []),
    [permissionsData?.roles]
  )

  const hasPermission = useCallback(
    (permission: string): boolean => {
      return permissionSet.has(permission)
    },
    [permissionSet]
  )

  const hasAllPermissions = useCallback(
    (permissions: string[]): boolean => {
      return permissions.every(p => permissionSet.has(p))
    },
    [permissionSet]
  )

  const hasAnyPermission = useCallback(
    (permissions: string[]): boolean => {
      return permissions.some(p => permissionSet.has(p))
    },
    [permissionSet]
  )

  const hasRole = useCallback(
    (role: string): boolean => {
      return roleSet.has(role)
    },
    [roleSet]
  )

  return {
    permissions: permissionsData?.permissions ?? [],
    roles: permissionsData?.roles ?? [],
    isLoading,
    error,
    hasPermission,
    hasAllPermissions,
    hasAnyPermission,
    hasRole,
    refreshPermissions: fetchPermissions,
  }
}

// Common permission constants for type-safe usage
export const Permissions = {
  // Users
  UsersRead: 'users:read',
  UsersCreate: 'users:create',
  UsersUpdate: 'users:update',
  UsersDelete: 'users:delete',
  // Roles
  RolesRead: 'roles:read',
  RolesCreate: 'roles:create',
  RolesUpdate: 'roles:update',
  RolesDelete: 'roles:delete',
  // Permissions
  PermissionsRead: 'permissions:read',
  PermissionsAssign: 'permissions:assign',
  // Email Templates
  EmailTemplatesRead: 'email-templates:read',
  EmailTemplatesUpdate: 'email-templates:update',
  // Legal Pages
  LegalPagesRead: 'legal-pages:read',
  LegalPagesUpdate: 'legal-pages:update',
  // Tenant Settings
  TenantSettingsRead: 'tenant-settings:read',
  TenantSettingsUpdate: 'tenant-settings:update',
  // Tenants
  TenantsRead: 'tenants:read',
  TenantsCreate: 'tenants:create',
  TenantsUpdate: 'tenants:update',
  TenantsDelete: 'tenants:delete',
  // System (platform-level only)
  SystemAdmin: 'system:admin',
  SystemAuditLogs: 'system:audit-logs',
  SystemSettings: 'system:settings',
  HangfireDashboard: 'system:hangfire',
  // Audit
  AuditRead: 'audit:read',
  AuditExport: 'audit:export',
  AuditEntityHistory: 'audit:entity-history',
  // Blog Posts
  BlogPostsRead: 'blog-posts:read',
  BlogPostsCreate: 'blog-posts:create',
  BlogPostsUpdate: 'blog-posts:update',
  BlogPostsDelete: 'blog-posts:delete',
  BlogPostsPublish: 'blog-posts:publish',
  // Blog Categories
  BlogCategoriesRead: 'blog-categories:read',
  BlogCategoriesCreate: 'blog-categories:create',
  BlogCategoriesUpdate: 'blog-categories:update',
  BlogCategoriesDelete: 'blog-categories:delete',
  // Blog Tags
  BlogTagsRead: 'blog-tags:read',
  BlogTagsCreate: 'blog-tags:create',
  BlogTagsUpdate: 'blog-tags:update',
  BlogTagsDelete: 'blog-tags:delete',
  // Platform Settings
  PlatformSettingsRead: 'platform-settings:read',
  PlatformSettingsManage: 'platform-settings:manage',
  // Payment Gateways
  PaymentGatewaysRead: 'payment-gateways:read',
  PaymentGatewaysManage: 'payment-gateways:manage',
} as const

export type PermissionKey = (typeof Permissions)[keyof typeof Permissions]
