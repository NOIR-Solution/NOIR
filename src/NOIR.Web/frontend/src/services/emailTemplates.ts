/**
 * Email Templates API Service
 *
 * Provides API client functions for email template management.
 * Admin-only endpoints for viewing, editing, and testing email templates.
 */
import { apiClient } from './apiClient'

/**
 * Full email template details for editing.
 */
export interface EmailTemplateDto {
  id: string
  name: string
  subject: string
  htmlBody: string
  plainTextBody?: string | null
  isActive: boolean
  version: number
  description?: string | null
  availableVariables: string[]
  createdAt: string
  modifiedAt?: string | null
  /** True if this is a platform template viewed by a tenant user (can be customized via Copy-on-Write) */
  isInherited: boolean
}

/**
 * Simplified email template for list views.
 */
export interface EmailTemplateListDto {
  id: string
  name: string
  subject: string
  isActive: boolean
  version: number
  description?: string | null
  availableVariables: string[]
  /** True if this is a platform template viewed by a tenant user (can be customized via Copy-on-Write) */
  isInherited: boolean
}

/**
 * Request to update an email template.
 */
export interface UpdateEmailTemplateRequest {
  subject: string
  htmlBody: string
  plainTextBody?: string | null
  description?: string | null
}

/**
 * Request to send a test email.
 */
export interface SendTestEmailRequest {
  recipientEmail: string
  sampleData: Record<string, string>
}

/**
 * Request to preview an email template with sample data.
 */
export interface PreviewEmailTemplateRequest {
  sampleData: Record<string, string>
}

/**
 * Response from preview/test email operations.
 */
export interface EmailPreviewResponse {
  subject: string
  htmlBody: string
  plainTextBody?: string | null
}

/**
 * Get all email templates with optional filtering.
 */
export const getEmailTemplates = async (search?: string): Promise<EmailTemplateListDto[]> => {
  const params = new URLSearchParams()
  if (search) params.append('search', search)

  const queryString = params.toString()
  const endpoint = `/email-templates${queryString ? `?${queryString}` : ''}`

  return apiClient<EmailTemplateListDto[]>(endpoint)
}

/**
 * Get a single email template by ID.
 */
export const getEmailTemplate = async (id: string): Promise<EmailTemplateDto> => {
  return apiClient<EmailTemplateDto>(`/email-templates/${id}`)
}

/**
 * Update an email template.
 */
export const updateEmailTemplate = async (
  id: string,
  request: UpdateEmailTemplateRequest
): Promise<EmailTemplateDto> => {
  return apiClient<EmailTemplateDto>(`/email-templates/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Send a test email using a template.
 */
export const sendTestEmail = async (
  id: string,
  request: SendTestEmailRequest
): Promise<EmailPreviewResponse> => {
  return apiClient<EmailPreviewResponse>(`/email-templates/${id}/test`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Preview an email template with sample data.
 */
export const previewEmailTemplate = async (
  id: string,
  request: PreviewEmailTemplateRequest
): Promise<EmailPreviewResponse> => {
  return apiClient<EmailPreviewResponse>(`/email-templates/${id}/preview`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Revert a tenant's customized template to the platform default.
 * Deletes the tenant's custom version and returns the platform template.
 */
export const revertToPlatformDefault = async (id: string): Promise<EmailTemplateDto> => {
  return apiClient<EmailTemplateDto>(`/email-templates/${id}/revert`, {
    method: 'DELETE',
  })
}

/**
 * Toggle email template active/inactive status.
 */
export const toggleEmailTemplateActive = async (
  id: string,
  isActive: boolean
): Promise<EmailTemplateDto> => {
  return apiClient<EmailTemplateDto>(`/email-templates/${id}/toggle-active`, {
    method: 'PATCH',
    body: JSON.stringify({ isActive }),
  })
}

/**
 * Get default sample data for a template's variables.
 */
export const getDefaultSampleData = (variables: string[]): Record<string, string> => {
  const sampleValues: Record<string, string> = {
    UserName: 'John Doe',
    OtpCode: '123456',
    ExpiryMinutes: '15',
    Email: 'john.doe@example.com',
    TemporaryPassword: 'TempPass123!',
    ApplicationName: 'NOIR',
    LoginUrl: 'https://example.com/login',
    ActivationLink: 'https://example.com/activate?token=abc123',
    ExpiryHours: '24',
  }

  const result: Record<string, string> = {}
  for (const variable of variables) {
    result[variable] = sampleValues[variable] || `{{${variable}}}`
  }
  return result
}
