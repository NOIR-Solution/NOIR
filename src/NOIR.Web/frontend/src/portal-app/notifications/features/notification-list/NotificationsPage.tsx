/**
 * Notifications Page
 *
 * Full notification history page with filtering and pagination.
 */
import { useTranslation } from 'react-i18next'
import { Bell, Settings } from 'lucide-react'
import { Button, PageHeader } from '@uikit'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { usePageContext } from '@/hooks/usePageContext'
import { useNotificationContext } from '@/contexts/NotificationContext'
import { NotificationList } from '../../components/notifications'

export const NotificationsPage = () => {
  const { t } = useTranslation('common')
  const { connectionState } = useNotificationContext()
  usePageContext('Notifications')

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Bell}
        title={t('notifications.title')}
        description={
          connectionState === 'connected'
            ? `${t('notifications.description')} — ${t('notifications.live')}`
            : t('notifications.description')
        }
        responsive
        action={
          <Button variant="outline" asChild className="cursor-pointer">
            <ViewTransitionLink to="/portal/settings/notifications">
              <Settings className="size-4 mr-2" />
              {t('notifications.preferences')}
            </ViewTransitionLink>
          </Button>
        }
      />

      {/* Live indicator */}
      {connectionState === 'connected' && (
        <div className="flex items-center gap-2 text-sm text-green-600 dark:text-green-400">
          <span className="size-2 rounded-full bg-green-600 dark:bg-green-400 animate-pulse" />
          {t('notifications.live')}
        </div>
      )}

      {/* Notification List */}
      <NotificationList />
    </div>
  )
}

export default NotificationsPage
