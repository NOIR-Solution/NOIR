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
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Info, CheckCircle, AlertTriangle, XCircle, Trash2, ExternalLink } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
} from '@uikit'
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

export const NotificationItem = ({
  notification,
  onMarkAsRead,
  onDelete,
  compact = false,
  className,
}: NotificationItemProps) => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { formatRelativeTime } = useRegionalSettings()
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
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
    setShowDeleteConfirm(true)
  }

  const handleDeleteConfirm = () => {
    onDelete?.(notification.id)
    setShowDeleteConfirm(false)
  }

  const handleAction = (e: React.MouseEvent, url: string, method?: string) => {
    e.stopPropagation()
    if (method === 'POST' || method === 'DELETE') {
      // API actions would typically be handled by the parent component
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
        <Icon className={cn('h-4 w-4', config.color)} />
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
                className="cursor-pointer"
                onClick={(e) => handleAction(e, action.url, action.method)}
              >
                {action.label}
                {action.url && !action.url.startsWith('/') && (
                  <ExternalLink className="ml-1 h-3 w-3" />
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
          className="opacity-0 group-hover:opacity-100 shrink-0 size-8 cursor-pointer"
          onClick={handleDelete}
          aria-label={t('notifications.deleteNotification', { defaultValue: `Delete notification: ${notification.title}` })}
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      )}

      {/* Delete Confirmation Dialog */}
      <Credenza open={showDeleteConfirm} onOpenChange={setShowDeleteConfirm}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('notifications.deleteTitle', 'Delete Notification')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('notifications.deleteConfirmation', 'Are you sure you want to delete this notification? This action cannot be undone.')}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button
              variant="outline"
              onClick={() => setShowDeleteConfirm(false)}
              className="cursor-pointer"
            >
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteConfirm}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {t('labels.delete', 'Delete')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}
