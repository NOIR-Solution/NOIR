/**
 * Tenant-related types
 * These mirror the backend Tenant DTOs exactly
 */

/**
 * Full tenant details
 * Matches backend TenantDto from NOIR.Application.Features.Tenants.DTOs
 */
export interface Tenant {
  id: string
  identifier: string
  name: string | null
  logoUrl: string | null
  primaryColor: string | null
  accentColor: string | null
  theme: string | null
  isActive: boolean
  createdAt: string
  modifiedAt: string | null
}

/**
 * Tenant list item (lighter version for table display)
 * Matches backend TenantListDto
 */
export interface TenantListItem {
  id: string
  identifier: string
  name: string | null
  isActive: boolean
  createdAt: string
}

/**
 * Create tenant request
 * Matches backend CreateTenantCommand
 */
export interface CreateTenantRequest {
  identifier: string
  name: string
  logoUrl?: string | null
  primaryColor?: string | null
  accentColor?: string | null
  theme?: string | null
}

/**
 * Update tenant request
 * Matches backend UpdateTenantCommand
 */
export interface UpdateTenantRequest {
  name: string
  logoUrl?: string | null
  primaryColor?: string | null
  accentColor?: string | null
  theme?: string | null
  isActive: boolean
}
