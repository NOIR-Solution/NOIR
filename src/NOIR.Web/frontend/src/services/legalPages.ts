/**
 * Legal Pages API Service
 *
 * Provides methods for managing legal pages (Terms of Service, Privacy Policy, etc.)
 * with Copy-on-Write support for multi-tenant customization.
 */
import { apiClient } from './apiClient'

// ============================================================================
// Types
// ============================================================================

export interface LegalPageDto {
  id: string
  slug: string
  title: string
  htmlContent: string
  metaTitle: string | null
  metaDescription: string | null
  canonicalUrl: string | null
  allowIndexing: boolean
  isActive: boolean
  version: number
  lastModified: string
  createdAt: string
  modifiedAt: string | null
  isInherited: boolean
}

export interface LegalPageListDto {
  id: string
  slug: string
  title: string
  isActive: boolean
  version: number
  lastModified: string
  isInherited: boolean
}

export interface PublicLegalPageDto {
  slug: string
  title: string
  htmlContent: string
  metaTitle: string | null
  metaDescription: string | null
  canonicalUrl: string | null
  allowIndexing: boolean
  lastModified: string
}

export interface UpdateLegalPageRequest {
  title: string
  htmlContent: string
  metaTitle?: string | null
  metaDescription?: string | null
  canonicalUrl?: string | null
  allowIndexing?: boolean
}

// ============================================================================
// Admin API
// ============================================================================

/**
 * Fetch list of all legal pages (admin)
 */
export const getLegalPages = async (): Promise<LegalPageListDto[]> => {
  return apiClient<LegalPageListDto[]>('/legal-pages')
}

/**
 * Fetch a single legal page by ID (admin)
 */
export const getLegalPageById = async (id: string): Promise<LegalPageDto> => {
  return apiClient<LegalPageDto>(`/legal-pages/${id}`)
}

/**
 * Update a legal page (implements copy-on-write for tenants)
 */
export const updateLegalPage = async (id: string, request: UpdateLegalPageRequest): Promise<LegalPageDto> => {
  return apiClient<LegalPageDto>(`/legal-pages/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Revert a tenant's customized legal page to the platform default
 */
export const revertLegalPageToDefault = async (id: string): Promise<LegalPageDto> => {
  return apiClient<LegalPageDto>(`/legal-pages/${id}/revert`, {
    method: 'POST',
  })
}

// ============================================================================
// Public API
// ============================================================================

/**
 * Fetch a legal page by slug for public display
 * Resolves tenant override → platform default
 */
export const getPublicLegalPage = async (slug: string): Promise<PublicLegalPageDto> => {
  return apiClient<PublicLegalPageDto>(`/public/legal/${slug}`)
}
