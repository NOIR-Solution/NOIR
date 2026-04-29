/**
 * Dashboard Page
 *
 * Modular widget-based dashboard with feature-gated sections.
 * Core widgets always visible; ecommerce, blog, and inventory
 * sections lazy-loaded and gated by feature flags.
 */
import { Suspense, lazy } from 'react'
import { useTranslation } from 'react-i18next'
import { LayoutDashboard } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle, PageHeader } from '@uikit'
import { useAuthContext } from '@/contexts/AuthContext'
import { usePageContext } from '@/hooks/usePageContext'
import { useFeature } from '@/hooks/useFeatures'
import { isPlatformAdmin } from '@/lib/roles'
import { CoreWidgetGroup } from './components/CoreWidgetGroup'
import { DashboardSkeleton } from './components/widgets/DashboardSkeleton'

const EcommerceWidgetGroup = lazy(() =>
  import('./components/EcommerceWidgetGroup').then((m) => ({ default: m.EcommerceWidgetGroup }))
)
const BlogWidgetGroup = lazy(() =>
  import('./components/BlogWidgetGroup').then((m) => ({ default: m.BlogWidgetGroup }))
)
const InventoryWidgetGroup = lazy(() =>
  import('./components/InventoryWidgetGroup').then((m) => ({ default: m.InventoryWidgetGroup }))
)
const CrmWidgetGroup = lazy(() => import('./components/CrmWidgetGroup'))

export const DashboardPage = () => {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()
  usePageContext('Dashboard')

  const { isEnabled: isEcommerceEnabled } = useFeature('Ecommerce.Orders')
  const { isEnabled: isBlogEnabled } = useFeature('Content.Blog')
  const { isEnabled: isInventoryEnabled } = useFeature('Ecommerce.Inventory')
  const { isEnabled: isCrmEnabled } = useFeature('Erp.Crm')

  // Platform admins have no tenant context — tenant-scoped dashboard widgets crash with NPE
  // because EF Core HasQueryFilter dereferences null TenantInfo. Show a system-level welcome instead.
  const isPlatform = isPlatformAdmin(user?.roles)

  return (
    <div className="py-6 space-y-6">
      <PageHeader
        icon={LayoutDashboard}
        title={t('dashboard.title', 'Dashboard')}
        description={t('dashboard.welcome', { name: user?.fullName || t('labels.user', { defaultValue: 'User' }) })}
        responsive
      />

      {isPlatform ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('dashboard.platformAdminTitle', 'Platform Administration')}</CardTitle>
            <CardDescription>
              {t('dashboard.platformAdminDescription', 'Manage tenants, platform settings, and system diagnostics from the sidebar.')}
            </CardDescription>
          </CardHeader>
          <CardContent />
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          <CoreWidgetGroup />

          {isEcommerceEnabled && (
            <Suspense fallback={<DashboardSkeleton count={4} />}>
              <EcommerceWidgetGroup />
            </Suspense>
          )}

          {isBlogEnabled && (
            <Suspense fallback={<DashboardSkeleton count={2} />}>
              <BlogWidgetGroup />
            </Suspense>
          )}

          {isInventoryEnabled && (
            <Suspense fallback={<DashboardSkeleton count={3} />}>
              <InventoryWidgetGroup />
            </Suspense>
          )}

          {isCrmEnabled && (
            <Suspense fallback={<DashboardSkeleton count={3} />}>
              <CrmWidgetGroup />
            </Suspense>
          )}
        </div>
      )}
    </div>
  )
}

export default DashboardPage
