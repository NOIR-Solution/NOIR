import { APIRequestContext } from '@playwright/test'

const API_BASE_URL = 'http://localhost:4000'

export class ApiHelper {
  private token: string | null = null

  constructor(private request: APIRequestContext) {}

  async login(email: string = 'admin@noir.local', password: string = '123qwe'): Promise<string> {
    const response = await this.request.post(`${API_BASE_URL}/api/auth/login?useCookies=true`, {
      data: { email, password },
    })

    if (!response.ok()) {
      throw new Error(`Login failed: ${response.status()}`)
    }

    const data = await response.json()
    this.token = data.accessToken
    return data.accessToken
  }

  private getAuthHeaders() {
    if (!this.token) {
      throw new Error('Not authenticated. Call login() first.')
    }
    return { Authorization: `Bearer ${this.token}` }
  }

  async getNotifications(page = 1, pageSize = 20, includeRead = true) {
    const response = await this.request.get(
      `${API_BASE_URL}/api/notifications?page=${page}&pageSize=${pageSize}&includeRead=${includeRead}`,
      { headers: this.getAuthHeaders() }
    )

    if (!response.ok()) {
      throw new Error(`Failed to get notifications: ${response.status()}`)
    }

    return response.json()
  }

  async getUnreadCount(): Promise<number> {
    const response = await this.request.get(`${API_BASE_URL}/api/notifications/unread-count`, {
      headers: this.getAuthHeaders(),
    })

    if (!response.ok()) {
      throw new Error(`Failed to get unread count: ${response.status()}`)
    }

    const data = await response.json()
    return data.count
  }

  async markAsRead(notificationId: string): Promise<void> {
    const response = await this.request.post(`${API_BASE_URL}/api/notifications/${notificationId}/read`, {
      headers: this.getAuthHeaders(),
    })

    if (!response.ok()) {
      throw new Error(`Failed to mark as read: ${response.status()}`)
    }
  }

  async markAllAsRead(): Promise<number> {
    const response = await this.request.post(`${API_BASE_URL}/api/notifications/read-all`, {
      headers: this.getAuthHeaders(),
    })

    if (!response.ok()) {
      throw new Error(`Failed to mark all as read: ${response.status()}`)
    }

    return response.json()
  }

  async deleteNotification(notificationId: string): Promise<void> {
    const response = await this.request.delete(`${API_BASE_URL}/api/notifications/${notificationId}`, {
      headers: this.getAuthHeaders(),
    })

    if (!response.ok()) {
      throw new Error(`Failed to delete notification: ${response.status()}`)
    }
  }

  async getPreferences() {
    const response = await this.request.get(`${API_BASE_URL}/api/notifications/preferences`, {
      headers: this.getAuthHeaders(),
    })

    if (!response.ok()) {
      throw new Error(`Failed to get preferences: ${response.status()}`)
    }

    return response.json()
  }

  async updatePreferences(preferences: Array<{
    category: string
    inAppEnabled: boolean
    emailFrequency: string
  }>): Promise<void> {
    const response = await this.request.put(`${API_BASE_URL}/api/notifications/preferences`, {
      headers: this.getAuthHeaders(),
      data: { preferences },
    })

    if (!response.ok()) {
      throw new Error(`Failed to update preferences: ${response.status()}`)
    }
  }
}
