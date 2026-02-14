/**
 * Page Context Tracker for Audit Logging
 *
 * Provides a simple mechanism to track the current UI page context
 * (e.g., "Users", "Tenants", "Roles") and include it in API requests
 * via the X-Page-Context header.
 *
 * This enables the Activity Timeline to show user-friendly context
 * like "Users" instead of technical handler names.
 *
 * Usage:
 * 1. Call setPageContext('Users') when entering a page
 * 2. Call clearPageContext() when leaving
 * 3. The apiClient automatically includes the header
 */

let currentPageContext: string | null = null

/**
 * Set the current page context for audit logging.
 * @param context The page context name (e.g., "Users", "Tenants", "Roles")
 */
export const setPageContext = (context: string): void => {
  currentPageContext = context
}

/**
 * Clear the current page context.
 */
export const clearPageContext = (): void => {
  currentPageContext = null
}

/**
 * Get the current page context.
 * Used by apiClient to add the X-Page-Context header.
 */
export const getPageContext = (): string | null => {
  return currentPageContext
}
