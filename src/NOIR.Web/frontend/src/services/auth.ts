/**
 * Authentication API service
 * Handles all auth-related API calls
 */
import type { LoginRequest, AuthResponse, CurrentUser, ApiError } from '@/types'

const API_BASE = '/api'

/**
 * Authenticate user with email and password
 * @param request Login credentials
 * @param useCookies Whether to store tokens in HTTP-only cookies (recommended for browser)
 */
export async function login(request: LoginRequest, useCookies = true): Promise<AuthResponse> {
  const response = await fetch(`${API_BASE}/auth/login?useCookies=${useCookies}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include',
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    const error: ApiError = await response.json()
    throw new Error(error.detail || error.title || 'Login failed')
  }

  return response.json()
}

/**
 * Get the current authenticated user's information
 * @returns User info or null if not authenticated
 */
export async function getCurrentUser(): Promise<CurrentUser | null> {
  try {
    const response = await fetch(`${API_BASE}/auth/me`, {
      credentials: 'include',
    })

    if (!response.ok) {
      return null
    }

    return response.json()
  } catch {
    return null
  }
}

/**
 * Log out the current user
 * Clears authentication cookies on the server
 */
export async function logout(): Promise<void> {
  await fetch(`${API_BASE}/auth/logout`, {
    method: 'POST',
    credentials: 'include',
  })
}

/**
 * Refresh the authentication token
 * Uses the refresh token cookie to get new tokens
 * @returns New auth response or null if refresh failed
 */
export async function refreshToken(): Promise<AuthResponse | null> {
  try {
    const response = await fetch(`${API_BASE}/auth/refresh?useCookies=true`, {
      method: 'POST',
      credentials: 'include',
    })

    if (!response.ok) {
      return null
    }

    return response.json()
  } catch {
    return null
  }
}
