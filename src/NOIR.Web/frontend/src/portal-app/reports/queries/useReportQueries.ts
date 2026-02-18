import { useQuery } from '@tanstack/react-query'
import {
  getRevenueReport,
  getBestSellersReport,
  getInventoryReport,
  getCustomerReport,
  type GetRevenueReportParams,
  type GetBestSellersReportParams,
  type GetInventoryReportParams,
  type GetCustomerReportParams,
} from '@/services/reports'
import { reportKeys } from './queryKeys'

export const useRevenueReportQuery = (params: GetRevenueReportParams = {}) =>
  useQuery({
    queryKey: reportKeys.revenue(params),
    queryFn: () => getRevenueReport(params),
    staleTime: 60_000,
  })

export const useBestSellersReportQuery = (params: GetBestSellersReportParams = {}) =>
  useQuery({
    queryKey: reportKeys.bestSellers(params),
    queryFn: () => getBestSellersReport(params),
    staleTime: 60_000,
  })

export const useInventoryReportQuery = (params: GetInventoryReportParams = {}) =>
  useQuery({
    queryKey: reportKeys.inventory(params),
    queryFn: () => getInventoryReport(params),
    staleTime: 60_000,
  })

export const useCustomerReportQuery = (params: GetCustomerReportParams = {}) =>
  useQuery({
    queryKey: reportKeys.customers(params),
    queryFn: () => getCustomerReport(params),
    staleTime: 60_000,
  })
