/**
 * User-related types for User Management
 * These mirror the backend User DTOs exactly
 */

/**
 * Full user details
 * Matches backend UserDto from NOIR.Application.Features.Users.DTOs
 */
export interface User {
  id: string
  email: string
  userName: string | null
  displayName: string | null
  firstName: string | null
  lastName: string | null
  emailConfirmed: boolean
  lockoutEnabled: boolean
  lockoutEnd: string | null
  roles: string[]
}

/**
 * User list item (lighter version for table display)
 * Matches backend UserListDto
 */
export interface UserListItem {
  id: string
  email: string
  displayName: string | null
  isLocked: boolean
  roles: string[]
}

/**
 * User profile with more details
 * Matches backend UserProfileDto
 */
export interface UserProfile {
  id: string
  email: string
  firstName: string | null
  lastName: string | null
  displayName: string | null
  fullName: string
  phoneNumber: string | null
  avatarUrl: string | null
  roles: string[]
  tenantId: string | null
  isActive: boolean
  createdAt: string
  modifiedAt: string | null
}

/**
 * User's effective permissions
 * Matches backend UserPermissionsDto
 */
export interface UserPermissions {
  userId: string
  email: string
  roles: string[]
  permissions: string[]
}

/**
 * Update user request
 * Matches backend UpdateUserCommand
 */
export interface UpdateUserRequest {
  userId: string
  displayName?: string | null
  firstName?: string | null
  lastName?: string | null
  lockoutEnabled?: boolean | null
}

/**
 * Assign roles to user request
 * Matches backend AssignRolesToUserCommand
 */
export interface AssignRolesToUserRequest {
  userId: string
  roleNames: string[]
}

/**
 * Create user request
 * Matches backend CreateUserCommand
 */
export interface CreateUserRequest {
  email: string
  password: string
  firstName?: string | null
  lastName?: string | null
  displayName?: string | null
  roleNames?: string[] | null
}
