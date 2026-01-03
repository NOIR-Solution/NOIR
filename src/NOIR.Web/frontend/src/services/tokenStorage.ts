/**
 * Token Storage Service
 *
 * Manages JWT tokens in localStorage for authentication.
 * This approach is more compatible with webviews and embedded contexts
 * (like Vibe Kanban) where HTTP-only cookies may not work properly.
 *
 * Security considerations:
 * - Tokens are stored in localStorage (accessible to JavaScript)
 * - This is a trade-off for compatibility with webview environments
 * - For maximum security in pure browser contexts, HTTP-only cookies are preferred
 * - Tokens have short expiration times to limit exposure
 */

const STORAGE_KEYS = {
  ACCESS_TOKEN: 'noir.accessToken',
  REFRESH_TOKEN: 'noir.refreshToken',
  TOKEN_EXPIRY: 'noir.tokenExpiry',
} as const

export interface StoredTokens {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

/**
 * Store authentication tokens in localStorage
 * @returns true if successful, false if storage unavailable (e.g., Safari private mode)
 */
export function storeTokens(tokens: StoredTokens): boolean {
  try {
    localStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, tokens.accessToken)
    localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, tokens.refreshToken)
    localStorage.setItem(STORAGE_KEYS.TOKEN_EXPIRY, tokens.expiresAt)
    return true
  } catch (error) {
    console.error('Failed to store tokens:', error)
    return false
  }
}

/**
 * Retrieve stored tokens from localStorage
 */
export function getStoredTokens(): StoredTokens | null {
  try {
    const accessToken = localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN)
    const refreshToken = localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN)
    const expiresAt = localStorage.getItem(STORAGE_KEYS.TOKEN_EXPIRY)

    if (!accessToken || !refreshToken || !expiresAt) {
      return null
    }

    return { accessToken, refreshToken, expiresAt }
  } catch (error) {
    console.error('Failed to retrieve tokens:', error)
    return null
  }
}

/**
 * Get just the access token for Authorization header
 */
export function getAccessToken(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN)
  } catch {
    return null
  }
}

/**
 * Get the refresh token
 */
export function getRefreshToken(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN)
  } catch {
    return null
  }
}

/**
 * Clear all stored tokens (logout)
 */
export function clearTokens(): void {
  try {
    localStorage.removeItem(STORAGE_KEYS.ACCESS_TOKEN)
    localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN)
    localStorage.removeItem(STORAGE_KEYS.TOKEN_EXPIRY)
  } catch (error) {
    console.error('Failed to clear tokens:', error)
  }
}

/**
 * Check if the access token is expired
 */
export function isTokenExpired(): boolean {
  try {
    const expiresAt = localStorage.getItem(STORAGE_KEYS.TOKEN_EXPIRY)
    if (!expiresAt) return true

    const expiryDate = new Date(expiresAt)
    // Consider token expired 30 seconds before actual expiry for safety
    return expiryDate.getTime() - 30000 < Date.now()
  } catch {
    return true
  }
}

/**
 * Check if we have valid stored tokens
 */
export function hasValidTokens(): boolean {
  const tokens = getStoredTokens()
  return tokens !== null && !isTokenExpired()
}
