import { useCallback } from 'react'
import { login as loginApi } from '@/services/auth'
import { useAuthContext } from '@/contexts/AuthContext'
import type { LoginRequest, LoginResponse } from '@/types'

/**
 * Custom hook for handling login with automatic auth context synchronization.
 *
 * This hook encapsulates the pattern of:
 * 1. Calling the login API to authenticate (tokens stored in localStorage)
 * 2. Refreshing the auth context so ProtectedRoute recognizes the authenticated state
 *
 * The login API may return either:
 * - success=true with auth tokens (single tenant match, login complete)
 * - requiresTenantSelection=true with tenant list (user must select tenant and retry)
 *
 * Auth context is only synced when login is successful (success=true).
 *
 * @example
 * const { login } = useLogin()
 *
 * const handleSubmit = async () => {
 *   try {
 *     const result = await login({ email, password })
 *     if (result.success) {
 *       navigate('/dashboard')
 *     } else if (result.requiresTenantSelection) {
 *       // Show tenant selection UI
 *     }
 *   } catch (err) {
 *     setError(err.message)
 *   }
 * }
 */
export function useLogin() {
  const { checkAuth } = useAuthContext()

  const login = useCallback(async (credentials: LoginRequest): Promise<LoginResponse> => {
    // Authenticate with the server (tokens stored in localStorage if successful)
    const response = await loginApi(credentials)

    // Only sync auth context when login is successful (not when tenant selection required)
    if (response.success) {
      await checkAuth()
    }

    return response
  }, [checkAuth])

  return { login }
}
