/**
 * Platform Settings API Service
 *
 * Provides API client functions for platform-level settings management.
 * Platform admin only - manages SMTP and other platform configuration.
 */
import { apiClient } from './apiClient'

/**
 * SMTP settings DTO.
 */
export interface SmtpSettingsDto {
  host: string
  port: number
  username?: string | null
  hasPassword: boolean
  fromEmail: string
  fromName: string
  useSsl: boolean
  isConfigured: boolean
}

/**
 * Request to update SMTP settings.
 */
export interface UpdateSmtpSettingsRequest {
  host: string
  port: number
  username?: string | null
  password?: string | null
  fromEmail: string
  fromName: string
  useSsl: boolean
}

/**
 * Request to test SMTP connection.
 */
export interface TestSmtpRequest {
  recipientEmail: string
}

/**
 * Get platform SMTP settings.
 */
export async function getSmtpSettings(): Promise<SmtpSettingsDto> {
  return apiClient<SmtpSettingsDto>('/platform-settings/smtp')
}

/**
 * Update platform SMTP settings.
 */
export async function updateSmtpSettings(
  request: UpdateSmtpSettingsRequest
): Promise<SmtpSettingsDto> {
  return apiClient<SmtpSettingsDto>('/platform-settings/smtp', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Test SMTP connection by sending a test email.
 */
export async function testSmtpConnection(
  request: TestSmtpRequest
): Promise<boolean> {
  return apiClient<boolean>('/platform-settings/smtp/test', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}
