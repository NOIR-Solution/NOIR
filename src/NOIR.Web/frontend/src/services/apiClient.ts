/**
 * Centralized API Client
 *
 * Provides a unified HTTP client with:
 * - Automatic Bearer token injection from localStorage
 * - Auto-refresh on 401 responses (single retry)
 * - Consistent error handling across all API calls
 *
 * Security Note: Tokens are stored in localStorage for Vibe Kanban webview
 * compatibility. This is vulnerable to XSS - mitigated by CSP headers,
 * short token TTL (15 min), and refresh token rotation on the backend.
 */
import { getAccessToken, getRefreshToken, storeTokens, clearTokens } from './tokenStorage'
import type { AuthResponse, ApiError as ApiErrorType } from '@/types'

const API_BASE = '/api'

/**
 * Custom API Error class for consistent error handling
 */
export class ApiError extends Error {
  status: number
  response?: ApiErrorType

  constructor(message: string, status: number, response?: ApiErrorType) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.response = response
  }
}

/**
 * Try to refresh the access token using stored refresh token
 * @returns true if refresh succeeded, false otherwise
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
 * Enhanced fetch with automatic Bearer token injection and token refresh
 *
 * @param endpoint - API endpoint (e.g., '/auth/me')
 * @param options - Standard fetch options
 * @returns Parsed JSON response
 * @throws ApiError on failure
 */
export async function apiClient<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token = getAccessToken()

  // Merge headers with Authorization if token exists
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  }

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`
  }

  let response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
  })

  // Auto-refresh on 401 (one retry only)
  if (response.status === 401 && token) {
    const refreshed = await tryRefreshToken()
    if (refreshed) {
      // Retry with new token
      const newToken = getAccessToken()
      if (newToken) {
        (headers as Record<string, string>)['Authorization'] = `Bearer ${newToken}`
        response = await fetch(`${API_BASE}${endpoint}`, {
          ...options,
          headers,
        })
      }
    }
  }

  // Handle errors consistently
  if (!response.ok) {
    const error = await response.json().catch(() => ({
      title: 'Request failed',
      status: response.status,
    })) as ApiErrorType

    throw new ApiError(
      error.detail || error.title || `HTTP ${response.status}`,
      response.status,
      error
    )
  }

  // Handle empty responses (204 No Content)
  const text = await response.text()
  if (!text) {
    return undefined as T
  }

  return JSON.parse(text) as T
}

/**
 * API client without authentication (for public endpoints like login)
 */
export async function apiClientPublic<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({
      title: 'Request failed',
      status: response.status,
    })) as ApiErrorType

    throw new ApiError(
      error.detail || error.title || `HTTP ${response.status}`,
      response.status,
      error
    )
  }

  const text = await response.text()
  if (!text) {
    return undefined as T
  }

  return JSON.parse(text) as T
}
