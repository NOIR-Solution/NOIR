/**
 * Authentication API service
 *
 * Uses localStorage-based token storage for compatibility with
 * webview environments (like Vibe Kanban) where HTTP-only cookies
 * may not work properly.
 */
import type { LoginRequest, AuthResponse, CurrentUser, ApiError } from '@/types'
import { storeTokens, clearTokens, getAccessToken, getRefreshToken } from './tokenStorage'

const API_BASE = '/api'

/**
 * Authenticate user with email and password
 * Stores tokens in localStorage for subsequent requests
 */
export async function login(request: LoginRequest): Promise<AuthResponse> {
  // Use useCookies=false since we're managing tokens in localStorage
  const response = await fetch(`${API_BASE}/auth/login?useCookies=false`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    const error: ApiError = await response.json()
    throw new Error(error.detail || error.title || 'Login failed')
  }

  const data: AuthResponse = await response.json()

  // Store tokens in localStorage
  storeTokens({
    accessToken: data.accessToken,
    refreshToken: data.refreshToken,
    expiresAt: data.expiresAt,
  })

  return data
}

/**
 * Try to refresh the access token
 */
async function tryRefreshToken(): Promise<boolean> {
  const refreshTokenValue = getRefreshToken()

  if (!refreshTokenValue) {
    return false
  }

  try {
    const response = await fetch(`${API_BASE}/auth/refresh?useCookies=false`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken: refreshTokenValue }),
    })

    if (!response.ok) {
      clearTokens()
      return false
    }

    const data: AuthResponse = await response.json()
    storeTokens({
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      expiresAt: data.expiresAt,
    })

    return true
  } catch {
    clearTokens()
    return false
  }
}

/**
 * Get the current authenticated user's information
 * Uses the stored access token for authentication
 */
export async function getCurrentUser(): Promise<CurrentUser | null> {
  const token = getAccessToken()
  if (!token) {
    return null
  }

  try {
    const response = await fetch(`${API_BASE}/auth/me`, {
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    })

    if (!response.ok) {
      // Token might be invalid - try to refresh
      if (response.status === 401) {
        const refreshed = await tryRefreshToken()
        if (refreshed) {
          // Retry with new token
          const newToken = getAccessToken()
          if (newToken) {
            const retryResponse = await fetch(`${API_BASE}/auth/me`, {
              headers: {
                'Authorization': `Bearer ${newToken}`,
              },
            })
            if (retryResponse.ok) {
              return retryResponse.json()
            }
          }
        }
        // Refresh failed, clear tokens
        clearTokens()
      }
      return null
    }

    return response.json()
  } catch {
    return null
  }
}

/**
 * Log out the current user
 * Clears stored tokens and optionally notifies the server
 */
export async function logout(): Promise<void> {
  const token = getAccessToken()

  // Clear tokens first (even if server call fails)
  clearTokens()

  // Notify server to revoke refresh token (best effort)
  if (token) {
    try {
      await fetch(`${API_BASE}/auth/logout`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      })
    } catch {
      // Ignore errors - tokens are already cleared locally
    }
  }
}

