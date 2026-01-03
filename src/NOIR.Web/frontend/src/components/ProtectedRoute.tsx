import { Navigate, useLocation } from 'react-router-dom'
import { useAuthContext } from '@/contexts/AuthContext'
import type { ReactNode } from 'react'
import { themeClasses } from '@/config/theme'

interface ProtectedRouteProps {
  children: ReactNode
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { isAuthenticated, isLoading } = useAuthContext()
  const location = useLocation()

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className={`animate-spin rounded-full h-8 w-8 border-b-2 ${themeClasses.borderPrimary}`} />
      </div>
    )
  }

  if (!isAuthenticated) {
    const returnUrl = encodeURIComponent(location.pathname + location.search)
    return <Navigate to={`/login?returnUrl=${returnUrl}`} replace />
  }

  return <>{children}</>
}
