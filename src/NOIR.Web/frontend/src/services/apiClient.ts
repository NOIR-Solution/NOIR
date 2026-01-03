/**
 * Centralized API Client
 *
 * Provides a unified HTTP client with:
 * - Automatic Bearer token injection from localStorage
 * - Auto-refresh on 401 responses (single retry)
 * - Consistent error handling across all API calls
 * - Dual auth support: cookies for server pages + localStorage for API calls
 *
 * Security Note: Tokens are stored in localStorage for Vibe Kanban webview
 * compatibility. This is vulnerable to XSS - mitigated by CSP headers,
 * short token TTL (15 min), and refresh token rotation on the backend.
 *
 * Authentication Strategy:
 * - Login/refresh use useCookies=true to set HTTP-only cookies (for /api/docs, /hangfire)
 * - Tokens are also stored in localStorage for Bearer auth (for webview compatibility)
 * - All requests include credentials to send/receive cookies
 */
import { getAccessToken, getRefreshToken, storeTokens, clearTokens } from './tokenStorage'
import type { AuthResponse, ApiError as ApiErrorType } from '@/types'
import i18n from '@/i18n'

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
    // Use useCookies=true to also refresh the HTTP-only cookies (for /api/docs, /hangfire)
    const response = await fetch(`${API_BASE}/auth/refresh?useCookies=true`, {
      method: 'POST',
      credentials: 'include', // Send and receive cookies
      headers: {
        'Content-Type': 'application/json',
        'Accept-Language': i18n.language,
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

  // Merge headers with Authorization and Accept-Language
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    'Accept-Language': i18n.language,
    ...options.headers,
  }

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`
  }

  let response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    credentials: 'include', // Send cookies for dual auth support
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
          credentials: 'include',
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
    'Accept-Language': i18n.language,
    ...options.headers,
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    credentials: 'include', // Send cookies for dual auth support
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
