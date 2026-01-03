import { useTranslation } from 'react-i18next'
import { useAuthContext } from '@/contexts/AuthContext'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { ExternalLink } from 'lucide-react'

export default function Dashboard() {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">{t('dashboard.title')}</h1>
        <p className="text-gray-600">{t('dashboard.welcome', { name: user?.fullName || 'User' })}</p>
      </div>

      {/* Quick Links */}
      <div className="max-w-md">
        <Card>
          <CardHeader>
            <CardTitle>{t('dashboard.quickLinks')}</CardTitle>
            <CardDescription>{t('dashboard.quickLinksDescription')}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <a
              href="/api/docs"
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center justify-between rounded-lg border border-gray-200 p-3 hover:bg-gray-50 transition-colors"
            >
              <span className="text-sm font-medium text-gray-900">{t('dashboard.apiDocs')}</span>
              <ExternalLink className="h-4 w-4 text-gray-400" />
            </a>
            <a
              href="/hangfire"
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center justify-between rounded-lg border border-gray-200 p-3 hover:bg-gray-50 transition-colors"
            >
              <span className="text-sm font-medium text-gray-900">{t('dashboard.hangfire')}</span>
              <ExternalLink className="h-4 w-4 text-gray-400" />
            </a>
            <div className="rounded-lg border border-gray-200 p-3">
              <p className="text-sm font-medium text-gray-900">{t('dashboard.yourProfile')}</p>
              <div className="mt-2 space-y-1 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-500">{t('profile.email')}:</span>
                  <span className="font-medium text-gray-900">{user?.email}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">{t('profile.tenant')}:</span>
                  <span className="font-medium text-gray-900">{user?.tenantId || 'N/A'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">{t('profile.roles')}:</span>
                  <span className="font-medium text-gray-900">{user?.roles?.join(', ') || 'None'}</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
