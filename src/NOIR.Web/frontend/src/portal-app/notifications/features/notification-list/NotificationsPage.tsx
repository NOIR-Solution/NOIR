/**
 * Notifications Page
 *
 * Full notification history page with filtering and pagination.
 */
import { useTranslation } from 'react-i18next'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { Settings } from 'lucide-react'
import { Button } from '@uikit'
import { NotificationList } from '../../components/notifications'
import { useNotificationContext } from '@/contexts/NotificationContext'

const NotificationsPage = () => {
  const { t } = useTranslation('common')
  const { connectionState } = useNotificationContext()

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">{t('notifications.title')}</h1>
          <p className="text-muted-foreground">
            {t('notifications.description')}
            {connectionState === 'connected' && (
              <span className="ml-2 inline-flex items-center text-green-600">
                <span className="size-2 rounded-full bg-green-600 mr-1.5 animate-pulse" />
                {t('notifications.live')}
              </span>
            )}
          </p>
        </div>
        <Button variant="outline" asChild>
          <ViewTransitionLink to="/portal/settings/notifications">
            <Settings className="size-4 mr-2" />
            {t('notifications.preferences')}
          </ViewTransitionLink>
        </Button>
      </div>

      {/* Notification List */}
      <NotificationList />
    </div>
  )
}

export default NotificationsPage
