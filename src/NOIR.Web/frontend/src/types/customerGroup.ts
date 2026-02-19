/**
 * Customer Group-related TypeScript types
 * Mirrors backend DTOs from NOIR.Application.Features.CustomerGroups.DTOs
 */

// ============================================================================
// Customer Group Types
// ============================================================================

/**
 * Full customer group details for editing
 */
export interface CustomerGroup {
  id: string
  name: string
  slug: string
  description?: string | null
  isActive: boolean
  memberCount: number
  createdAt: string
  modifiedAt?: string | null
}

/**
 * Simplified customer group for list views
 */
export interface CustomerGroupListItem {
  id: string
  name: string
  slug: string
  description?: string | null
  isActive: boolean
  memberCount: number
}

// ============================================================================
// Request Types
// ============================================================================

export interface CreateCustomerGroupRequest {
  name: string
  description?: string | null
}

export interface UpdateCustomerGroupRequest {
  name: string
  description?: string | null
  isActive: boolean
}

export interface AssignCustomersToGroupRequest {
  customerIds: string[]
}

export interface RemoveCustomersFromGroupRequest {
  customerIds: string[]
}

// ============================================================================
// Paged Result Types
// ============================================================================

export interface CustomerGroupPagedResult {
  items: CustomerGroupListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
