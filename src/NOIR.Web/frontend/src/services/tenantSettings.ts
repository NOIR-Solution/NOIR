/**
 * Tenant Settings API Service
 *
 * Provides methods for managing tenant-level settings
 * including branding, contact, and regional configuration.
 */
import { apiClient } from './apiClient'

// ============================================================================
// Types - Branding
// ============================================================================

export interface BrandingSettingsDto {
  logoUrl: string | null
  faviconUrl: string | null
  primaryColor: string | null
  secondaryColor: string | null
  darkModeDefault: boolean
}

export interface UpdateBrandingSettingsRequest {
  logoUrl: string | null
  faviconUrl: string | null
  primaryColor: string | null
  secondaryColor: string | null
  darkModeDefault: boolean
}

// ============================================================================
// Types - Contact
// ============================================================================

export interface ContactSettingsDto {
  email: string | null
  phone: string | null
  address: string | null
}

export interface UpdateContactSettingsRequest {
  email: string | null
  phone: string | null
  address: string | null
}

// ============================================================================
// Types - Regional
// ============================================================================

export interface RegionalSettingsDto {
  timezone: string
  language: string
  dateFormat: string
}

export interface UpdateRegionalSettingsRequest {
  timezone: string
  language: string
  dateFormat: string
}

// ============================================================================
// Types - SMTP
// ============================================================================

export interface TenantSmtpSettingsDto {
  host: string
  port: number
  username: string | null
  hasPassword: boolean
  fromEmail: string
  fromName: string
  useSsl: boolean
  isConfigured: boolean
  /** True if using platform defaults (not customized at tenant level) */
  isInherited: boolean
}

export interface UpdateTenantSmtpSettingsRequest {
  host: string
  port: number
  username: string | null
  password: string | null
  fromEmail: string
  fromName: string
  useSsl: boolean
}

export interface TestTenantSmtpRequest {
  recipientEmail: string
}

// ============================================================================
// SMTP API
// ============================================================================

/**
 * Get SMTP settings for the current tenant.
 * Returns platform defaults if tenant hasn't customized.
 */
export const getTenantSmtpSettings = async (): Promise<TenantSmtpSettingsDto> => {
  return apiClient<TenantSmtpSettingsDto>('/tenant-settings/smtp')
}

/**
 * Update SMTP settings for the current tenant (Copy-on-Write).
 * Creates tenant-specific settings on first save.
 */
export const updateTenantSmtpSettings = async (
  request: UpdateTenantSmtpSettingsRequest
): Promise<TenantSmtpSettingsDto> => {
  return apiClient<TenantSmtpSettingsDto>('/tenant-settings/smtp', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Revert tenant SMTP settings to platform defaults.
 * Deletes tenant-specific settings.
 */
export const revertTenantSmtpSettings = async (): Promise<TenantSmtpSettingsDto> => {
  return apiClient<TenantSmtpSettingsDto>('/tenant-settings/smtp/revert', {
    method: 'POST',
  })
}

/**
 * Test tenant SMTP connection by sending a test email.
 */
export const testTenantSmtpConnection = async (
  request: TestTenantSmtpRequest
): Promise<boolean> => {
  return apiClient<boolean>('/tenant-settings/smtp/test', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

// ============================================================================
// Branding API
// ============================================================================

/**
 * Get branding settings for the current tenant
 */
export const getBrandingSettings = async (): Promise<BrandingSettingsDto> => {
  return apiClient<BrandingSettingsDto>('/tenant-settings/branding')
}

/**
 * Update branding settings for the current tenant
 */
export const updateBrandingSettings = async (
  request: UpdateBrandingSettingsRequest
): Promise<BrandingSettingsDto> => {
  return apiClient<BrandingSettingsDto>('/tenant-settings/branding', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

// ============================================================================
// Contact API
// ============================================================================

/**
 * Get contact settings for the current tenant
 */
export const getContactSettings = async (): Promise<ContactSettingsDto> => {
  return apiClient<ContactSettingsDto>('/tenant-settings/contact')
}

/**
 * Update contact settings for the current tenant
 */
export const updateContactSettings = async (
  request: UpdateContactSettingsRequest
): Promise<ContactSettingsDto> => {
  return apiClient<ContactSettingsDto>('/tenant-settings/contact', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

// ============================================================================
// Regional API
// ============================================================================

/**
 * Get regional settings for the current tenant
 */
export const getRegionalSettings = async (): Promise<RegionalSettingsDto> => {
  return apiClient<RegionalSettingsDto>('/tenant-settings/regional')
}

/**
 * Update regional settings for the current tenant
 */
export const updateRegionalSettings = async (
  request: UpdateRegionalSettingsRequest
): Promise<RegionalSettingsDto> => {
  return apiClient<RegionalSettingsDto>('/tenant-settings/regional', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}
