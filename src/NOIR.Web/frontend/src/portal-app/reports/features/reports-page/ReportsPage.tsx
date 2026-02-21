/**
 * Reports & Analytics Page
 *
 * Dashboard-style page aggregating revenue, best sellers,
 * inventory, and customer reports with date range filtering and export.
 */
import { useState, useMemo } from 'react'
import { useUrlTab } from '@/hooks/useUrlTab'
import { useTranslation } from 'react-i18next'
import type { DateRange } from 'react-day-picker'
import { subDays, startOfDay, endOfDay } from 'date-fns'
import {
  BarChart3,
  DollarSign,
  ShoppingCart,
  TrendingUp,
  TrendingDown,
  Package,
  AlertTriangle,
  Users,
  UserPlus,
  UserCheck,
  Award,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { formatCurrency } from '@/lib/utils/currency'
import {
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  PageHeader,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import {
  useRevenueReportQuery,
  useBestSellersReportQuery,
  useInventoryReportQuery,
  useCustomerReportQuery,
} from '@/portal-app/reports/queries'
import { DateRangePresets } from '@/portal-app/reports/components/DateRangePresets'
import { ExportButton } from '@/portal-app/reports/components/ExportButton'
import type { ReportType, DailyRevenueDto, CategoryRevenueDto } from '@/types/report'

// ─── Helper Components ──────────────────────────────────────────────────

const MetricCard = ({
  title,
  value,
  subtitle,
  icon: Icon,
  trend,
  trendValue,
  iconColor = 'text-primary',
  iconBg = 'bg-primary/10',
}: {
  title: string
  value: string
  subtitle?: string
  icon: typeof DollarSign
  trend?: 'up' | 'down' | 'neutral'
  trendValue?: string
  iconColor?: string
  iconBg?: string
}) => (
  <Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
    <CardContent className="pt-6">
      <div className="flex items-start justify-between">
        <div className="space-y-2">
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          <p className="text-2xl font-bold">{value}</p>
          {subtitle && (
            <p className="text-xs text-muted-foreground">{subtitle}</p>
          )}
          {trendValue && (
            <div className="flex items-center gap-1 text-xs">
              {trend === 'up' && <TrendingUp className="h-3 w-3 text-green-600" />}
              {trend === 'down' && <TrendingDown className="h-3 w-3 text-red-600" />}
              <span className={trend === 'up' ? 'text-green-600' : trend === 'down' ? 'text-red-600' : 'text-muted-foreground'}>
                {trendValue}
              </span>
            </div>
          )}
        </div>
        <div className={`p-3 rounded-xl ${iconBg}`}>
          <Icon className={`h-5 w-5 ${iconColor}`} />
        </div>
      </div>
    </CardContent>
  </Card>
)

/**
 * Simple CSS-based bar chart using Tailwind classes.
 * Each bar represents a day's revenue as a percentage of the max.
 */
const RevenueBarChart = ({
  data,
  isLoading,
}: {
  data: DailyRevenueDto[]
  isLoading: boolean
}) => {
  const { t } = useTranslation('common')

  if (isLoading) {
    return (
      <div className="flex items-end gap-1 h-48">
        {[...Array(14)].map((_, i) => (
          <Skeleton key={i} className="flex-1 h-full" />
        ))}
      </div>
    )
  }

  if (!data.length) {
    return (
      <div className="flex items-center justify-center h-48 text-sm text-muted-foreground">
        {t('labels.noData', 'No data available')}
      </div>
    )
  }

  const maxRevenue = Math.max(...data.map((d) => d.revenue), 1)

  return (
    <div className="space-y-2">
      <div className="flex items-end gap-1 h-48" role="img" aria-label={t('reports.revenueChart', 'Revenue chart')}>
        {data.map((day, i) => {
          const heightPercent = (day.revenue / maxRevenue) * 100
          const dateLabel = new Date(day.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
          return (
            <div
              key={i}
              className="flex-1 flex flex-col items-center justify-end h-full group"
            >
              <div className="relative w-full">
                <div
                  className="w-full bg-primary/80 hover:bg-primary rounded-t-sm transition-colors duration-200 min-h-[2px]"
                  style={{ height: `${Math.max(heightPercent, 1)}%`, minHeight: heightPercent > 0 ? '4px' : '2px' }}
                  title={`${dateLabel}: ${formatCurrency(day.revenue)} (${day.orderCount} orders)`}
                />
              </div>
            </div>
          )
        })}
      </div>
      {/* X-axis labels - show every few labels to avoid overlap */}
      <div className="flex gap-1">
        {data.map((day, i) => {
          const showLabel = data.length <= 14 || i % Math.ceil(data.length / 7) === 0 || i === data.length - 1
          return (
            <div key={i} className="flex-1 text-center">
              {showLabel && (
                <span className="text-[10px] text-muted-foreground">
                  {new Date(day.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
                </span>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}

/**
 * Horizontal bar chart for category breakdown.
 */
const CategoryBreakdownChart = ({
  data,
  isLoading,
}: {
  data: CategoryRevenueDto[]
  isLoading: boolean
}) => {
  if (isLoading) {
    return (
      <div className="space-y-3">
        {[...Array(5)].map((_, i) => (
          <Skeleton key={i} className="h-8 w-full" />
        ))}
      </div>
    )
  }

  if (!data.length) {
    return null
  }

  const maxRevenue = Math.max(...data.map((d) => d.revenue), 1)
  const colors = [
    'bg-primary',
    'bg-blue-500',
    'bg-emerald-500',
    'bg-amber-500',
    'bg-rose-500',
    'bg-violet-500',
    'bg-cyan-500',
    'bg-orange-500',
  ]

  return (
    <div className="space-y-3">
      {data.slice(0, 8).map((cat, i) => {
        const widthPercent = (cat.revenue / maxRevenue) * 100
        return (
          <div key={cat.categoryId ?? i} className="space-y-1">
            <div className="flex items-center justify-between text-sm">
              <span className="font-medium truncate">{cat.categoryName}</span>
              <span className="text-muted-foreground ml-2 shrink-0">
                {formatCurrency(cat.revenue)}
              </span>
            </div>
            <div className="h-2 w-full rounded-full bg-muted overflow-hidden">
              <div
                className={`h-full rounded-full ${colors[i % colors.length]} transition-all duration-500`}
                style={{ width: `${Math.max(widthPercent, 2)}%` }}
              />
            </div>
          </div>
        )
      })}
    </div>
  )
}

const SkeletonRows = ({ columns, rows = 5 }: { columns: number; rows?: number }) => (
  <>
    {[...Array(rows)].map((_, i) => (
      <TableRow key={i} className="animate-pulse">
        {[...Array(columns)].map((_, j) => (
          <TableCell key={j}>
            <Skeleton className="h-4 w-full" />
          </TableCell>
        ))}
      </TableRow>
    ))}
  </>
)

// ─── Main Page Component ────────────────────────────────────────────────

export const ReportsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Reports')

  // Date range state - default to last 30 days
  const [dateRange, setDateRange] = useState<DateRange | undefined>({
    from: startOfDay(subDays(new Date(), 30)),
    to: endOfDay(new Date()),
  })
  const { activeTab, handleTabChange, isPending: isTabPending } = useUrlTab({ defaultTab: 'revenue' })

  // Derive ISO date strings for API calls
  const dateParams = useMemo(() => ({
    startDate: dateRange?.from?.toISOString(),
    endDate: dateRange?.to?.toISOString(),
  }), [dateRange])

  // Active export type based on selected tab
  const exportReportType: ReportType = useMemo(() => {
    const map: Record<string, ReportType> = {
      revenue: 'Revenue',
      bestSellers: 'BestSellers',
      inventory: 'Inventory',
      customers: 'CustomerAcquisition',
    }
    return map[activeTab] ?? 'Revenue'
  }, [activeTab])

  // ─── Queries ────────────────────────────────────────────────────────

  const {
    data: revenueData,
    isLoading: revenueLoading,
  } = useRevenueReportQuery({
    period: 'daily',
    startDate: dateParams.startDate,
    endDate: dateParams.endDate,
  })

  const {
    data: bestSellersData,
    isLoading: bestSellersLoading,
  } = useBestSellersReportQuery({
    startDate: dateParams.startDate,
    endDate: dateParams.endDate,
    topN: 10,
  })

  const {
    data: inventoryData,
    isLoading: inventoryLoading,
  } = useInventoryReportQuery({
    lowStockThreshold: 10,
  })

  const {
    data: customerData,
    isLoading: customerLoading,
  } = useCustomerReportQuery({
    startDate: dateParams.startDate,
    endDate: dateParams.endDate,
  })

  // ─── Derived Values ─────────────────────────────────────────────────

  const revenueChange = revenueData?.comparedToPreviousPeriod?.revenueChange ?? 0
  const revenueTrend: 'up' | 'down' | 'neutral' = revenueChange > 0 ? 'up' : revenueChange < 0 ? 'down' : 'neutral'

  const getStockStatusBadge = (currentStock: number, reorderLevel: number) => {
    if (currentStock === 0) {
      return <Badge variant="destructive">{t('products.outOfStock', 'Out of Stock')}</Badge>
    }
    if (currentStock <= reorderLevel) {
      return <Badge variant="outline" className="border-amber-500 text-amber-600">{t('products.lowStock', 'Low Stock')}</Badge>
    }
    return <Badge variant="outline" className="border-green-500 text-green-600">{t('products.inStock', 'In Stock')}</Badge>
  }

  // ─── Render ─────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <PageHeader
        icon={BarChart3}
        title={t('reports.title', 'Reports & Analytics')}
        description={t('reports.description', 'Monitor business performance with detailed analytics')}
        responsive
      />

      {/* Controls: Date range + Export */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <DateRangePresets value={dateRange} onChange={setDateRange} />
        <ExportButton
          reportType={exportReportType}
          startDate={dateParams.startDate}
          endDate={dateParams.endDate}
        />
      </div>

      {/* Summary Metric Cards */}
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
        {revenueLoading ? (
          [...Array(4)].map((_, i) => (
            <Card key={i} className="shadow-sm border-border/50">
              <CardContent className="pt-6">
                <Skeleton className="h-20 w-full" />
              </CardContent>
            </Card>
          ))
        ) : (
          <>
            <MetricCard
              title={t('reports.totalRevenue', 'Total Revenue')}
              value={formatCurrency(revenueData?.totalRevenue ?? 0)}
              subtitle={t('reports.forSelectedPeriod', 'For selected period')}
              icon={DollarSign}
              trend={revenueTrend}
              trendValue={`${revenueChange >= 0 ? '+' : ''}${revenueChange.toFixed(1)}% ${t('reports.vsPreviousPeriod', 'vs previous period')}`}
              iconColor="text-emerald-600"
              iconBg="bg-emerald-100 dark:bg-emerald-900/30"
            />
            <MetricCard
              title={t('reports.totalOrders', 'Total Orders')}
              value={(revenueData?.totalOrders ?? 0).toLocaleString()}
              subtitle={t('reports.forSelectedPeriod', 'For selected period')}
              icon={ShoppingCart}
              iconColor="text-blue-600"
              iconBg="bg-blue-100 dark:bg-blue-900/30"
            />
            <MetricCard
              title={t('reports.avgOrderValue', 'Avg Order Value')}
              value={formatCurrency(revenueData?.averageOrderValue ?? 0)}
              icon={TrendingUp}
              iconColor="text-violet-600"
              iconBg="bg-violet-100 dark:bg-violet-900/30"
            />
            <MetricCard
              title={t('reports.lowStockAlerts', 'Low Stock Alerts')}
              value={(inventoryData?.lowStockProducts.length ?? 0).toString()}
              subtitle={`${inventoryData?.totalProducts ?? 0} ${t('reports.totalProducts', 'total products')}`}
              icon={AlertTriangle}
              iconColor="text-amber-600"
              iconBg="bg-amber-100 dark:bg-amber-900/30"
            />
          </>
        )}
      </div>

      {/* Tabbed Report Sections */}
      <Tabs value={activeTab} onValueChange={handleTabChange} className={isTabPending ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="revenue" className="cursor-pointer">
            <DollarSign className="h-4 w-4 mr-2 hidden sm:inline-block" />
            {t('reports.tabs.revenue', 'Revenue')}
          </TabsTrigger>
          <TabsTrigger value="bestSellers" className="cursor-pointer">
            <Award className="h-4 w-4 mr-2 hidden sm:inline-block" />
            {t('reports.tabs.bestSellers', 'Best Sellers')}
          </TabsTrigger>
          <TabsTrigger value="inventory" className="cursor-pointer">
            <Package className="h-4 w-4 mr-2 hidden sm:inline-block" />
            {t('reports.tabs.inventory', 'Inventory')}
          </TabsTrigger>
          <TabsTrigger value="customers" className="cursor-pointer">
            <Users className="h-4 w-4 mr-2 hidden sm:inline-block" />
            {t('reports.tabs.customers', 'Customers')}
          </TabsTrigger>
        </TabsList>

          {/* ─── Revenue Tab ──────────────────────────────────────────── */}
          <TabsContent value="revenue" className="space-y-6 mt-6">
            {/* Revenue Chart */}
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader>
                <CardTitle>{t('reports.revenueOverTime', 'Revenue Over Time')}</CardTitle>
                <CardDescription>{t('reports.revenueOverTimeDesc', 'Daily revenue breakdown for the selected period')}</CardDescription>
              </CardHeader>
              <CardContent>
                <RevenueBarChart
                  data={revenueData?.revenueByDay ?? []}
                  isLoading={revenueLoading}
                />
              </CardContent>
            </Card>

            <div className="grid gap-6 grid-cols-1 lg:grid-cols-2">
              {/* Revenue by Category */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <CardTitle>{t('reports.revenueByCategory', 'Revenue by Category')}</CardTitle>
                  <CardDescription>{t('reports.revenueByCategoryDesc', 'How revenue is distributed across product categories')}</CardDescription>
                </CardHeader>
                <CardContent>
                  <CategoryBreakdownChart
                    data={revenueData?.revenueByCategory ?? []}
                    isLoading={revenueLoading}
                  />
                  {!revenueLoading && !revenueData?.revenueByCategory?.length && (
                    <p className="text-sm text-muted-foreground text-center py-8">
                      {t('labels.noData', 'No data available')}
                    </p>
                  )}
                </CardContent>
              </Card>

              {/* Revenue by Payment Method */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <CardTitle>{t('reports.revenueByPayment', 'Revenue by Payment Method')}</CardTitle>
                  <CardDescription>{t('reports.revenueByPaymentDesc', 'Breakdown of revenue by payment provider')}</CardDescription>
                </CardHeader>
                <CardContent>
                  {revenueLoading ? (
                    <div className="space-y-3">
                      {[...Array(4)].map((_, i) => (
                        <Skeleton key={i} className="h-8 w-full" />
                      ))}
                    </div>
                  ) : revenueData?.revenueByPaymentMethod?.length ? (
                    <div className="rounded-xl border border-border/50 overflow-hidden">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>{t('reports.paymentMethod', 'Payment Method')}</TableHead>
                            <TableHead className="text-right">{t('reports.revenue', 'Revenue')}</TableHead>
                            <TableHead className="text-right">{t('reports.transactions', 'Transactions')}</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {revenueData.revenueByPaymentMethod.map((pm, i) => (
                            <TableRow key={i}>
                              <TableCell className="font-medium">{pm.method}</TableCell>
                              <TableCell className="text-right">{formatCurrency(pm.revenue)}</TableCell>
                              <TableCell className="text-right">{pm.count}</TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground text-center py-8">
                      {t('labels.noData', 'No data available')}
                    </p>
                  )}
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          {/* ─── Best Sellers Tab ─────────────────────────────────────── */}
          <TabsContent value="bestSellers" className="mt-6">
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader>
                <CardTitle>{t('reports.topSellingProducts', 'Top Selling Products')}</CardTitle>
                <CardDescription>
                  {t('reports.topSellingDesc', 'Products ranked by units sold in the selected period')}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="rounded-xl border border-border/50 overflow-hidden">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-10">#</TableHead>
                        <TableHead>{t('labels.product', 'Product')}</TableHead>
                        <TableHead className="text-right">{t('reports.unitsSold', 'Units Sold')}</TableHead>
                        <TableHead className="text-right">{t('reports.revenue', 'Revenue')}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {bestSellersLoading ? (
                        <SkeletonRows columns={4} />
                      ) : bestSellersData?.products?.length ? (
                        bestSellersData.products.map((product, index) => (
                          <TableRow key={product.productId} className="transition-colors hover:bg-muted/50">
                            <TableCell>
                              <Badge variant="outline" className="w-7 h-7 justify-center rounded-full text-xs">
                                {index + 1}
                              </Badge>
                            </TableCell>
                            <TableCell>
                              <div className="flex items-center gap-3">
                                {product.imageUrl ? (
                                  <img
                                    src={product.imageUrl}
                                    alt={product.productName}
                                    className="h-10 w-10 rounded-lg object-cover border border-border/50"
                                  />
                                ) : (
                                  <div className="h-10 w-10 rounded-lg bg-muted flex items-center justify-center">
                                    <Package className="h-4 w-4 text-muted-foreground" />
                                  </div>
                                )}
                                <span className="font-medium text-sm">{product.productName}</span>
                              </div>
                            </TableCell>
                            <TableCell className="text-right font-medium">
                              {product.unitsSold.toLocaleString()}
                            </TableCell>
                            <TableCell className="text-right font-medium">
                              {formatCurrency(product.revenue)}
                            </TableCell>
                          </TableRow>
                        ))
                      ) : (
                        <TableRow>
                          <TableCell colSpan={4} className="text-center py-8 text-muted-foreground">
                            {t('labels.noData', 'No data available')}
                          </TableCell>
                        </TableRow>
                      )}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* ─── Inventory Tab ────────────────────────────────────────── */}
          <TabsContent value="inventory" className="space-y-6 mt-6">
            {/* Inventory Summary Cards */}
            <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
              {inventoryLoading ? (
                [...Array(4)].map((_, i) => (
                  <Card key={i} className="shadow-sm border-border/50">
                    <CardContent className="pt-6">
                      <Skeleton className="h-16 w-full" />
                    </CardContent>
                  </Card>
                ))
              ) : (
                <>
                  <MetricCard
                    title={t('reports.totalProducts', 'Total Products')}
                    value={(inventoryData?.totalProducts ?? 0).toLocaleString()}
                    icon={Package}
                    iconColor="text-blue-600"
                    iconBg="bg-blue-100 dark:bg-blue-900/30"
                  />
                  <MetricCard
                    title={t('reports.totalVariants', 'Total Variants')}
                    value={(inventoryData?.totalVariants ?? 0).toLocaleString()}
                    icon={Package}
                    iconColor="text-violet-600"
                    iconBg="bg-violet-100 dark:bg-violet-900/30"
                  />
                  <MetricCard
                    title={t('reports.stockValue', 'Stock Value')}
                    value={formatCurrency(inventoryData?.totalStockValue ?? 0)}
                    icon={DollarSign}
                    iconColor="text-emerald-600"
                    iconBg="bg-emerald-100 dark:bg-emerald-900/30"
                  />
                  <MetricCard
                    title={t('reports.turnoverRate', 'Turnover Rate')}
                    value={`${(inventoryData?.turnoverRate ?? 0).toFixed(2)}x`}
                    icon={TrendingUp}
                    iconColor="text-amber-600"
                    iconBg="bg-amber-100 dark:bg-amber-900/30"
                  />
                </>
              )}
            </div>

            {/* Low Stock Products Table */}
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <AlertTriangle className="h-5 w-5 text-amber-500" />
                  <div>
                    <CardTitle>{t('reports.lowStockProducts', 'Low Stock Products')}</CardTitle>
                    <CardDescription>
                      {t('reports.lowStockDesc', 'Products that need restocking attention')}
                    </CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="rounded-xl border border-border/50 overflow-hidden">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>{t('labels.product', 'Product')}</TableHead>
                        <TableHead>{t('labels.sku', 'SKU')}</TableHead>
                        <TableHead className="text-right">{t('reports.currentStock', 'Current Stock')}</TableHead>
                        <TableHead className="text-right">{t('reports.reorderLevel', 'Reorder Level')}</TableHead>
                        <TableHead>{t('labels.status', 'Status')}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {inventoryLoading ? (
                        <SkeletonRows columns={5} />
                      ) : inventoryData?.lowStockProducts?.length ? (
                        inventoryData.lowStockProducts.map((product) => (
                          <TableRow key={`${product.productId}-${product.variantSku}`} className="transition-colors hover:bg-muted/50">
                            <TableCell className="font-medium text-sm">{product.name}</TableCell>
                            <TableCell>
                              <span className="font-mono text-xs text-muted-foreground">
                                {product.variantSku ?? '-'}
                              </span>
                            </TableCell>
                            <TableCell className="text-right font-medium">
                              {product.currentStock}
                            </TableCell>
                            <TableCell className="text-right text-muted-foreground">
                              {product.reorderLevel}
                            </TableCell>
                            <TableCell>
                              {getStockStatusBadge(product.currentStock, product.reorderLevel)}
                            </TableCell>
                          </TableRow>
                        ))
                      ) : (
                        <TableRow>
                          <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                            {t('reports.noLowStock', 'All products are well stocked')}
                          </TableCell>
                        </TableRow>
                      )}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* ─── Customers Tab ────────────────────────────────────────── */}
          <TabsContent value="customers" className="space-y-6 mt-6">
            {/* Customer Summary Cards */}
            <div className="grid gap-4 grid-cols-1 sm:grid-cols-3">
              {customerLoading ? (
                [...Array(3)].map((_, i) => (
                  <Card key={i} className="shadow-sm border-border/50">
                    <CardContent className="pt-6">
                      <Skeleton className="h-16 w-full" />
                    </CardContent>
                  </Card>
                ))
              ) : (
                <>
                  <MetricCard
                    title={t('reports.newCustomers', 'New Customers')}
                    value={(customerData?.newCustomers ?? 0).toLocaleString()}
                    subtitle={t('reports.forSelectedPeriod', 'For selected period')}
                    icon={UserPlus}
                    iconColor="text-emerald-600"
                    iconBg="bg-emerald-100 dark:bg-emerald-900/30"
                  />
                  <MetricCard
                    title={t('reports.returningCustomers', 'Returning Customers')}
                    value={(customerData?.returningCustomers ?? 0).toLocaleString()}
                    icon={UserCheck}
                    iconColor="text-blue-600"
                    iconBg="bg-blue-100 dark:bg-blue-900/30"
                  />
                  <MetricCard
                    title={t('reports.churnRate', 'Churn Rate')}
                    value={`${((customerData?.churnRate ?? 0) * 100).toFixed(1)}%`}
                    icon={Users}
                    iconColor={(customerData?.churnRate ?? 0) > 0.1 ? 'text-red-600' : 'text-green-600'}
                    iconBg={(customerData?.churnRate ?? 0) > 0.1 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-green-100 dark:bg-green-900/30'}
                  />
                </>
              )}
            </div>

            <div className="grid gap-6 grid-cols-1 lg:grid-cols-2">
              {/* Monthly Acquisition */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <CardTitle>{t('reports.customerAcquisition', 'Customer Acquisition')}</CardTitle>
                  <CardDescription>{t('reports.customerAcquisitionDesc', 'Monthly new customer and revenue trends')}</CardDescription>
                </CardHeader>
                <CardContent>
                  {customerLoading ? (
                    <div className="space-y-3">
                      {[...Array(5)].map((_, i) => (
                        <Skeleton key={i} className="h-8 w-full" />
                      ))}
                    </div>
                  ) : customerData?.acquisitionByMonth?.length ? (
                    <div className="rounded-xl border border-border/50 overflow-hidden">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>{t('reports.month', 'Month')}</TableHead>
                            <TableHead className="text-right">{t('reports.newCustomers', 'New Customers')}</TableHead>
                            <TableHead className="text-right">{t('reports.revenue', 'Revenue')}</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {customerData.acquisitionByMonth.map((month) => (
                            <TableRow key={month.month}>
                              <TableCell className="font-medium">{month.month}</TableCell>
                              <TableCell className="text-right">{month.newCustomers}</TableCell>
                              <TableCell className="text-right">{formatCurrency(month.revenue)}</TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground text-center py-8">
                      {t('labels.noData', 'No data available')}
                    </p>
                  )}
                </CardContent>
              </Card>

              {/* Top Customers */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <CardTitle>{t('reports.topCustomers', 'Top Customers')}</CardTitle>
                  <CardDescription>{t('reports.topCustomersDesc', 'Customers ranked by total spending')}</CardDescription>
                </CardHeader>
                <CardContent>
                  {customerLoading ? (
                    <div className="space-y-3">
                      {[...Array(5)].map((_, i) => (
                        <Skeleton key={i} className="h-8 w-full" />
                      ))}
                    </div>
                  ) : customerData?.topCustomers?.length ? (
                    <div className="rounded-xl border border-border/50 overflow-hidden">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>{t('labels.name', 'Name')}</TableHead>
                            <TableHead className="text-right">{t('reports.orders', 'Orders')}</TableHead>
                            <TableHead className="text-right">{t('reports.totalSpent', 'Total Spent')}</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {customerData.topCustomers.map((customer, i) => (
                            <TableRow key={customer.customerId ?? i} className="transition-colors hover:bg-muted/50">
                              <TableCell className="font-medium">{customer.name}</TableCell>
                              <TableCell className="text-right">{customer.orderCount}</TableCell>
                              <TableCell className="text-right font-medium">
                                {formatCurrency(customer.totalSpent)}
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground text-center py-8">
                      {t('labels.noData', 'No data available')}
                    </p>
                  )}
                </CardContent>
              </Card>
            </div>
          </TabsContent>

      </Tabs>
    </div>
  )
}

export default ReportsPage
