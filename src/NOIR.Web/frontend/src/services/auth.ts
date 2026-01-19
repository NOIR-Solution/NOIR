/**
 * Authentication API service
 *
 * Dual Authentication Strategy:
 * - Sets HTTP-only cookies via useCookies=true (for server-rendered pages: /api/docs, /hangfire)
 * - Stores tokens in localStorage (for API calls in Vibe Kanban webview)
 *
 * This dual approach ensures:
 * - Server-rendered pages work with cookie auth
 * - SPA/webview pages work with Bearer token auth
 *
 * Error Handling Contract:
 * - login() - THROWS on failure (user must handle)
 * - getCurrentUser() - Returns null on auth failure, throws on network/server errors
 * - logout() - Never throws (best effort server notification)
 */
import type { LoginRequest, LoginResponse, AuthResponse, CurrentUser, ActiveSession } from '@/types'
import { storeTokens, clearTokens, getAccessToken } from './tokenStorage'
import { apiClient, apiClientPublic, ApiError } from './apiClient'

/**
 * Authenticate user with email and password
 * Single-step login that may return either:
 * - success=true with auth tokens (login complete)
 * - requiresTenantSelection=true with tenant list (user needs to select tenant)
 *
 * Sets HTTP-only cookies AND stores tokens in localStorage (dual auth) on success
 * @returns LoginResponse with either auth tokens or tenant selection requirement
 * @throws Error on login failure or storage unavailable
 */
export async function login(request: LoginRequest): Promise<LoginResponse> {
  // useCookies=true sets HTTP-only cookies for server-rendered pages (/api/docs, /hangfire)
  const data = await apiClientPublic<LoginResponse>(
    '/auth/login?useCookies=true',
    {
      method: 'POST',
      body: JSON.stringify(request),
    }
  )

  // If login successful (single tenant match), store tokens
  if (data.success && data.auth) {
    const stored = storeTokens({
      accessToken: data.auth.accessToken,
      refreshToken: data.auth.refreshToken,
      expiresAt: data.auth.expiresAt,
    })

    if (!stored) {
      throw new Error(
        'Unable to store authentication tokens. Please enable localStorage or disable private browsing.'
      )
    }
  }

  return data
}

/**
 * Get the current authenticated user's information
 * Uses the stored access token for authentication
 * @returns CurrentUser if authenticated, null if not logged in
 * @throws ApiError on network/server errors (not 401)
 */
export async function getCurrentUser(): Promise<CurrentUser | null> {
  const token = getAccessToken()
  if (!token) {
    return null
  }

  try {
    return await apiClient<CurrentUser>('/auth/me')
  } catch (error) {
    // Auth errors - return null (user not logged in)
    if (error instanceof ApiError && error.status === 401) {
      clearTokens()
      return null
    }
    // Network/server errors - propagate (something's broken)
    throw error
  }
}

/**
 * Log out the current user
 * Clears stored tokens and optionally notifies the server
 * Never throws - tokens are cleared regardless of server response
 */
export async function logout(): Promise<void> {
  try {
    await apiClient('/auth/logout', { method: 'POST' })
  } catch {
    // Ignore errors - tokens will be cleared locally regardless
  } finally {
    clearTokens()
  }
}

/**
 * Get active sessions for the current user
 * @returns List of active sessions
 * @throws ApiError on network/server errors
 */
export async function getActiveSessions(): Promise<ActiveSession[]> {
  return await apiClient<ActiveSession[]>('/auth/me/sessions')
}

/**
 * Revoke a specific session by ID
 * @param sessionId The session ID to revoke
 * @throws ApiError on failure
 */
export async function revokeSession(sessionId: string): Promise<void> {
  await apiClient(`/auth/me/sessions/${sessionId}`, { method: 'DELETE' })
}

/**
 * User permissions response from /auth/me/permissions
 */
export interface UserPermissions {
  userId: string
  email: string
  roles: string[]
  permissions: string[]
}

/**
 * Get the current user's effective permissions
 * @returns UserPermissions with roles and permissions arrays
 * @throws ApiError on network/server errors
 */
export async function getUserPermissions(): Promise<UserPermissions> {
  return await apiClient<UserPermissions>('/auth/me/permissions')
}
