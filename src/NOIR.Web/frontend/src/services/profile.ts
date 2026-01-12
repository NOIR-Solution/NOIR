/**
 * Profile API service
 *
 * Handles user profile operations including:
 * - Profile updates (name, phone)
 * - Avatar upload/delete
 * - Email change with OTP verification
 */
import { apiClient, ApiError } from './apiClient'

// Profile update types
export interface UpdateProfileRequest {
  firstName?: string | null
  lastName?: string | null
  displayName?: string | null
  phoneNumber?: string | null
}

export interface UpdateProfileResponse {
  id: string
  email: string
  firstName: string | null
  lastName: string | null
  displayName: string | null
  fullName: string
  phoneNumber: string | null
  avatarUrl: string | null
}

// Avatar types
export interface AvatarUploadResponse {
  avatarUrl: string
  message: string
}

export interface AvatarDeleteResponse {
  success: boolean
  message: string
}

// Email change types
export interface EmailChangeRequestResponse {
  sessionToken: string
  maskedEmail: string
  expiresAt: string
  otpLength: number
}

export interface EmailChangeVerifyResponse {
  newEmail: string
  message: string
}

export interface EmailChangeResendResponse {
  success: boolean
  nextResendAt: string | null
  remainingResends: number
}

/**
 * Update user profile
 */
export async function updateProfile(request: UpdateProfileRequest): Promise<UpdateProfileResponse> {
  return apiClient<UpdateProfileResponse>('/auth/me', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Upload user avatar
 * @param file The image file to upload
 */
export async function uploadAvatar(file: File): Promise<AvatarUploadResponse> {
  const formData = new FormData()
  formData.append('file', file)

  // Use fetch directly for multipart/form-data (don't set Content-Type header)
  const response = await fetch('/api/auth/me/avatar', {
    method: 'POST',
    body: formData,
    credentials: 'include',
  })

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}))
    throw new ApiError(
      errorData.detail || errorData.title || 'Failed to upload avatar',
      response.status,
      errorData.errors
    )
  }

  return response.json()
}

/**
 * Delete user avatar
 */
export async function deleteAvatar(): Promise<AvatarDeleteResponse> {
  return apiClient<AvatarDeleteResponse>('/auth/me/avatar', {
    method: 'DELETE',
  })
}

/**
 * Request email change - sends OTP to new email
 */
export async function requestEmailChange(newEmail: string): Promise<EmailChangeRequestResponse> {
  return apiClient<EmailChangeRequestResponse>('/auth/me/email/request', {
    method: 'POST',
    body: JSON.stringify({ newEmail }),
  })
}

/**
 * Verify email change OTP
 */
export async function verifyEmailChange(
  sessionToken: string,
  otp: string
): Promise<EmailChangeVerifyResponse> {
  return apiClient<EmailChangeVerifyResponse>('/auth/me/email/verify', {
    method: 'POST',
    body: JSON.stringify({ sessionToken, otp }),
  })
}

/**
 * Resend email change OTP
 */
export async function resendEmailChangeOtp(
  sessionToken: string
): Promise<EmailChangeResendResponse> {
  return apiClient<EmailChangeResendResponse>('/auth/me/email/resend', {
    method: 'POST',
    body: JSON.stringify({ sessionToken }),
  })
}

export { ApiError }
