/**
 * Notification Service
 *
 * Provides API calls for notification management:
 * - Fetching notifications (paginated)
 * - Getting unread count
 * - Marking notifications as read
 * - Deleting notifications
 * - Managing preferences
 */
import { apiClient } from './apiClient'
import type {
  NotificationPreference,
  PaginatedNotificationsResponse,
  UpdatePreferencesRequest,
} from '@/types'
import { mapNotificationCategory, mapEmailFrequency } from '@/types'

/**
 * Raw preference response from API (enums as numbers)
 */
interface RawNotificationPreference {
  id: string
  category: number | string
  categoryName: string
  inAppEnabled: boolean
  emailFrequency: number | string
}

/**
 * Get paginated notifications for the current user
 */
export async function getNotifications(
  page: number = 1,
  pageSize: number = 10,
  unreadOnly: boolean = false
): Promise<PaginatedNotificationsResponse> {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
  })
  if (unreadOnly) {
    params.append('unreadOnly', 'true')
  }
  return apiClient<PaginatedNotificationsResponse>(`/notifications?${params}`)
}

/**
 * Get the count of unread notifications
 */
export async function getUnreadCount(): Promise<number> {
  const response = await apiClient<{ count: number }>('/notifications/unread-count')
  return response.count
}

/**
 * Mark a single notification as read
 */
export async function markAsRead(notificationId: string): Promise<void> {
  await apiClient(`/notifications/${notificationId}/read`, {
    method: 'POST',
  })
}

/**
 * Mark all notifications as read for the current user
 */
export async function markAllAsRead(): Promise<void> {
  await apiClient('/notifications/read-all', {
    method: 'POST',
  })
}

/**
 * Delete (soft delete) a notification
 */
export async function deleteNotification(notificationId: string): Promise<void> {
  await apiClient(`/notifications/${notificationId}`, {
    method: 'DELETE',
  })
}

/**
 * Get notification preferences for the current user
 */
export async function getPreferences(): Promise<NotificationPreference[]> {
  const raw = await apiClient<RawNotificationPreference[]>('/notifications/preferences')
  // Map backend enum numbers to frontend string types
  return raw.map((pref) => ({
    id: pref.id,
    category: mapNotificationCategory(pref.category),
    categoryName: pref.categoryName,
    inAppEnabled: pref.inAppEnabled,
    emailFrequency: mapEmailFrequency(pref.emailFrequency),
  }))
}

/**
 * Update notification preferences
 */
export async function updatePreferences(
  request: UpdatePreferencesRequest
): Promise<void> {
  await apiClient('/notifications/preferences', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Notification service object for convenience
 */
export const notificationService = {
  getNotifications,
  getUnreadCount,
  markAsRead,
  markAllAsRead,
  deleteNotification,
  getPreferences,
  updatePreferences,
}
