/**
 * Forgot Password API service
 *
 * Handles the password reset flow:
 * 1. Request OTP (sends email)
 * 2. Verify OTP
 * 3. Resend OTP
 * 4. Reset Password
 */
import { apiClientPublic, ApiError } from './apiClient'

// Response types
export interface PasswordResetRequestResult {
  sessionToken: string
  maskedEmail: string
  expiresAt: string
  otpLength: number
}

export interface PasswordResetVerifyResult {
  resetToken: string
  expiresAt: string
}

export interface PasswordResetResendResult {
  success: boolean
  nextResendAt: string
  remainingResends: number
}

/**
 * Request a password reset OTP
 * Sends an email with OTP if user exists (always returns success for security)
 */
export const requestPasswordReset = async (email: string): Promise<PasswordResetRequestResult> => {
  return apiClientPublic<PasswordResetRequestResult>('/auth/forgot-password', {
    method: 'POST',
    body: JSON.stringify({ email }),
  })
}

/**
 * Verify OTP and get reset token
 */
export const verifyOtp = async (sessionToken: string, otp: string): Promise<PasswordResetVerifyResult> => {
  return apiClientPublic<PasswordResetVerifyResult>('/auth/forgot-password/verify', {
    method: 'POST',
    body: JSON.stringify({ sessionToken, otp }),
  })
}

/**
 * Resend OTP email
 */
export const resendOtp = async (sessionToken: string): Promise<PasswordResetResendResult> => {
  return apiClientPublic<PasswordResetResendResult>('/auth/forgot-password/resend', {
    method: 'POST',
    body: JSON.stringify({ sessionToken }),
  })
}

/**
 * Reset password with reset token
 */
export const resetPassword = async (resetToken: string, newPassword: string): Promise<void> => {
  return apiClientPublic<void>('/auth/forgot-password/reset', {
    method: 'POST',
    body: JSON.stringify({ resetToken, newPassword }),
  })
}

export { ApiError }
