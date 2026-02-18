/**
 * Customer types matching backend DTOs.
 */

export type CustomerSegment =
  | 'New'
  | 'Active'
  | 'AtRisk'
  | 'Dormant'
  | 'Lost'
  | 'VIP'

export type CustomerTier =
  | 'Standard'
  | 'Silver'
  | 'Gold'
  | 'Platinum'
  | 'Diamond'

export type AddressType =
  | 'Shipping'
  | 'Billing'
  | 'Both'

export interface CustomerDto {
  id: string
  userId?: string | null
  email: string
  firstName: string
  lastName: string
  phone?: string | null
  segment: CustomerSegment
  tier: CustomerTier
  lastOrderDate?: string | null
  totalOrders: number
  totalSpent: number
  averageOrderValue: number
  loyaltyPoints: number
  lifetimeLoyaltyPoints: number
  tags?: string | null
  notes?: string | null
  isActive: boolean
  createdAt: string
  addresses: CustomerAddressDto[]
}

export interface CustomerSummaryDto {
  id: string
  email: string
  firstName: string
  lastName: string
  phone?: string | null
  segment: CustomerSegment
  tier: CustomerTier
  totalOrders: number
  totalSpent: number
  loyaltyPoints: number
  isActive: boolean
  createdAt: string
}

export interface CustomerAddressDto {
  id: string
  customerId: string
  addressType: AddressType
  fullName: string
  phone: string
  addressLine1: string
  addressLine2?: string | null
  ward?: string | null
  district?: string | null
  province: string
  postalCode?: string | null
  isDefault: boolean
}

export interface SegmentDistributionDto {
  segment: CustomerSegment
  count: number
}

export interface TierDistributionDto {
  tier: CustomerTier
  count: number
}

export interface CustomerStatsDto {
  totalCustomers: number
  activeCustomers: number
  segmentDistribution: SegmentDistributionDto[]
  tierDistribution: TierDistributionDto[]
  topSpenders: CustomerSummaryDto[]
}

export interface CustomerPagedResult {
  items: CustomerSummaryDto[]
  totalCount: number
  pageIndex: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface OrderPagedResultForCustomer {
  items: CustomerOrderSummaryDto[]
  totalCount: number
  pageIndex: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface CustomerOrderSummaryDto {
  id: string
  orderNumber: string
  status: string
  grandTotal: number
  currency: string
  customerEmail: string
  customerName?: string | null
  itemCount: number
  createdAt: string
}

export interface CreateCustomerRequest {
  email: string
  firstName: string
  lastName: string
  phone?: string | null
  tags?: string | null
  notes?: string | null
}

export interface UpdateCustomerRequest {
  firstName: string
  lastName: string
  email: string
  phone?: string | null
  tags?: string | null
  notes?: string | null
}

export interface CreateCustomerAddressRequest {
  addressType: AddressType
  fullName: string
  phone: string
  addressLine1: string
  addressLine2?: string | null
  ward?: string | null
  district?: string | null
  province: string
  postalCode?: string | null
  isDefault: boolean
}

export interface UpdateCustomerAddressRequest {
  addressType: AddressType
  fullName: string
  phone: string
  addressLine1: string
  addressLine2?: string | null
  ward?: string | null
  district?: string | null
  province: string
  postalCode?: string | null
  isDefault: boolean
}

export interface UpdateCustomerSegmentRequest {
  segment: CustomerSegment
}

export interface LoyaltyPointsRequest {
  points: number
  reason?: string | null
}
