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
  domain: string | null
  description: string | null
  note: string | null
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
}

/**
 * Update tenant request
 * Matches backend UpdateTenantCommand
 */
export interface UpdateTenantRequest {
  identifier: string
  name: string
  domain?: string
  description?: string
  note?: string
  isActive: boolean
}

/**
 * Provision tenant request
 * Matches backend ProvisionTenantCommand
 */
export interface ProvisionTenantRequest {
  identifier: string
  name: string
  domain?: string
  description?: string
  note?: string
  createAdminUser?: boolean
  adminEmail?: string
  adminPassword?: string
  adminFirstName?: string
  adminLastName?: string
}

/**
 * Provision tenant result
 * Matches backend ProvisionTenantResult
 */
export interface ProvisionTenantResult {
  tenantId: string
  identifier: string
  name: string
  domain: string | null
  isActive: boolean
  createdAt: string
  adminUserCreated: boolean
  adminUserId: string | null
  adminEmail: string | null
  /** Error message if admin user creation failed */
  adminCreationError: string | null
}
