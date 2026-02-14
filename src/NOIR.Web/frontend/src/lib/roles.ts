/**
 * Role name constants.
 * These MUST match the backend Roles.cs constants exactly.
 * @see NOIR.Domain.Common.Roles
 */
export const Roles = {
  /** Platform-level administrator (TenantId = null, IsPlatformRole = true) */
  PlatformAdmin: 'Platform Admin',
  /** Tenant-level administrator */
  Admin: 'Admin',
  /** Standard user */
  User: 'User',
} as const

export type RoleName = (typeof Roles)[keyof typeof Roles]

/**
 * Check if a user has a specific role.
 * Handles null/undefined safely.
 */
export const hasRole = (userRoles: string[] | undefined | null, role: RoleName): boolean => {
  return userRoles?.includes(role) ?? false
}

/**
 * Check if user is a Platform Admin.
 * Platform Admins have TenantId = null and full system access.
 * Platform Admins do not receive notifications (notifications are tenant-scoped).
 */
export const isPlatformAdmin = (userRoles: string[] | undefined | null): boolean => {
  return hasRole(userRoles, Roles.PlatformAdmin)
}
