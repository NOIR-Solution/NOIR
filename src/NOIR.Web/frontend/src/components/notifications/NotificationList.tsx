/**
 * NotificationList Component
 *
 * Full list of notifications with:
 * - Infinite scroll / load more
 * - Filtering by read/unread
 * - Bulk actions
 */
import { useState } from 'react'
import { Check, RefreshCw, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { NotificationItem } from './NotificationItem'
import { NotificationEmpty } from './NotificationEmpty'
import { useNotificationContext } from '@/contexts/NotificationContext'
import { cn } from '@/lib/utils'

type FilterType = 'all' | 'unread' | 'read'

interface NotificationListProps {
  className?: string
}

export function NotificationList({ className }: NotificationListProps) {
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
    } catch (error) {
      console.error('Failed to mark all as read:', error)
    }
  }

  const handleRefresh = async () => {
    try {
      await refreshNotifications()
    } catch (error) {
      console.error('Failed to refresh notifications:', error)
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
              className={cn('capitalize', filter === f && 'bg-background shadow-sm')}
              onClick={() => setFilter(f)}
            >
              {f}
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
            onClick={handleRefresh}
            disabled={isLoading}
          >
            <RefreshCw className={cn('size-4 mr-1', isLoading && 'animate-spin')} />
            Refresh
          </Button>
          {unreadCount > 0 && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleMarkAllAsRead}
            >
              <Check className="size-4 mr-1" />
              Mark all read
            </Button>
          )}
        </div>
      </div>

      {/* Stats */}
      <p className="text-sm text-muted-foreground">
        Showing {filteredNotifications.length} of {totalCount} notifications
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
            onClick={loadMore}
            disabled={isLoading}
          >
            {isLoading ? (
              <>
                <Loader2 className="size-4 mr-2 animate-spin" />
                Loading...
              </>
            ) : (
              'Load more'
            )}
          </Button>
        </div>
      )}
    </div>
  )
}
