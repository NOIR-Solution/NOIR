/**
 * NotificationBell Component
 *
 * Bell icon with unread notification badge for the header.
 * Triggers the notification dropdown when clicked.
 */
import { useTranslation } from 'react-i18next'
import { Bell } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Badge, Button } from '@uikit'

import { useNotificationContext } from '@/contexts/NotificationContext'

interface NotificationBellProps {
  onClick?: () => void
  className?: string
}

export const NotificationBell = ({ onClick, className }: NotificationBellProps) => {
  const { t } = useTranslation('common')
  const { unreadCount, connectionState } = useNotificationContext()

  const displayCount = unreadCount > 99 ? '99+' : unreadCount.toString()

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={onClick}
      className={cn('relative', className)}
      aria-label={unreadCount > 0
        ? t('notifications.bellAriaLabelWithCount', { count: unreadCount, defaultValue: `Notifications (${unreadCount} unread)` })
        : t('notifications.bellAriaLabel', 'Notifications')}
    >
      <Bell className="h-5 w-5" />

      {/* Unread count badge */}
      {unreadCount > 0 && (
        <Badge
          variant="destructive"
          className="absolute -top-1 -right-1 size-5 p-0 text-[10px] flex items-center justify-center"
        >
          {displayCount}
        </Badge>
      )}

      {/* Connection state indicator (subtle) */}
      {connectionState === 'connecting' || connectionState === 'reconnecting' ? (
        <span className="absolute bottom-0 right-0 size-2 rounded-full bg-amber-500 animate-pulse" />
      ) : connectionState === 'disconnected' ? (
        <span className="absolute bottom-0 right-0 size-2 rounded-full bg-red-500" />
      ) : null}
    </Button>
  )
}
