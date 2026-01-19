/**
 * NotificationDropdown Component
 *
 * Modern dropdown popover showing recent notifications with:
 * - Animated entrance/exit
 * - Time-grouped notifications (Today, Yesterday, Earlier)
 * - Spacious layout with smooth interactions
 */
import { useState, useRef, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { Check, Bell } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { NotificationItem } from './NotificationItem'
import { NotificationEmpty } from './NotificationEmpty'
import { useNotificationContext } from '@/contexts/NotificationContext'
import type { Notification } from '@/types'

interface NotificationDropdownProps {
  className?: string
}

interface NotificationGroup {
  label: string
  notifications: Notification[]
}

/**
 * Get time label for grouping notifications
 */
function getTimeLabel(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffTime = Math.abs(now.getTime() - date.getTime())
  const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  if (diffDays <= 7) return 'Earlier this week'
  return 'Older'
}

/**
 * Group notifications by time period
 */
function groupNotificationsByTime(notifications: Notification[]): NotificationGroup[] {
  const groups: { [key: string]: Notification[] } = {
    Today: [],
    Yesterday: [],
    'Earlier this week': [],
    Older: [],
  }

  notifications.forEach((notification) => {
    const label = getTimeLabel(notification.createdAt)
    groups[label].push(notification)
  })

  return Object.entries(groups)
    .filter(([_, notifs]) => notifs.length > 0)
    .map(([label, notifs]) => ({ label, notifications: notifs }))
}

export function NotificationDropdown({ className }: NotificationDropdownProps) {
  const [open, setOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const {
    notifications,
    unreadCount,
    isLoading,
    connectionState,
    markAsRead,
    markAllAsRead,
    deleteNotification,
  } = useNotificationContext()

  // Show only the 5 most recent notifications in dropdown
  const recentNotifications = notifications.slice(0, 5)
  const groupedNotifications = groupNotificationsByTime(recentNotifications)
  const displayCount = unreadCount > 99 ? '99+' : unreadCount.toString()

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    if (open) {
      document.addEventListener('mousedown', handleClickOutside)
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [open])

  const handleMarkAllAsRead = async () => {
    try {
      await markAllAsRead()
    } catch {
      // Error handled by NotificationContext
    }
  }

  return (
    <div className={cn('relative', className)} ref={dropdownRef}>
      {/* Bell Button */}
      <button
        onClick={() => setOpen(!open)}
        className={cn(
          'relative p-2.5 rounded-lg transition-all duration-200',
          'hover:bg-muted focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
          open && 'bg-muted'
        )}
        aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`}
      >
        <Bell className="size-5 text-foreground" />

        {/* Unread count badge */}
        {unreadCount > 0 && (
          <motion.span
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            className="absolute -top-1 -right-1 min-w-5 h-5 px-1 bg-destructive text-destructive-foreground text-[10px] font-semibold rounded-full flex items-center justify-center shadow-lg"
          >
            {displayCount}
          </motion.span>
        )}

        {/* Connection state indicator */}
        {connectionState === 'connecting' || connectionState === 'reconnecting' ? (
          <span className="absolute bottom-0.5 right-0.5 size-2 rounded-full bg-amber-500 animate-pulse" />
        ) : connectionState === 'disconnected' ? (
          <span className="absolute bottom-0.5 right-0.5 size-2 rounded-full bg-red-500" />
        ) : null}
      </button>

      {/* Dropdown Panel */}
      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -10, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -10, scale: 0.95 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            className={cn(
              'absolute right-0 mt-2 w-[380px] max-w-[calc(100vw-2rem)]',
              'bg-popover border border-border rounded-xl shadow-2xl',
              'overflow-hidden z-50'
            )}
          >
            {/* Header */}
            <div className="flex items-center justify-between p-4 border-b border-border">
              <div>
                <h3 className="text-base font-semibold text-foreground">Notifications</h3>
                {unreadCount > 0 && (
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {unreadCount} unread
                  </p>
                )}
              </div>
              {unreadCount > 0 && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 text-xs"
                  onClick={handleMarkAllAsRead}
                >
                  <Check className="size-3.5 mr-1" />
                  Mark all read
                </Button>
              )}
            </div>

            {/* Notification list */}
            <div className="max-h-[420px] overflow-y-auto">
              {isLoading && notifications.length === 0 ? (
                <div className="flex items-center justify-center py-12">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary" />
                </div>
              ) : recentNotifications.length === 0 ? (
                <NotificationEmpty />
              ) : (
                <div className="p-2">
                  {groupedNotifications.map((group) => (
                    <div key={group.label} className="mb-3 last:mb-0">
                      <h4 className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider px-3 py-2">
                        {group.label}
                      </h4>
                      <div className="space-y-0.5">
                        <AnimatePresence>
                          {group.notifications.map((notification) => (
                            <motion.div
                              key={notification.id}
                              initial={{ opacity: 0, x: -20 }}
                              animate={{ opacity: 1, x: 0 }}
                              exit={{ opacity: 0, x: 20 }}
                              transition={{ type: 'spring', stiffness: 300, damping: 30 }}
                            >
                              <NotificationItem
                                notification={notification}
                                onMarkAsRead={markAsRead}
                                onDelete={deleteNotification}
                                compact
                              />
                            </motion.div>
                          ))}
                        </AnimatePresence>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="p-2.5 border-t border-border bg-muted/30">
              <Button
                variant="ghost"
                className="w-full justify-center text-sm font-medium h-9"
                asChild
                onClick={() => setOpen(false)}
              >
                <Link to="/portal/notifications">View all notifications</Link>
              </Button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
