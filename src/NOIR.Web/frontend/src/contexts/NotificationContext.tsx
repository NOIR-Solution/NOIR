/**
 * Notification Context
 *
 * Provides centralized state management for notifications:
 * - Fetches and caches notifications
 * - Manages unread count
 * - Handles real-time updates via SignalR
 * - Provides actions for marking as read, deleting, etc.
 */
import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  type ReactNode,
} from 'react'
import { useSignalR } from '@/hooks/useSignalR'
import {
  getNotifications,
  getUnreadCount,
  markAsRead as markAsReadApi,
  markAllAsRead as markAllAsReadApi,
  deleteNotification as deleteNotificationApi,
} from '@/services/notifications'
import type { Notification } from '@/types'
import { useAuthContext } from './AuthContext'
import { toast } from 'sonner'

interface NotificationContextValue {
  /** List of notifications */
  notifications: Notification[]
  /** Total unread count */
  unreadCount: number
  /** Whether notifications are being loaded */
  isLoading: boolean
  /** Whether there are more notifications to load */
  hasMore: boolean
  /** Current page */
  page: number
  /** Total count of notifications */
  totalCount: number
  /** SignalR connection state */
  connectionState: 'disconnected' | 'connecting' | 'connected' | 'reconnecting'
  /** Refresh notifications from server */
  refreshNotifications: () => Promise<void>
  /** Load more notifications (pagination) */
  loadMore: () => Promise<void>
  /** Mark a notification as read */
  markAsRead: (id: string) => Promise<void>
  /** Mark all notifications as read */
  markAllAsRead: () => Promise<void>
  /** Delete a notification */
  deleteNotification: (id: string) => Promise<void>
}

const NotificationContext = createContext<NotificationContextValue | undefined>(undefined)

const PAGE_SIZE = 10

export function NotificationProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuthContext()
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [isLoading, setIsLoading] = useState(false)
  const [page, setPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [hasMore, setHasMore] = useState(false)

  // Handle new notification from SignalR
  const handleNewNotification = useCallback((notification: Notification) => {
    // Add to top of list
    setNotifications((prev) => [notification, ...prev])
    setTotalCount((prev) => prev + 1)

    // Show toast notification
    const toastType = notification.type === 'error' ? 'error' :
                      notification.type === 'warning' ? 'warning' :
                      notification.type === 'success' ? 'success' : 'info'

    toast[toastType](notification.title, {
      description: notification.message,
      action: notification.actionUrl ? {
        label: 'View',
        onClick: () => window.location.href = notification.actionUrl!,
      } : undefined,
    })
  }, [])

  // Handle unread count update from SignalR
  const handleUnreadCountUpdate = useCallback((count: number) => {
    setUnreadCount(count)
  }, [])

  // SignalR connection
  const { connectionState } = useSignalR({
    autoConnect: isAuthenticated,
    onNotification: handleNewNotification,
    onUnreadCountUpdate: handleUnreadCountUpdate,
  })

  // Fetch notifications from server
  const fetchNotifications = useCallback(async (pageNum: number, append: boolean = false) => {
    if (!isAuthenticated) return

    setIsLoading(true)
    try {
      const response = await getNotifications(pageNum, PAGE_SIZE)
      setNotifications((prev) => append ? [...prev, ...response.items] : response.items)
      setTotalCount(response.totalCount)
      setHasMore(response.hasNextPage)
      setPage(pageNum)
    } catch (error) {
      console.error('Failed to fetch notifications:', error)
    } finally {
      setIsLoading(false)
    }
  }, [isAuthenticated])

  // Fetch unread count
  const fetchUnreadCount = useCallback(async () => {
    if (!isAuthenticated) return

    try {
      const count = await getUnreadCount()
      setUnreadCount(count)
    } catch (error) {
      console.error('Failed to fetch unread count:', error)
    }
  }, [isAuthenticated])

  // Refresh notifications
  const refreshNotifications = useCallback(async () => {
    await Promise.all([
      fetchNotifications(1, false),
      fetchUnreadCount(),
    ])
  }, [fetchNotifications, fetchUnreadCount])

  // Load more notifications
  const loadMore = useCallback(async () => {
    if (isLoading || !hasMore) return
    await fetchNotifications(page + 1, true)
  }, [fetchNotifications, page, isLoading, hasMore])

  // Mark single notification as read
  const markAsRead = useCallback(async (id: string) => {
    try {
      await markAsReadApi(id)
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true, readAt: new Date().toISOString() } : n))
      )
      setUnreadCount((prev) => Math.max(0, prev - 1))
    } catch (error) {
      console.error('Failed to mark notification as read:', error)
      throw error
    }
  }, [])

  // Mark all notifications as read
  const markAllAsRead = useCallback(async () => {
    try {
      await markAllAsReadApi()
      setNotifications((prev) =>
        prev.map((n) => ({ ...n, isRead: true, readAt: new Date().toISOString() }))
      )
      setUnreadCount(0)
    } catch (error) {
      console.error('Failed to mark all as read:', error)
      throw error
    }
  }, [])

  // Delete notification
  const deleteNotification = useCallback(async (id: string) => {
    try {
      await deleteNotificationApi(id)
      const notification = notifications.find((n) => n.id === id)
      setNotifications((prev) => prev.filter((n) => n.id !== id))
      setTotalCount((prev) => Math.max(0, prev - 1))
      if (notification && !notification.isRead) {
        setUnreadCount((prev) => Math.max(0, prev - 1))
      }
    } catch (error) {
      console.error('Failed to delete notification:', error)
      throw error
    }
  }, [notifications])

  // Initial fetch when authenticated
  useEffect(() => {
    if (isAuthenticated) {
      refreshNotifications()
    } else {
      // Clear state when logged out
      setNotifications([])
      setUnreadCount(0)
      setPage(1)
      setTotalCount(0)
      setHasMore(false)
    }
  }, [isAuthenticated, refreshNotifications])

  return (
    <NotificationContext.Provider
      value={{
        notifications,
        unreadCount,
        isLoading,
        hasMore,
        page,
        totalCount,
        connectionState,
        refreshNotifications,
        loadMore,
        markAsRead,
        markAllAsRead,
        deleteNotification,
      }}
    >
      {children}
    </NotificationContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export function useNotificationContext() {
  const context = useContext(NotificationContext)
  if (!context) {
    throw new Error('useNotificationContext must be used within NotificationProvider')
  }
  return context
}
