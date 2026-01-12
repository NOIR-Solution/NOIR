/**
 * Authentication-related types
 * These mirror the backend Auth DTOs exactly
 */

/**
 * Login request payload
 * Maps to backend LoginCommand (UseCookies handled by auth.ts service)
 */
export interface LoginRequest {
  email: string
  password: string
}

/**
 * Authentication response with tokens
 * Matches backend AuthResponse DTO from NOIR.Application.Features.Auth.DTOs
 */
export interface AuthResponse {
  userId: string
  email: string
  accessToken: string
  refreshToken: string
  expiresAt: string // DateTimeOffset serializes as ISO 8601 string
}

/**
 * Current authenticated user info
 * Matches backend CurrentUserDto from GetCurrentUser query
 */
export interface CurrentUser {
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
  createdAt: string // DateTimeOffset serializes as ISO 8601 string
}
