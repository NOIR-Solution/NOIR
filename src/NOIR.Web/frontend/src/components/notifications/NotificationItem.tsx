/**
 * NotificationItem Component
 *
 * Displays a single notification with:
 * - Icon based on type (info, success, warning, error)
 * - Title and message
 * - Timestamp
 * - Action buttons
 * - Read/unread state
 */
import { Info, CheckCircle, AlertTriangle, XCircle, Trash2, ExternalLink } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import type { Notification, NotificationType } from '@/types'
import { useNavigate } from 'react-router-dom'

interface NotificationItemProps {
  notification: Notification
  onMarkAsRead?: (id: string) => void
  onDelete?: (id: string) => void
  /** Whether to show in compact mode (for dropdown) */
  compact?: boolean
  className?: string
}

const typeConfig: Record<NotificationType, { icon: typeof Info; color: string; bg: string }> = {
  info: {
    icon: Info,
    color: 'text-blue-600 dark:text-blue-400',
    bg: 'bg-blue-600/10',
  },
  success: {
    icon: CheckCircle,
    color: 'text-green-600 dark:text-green-400',
    bg: 'bg-green-600/10',
  },
  warning: {
    icon: AlertTriangle,
    color: 'text-amber-600 dark:text-amber-400',
    bg: 'bg-amber-600/10',
  },
  error: {
    icon: XCircle,
    color: 'text-red-600 dark:text-red-400',
    bg: 'bg-red-600/10',
  },
}

/**
 * Format relative time (e.g., "2 min ago", "1 hour ago")
 */
function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffSec = Math.floor(diffMs / 1000)
  const diffMin = Math.floor(diffSec / 60)
  const diffHour = Math.floor(diffMin / 60)
  const diffDay = Math.floor(diffHour / 24)

  if (diffSec < 60) return 'Just now'
  if (diffMin < 60) return `${diffMin}m ago`
  if (diffHour < 24) return `${diffHour}h ago`
  if (diffDay < 7) return `${diffDay}d ago`

  return date.toLocaleDateString()
}

export function NotificationItem({
  notification,
  onMarkAsRead,
  onDelete,
  compact = false,
  className,
}: NotificationItemProps) {
  const navigate = useNavigate()
  const config = typeConfig[notification.type] || typeConfig.info
  const Icon = config.icon

  const handleClick = () => {
    if (!notification.isRead && onMarkAsRead) {
      onMarkAsRead(notification.id)
    }
    if (notification.actionUrl) {
      // Check if internal or external link
      if (notification.actionUrl.startsWith('/')) {
        navigate(notification.actionUrl)
      } else {
        window.open(notification.actionUrl, '_blank')
      }
    }
  }

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation()
    onDelete?.(notification.id)
  }

  const handleAction = (e: React.MouseEvent, url: string, method?: string) => {
    e.stopPropagation()
    if (method === 'POST' || method === 'DELETE') {
      // Handle API actions - these would typically be handled by the page
      console.log('Action:', method, url)
    } else {
      if (url.startsWith('/')) {
        navigate(url)
      } else {
        window.open(url, '_blank')
      }
    }
  }

  return (
    <div
      onClick={handleClick}
      className={cn(
        'group relative flex gap-3 p-3 transition-colors cursor-pointer hover:bg-muted/50',
        !notification.isRead && 'bg-primary/5',
        !notification.isRead && 'border-l-2 border-l-primary',
        compact ? 'p-2' : 'p-3',
        className
      )}
    >
      {/* Icon */}
      <div className={cn('shrink-0 rounded-full p-2', config.bg)}>
        <Icon className={cn('size-4', config.color)} />
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0 space-y-1">
        <div className="flex items-start justify-between gap-2">
          <p
            className={cn(
              'text-sm truncate',
              !notification.isRead ? 'font-semibold' : 'font-medium'
            )}
          >
            {notification.title}
          </p>
          <span className="text-xs text-muted-foreground shrink-0">
            {formatRelativeTime(notification.createdAt)}
          </span>
        </div>

        <p className={cn('text-sm text-muted-foreground', compact && 'line-clamp-2')}>
          {notification.message}
        </p>

        {/* Actions (only show in non-compact mode) */}
        {!compact && notification.actions && notification.actions.length > 0 && (
          <div className="flex flex-wrap gap-2 pt-2">
            {notification.actions.map((action, index) => (
              <Button
                key={index}
                variant={
                  action.style === 'primary'
                    ? 'default'
                    : action.style === 'destructive'
                    ? 'destructive'
                    : 'outline'
                }
                size="sm"
                onClick={(e) => handleAction(e, action.url, action.method)}
              >
                {action.label}
                {action.url && !action.url.startsWith('/') && (
                  <ExternalLink className="ml-1 size-3" />
                )}
              </Button>
            ))}
          </div>
        )}
      </div>

      {/* Delete button (shows on hover) */}
      {onDelete && (
        <Button
          variant="ghost"
          size="icon"
          className="opacity-0 group-hover:opacity-100 shrink-0 size-8"
          onClick={handleDelete}
        >
          <Trash2 className="size-4" />
          <span className="sr-only">Delete notification</span>
        </Button>
      )}
    </div>
  )
}
