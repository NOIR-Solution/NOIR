/**
 * Order types matching backend DTOs.
 */

export type OrderStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Processing'
  | 'Shipped'
  | 'Delivered'
  | 'Completed'
  | 'Cancelled'
  | 'Refunded'
  | 'Returned'

export interface OrderDto {
  id: string
  orderNumber: string
  customerId?: string | null
  status: OrderStatus
  subTotal: number
  discountAmount: number
  shippingAmount: number
  taxAmount: number
  grandTotal: number
  currency: string
  customerEmail: string
  customerPhone?: string | null
  customerName?: string | null
  shippingAddress?: AddressDto | null
  billingAddress?: AddressDto | null
  shippingMethod?: string | null
  trackingNumber?: string | null
  shippingCarrier?: string | null
  estimatedDeliveryAt?: string | null
  couponCode?: string | null
  customerNotes?: string | null
  items: OrderItemDto[]
  createdAt: string
  confirmedAt?: string | null
  shippedAt?: string | null
  deliveredAt?: string | null
  completedAt?: string | null
  cancelledAt?: string | null
  cancellationReason?: string | null
  returnedAt?: string | null
  returnReason?: string | null
}

export interface OrderItemDto {
  id: string
  productId: string
  productVariantId: string
  productName: string
  variantName: string
  sku?: string | null
  imageUrl?: string | null
  optionsSnapshot?: string | null
  unitPrice: number
  quantity: number
  discountAmount: number
  taxAmount: number
  lineTotal: number
  subtotal: number
}

export interface AddressDto {
  fullName: string
  phone: string
  addressLine1: string
  addressLine2?: string | null
  ward: string
  district: string
  province: string
  country: string
  postalCode?: string | null
  isDefault: boolean
}

export interface OrderSummaryDto {
  id: string
  orderNumber: string
  status: OrderStatus
  grandTotal: number
  currency: string
  customerEmail: string
  customerName?: string | null
  itemCount: number
  createdAt: string
}

export interface OrderPagedResult {
  items: OrderSummaryDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface CreateOrderItemRequest {
  productId: string
  productVariantId: string
  productName: string
  variantName: string
  unitPrice: number
  quantity: number
  sku?: string | null
  imageUrl?: string | null
  optionsSnapshot?: string | null
}

export interface CreateAddressRequest {
  fullName: string
  phone: string
  addressLine1: string
  addressLine2?: string | null
  ward: string
  district: string
  province: string
  country?: string
  postalCode?: string | null
}

export interface OrderNoteDto {
  id: string
  orderId: string
  content: string
  createdByUserId: string
  createdByUserName: string
  isInternal: boolean
  createdAt: string
}

export interface CreateOrderRequest {
  customerEmail: string
  customerPhone?: string | null
  customerName?: string | null
  items: CreateOrderItemRequest[]
  shippingAddress?: CreateAddressRequest | null
  billingAddress?: CreateAddressRequest | null
  shippingMethod?: string | null
  couponCode?: string | null
  customerNotes?: string | null
}
