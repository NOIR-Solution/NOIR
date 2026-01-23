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
// Branding API
// ============================================================================

/**
 * Get branding settings for the current tenant
 */
export async function getBrandingSettings(): Promise<BrandingSettingsDto> {
  return apiClient<BrandingSettingsDto>('/tenant-settings/branding')
}

/**
 * Update branding settings for the current tenant
 */
export async function updateBrandingSettings(
  request: UpdateBrandingSettingsRequest
): Promise<BrandingSettingsDto> {
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
export async function getContactSettings(): Promise<ContactSettingsDto> {
  return apiClient<ContactSettingsDto>('/tenant-settings/contact')
}

/**
 * Update contact settings for the current tenant
 */
export async function updateContactSettings(
  request: UpdateContactSettingsRequest
): Promise<ContactSettingsDto> {
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
export async function getRegionalSettings(): Promise<RegionalSettingsDto> {
  return apiClient<RegionalSettingsDto>('/tenant-settings/regional')
}

/**
 * Update regional settings for the current tenant
 */
export async function updateRegionalSettings(
  request: UpdateRegionalSettingsRequest
): Promise<RegionalSettingsDto> {
  return apiClient<RegionalSettingsDto>('/tenant-settings/regional', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}
