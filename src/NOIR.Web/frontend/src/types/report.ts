/**
 * Report types matching backend DTOs.
 * See: src/NOIR.Application/Features/Reports/DTOs/ReportDtos.cs
 */

// ─── Revenue Report ─────────────────────────────────────────────────────

export interface RevenueReportDto {
  period: string
  startDate: string
  endDate: string
  totalRevenue: number
  totalOrders: number
  averageOrderValue: number
  revenueByDay: DailyRevenueDto[]
  revenueByCategory: CategoryRevenueDto[]
  revenueByPaymentMethod: PaymentMethodRevenueDto[]
  comparedToPreviousPeriod: RevenueComparisonDto
}

export interface DailyRevenueDto {
  date: string
  revenue: number
  orderCount: number
}

export interface CategoryRevenueDto {
  categoryId: string | null
  categoryName: string
  revenue: number
  orderCount: number
}

export interface PaymentMethodRevenueDto {
  method: string
  revenue: number
  count: number
}

export interface RevenueComparisonDto {
  revenueChange: number
  orderCountChange: number
}

// ─── Best Sellers Report ────────────────────────────────────────────────

export interface BestSellersReportDto {
  products: BestSellerDto[]
  period: string
  startDate: string
  endDate: string
}

export interface BestSellerDto {
  productId: string
  productName: string
  imageUrl: string | null
  unitsSold: number
  revenue: number
}

// ─── Inventory Report ───────────────────────────────────────────────────

export interface InventoryReportDto {
  lowStockProducts: LowStockDto[]
  totalProducts: number
  totalVariants: number
  totalStockValue: number
  turnoverRate: number
}

export interface LowStockDto {
  productId: string
  name: string
  variantSku: string | null
  currentStock: number
  reorderLevel: number
}

// ─── Customer Report ────────────────────────────────────────────────────

export interface CustomerReportDto {
  newCustomers: number
  returningCustomers: number
  churnRate: number
  acquisitionByMonth: MonthlyAcquisitionDto[]
  topCustomers: TopCustomerDto[]
}

export interface MonthlyAcquisitionDto {
  month: string
  newCustomers: number
  revenue: number
}

export interface TopCustomerDto {
  customerId: string | null
  name: string
  totalSpent: number
  orderCount: number
}

// ─── Export ─────────────────────────────────────────────────────────────

export type ReportType = 'Revenue' | 'BestSellers' | 'Inventory' | 'CustomerAcquisition'

export type ExportFormat = 'CSV' | 'Excel'
