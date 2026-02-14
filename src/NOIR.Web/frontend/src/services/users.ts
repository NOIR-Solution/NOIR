/**
 * User Management API Service
 *
 * Provides methods for managing users, their roles, and permissions.
 */
import { apiClient } from './apiClient'
import type {
  User,
  UserListItem,
  UserProfile,
  UserPermissions,
  CreateUserRequest,
  UpdateUserRequest,
  AssignRolesToUserRequest,
  PaginatedResponse,
} from '@/types'

// ============================================================================
// Users
// ============================================================================

/**
 * Fetch paginated list of users
 */
export const getUsers = async (params: {
  search?: string
  role?: string
  isLocked?: boolean
  page?: number
  pageSize?: number
}): Promise<PaginatedResponse<UserListItem>> => {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('Search', params.search)
  if (params.role) queryParams.append('Role', params.role)
  if (params.isLocked !== undefined) queryParams.append('IsLocked', String(params.isLocked))
  if (params.page) queryParams.append('Page', params.page.toString())
  if (params.pageSize) queryParams.append('PageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<PaginatedResponse<UserListItem>>(`/users${query ? `?${query}` : ''}`)
}

/**
 * Create a new user (admin only)
 */
export const createUser = async (request: CreateUserRequest): Promise<User> => {
  return apiClient<User>('/users', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Fetch a single user by ID with full details
 */
export const getUserById = async (id: string): Promise<UserProfile> => {
  return apiClient<UserProfile>(`/users/${id}`)
}

/**
 * Update user details
 */
export const updateUser = async (request: UpdateUserRequest): Promise<User> => {
  return apiClient<User>(`/users/${request.userId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a user (soft delete - removes from system)
 */
export const deleteUser = async (id: string): Promise<void> => {
  return apiClient<void>(`/users/${id}`, {
    method: 'DELETE',
  })
}

/**
 * Lock a user account (prevents login)
 */
export const lockUser = async (id: string): Promise<void> => {
  return apiClient<void>(`/users/${id}/lock`, {
    method: 'POST',
  })
}

/**
 * Unlock a user account (allows login)
 */
export const unlockUser = async (id: string): Promise<void> => {
  return apiClient<void>(`/users/${id}/unlock`, {
    method: 'POST',
  })
}

// ============================================================================
// User Roles
// ============================================================================

/**
 * Get roles assigned to a user
 */
export const getUserRoles = async (userId: string): Promise<string[]> => {
  return apiClient<string[]>(`/users/${userId}/roles`)
}

/**
 * Assign roles to a user (replaces existing roles)
 */
export const assignRolesToUser = async (request: AssignRolesToUserRequest): Promise<User> => {
  return apiClient<User>(`/users/${request.userId}/roles`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

// ============================================================================
// User Permissions
// ============================================================================

/**
 * Get effective permissions for a user (combined from all roles)
 */
export const getUserPermissions = async (userId: string): Promise<UserPermissions> => {
  return apiClient<UserPermissions>(`/users/${userId}/permissions`)
}
