/**
 * Dashboard API Service
 *
 * Provides methods for fetching aggregated dashboard metrics.
 */
import { apiClient } from './apiClient'
import type { DashboardMetricsDto } from '@/types/dashboard'

export interface GetDashboardMetricsParams {
  topProducts?: number
  lowStockThreshold?: number
  recentOrders?: number
  salesDays?: number
}

export const getDashboardMetrics = async (
  params: GetDashboardMetricsParams = {}
): Promise<DashboardMetricsDto> => {
  const queryParams = new URLSearchParams()
  if (params.topProducts) queryParams.append('topProducts', params.topProducts.toString())
  if (params.lowStockThreshold) queryParams.append('lowStockThreshold', params.lowStockThreshold.toString())
  if (params.recentOrders) queryParams.append('recentOrders', params.recentOrders.toString())
  if (params.salesDays) queryParams.append('salesDays', params.salesDays.toString())

  const query = queryParams.toString()
  return apiClient<DashboardMetricsDto>(`/dashboard/metrics${query ? `?${query}` : ''}`)
}
