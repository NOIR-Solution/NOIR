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
import type { LoginRequest, AuthResponse, CurrentUser } from '@/types'
import { storeTokens, clearTokens, getAccessToken } from './tokenStorage'
import { apiClient, apiClientPublic, ApiError } from './apiClient'

/**
 * Authenticate user with email and password
 * Sets HTTP-only cookies AND stores tokens in localStorage (dual auth)
 * @throws Error on login failure or storage unavailable
 */
export async function login(request: LoginRequest): Promise<AuthResponse> {
  // useCookies=true sets HTTP-only cookies for server-rendered pages (/api/docs, /hangfire)
  // The response still contains tokens which we store in localStorage for API calls
  const data = await apiClientPublic<AuthResponse>(
    '/auth/login?useCookies=true',
    {
      method: 'POST',
      body: JSON.stringify(request),
    }
  )

  // Store tokens in localStorage
  const stored = storeTokens({
    accessToken: data.accessToken,
    refreshToken: data.refreshToken,
    expiresAt: data.expiresAt,
  })

  if (!stored) {
    throw new Error(
      'Unable to store authentication tokens. Please enable localStorage or disable private browsing.'
    )
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
