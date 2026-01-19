/**
 * Tenant API Service
 * Provides CRUD operations for tenant management
 */
import { apiClient } from './apiClient'
import type {
  Tenant,
  TenantListItem,
  CreateTenantRequest,
  UpdateTenantRequest,
  ProvisionTenantRequest,
  ProvisionTenantResult,
  PaginatedResponse,
} from '@/types'

/**
 * Tenant list query parameters
 */
export interface GetTenantsParams {
  pageNumber?: number
  pageSize?: number
  search?: string
  isActive?: boolean
}

/**
 * Get paginated list of tenants
 */
export async function getTenants(
  params: GetTenantsParams = {}
): Promise<PaginatedResponse<TenantListItem>> {
  const searchParams = new URLSearchParams()

  if (params.pageNumber !== undefined) {
    searchParams.set('pageNumber', params.pageNumber.toString())
  }
  if (params.pageSize !== undefined) {
    searchParams.set('pageSize', params.pageSize.toString())
  }
  if (params.search) {
    searchParams.set('search', params.search)
  }
  if (params.isActive !== undefined) {
    searchParams.set('isActive', params.isActive.toString())
  }

  const query = searchParams.toString()
  const endpoint = `/tenants${query ? `?${query}` : ''}`

  return apiClient<PaginatedResponse<TenantListItem>>(endpoint)
}

/**
 * Get tenant by ID
 */
export async function getTenant(id: string): Promise<Tenant> {
  return apiClient<Tenant>(`/tenants/${id}`)
}

/**
 * Create a new tenant (basic)
 */
export async function createTenant(data: CreateTenantRequest): Promise<Tenant> {
  return apiClient<Tenant>('/tenants', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

/**
 * Provision a new tenant with optional admin user
 * This is the preferred way to create tenants as it handles all setup in one operation.
 */
export async function provisionTenant(data: ProvisionTenantRequest): Promise<ProvisionTenantResult> {
  return apiClient<ProvisionTenantResult>('/tenants/provision', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

/**
 * Update an existing tenant
 */
export async function updateTenant(
  id: string,
  data: UpdateTenantRequest
): Promise<Tenant> {
  return apiClient<Tenant>(`/tenants/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

/**
 * Delete a tenant (soft delete)
 */
export async function deleteTenant(id: string): Promise<void> {
  await apiClient<void>(`/tenants/${id}`, {
    method: 'DELETE',
  })
}

/**
 * Result of resetting tenant admin password
 */
export interface ResetTenantAdminPasswordResult {
  success: boolean
  message: string
  adminUserId: string | null
  adminEmail: string | null
}

/**
 * Reset the password for a tenant's admin user
 */
export async function resetTenantAdminPassword(
  tenantId: string,
  newPassword: string
): Promise<ResetTenantAdminPasswordResult> {
  return apiClient<ResetTenantAdminPasswordResult>(`/tenants/${tenantId}/reset-admin-password`, {
    method: 'POST',
    body: JSON.stringify({ newPassword }),
  })
}
