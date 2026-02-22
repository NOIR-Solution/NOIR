/**
 * NotificationList Component
 *
 * Full list of notifications with:
 * - Infinite scroll / load more
 * - Filtering by read/unread
 * - Bulk actions
 */
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Check, RefreshCw, Loader2 } from 'lucide-react'
import { Button, Skeleton } from '@uikit'

import { NotificationItem } from './NotificationItem'
import { NotificationEmpty } from './NotificationEmpty'
import { useNotificationContext } from '@/contexts/NotificationContext'
import { cn } from '@/lib/utils'

type FilterType = 'all' | 'unread' | 'read'

interface NotificationListProps {
  className?: string
}

export const NotificationList = ({ className }: NotificationListProps) => {
  const { t } = useTranslation('common')
  const [filter, setFilter] = useState<FilterType>('all')
  const {
    notifications,
    unreadCount,
    isLoading,
    hasMore,
    totalCount,
    markAsRead,
    markAllAsRead,
    deleteNotification,
    loadMore,
    refreshNotifications,
  } = useNotificationContext()

  // Filter notifications
  const filteredNotifications = notifications.filter((n) => {
    if (filter === 'unread') return !n.isRead
    if (filter === 'read') return n.isRead
    return true
  })

  const handleMarkAllAsRead = async () => {
    try {
      await markAllAsRead()
    } catch {
      // Error handled by NotificationContext
    }
  }

  const handleRefresh = async () => {
    try {
      await refreshNotifications()
    } catch {
      // Error handled by NotificationContext
    }
  }

  return (
    <div className={cn('space-y-4', className)}>
      {/* Toolbar */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        {/* Filter tabs */}
        <div className="flex items-center gap-1 bg-muted rounded-lg p-1">
          {(['all', 'unread', 'read'] as const).map((f) => (
            <Button
              key={f}
              variant={filter === f ? 'secondary' : 'ghost'}
              size="sm"
              className={cn('cursor-pointer', filter === f && 'bg-background shadow-sm')}
              onClick={() => setFilter(f)}
            >
              {t(`notifications.filter.${f}`)}
              {f === 'unread' && unreadCount > 0 && (
                <span className="ml-1.5 text-xs bg-primary text-primary-foreground rounded-full px-1.5">
                  {unreadCount}
                </span>
              )}
            </Button>
          ))}
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            className="cursor-pointer"
            onClick={handleRefresh}
            disabled={isLoading}
          >
            <RefreshCw className={cn('h-4 w-4 mr-1', isLoading && 'animate-spin')} />
            {t('buttons.refresh')}
          </Button>
          {unreadCount > 0 && (
            <Button
              variant="outline"
              size="sm"
              className="cursor-pointer"
              onClick={handleMarkAllAsRead}
            >
              <Check className="h-4 w-4 mr-1" />
              {t('notifications.markAllRead')}
            </Button>
          )}
        </div>
      </div>

      {/* Stats */}
      <p className="text-sm text-muted-foreground">
        {t('notifications.showingOf', { filtered: filteredNotifications.length, total: totalCount })}
      </p>

      {/* List */}
      <div className="rounded-lg border bg-card">
        {isLoading && notifications.length === 0 ? (
          // Skeleton loading for notifications list - better UX than spinner
          <div className="divide-y">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="flex items-start gap-4 p-4">
                <Skeleton className="h-10 w-10 rounded-full flex-shrink-0" />
                <div className="flex-1 space-y-2">
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-3 w-1/2" />
                  <Skeleton className="h-3 w-1/4" />
                </div>
              </div>
            ))}
          </div>
        ) : filteredNotifications.length === 0 ? (
          <NotificationEmpty />
        ) : (
          <div className="divide-y">
            {filteredNotifications.map((notification) => (
              <NotificationItem
                key={notification.id}
                notification={notification}
                onMarkAsRead={markAsRead}
                onDelete={deleteNotification}
              />
            ))}
          </div>
        )}
      </div>

      {/* Load more */}
      {hasMore && filteredNotifications.length > 0 && (
        <div className="flex justify-center pt-4">
          <Button
            variant="outline"
            className="cursor-pointer"
            onClick={loadMore}
            disabled={isLoading}
          >
            {isLoading ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                {t('labels.loading')}
              </>
            ) : (
              t('notifications.loadMore')
            )}
          </Button>
        </div>
      )}
    </div>
  )
}
