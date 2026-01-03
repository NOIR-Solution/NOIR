import { useCallback } from 'react'
import { login as loginApi } from '@/services/auth'
import { useAuthContext } from '@/contexts/AuthContext'
import type { LoginRequest, AuthResponse } from '@/types'

/**
 * Custom hook for handling login with automatic auth context synchronization.
 *
 * This hook encapsulates the pattern of:
 * 1. Calling the login API to authenticate (tokens stored in localStorage)
 * 2. Refreshing the auth context so ProtectedRoute recognizes the authenticated state
 *
 * Using this hook prevents the common mistake of forgetting to call checkAuth()
 * after a successful login, which would cause ProtectedRoute to redirect back to login.
 *
 * @example
 * const { login } = useLogin()
 *
 * const handleSubmit = async () => {
 *   try {
 *     await login({ email, password })
 *     navigate('/dashboard')
 *   } catch (err) {
 *     setError(err.message)
 *   }
 * }
 */
export function useLogin() {
  const { checkAuth } = useAuthContext()

  const login = useCallback(async (credentials: LoginRequest): Promise<AuthResponse> => {
    // Authenticate with the server (tokens stored in localStorage)
    const response = await loginApi(credentials)

    // Sync auth context so ProtectedRoute sees us as authenticated
    await checkAuth()

    return response
  }, [checkAuth])

  return { login }
}
