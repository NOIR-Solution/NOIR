import type { GetRevenueReportParams, GetBestSellersReportParams, GetInventoryReportParams, GetCustomerReportParams } from '@/services/reports'

export const reportKeys = {
  all: ['reports'] as const,
  revenue: (params?: GetRevenueReportParams) => [...reportKeys.all, 'revenue', params] as const,
  bestSellers: (params?: GetBestSellersReportParams) => [...reportKeys.all, 'best-sellers', params] as const,
  inventory: (params?: GetInventoryReportParams) => [...reportKeys.all, 'inventory', params] as const,
  customers: (params?: GetCustomerReportParams) => [...reportKeys.all, 'customers', params] as const,
}
