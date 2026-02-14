import { useEffect } from 'react'
import { setPageContext, clearPageContext } from '@/services/pageContext'

/**
 * React hook to set the page context for audit logging.
 * Automatically clears the context when the component unmounts.
 *
 * Usage:
 * ```tsx
 * function UsersPage() {
 *   usePageContext('Users')
 *   // ... rest of component
 * }
 * ```
 *
 * @param context The page context name (e.g., "Users", "Tenants", "Roles")
 */
export const usePageContext = (context: string): void => {
  useEffect(() => {
    setPageContext(context)
    return () => clearPageContext()
  }, [context])
}
