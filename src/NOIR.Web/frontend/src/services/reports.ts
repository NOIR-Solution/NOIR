/**
 * Reports API Service
 *
 * Provides methods for fetching analytics reports and exporting report data.
 */
import { apiClient } from './apiClient'
import { getAccessToken } from './tokenStorage'
import { getPageContext } from './pageContext'
import { i18n } from '@/i18n'
import type {
  RevenueReportDto,
  BestSellersReportDto,
  InventoryReportDto,
  CustomerReportDto,
  ReportType,
  ExportFormat,
} from '@/types/report'

// ─── Parameters ─────────────────────────────────────────────────────────

export interface GetRevenueReportParams {
  period?: string
  startDate?: string
  endDate?: string
}

export interface GetBestSellersReportParams {
  startDate?: string
  endDate?: string
  topN?: number
}

export interface GetInventoryReportParams {
  lowStockThreshold?: number
}

export interface GetCustomerReportParams {
  startDate?: string
  endDate?: string
}

export interface ExportReportParams {
  reportType: ReportType
  format: ExportFormat
  startDate?: string
  endDate?: string
}

// ─── API Functions ──────────────────────────────────────────────────────

export const getRevenueReport = async (
  params: GetRevenueReportParams = {}
): Promise<RevenueReportDto> => {
  const queryParams = new URLSearchParams()
  if (params.period) queryParams.append('period', params.period)
  if (params.startDate) queryParams.append('startDate', params.startDate)
  if (params.endDate) queryParams.append('endDate', params.endDate)

  const query = queryParams.toString()
  return apiClient<RevenueReportDto>(`/reports/revenue${query ? `?${query}` : ''}`)
}

export const getBestSellersReport = async (
  params: GetBestSellersReportParams = {}
): Promise<BestSellersReportDto> => {
  const queryParams = new URLSearchParams()
  if (params.startDate) queryParams.append('startDate', params.startDate)
  if (params.endDate) queryParams.append('endDate', params.endDate)
  if (params.topN != null) queryParams.append('topN', params.topN.toString())

  const query = queryParams.toString()
  return apiClient<BestSellersReportDto>(`/reports/best-sellers${query ? `?${query}` : ''}`)
}

export const getInventoryReport = async (
  params: GetInventoryReportParams = {}
): Promise<InventoryReportDto> => {
  const queryParams = new URLSearchParams()
  if (params.lowStockThreshold != null) queryParams.append('lowStockThreshold', params.lowStockThreshold.toString())

  const query = queryParams.toString()
  return apiClient<InventoryReportDto>(`/reports/inventory${query ? `?${query}` : ''}`)
}

export const getCustomerReport = async (
  params: GetCustomerReportParams = {}
): Promise<CustomerReportDto> => {
  const queryParams = new URLSearchParams()
  if (params.startDate) queryParams.append('startDate', params.startDate)
  if (params.endDate) queryParams.append('endDate', params.endDate)

  const query = queryParams.toString()
  return apiClient<CustomerReportDto>(`/reports/customers${query ? `?${query}` : ''}`)
}

/**
 * Export report as file (CSV or Excel).
 * Uses raw fetch to handle blob response.
 */
export const exportReport = async (params: ExportReportParams): Promise<void> => {
  const queryParams = new URLSearchParams()
  queryParams.append('reportType', params.reportType)
  queryParams.append('format', params.format)
  if (params.startDate) queryParams.append('startDate', params.startDate)
  if (params.endDate) queryParams.append('endDate', params.endDate)

  const token = getAccessToken()
  const pageContext = getPageContext()
  const headers: Record<string, string> = {
    'Accept-Language': i18n.language,
  }
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }
  if (pageContext) {
    headers['X-Page-Context'] = pageContext
  }

  const response = await fetch(`/api/reports/export?${queryParams.toString()}`, {
    credentials: 'include',
    headers,
  })

  if (!response.ok) {
    throw new Error(`Export failed: ${response.status}`)
  }

  // Extract filename from Content-Disposition header or generate one
  const disposition = response.headers.get('Content-Disposition')
  let filename = `report.${params.format === 'CSV' ? 'csv' : 'xlsx'}`
  if (disposition) {
    const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
    if (match?.[1]) {
      filename = match[1].replace(/['"]/g, '')
    }
  }

  const blob = await response.blob()
  const url = window.URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  window.URL.revokeObjectURL(url)
}
