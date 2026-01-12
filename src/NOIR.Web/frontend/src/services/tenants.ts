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
 * Create a new tenant
 */
export async function createTenant(data: CreateTenantRequest): Promise<Tenant> {
  return apiClient<Tenant>('/tenants', {
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
