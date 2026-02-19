import { Suspense, lazy } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'sonner'
import { AuthProvider } from '@/contexts/AuthContext'
import { BrandingProvider } from '@/contexts/BrandingContext'
import { RegionalSettingsProvider } from '@/contexts/RegionalSettingsContext'
import { NotificationProvider } from '@/contexts/NotificationContext'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { AccessibilityProvider } from '@/contexts/AccessibilityContext'
import { DensityProvider } from '@/contexts/DensityContext'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { PortalLayout } from '@/layouts/PortalLayout'
import { PageSkeleton } from '@uikit'
import { CommandProvider, CommandPalette } from '@/components/command-palette'

import { LoginPage } from '@/layouts/auth/login/LoginPage'
import { WelcomePage } from '@/portal-app/welcome/features/welcome/WelcomePage'
import './index.css'

// Loading fallback for lazy-loaded routes - uses skeleton for better UX
const LazyFallback = () => <PageSkeleton />

// Lazy load portal pages for better loading experience
const DashboardPage = lazy(() => import('@/portal-app/dashboard/features/dashboard/DashboardPage'))
const PersonalSettingsPage = lazy(() => import('@/portal-app/settings/features/personal-settings/PersonalSettingsPage'))
const NotificationsPage = lazy(() => import('@/portal-app/notifications/features/notification-list/NotificationsPage'))
const NotificationPreferencesPage = lazy(() => import('@/portal-app/notifications/features/notification-preferences/NotificationPreferencesPage'))
const TenantsPage = lazy(() => import('@/portal-app/user-access/features/tenant-list/TenantsPage'))
const TenantDetailPage = lazy(() => import('@/portal-app/user-access/features/tenant-detail/TenantDetailPage'))
const RolesPage = lazy(() => import('@/portal-app/user-access/features/role-list/RolesPage'))
const UsersPage = lazy(() => import('@/portal-app/user-access/features/user-list/UsersPage'))
const ActivityTimelinePage = lazy(() => import('@/portal-app/systems/features/activity-timeline/ActivityTimelinePage'))
const DeveloperLogsPage = lazy(() => import('@/portal-app/systems/features/developer-logs/DeveloperLogsPage'))
// Blog CMS
const BlogPostsPage = lazy(() => import('@/portal-app/blogs/features/blog-post-list/BlogPostsPage'))
const BlogPostEditPage = lazy(() => import('@/portal-app/blogs/features/blog-post-edit/BlogPostEditPage'))
const BlogCategoriesPage = lazy(() => import('@/portal-app/blogs/features/blog-category-list/BlogCategoriesPage'))
const BlogTagsPage = lazy(() => import('@/portal-app/blogs/features/blog-tag-list/BlogTagsPage'))
// E-commerce
const ProductsPage = lazy(() => import('@/portal-app/products/features/product-list/ProductsPage'))
const ProductFormPage = lazy(() => import('@/portal-app/products/features/product-edit/ProductFormPage'))
const ProductCategoriesPage = lazy(() => import('@/portal-app/products/features/product-category-list/ProductCategoriesPage'))
const BrandsPage = lazy(() => import('@/portal-app/brands/features/brand-list/BrandsPage'))
const ProductAttributesPage = lazy(() => import('@/portal-app/products/features/product-attribute-list/ProductAttributesPage'))
// Orders, Inventory & Shipping
const OrdersPage = lazy(() => import('@/portal-app/orders/features/order-list/OrdersPage'))
const OrderDetailPage = lazy(() => import('@/portal-app/orders/features/order-detail/OrderDetailPage'))
const InventoryReceiptsPage = lazy(() => import('@/portal-app/inventory/features/inventory-receipts/InventoryReceiptsPage'))
const ShippingPage = lazy(() => import('@/portal-app/shipping/features/shipping-page/ShippingPage'))
// Customers
const CustomersPage = lazy(() => import('@/portal-app/customers/features/customer-list/CustomersPage'))
const CustomerDetailPage = lazy(() => import('@/portal-app/customers/features/customer-detail/CustomerDetailPage'))
// Customer Groups
const CustomerGroupsPage = lazy(() => import('@/portal-app/customer-groups/features/customer-group-list/CustomerGroupsPage'))
// Reviews
const ReviewsPage = lazy(() => import('@/portal-app/reviews/features/review-list/ReviewsPage'))
// Wishlists
const WishlistAnalyticsPage = lazy(() => import('@/portal-app/wishlists/features/wishlist-analytics/WishlistAnalyticsPage'))
const WishlistPage = lazy(() => import('@/portal-app/wishlists/features/wishlist-page/WishlistPage'))
// Marketing
const PromotionsPage = lazy(() => import('@/portal-app/promotions/features/promotion-list/PromotionsPage'))
const ReportsPage = lazy(() => import('@/portal-app/reports/features/reports-page/ReportsPage'))
// Platform Settings
const PlatformSettingsPage = lazy(() => import('@/portal-app/settings/features/platform-settings/PlatformSettingsPage'))
// Tenant Settings (includes Payment Gateways tab)
const TenantSettingsPage = lazy(() => import('@/portal-app/settings/features/tenant-settings/TenantSettingsPage'))
// Email templates - edit page only (list is in Tenant Settings)
import { EmailTemplateEditPage } from '@/portal-app/settings/features/email-template-edit/EmailTemplateEditPage'
// Legal pages - edit page only (list is in Tenant Settings)
const LegalPageEditPage = lazy(() => import('@/portal-app/settings/features/legal-page-edit/LegalPageEditPage'))
// Public legal pages
const TermsPage = lazy(() => import('@/portal-app/welcome/features/terms/TermsPage'))
const PrivacyPage = lazy(() => import('@/portal-app/welcome/features/privacy/PrivacyPage'))

// Forgot password flow - keep as eager load (auth flow should be fast)
import { ForgotPasswordPage } from '@/layouts/auth/forgot-password/ForgotPasswordPage'
import { VerifyOtpPage } from '@/layouts/auth/verify-otp/VerifyOtpPage'
import { ResetPasswordPage } from '@/layouts/auth/reset-password/ResetPasswordPage'
import { AuthSuccessPage } from '@/layouts/auth/auth-success/AuthSuccessPage'

export const App = () => {
  return (
    <ThemeProvider defaultTheme="system">
      <AccessibilityProvider>
        <DensityProvider>
          <AuthProvider>
          <BrandingProvider>
            <RegionalSettingsProvider>
              <NotificationProvider>
                <BrowserRouter>
                  <CommandProvider>
                    <Toaster
                      position="top-center"
                      richColors
                      toastOptions={{
                        classNames: {
                          success: 'bg-green-50 border-green-200 text-green-800',
                          error: 'bg-red-50 border-red-200 text-red-800',
                          info: 'bg-blue-50 border-blue-200 text-blue-800',
                        },
                      }}
                    />
                    <Routes>
                      {/* Public Routes */}
                      <Route path="/" element={<WelcomePage />} />
                      <Route path="/login" element={<LoginPage />} />

                      {/* Public Legal Pages */}
                      <Route path="/terms" element={<Suspense fallback={<LazyFallback />}><TermsPage /></Suspense>} />
                      <Route path="/privacy" element={<Suspense fallback={<LazyFallback />}><PrivacyPage /></Suspense>} />

                      {/* Forgot Password Flow */}
                      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                      <Route path="/forgot-password/verify" element={<VerifyOtpPage />} />
                      <Route path="/forgot-password/reset" element={<ResetPasswordPage />} />
                      <Route path="/forgot-password/success" element={<AuthSuccessPage />} />

                      {/* Protected Portal Routes */}
                      <Route
                        path="/portal"
                        element={
                          <ProtectedRoute>
                            <PortalLayout />
                          </ProtectedRoute>
                        }
                      >
                        <Route index element={<Suspense fallback={<LazyFallback />}><DashboardPage /></Suspense>} />
                        <Route path="settings" element={<Suspense fallback={<LazyFallback />}><PersonalSettingsPage /></Suspense>} />
                        {/* Email templates and Legal pages edit routes - list views are in Tenant Settings */}
                        <Route path="email-templates/:id" element={<EmailTemplateEditPage />} />
                        <Route path="legal-pages/:id" element={<Suspense fallback={<LazyFallback />}><LegalPageEditPage /></Suspense>} />
                        <Route path="notifications" element={<Suspense fallback={<LazyFallback />}><NotificationsPage /></Suspense>} />
                        <Route path="settings/notifications" element={<Suspense fallback={<LazyFallback />}><NotificationPreferencesPage /></Suspense>} />
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
                        <Route path="blog/posts/new" element={<Suspense fallback={<LazyFallback />}><BlogPostEditPage /></Suspense>} />
                        <Route path="blog/posts/:id/edit" element={<Suspense fallback={<LazyFallback />}><BlogPostEditPage /></Suspense>} />
                        <Route path="blog/categories" element={<Suspense fallback={<LazyFallback />}><BlogCategoriesPage /></Suspense>} />
                        <Route path="blog/tags" element={<Suspense fallback={<LazyFallback />}><BlogTagsPage /></Suspense>} />
                        {/* E-commerce */}
                        <Route path="ecommerce/products" element={<Suspense fallback={<LazyFallback />}><ProductsPage /></Suspense>} />
                        <Route path="ecommerce/products/new" element={<Suspense fallback={<LazyFallback />}><ProductFormPage /></Suspense>} />
                        <Route path="ecommerce/products/:id" element={<Suspense fallback={<LazyFallback />}><ProductFormPage /></Suspense>} />
                        <Route path="ecommerce/products/:id/edit" element={<Suspense fallback={<LazyFallback />}><ProductFormPage /></Suspense>} />
                        <Route path="ecommerce/categories" element={<Suspense fallback={<LazyFallback />}><ProductCategoriesPage /></Suspense>} />
                        <Route path="ecommerce/brands" element={<Suspense fallback={<LazyFallback />}><BrandsPage /></Suspense>} />
                        <Route path="ecommerce/attributes" element={<Suspense fallback={<LazyFallback />}><ProductAttributesPage /></Suspense>} />
                        <Route path="ecommerce/orders" element={<Suspense fallback={<LazyFallback />}><OrdersPage /></Suspense>} />
                        <Route path="ecommerce/orders/:id" element={<Suspense fallback={<LazyFallback />}><OrderDetailPage /></Suspense>} />
                        <Route path="ecommerce/inventory" element={<Suspense fallback={<LazyFallback />}><InventoryReceiptsPage /></Suspense>} />
                        <Route path="ecommerce/shipping" element={<Suspense fallback={<LazyFallback />}><ShippingPage /></Suspense>} />
                        {/* Customers */}
                        <Route path="ecommerce/customers" element={<Suspense fallback={<LazyFallback />}><CustomersPage /></Suspense>} />
                        <Route path="ecommerce/customers/:id" element={<Suspense fallback={<LazyFallback />}><CustomerDetailPage /></Suspense>} />
                        {/* Customer Groups */}
                        <Route path="ecommerce/customer-groups" element={<Suspense fallback={<LazyFallback />}><CustomerGroupsPage /></Suspense>} />
                        {/* Reviews */}
                        <Route path="ecommerce/reviews" element={<Suspense fallback={<LazyFallback />}><ReviewsPage /></Suspense>} />
                        {/* Wishlists */}
                        <Route path="ecommerce/wishlists" element={<Suspense fallback={<LazyFallback />}><WishlistAnalyticsPage /></Suspense>} />
                        <Route path="ecommerce/wishlists/manage" element={<Suspense fallback={<LazyFallback />}><WishlistPage /></Suspense>} />
                        {/* Marketing */}
                        <Route path="marketing/promotions" element={<Suspense fallback={<LazyFallback />}><PromotionsPage /></Suspense>} />
                        <Route path="marketing/reports" element={<Suspense fallback={<LazyFallback />}><ReportsPage /></Suspense>} />
                      </Route>

                      {/* Catch-all redirect to landing */}
                      <Route path="*" element={<Navigate to="/" replace />} />
                    </Routes>

                    {/* Global Command Palette (Cmd+K / Ctrl+K) */}
                    <CommandPalette />
                  </CommandProvider>
                </BrowserRouter>
              </NotificationProvider>
            </RegionalSettingsProvider>
          </BrandingProvider>
          </AuthProvider>
        </DensityProvider>
      </AccessibilityProvider>
    </ThemeProvider>
  )
}
