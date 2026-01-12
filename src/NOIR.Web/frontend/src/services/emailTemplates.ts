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
  language: string
  isActive: boolean
  version: number
  description?: string | null
  availableVariables: string[]
  createdAt: string
  modifiedAt?: string | null
}

/**
 * Simplified email template for list views.
 */
export interface EmailTemplateListDto {
  id: string
  name: string
  subject: string
  language: string
  isActive: boolean
  version: number
  description?: string | null
  availableVariables: string[]
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
export async function getEmailTemplates(
  language?: string,
  search?: string
): Promise<EmailTemplateListDto[]> {
  const params = new URLSearchParams()
  if (language) params.append('language', language)
  if (search) params.append('search', search)

  const queryString = params.toString()
  const endpoint = `/email-templates${queryString ? `?${queryString}` : ''}`

  return apiClient<EmailTemplateListDto[]>(endpoint)
}

/**
 * Get a single email template by ID.
 */
export async function getEmailTemplate(id: string): Promise<EmailTemplateDto> {
  return apiClient<EmailTemplateDto>(`/email-templates/${id}`)
}

/**
 * Update an email template.
 */
export async function updateEmailTemplate(
  id: string,
  request: UpdateEmailTemplateRequest
): Promise<EmailTemplateDto> {
  return apiClient<EmailTemplateDto>(`/email-templates/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Send a test email using a template.
 */
export async function sendTestEmail(
  id: string,
  request: SendTestEmailRequest
): Promise<EmailPreviewResponse> {
  return apiClient<EmailPreviewResponse>(`/email-templates/${id}/test`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Preview an email template with sample data.
 */
export async function previewEmailTemplate(
  id: string,
  request: PreviewEmailTemplateRequest
): Promise<EmailPreviewResponse> {
  return apiClient<EmailPreviewResponse>(`/email-templates/${id}/preview`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Get default sample data for a template's variables.
 */
export function getDefaultSampleData(variables: string[]): Record<string, string> {
  const sampleValues: Record<string, string> = {
    UserName: 'John Doe',
    OtpCode: '123456',
    ExpiryMinutes: '5',
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
