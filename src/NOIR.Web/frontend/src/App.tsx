import { Suspense, lazy } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'sonner'
import { AuthProvider } from '@/contexts/AuthContext'
import { BrandingProvider } from '@/contexts/BrandingContext'
import { RegionalSettingsProvider } from '@/contexts/RegionalSettingsContext'
import { NotificationProvider } from '@/contexts/NotificationContext'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { PortalLayout } from '@/layouts/PortalLayout'
import { PageSkeleton } from '@/components/ui/page-loader'

import LoginPage from '@/pages/Login'
import LandingPage from '@/pages/Landing'
import './index.css'

// Loading fallback for lazy-loaded routes - uses skeleton for better UX
const LazyFallback = () => <PageSkeleton />

// Lazy load portal pages for better loading experience
const Dashboard = lazy(() => import('@/pages/portal/Dashboard'))
const SettingsPage = lazy(() => import('@/pages/portal/Settings'))
const Notifications = lazy(() => import('@/pages/portal/Notifications'))
const NotificationPreferences = lazy(() => import('@/pages/portal/NotificationPreferences'))
const TenantsPage = lazy(() => import('@/pages/portal/admin/tenants/TenantsPage'))
const TenantDetailPage = lazy(() => import('@/pages/portal/admin/tenants/TenantDetailPage'))
const RolesPage = lazy(() => import('@/pages/portal/admin/roles/RolesPage'))
const UsersPage = lazy(() => import('@/pages/portal/admin/users/UsersPage'))
const ActivityTimelinePage = lazy(() => import('@/pages/portal/admin/activity-timeline/ActivityTimelinePage'))
const DeveloperLogsPage = lazy(() => import('@/pages/portal/admin/developer-logs/DeveloperLogsPage'))
// Blog CMS
const BlogPostsPage = lazy(() => import('@/pages/portal/blog/posts/BlogPostsPage'))
const PostEditorPage = lazy(() => import('@/pages/portal/blog/posts/PostEditorPage'))
const BlogCategoriesPage = lazy(() => import('@/pages/portal/blog/categories/BlogCategoriesPage'))
const BlogTagsPage = lazy(() => import('@/pages/portal/blog/tags/BlogTagsPage'))
// Platform Settings
const PlatformSettingsPage = lazy(() => import('@/pages/portal/admin/platform-settings/PlatformSettingsPage'))
// Tenant Settings (includes Payment Gateways tab)
const TenantSettingsPage = lazy(() => import('@/pages/portal/admin/tenant-settings/TenantSettingsPage'))
// Email templates - edit page only (list is in Tenant Settings)
import { EmailTemplateEditPage } from '@/pages/portal/email-templates'
// Legal pages - edit page only (list is in Tenant Settings)
const LegalPageEditPage = lazy(() => import('@/pages/portal/legal-pages/LegalPageEditPage'))
// Public legal pages
const TermsPage = lazy(() => import('@/pages/TermsPage'))
const PrivacyPage = lazy(() => import('@/pages/PrivacyPage'))

// Forgot password flow - keep as eager load (auth flow should be fast)
import ForgotPasswordPage from '@/pages/forgot-password/ForgotPassword'
import VerifyOtpPage from '@/pages/forgot-password/VerifyOtp'
import ResetPasswordPage from '@/pages/forgot-password/ResetPassword'
import SuccessPage from '@/pages/forgot-password/Success'

function App() {
  return (
    <ThemeProvider defaultTheme="system">
      <AuthProvider>
        <BrandingProvider>
        <RegionalSettingsProvider>
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

          {/* Public Legal Pages */}
          <Route path="/terms" element={<Suspense fallback={<LazyFallback />}><TermsPage /></Suspense>} />
          <Route path="/privacy" element={<Suspense fallback={<LazyFallback />}><PrivacyPage /></Suspense>} />

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
            {/* Email templates and Legal pages edit routes - list views are in Tenant Settings */}
            <Route path="email-templates/:id" element={<EmailTemplateEditPage />} />
            <Route path="legal-pages/:id" element={<Suspense fallback={<LazyFallback />}><LegalPageEditPage /></Suspense>} />
            <Route path="notifications" element={<Suspense fallback={<LazyFallback />}><Notifications /></Suspense>} />
            <Route path="settings/notifications" element={<Suspense fallback={<LazyFallback />}><NotificationPreferences /></Suspense>} />
            {/* Admin Routes */}
            <Route path="admin/platform-settings" element={<Suspense fallback={<LazyFallback />}><PlatformSettingsPage /></Suspense>} />
            <Route path="admin/tenant-settings" element={<Suspense fallback={<LazyFallback />}><TenantSettingsPage /></Suspense>} />
            {/* Payment Gateways redirect - now a tab in Tenant Settings */}
            <Route path="admin/payment-gateways" element={<Navigate to="/portal/admin/tenant-settings?tab=paymentGateways" replace />} />
            <Route path="admin/tenants" element={<Suspense fallback={<LazyFallback />}><TenantsPage /></Suspense>} />
            <Route path="admin/tenants/:id" element={<Suspense fallback={<LazyFallback />}><TenantDetailPage /></Suspense>} />
            <Route path="admin/roles" element={<Suspense fallback={<LazyFallback />}><RolesPage /></Suspense>} />
            <Route path="admin/users" element={<Suspense fallback={<LazyFallback />}><UsersPage /></Suspense>} />
            <Route path="activity-timeline" element={<Suspense fallback={<LazyFallback />}><ActivityTimelinePage /></Suspense>} />
            <Route path="developer-logs" element={<Suspense fallback={<LazyFallback />}><DeveloperLogsPage /></Suspense>} />
            {/* Blog CMS */}
            <Route path="blog/posts" element={<Suspense fallback={<LazyFallback />}><BlogPostsPage /></Suspense>} />
            <Route path="blog/posts/new" element={<Suspense fallback={<LazyFallback />}><PostEditorPage /></Suspense>} />
            <Route path="blog/posts/:id/edit" element={<Suspense fallback={<LazyFallback />}><PostEditorPage /></Suspense>} />
            <Route path="blog/categories" element={<Suspense fallback={<LazyFallback />}><BlogCategoriesPage /></Suspense>} />
            <Route path="blog/tags" element={<Suspense fallback={<LazyFallback />}><BlogTagsPage /></Suspense>} />
          </Route>

          {/* Catch-all redirect to landing */}
          <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
          </BrowserRouter>
        </NotificationProvider>
        </RegionalSettingsProvider>
        </BrandingProvider>
      </AuthProvider>
    </ThemeProvider>
  )
}

export default App
