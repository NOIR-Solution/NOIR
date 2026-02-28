/**
 * Dashboard Page
 *
 * Empty placeholder dashboard for both Platform Admin and Tenant Admin.
 * Will be populated with role-specific widgets in a future iteration.
 */
import { useTranslation } from 'react-i18next'
import { LayoutDashboard } from 'lucide-react'
import { useAuthContext } from '@/contexts/AuthContext'
import { usePageContext } from '@/hooks/usePageContext'
import { EmptyState, PageHeader } from '@uikit'

// ─── Main Page ────────────────────────────────────────────────────────────

export const DashboardPage = () => {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()
  usePageContext('Dashboard')

  return (
    <div className="py-6 space-y-6">
      <PageHeader
        icon={LayoutDashboard}
        title={t('dashboard.title', 'Dashboard')}
        description={t('dashboard.welcome', { name: user?.fullName || t('labels.user', { defaultValue: 'User' }) })}
        responsive
      />

      <EmptyState
        icon={LayoutDashboard}
        title={t('dashboard.comingSoon', 'Dashboard coming soon')}
        description={t('dashboard.comingSoonDesc', 'This dashboard is being built. Use the sidebar to navigate to different sections.')}
      />
    </div>
  )
}

export default DashboardPage
