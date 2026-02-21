import { useTranslation } from 'react-i18next'
import { useAuthContext } from '@/contexts/AuthContext'
import { usePageContext } from '@/hooks/usePageContext'
import { PageHeader } from '@uikit'
import { LayoutDashboard } from 'lucide-react'

export const DashboardPage = () => {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()
  usePageContext('Dashboard')

  return (
    <div className="space-y-6">
      <PageHeader
        icon={LayoutDashboard}
        title={t('dashboard.title', 'Dashboard')}
        description={t('dashboard.welcome', { name: user?.fullName || 'User' })}
        responsive
      />
    </div>
  )
}

export default DashboardPage
