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
import { getPageContext } from './pageContext'
import type { AuthResponse, ApiError as ApiErrorType } from '@/types'
import i18n from '@/i18n'

const API_BASE = '/api'

/**
 * Get the current tenant identifier for multi-tenant API calls.
 * Used by apiClientPublic for unauthenticated requests (login, forgot-password).
 * Authenticated requests use the tenant_id JWT claim instead.
 *
 * For production multi-tenancy, derive from hostname/subdomain.
 */
const getTenantIdentifier = (): string => {
  return 'default'
}

/**
 * Custom API Error class for consistent error handling
 * Automatically logs error details to console for customer support
 */
export class ApiError extends Error {
  status: number
  response?: ApiErrorType

  constructor(message: string, status: number, response?: ApiErrorType) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.response = response

    // Always log to console for support debugging
    this.logToConsole()
  }

  /**
   * Get the NOIR error code from the response
   */
  get errorCode(): string | undefined {
    return this.response?.errorCode
  }

  /**
   * Get the correlation ID for support tracking
   */
  get correlationId(): string | undefined {
    return this.response?.correlationId
  }

  /**
   * Get the error timestamp
   */
  get timestamp(): string | undefined {
    return this.response?.timestamp
  }

  /**
   * Get validation errors by field name
   */
  get errors(): Record<string, string[]> | undefined {
    return this.response?.errors
  }

  /**
   * Check if this is a validation error with field-specific errors
   */
  get hasFieldErrors(): boolean {
    return !!this.response?.errors && Object.keys(this.response.errors).length > 0
  }

  /**
   * Get a formatted message including field errors if available
   */
  getDetailedMessage(): string {
    if (!this.hasFieldErrors || !this.errors) {
      return this.message
    }

    const fieldMessages = Object.entries(this.errors)
      .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
      .join('\n')

    return `${this.message}\n${fieldMessages}`
  }

  /**
   * Log error details to console for customer support
   * Format is designed to be easily copied and shared with support team
   */
  private logToConsole(): void {
    const errorCode = this.response?.errorCode || 'UNKNOWN'
    const correlationId = this.response?.correlationId || 'N/A'
    const timestamp = this.response?.timestamp || new Date().toISOString()

    console.error(
      `%c Error [${errorCode}] `,
      'background: #dc2626; color: white; font-weight: bold; padding: 2px 6px; border-radius: 3px;'
    )
    console.error(
      '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n' +
      `Code:         ${errorCode}\n` +
      `Correlation:  ${correlationId}\n` +
      `Timestamp:    ${timestamp}\n` +
      `Status:       ${this.status}\n` +
      '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n' +
      'Please provide this information to support if needed.'
    )
  }
}

/**
 * Try to refresh the access token using stored refresh token
 * @returns true if refresh succeeded, false otherwise
 */
const tryRefreshToken = async (): Promise<boolean> => {
  const refreshTokenValue = getRefreshToken()
  const accessTokenValue = getAccessToken()
  if (!refreshTokenValue || !accessTokenValue) {
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
      body: JSON.stringify({ accessToken: accessTokenValue, refreshToken: refreshTokenValue }),
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
export const apiClient = async <T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> => {
  const token = getAccessToken()
  const pageContext = getPageContext()

  // Merge headers with Authorization, Accept-Language, and Page Context
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    'Accept-Language': i18n.language,
    ...options.headers,
  }

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`
  }

  // Add page context for audit logging (Activity Timeline display)
  if (pageContext) {
    (headers as Record<string, string>)['X-Page-Context'] = pageContext
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

    // Provide user-friendly error messages for common HTTP status codes
    let message = error.detail || error.title || `HTTP ${response.status}`
    if (response.status === 403) {
      // Use translation key that will be handled by the calling code
      message = i18n.t('messages.permissionDenied', { ns: 'common' }) || 'You don\'t have permission to perform this action.'
    } else if (response.status === 401 && !token) {
      message = i18n.t('messages.sessionExpired', { ns: 'common' }) || 'Your session has expired. Please sign in again.'
    }

    throw new ApiError(
      message,
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
export const apiClientPublic = async <T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> => {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    'Accept-Language': i18n.language,
    'X-Tenant': getTenantIdentifier(),
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

    // Provide user-friendly error messages for common HTTP status codes
    let message = error.detail || error.title || `HTTP ${response.status}`
    if (response.status === 403) {
      message = i18n.t('messages.permissionDenied', { ns: 'common' }) || 'You don\'t have permission to perform this action.'
    }

    throw new ApiError(
      message,
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
