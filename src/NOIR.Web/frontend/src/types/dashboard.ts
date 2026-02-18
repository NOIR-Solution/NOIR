/**
 * Dashboard types matching backend DTOs.
 */

export interface DashboardMetricsDto {
  revenue: RevenueMetricsDto
  orderCounts: OrderStatusCountsDto
  topSellingProducts: TopSellingProductDto[]
  lowStockProducts: LowStockProductDto[]
  recentOrders: RecentOrderDto[]
  salesOverTime: SalesOverTimeDto[]
  productDistribution: ProductStatusDistributionDto
}

export interface RevenueMetricsDto {
  totalRevenue: number
  revenueThisMonth: number
  revenueLastMonth: number
  revenueToday: number
  totalOrders: number
  ordersThisMonth: number
  ordersToday: number
  averageOrderValue: number
}

export interface OrderStatusCountsDto {
  pending: number
  confirmed: number
  processing: number
  shipped: number
  delivered: number
  completed: number
  cancelled: number
  refunded: number
  returned: number
}

export interface TopSellingProductDto {
  productId: string
  productName: string
  imageUrl?: string | null
  totalQuantitySold: number
  totalRevenue: number
}

export interface LowStockProductDto {
  productId: string
  variantId: string
  productName: string
  variantName: string
  sku?: string | null
  stockQuantity: number
  lowStockThreshold: number
}

export interface RecentOrderDto {
  orderId: string
  orderNumber: string
  customerEmail?: string | null
  grandTotal: number
  status: string
  createdAt: string
}

export interface SalesOverTimeDto {
  date: string
  revenue: number
  orderCount: number
}

export interface ProductStatusDistributionDto {
  draft: number
  active: number
  archived: number
}
