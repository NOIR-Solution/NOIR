/**
 * Notification-related types
 * These mirror the backend Notification DTOs exactly
 */

/**
 * Notification type enum - mirrors NotificationType from backend
 */
export type NotificationType = 'info' | 'success' | 'warning' | 'error'

/**
 * Notification category enum - mirrors NotificationCategory from backend
 */
export type NotificationCategory = 'system' | 'userAction' | 'workflow' | 'security' | 'integration'

/**
 * Email frequency enum - mirrors EmailFrequency from backend
 */
export type EmailFrequency = 'none' | 'immediate' | 'daily' | 'weekly'

/**
 * Notification action button
 * Matches NotificationActionDto from backend
 */
export interface NotificationAction {
  label: string
  url: string
  style?: 'primary' | 'secondary' | 'destructive'
  method?: 'GET' | 'POST' | 'DELETE'
}

/**
 * Notification entity
 * Matches NotificationDto from backend
 */
export interface Notification {
  id: string
  type: NotificationType
  category: NotificationCategory
  title: string
  message: string
  iconClass?: string
  isRead: boolean
  readAt?: string // DateTimeOffset serializes as ISO 8601 string
  actionUrl?: string
  actions: NotificationAction[]
  createdAt: string // DateTimeOffset serializes as ISO 8601 string
}

/**
 * Notification preference
 * Matches NotificationPreferenceDto from backend
 */
export interface NotificationPreference {
  id: string
  category: NotificationCategory
  categoryName: string
  inAppEnabled: boolean
  emailFrequency: EmailFrequency
}

/**
 * Paginated notifications response
 * Matches PaginatedNotificationsResponse from backend
 */
export interface PaginatedNotificationsResponse {
  items: Notification[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

/**
 * Update preferences request
 * Matches UpdatePreferencesCommand from backend
 */
export interface UpdatePreferencesRequest {
  preferences: NotificationPreferenceUpdate[]
}

/**
 * Single preference update
 */
export interface NotificationPreferenceUpdate {
  category: NotificationCategory
  inAppEnabled: boolean
  emailFrequency: EmailFrequency
}

/**
 * Maps backend enum values to frontend types
 */
export const NotificationTypeMap: Record<number, NotificationType> = {
  0: 'info',
  1: 'success',
  2: 'warning',
  3: 'error',
}

export const NotificationCategoryMap: Record<number, NotificationCategory> = {
  0: 'system',
  1: 'userAction',
  2: 'workflow',
  3: 'security',
  4: 'integration',
}

export const EmailFrequencyMap: Record<number, EmailFrequency> = {
  0: 'none',
  1: 'immediate',
  2: 'daily',
  3: 'weekly',
}

/**
 * Helper to convert backend enum values to frontend types
 */
export function mapNotificationType(value: number | string): NotificationType {
  if (typeof value === 'number') return NotificationTypeMap[value] || 'info'
  return value as NotificationType
}

export function mapNotificationCategory(value: number | string): NotificationCategory {
  if (typeof value === 'number') return NotificationCategoryMap[value] || 'system'
  return value as NotificationCategory
}

export function mapEmailFrequency(value: number | string): EmailFrequency {
  if (typeof value === 'number') return EmailFrequencyMap[value] || 'none'
  return value as EmailFrequency
}
