/**
 * Inventory movement types matching backend InventoryMovementType enum.
 */
export type InventoryMovementType =
  | 'StockIn'
  | 'StockOut'
  | 'Adjustment'
  | 'Return'
  | 'Reservation'
  | 'ReservationRelease'
  | 'Damaged'
  | 'Expired'

/**
 * Inventory movement record for stock history display.
 */
export interface InventoryMovement {
  id: string
  productVariantId: string
  productId: string
  movementType: InventoryMovementType
  quantityBefore: number
  quantityMoved: number
  quantityAfter: number
  reference?: string | null
  notes?: string | null
  userId?: string | null
  correlationId?: string | null
  createdAt: string
}

/**
 * Paginated result for stock history queries.
 */
export interface StockHistoryPagedResult {
  items: InventoryMovement[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

/**
 * Parameters for fetching stock history.
 */
export interface GetStockHistoryParams {
  productId: string
  variantId: string
  page?: number
  pageSize?: number
}

// ============================================================================
// Inventory Receipts
// ============================================================================

export type InventoryReceiptType = 'StockIn' | 'StockOut'
export type InventoryReceiptStatus = 'Draft' | 'Confirmed' | 'Cancelled'

export interface InventoryReceiptDto {
  id: string
  receiptNumber: string
  type: InventoryReceiptType
  status: InventoryReceiptStatus
  notes?: string | null
  confirmedBy?: string | null
  confirmedAt?: string | null
  cancelledBy?: string | null
  cancelledAt?: string | null
  cancellationReason?: string | null
  totalQuantity: number
  totalCost: number
  items: InventoryReceiptItemDto[]
  createdAt: string
  createdBy?: string | null
}

export interface InventoryReceiptSummaryDto {
  id: string
  receiptNumber: string
  type: InventoryReceiptType
  status: InventoryReceiptStatus
  totalQuantity: number
  totalCost: number
  itemCount: number
  createdAt: string
  createdBy?: string | null
}

export interface InventoryReceiptItemDto {
  id: string
  productVariantId: string
  productId: string
  productName: string
  variantName: string
  sku?: string | null
  quantity: number
  unitCost: number
  lineTotal: number
}

export interface InventoryReceiptPagedResult {
  items: InventoryReceiptSummaryDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface CreateInventoryReceiptItemRequest {
  productVariantId: string
  productId: string
  productName: string
  variantName: string
  sku?: string | null
  quantity: number
  unitCost: number
}

export interface CreateInventoryReceiptRequest {
  type: InventoryReceiptType
  notes?: string | null
  items: CreateInventoryReceiptItemRequest[]
}

export interface GetInventoryReceiptsParams {
  page?: number
  pageSize?: number
  search?: string
  type?: InventoryReceiptType
  status?: InventoryReceiptStatus
}
