/**
 * Customer Groups API Service
 *
 * Provides methods for managing customer groups/segments.
 */
import { apiClient } from './apiClient'
import type {
  CustomerGroup,
  CustomerGroupPagedResult,
  CreateCustomerGroupRequest,
  UpdateCustomerGroupRequest,
  AssignCustomersToGroupRequest,
  RemoveCustomersFromGroupRequest,
} from '@/types/customerGroup'

// ============================================================================
// Customer Groups
// ============================================================================

export interface GetCustomerGroupsParams {
  search?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}

/**
 * Fetch paginated list of customer groups
 */
export const getCustomerGroups = async (params: GetCustomerGroupsParams = {}): Promise<CustomerGroupPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)
  if (params.isActive !== undefined) queryParams.append('isActive', String(params.isActive))
  if (params.page) queryParams.append('page', params.page.toString())
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<CustomerGroupPagedResult>(`/customer-groups${query ? `?${query}` : ''}`)
}

/**
 * Fetch a single customer group by ID
 */
export const getCustomerGroupById = async (id: string): Promise<CustomerGroup> => {
  return apiClient<CustomerGroup>(`/customer-groups/${id}`)
}

/**
 * Create a new customer group
 */
export const createCustomerGroup = async (request: CreateCustomerGroupRequest): Promise<CustomerGroup> => {
  return apiClient<CustomerGroup>('/customer-groups', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing customer group
 */
export const updateCustomerGroup = async (id: string, request: UpdateCustomerGroupRequest): Promise<CustomerGroup> => {
  return apiClient<CustomerGroup>(`/customer-groups/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a customer group (soft delete)
 */
export const deleteCustomerGroup = async (id: string): Promise<void> => {
  return apiClient<void>(`/customer-groups/${id}`, {
    method: 'DELETE',
  })
}

/**
 * Assign customers to a group.
 * Called from the member management UI on the customer group detail page.
 */
export const assignCustomersToGroup = async (groupId: string, request: AssignCustomersToGroupRequest): Promise<void> => {
  return apiClient<void>(`/customer-groups/${groupId}/members`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Remove customers from a group.
 * Called from the member management UI on the customer group detail page.
 */
export const removeCustomersFromGroup = async (groupId: string, request: RemoveCustomersFromGroupRequest): Promise<void> => {
  return apiClient<void>(`/customer-groups/${groupId}/members`, {
    method: 'DELETE',
    body: JSON.stringify(request),
  })
}
