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
