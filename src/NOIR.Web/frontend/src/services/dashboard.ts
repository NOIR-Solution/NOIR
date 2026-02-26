/**
 * Dashboard API Service
 *
 * Provides methods for fetching dashboard metrics and KPI data.
 */
import { apiClient } from './apiClient'

// ─── Types ───────────────────────────────────────────────────────────────

export interface DashboardMetricsParams {
  topProducts?: number
  lowStockThreshold?: number
  recentOrders?: number
  salesDays?: number
}

export interface RevenueMetrics {
  totalRevenue: number
  revenueThisMonth: number
  revenueLastMonth: number
  revenueToday: number
  totalOrders: number
  ordersThisMonth: number
  ordersToday: number
  averageOrderValue: number
}

export interface OrderCounts {
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

export interface TopSellingProduct {
  productId: string
  productName: string
  imageUrl?: string | null
  totalQuantitySold: number
  totalRevenue: number
}

export interface LowStockProduct {
  productId: string
  variantId: string
  productName: string
  variantName: string
  sku: string
  stockQuantity: number
  lowStockThreshold: number
}

export interface RecentOrder {
  orderId: string
  orderNumber: string
  customerEmail: string
  grandTotal: number
  status: string
  createdAt: string
}

export interface SalesOverTime {
  date: string
  revenue: number
  orderCount: number
}

export interface ProductDistribution {
  draft: number
  active: number
  archived: number
}

export interface DashboardMetricsDto {
  revenue: RevenueMetrics
  orderCounts: OrderCounts
  topSellingProducts: TopSellingProduct[]
  lowStockProducts: LowStockProduct[]
  recentOrders: RecentOrder[]
  salesOverTime: SalesOverTime[]
  productDistribution: ProductDistribution
}

// ─── API Functions ───────────────────────────────────────────────────────

export const getDashboardMetrics = async (
  params: DashboardMetricsParams = {}
): Promise<DashboardMetricsDto> => {
  const queryParams = new URLSearchParams()
  if (params.topProducts != null) queryParams.append('topProducts', params.topProducts.toString())
  if (params.lowStockThreshold != null) queryParams.append('lowStockThreshold', params.lowStockThreshold.toString())
  if (params.recentOrders != null) queryParams.append('recentOrders', params.recentOrders.toString())
  if (params.salesDays != null) queryParams.append('salesDays', params.salesDays.toString())

  const query = queryParams.toString()
  return apiClient<DashboardMetricsDto>(`/dashboard/metrics${query ? `?${query}` : ''}`)
}
