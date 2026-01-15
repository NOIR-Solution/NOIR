import { Suspense, lazy } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'sonner'
import { AuthProvider } from '@/contexts/AuthContext'
import { NotificationProvider } from '@/contexts/NotificationContext'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { PortalLayout } from '@/layouts/PortalLayout'
import { PageLoader } from '@/components/ui/page-loader'

import LoginPage from '@/pages/Login'
import LandingPage from '@/pages/Landing'
import './index.css'

// Loading fallback for lazy-loaded routes
const LazyFallback = () => <PageLoader />

// Lazy load portal pages for better loading experience
const Dashboard = lazy(() => import('@/pages/portal/Dashboard'))
const SettingsPage = lazy(() => import('@/pages/portal/Settings'))
const Notifications = lazy(() => import('@/pages/portal/Notifications'))
const NotificationPreferences = lazy(() => import('@/pages/portal/NotificationPreferences'))
const TenantsPage = lazy(() => import('@/pages/portal/admin/tenants/TenantsPage'))
const TenantDetailPage = lazy(() => import('@/pages/portal/admin/tenants/TenantDetailPage'))
const RolesPage = lazy(() => import('@/pages/portal/admin/roles/RolesPage'))
const UsersPage = lazy(() => import('@/pages/portal/admin/users/UsersPage'))
// Email templates - keep named exports as eager load (smaller components)
import { EmailTemplatesPage, EmailTemplateEditPage } from '@/pages/portal/email-templates'

// Forgot password flow - keep as eager load (auth flow should be fast)
import ForgotPasswordPage from '@/pages/forgot-password/ForgotPassword'
import VerifyOtpPage from '@/pages/forgot-password/VerifyOtp'
import ResetPasswordPage from '@/pages/forgot-password/ResetPassword'
import SuccessPage from '@/pages/forgot-password/Success'

function App() {
  return (
    <AuthProvider>
      <NotificationProvider>
        <BrowserRouter>
        <Toaster
          position="top-center"
          richColors
          toastOptions={{
            classNames: {
              success: 'bg-green-50 border-green-200 text-green-800',
              error: 'bg-red-50 border-red-200 text-red-800',
              info: 'bg-blue-50 border-blue-200 text-blue-800',
            }
          }}
        />
        <Routes>
          {/* Public Routes */}
          <Route path="/" element={<LandingPage />} />
          <Route path="/login" element={<LoginPage />} />

          {/* Forgot Password Flow */}
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/forgot-password/verify" element={<VerifyOtpPage />} />
          <Route path="/forgot-password/reset" element={<ResetPasswordPage />} />
          <Route path="/forgot-password/success" element={<SuccessPage />} />

          {/* Protected Portal Routes */}
          <Route
            path="/portal"
            element={
              <ProtectedRoute>
                <PortalLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<Suspense fallback={<LazyFallback />}><Dashboard /></Suspense>} />
            <Route path="settings" element={<Suspense fallback={<LazyFallback />}><SettingsPage /></Suspense>} />
            <Route path="email-templates" element={<EmailTemplatesPage />} />
            <Route path="email-templates/:id" element={<EmailTemplateEditPage />} />
            <Route path="notifications" element={<Suspense fallback={<LazyFallback />}><Notifications /></Suspense>} />
            <Route path="settings/notifications" element={<Suspense fallback={<LazyFallback />}><NotificationPreferences /></Suspense>} />
            {/* Admin Routes */}
            <Route path="admin/tenants" element={<Suspense fallback={<LazyFallback />}><TenantsPage /></Suspense>} />
            <Route path="admin/tenants/:id" element={<Suspense fallback={<LazyFallback />}><TenantDetailPage /></Suspense>} />
            <Route path="admin/roles" element={<Suspense fallback={<LazyFallback />}><RolesPage /></Suspense>} />
            <Route path="admin/users" element={<Suspense fallback={<LazyFallback />}><UsersPage /></Suspense>} />
          </Route>

          {/* Catch-all redirect to landing */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
        </BrowserRouter>
      </NotificationProvider>
    </AuthProvider>
  )
}

export default App
