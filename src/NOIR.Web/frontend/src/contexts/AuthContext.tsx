import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react'
import { getCurrentUser, logout as logoutApi } from '@/services/auth'
import type { CurrentUser } from '@/types'

interface AuthContextValue {
  user: CurrentUser | null
  isLoading: boolean
  isAuthenticated: boolean
  checkAuth: () => Promise<void>
  refreshUser: () => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const checkAuth = useCallback(async () => {
    setIsLoading(true)
    try {
      const currentUser = await getCurrentUser()
      setUser(currentUser)
    } catch {
      setUser(null)
    } finally {
      setIsLoading(false)
    }
  }, [])

  /**
   * Silently refresh user data without triggering loading state.
   * Use this after local mutations (profile update, avatar change) to avoid UI flash.
   */
  const refreshUser = useCallback(async () => {
    try {
      const currentUser = await getCurrentUser()
      setUser(currentUser)
    } catch {
      // Keep existing user data on refresh failure
      // Don't set to null - this is a background refresh
    }
  }, [])

  useEffect(() => {
    checkAuth()
  }, [checkAuth])

  const logout = useCallback(async () => {
    await logoutApi()
    setUser(null)
  }, [])

  return (
    <AuthContext.Provider value={{ user, isLoading, isAuthenticated: !!user, checkAuth, refreshUser, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuthContext() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuthContext must be used within AuthProvider')
  }
  return context
}
