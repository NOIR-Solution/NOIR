import { useTranslation } from 'react-i18next'
import { useAuthContext } from '@/contexts/AuthContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle, PageHeader } from '@uikit'
import { ExternalLink, BookOpen, Cpu, User, LayoutDashboard } from 'lucide-react'

const DashboardPage = () => {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()

  return (
    <div className="space-y-6">
      <PageHeader
        icon={LayoutDashboard}
        title={t('dashboard.title')}
        description={t('dashboard.welcome', { name: user?.fullName || 'User' })}
      />

      {/* Quick Links - Icon color coding: blue=documentation, cyan=system tools, teal=user info */}
      <div className="max-w-md">
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
          <CardHeader>
            <CardTitle className="text-foreground">{t('dashboard.quickLinks')}</CardTitle>
            <CardDescription>{t('dashboard.quickLinksDescription')}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <a
              href="/api/docs"
              target="_blank"
              rel="noopener noreferrer"
              className="group flex items-center justify-between rounded-lg border border-border p-3 hover:bg-accent hover:border-blue-600/30 transition-colors cursor-pointer"
            >
              <div className="flex items-center gap-3">
                <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-blue-600/10">
                  <BookOpen className="h-4 w-4 text-blue-600" />
                </div>
                <span className="text-sm font-medium text-foreground group-hover:text-blue-600 transition-colors">{t('dashboard.apiDocs')}</span>
              </div>
              <ExternalLink className="h-4 w-4 text-muted-foreground group-hover:text-blue-600 transition-colors" />
            </a>
            <a
              href="/hangfire"
              target="_blank"
              rel="noopener noreferrer"
              className="group flex items-center justify-between rounded-lg border border-border p-3 hover:bg-accent hover:border-blue-600/30 transition-colors cursor-pointer"
            >
              <div className="flex items-center gap-3">
                <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-cyan-600/10">
                  <Cpu className="h-4 w-4 text-cyan-600" />
                </div>
                <span className="text-sm font-medium text-foreground group-hover:text-cyan-600 transition-colors">{t('dashboard.hangfire')}</span>
              </div>
              <ExternalLink className="h-4 w-4 text-muted-foreground group-hover:text-cyan-600 transition-colors" />
            </a>
            <div className="rounded-lg border border-border p-3">
              <div className="flex items-center gap-3 mb-3">
                <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-teal-600/10">
                  <User className="h-4 w-4 text-teal-600" />
                </div>
                <p className="text-sm font-medium text-foreground">{t('dashboard.yourProfile')}</p>
              </div>
              <div className="space-y-1 text-sm pl-11">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('profile.email')}:</span>
                  <span className="font-medium text-foreground">{user?.email}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('profile.tenant')}:</span>
                  <span className="font-medium text-foreground">{user?.tenantId ?? t('profile.platform')}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('profile.roles')}:</span>
                  <span className="font-medium text-foreground">{user?.roles?.join(', ') || 'None'}</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

export default DashboardPage
