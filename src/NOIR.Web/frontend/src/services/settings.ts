/**
 * Settings API service
 *
 * Handles user settings operations including password changes.
 * Uses authenticated apiClient for all requests.
 */
import { apiClient, ApiError } from './apiClient'

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

/**
 * Change the current user's password
 * Requires authentication. After success, all sessions are revoked.
 * @throws ApiError on failure (incorrect password, validation errors, etc.)
 */
export async function changePassword(request: ChangePasswordRequest): Promise<void> {
  return apiClient<void>('/auth/change-password', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export { ApiError }
