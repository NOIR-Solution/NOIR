/**
 * Dashboard Page
 *
 * Rich dashboard with KPI cards, charts, recent orders, top sellers,
 * and low stock alerts. Data sourced from GET /api/dashboard/metrics.
 */
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
} from 'recharts'
import {
  AlertTriangle,
  ArrowRight,
  DollarSign,
  LayoutDashboard,
  Package,
  RefreshCw,
  ShoppingCart,
  TrendingDown,
  TrendingUp,
} from 'lucide-react'
import { useAuthContext } from '@/contexts/AuthContext'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { usePageContext } from '@/hooks/usePageContext'
import { useDashboardMetrics } from '@/hooks/useDashboard'
import { formatCurrency } from '@/lib/utils/currency'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { getOrderStatusColor } from '@/portal-app/orders/utils/orderStatus'
import type { OrderStatus } from '@/types/order'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  FilePreviewTrigger,
  PageHeader,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'

// ─── Constants ────────────────────────────────────────────────────────────

const ORDER_STATUS_CHART_COLORS: Record<string, string> = {
  pending: '#f59e0b',
  confirmed: '#3b82f6',
  processing: '#6366f1',
  shipped: '#a855f7',
  delivered: '#10b981',
  completed: '#22c55e',
  cancelled: '#ef4444',
  refunded: '#f97316',
  returned: '#f43f5e',
}

// ─── Helper Components ────────────────────────────────────────────────────

const MetricCard = ({
  title,
  value,
  icon: Icon,
  trend,
  trendValue,
  trendLabel,
  iconColor = 'text-primary',
  iconBg = 'bg-primary/10',
  isLoading,
}: {
  title: string
  value: string
  icon: typeof DollarSign
  trend?: 'up' | 'down' | 'neutral'
  trendValue?: string
  trendLabel?: string
  iconColor?: string
  iconBg?: string
  isLoading?: boolean
}) => (
  <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
    <CardContent className="pt-6">
      {isLoading ? (
        <div className="space-y-3">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-8 w-32" />
          <Skeleton className="h-3 w-20" />
        </div>
      ) : (
        <div className="flex items-start justify-between">
          <div className="space-y-2">
            <p className="text-sm font-medium text-muted-foreground">{title}</p>
            <p className="text-2xl font-bold">{value}</p>
            {trendValue && (
              <div className="flex items-center gap-1 text-xs">
                {trend === 'up' && <TrendingUp className="h-3 w-3 text-green-600" />}
                {trend === 'down' && <TrendingDown className="h-3 w-3 text-red-600" />}
                <span className={trend === 'up' ? 'text-green-600' : trend === 'down' ? 'text-red-600' : 'text-muted-foreground'}>
                  {trendValue}
                </span>
                {trendLabel && (
                  <span className="text-muted-foreground">{trendLabel}</span>
                )}
              </div>
            )}
          </div>
          <div className={`p-3 rounded-xl ${iconBg}`}>
            <Icon className={`h-5 w-5 ${iconColor}`} />
          </div>
        </div>
      )}
    </CardContent>
  </Card>
)

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

// ─── Main Page ────────────────────────────────────────────────────────────

export const DashboardPage = () => {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()
  const { formatDateTime } = useRegionalSettings()
  const navigate = useNavigate()
  usePageContext('Dashboard')

  const { data, isLoading, error, refetch } = useDashboardMetrics({
    topProducts: 5,
    lowStockThreshold: 10,
    recentOrders: 10,
    salesDays: 30,
  })

  // ─── Derived Values ──────────────────────────────────────────────────

  const monthOverMonthRevenue = useMemo(() => {
    if (!data?.revenue) return { percent: 0, trend: 'neutral' as const }
    const { revenueThisMonth, revenueLastMonth } = data.revenue
    if (revenueLastMonth === 0) return { percent: revenueThisMonth > 0 ? 100 : 0, trend: revenueThisMonth > 0 ? 'up' as const : 'neutral' as const }
    const percent = ((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100
    return { percent, trend: percent > 0 ? 'up' as const : percent < 0 ? 'down' as const : 'neutral' as const }
  }, [data?.revenue])

  const orderStatusChartData = useMemo(() => {
    if (!data?.orderCounts) return []
    return Object.entries(data.orderCounts)
      .filter(([, count]) => count > 0)
      .map(([status, count]) => ({
        name: t(`orders.status.${status}`, status),
        value: count,
        key: status,
      }))
  }, [data?.orderCounts, t])

  const salesChartData = useMemo(() => {
    if (!data?.salesOverTime) return []
    return data.salesOverTime.map((d) => ({
      ...d,
      dateLabel: new Date(d.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
    }))
  }, [data?.salesOverTime])

  // ─── Error State ─────────────────────────────────────────────────────

  if (error && !data) {
    return (
      <div className="container max-w-7xl py-6 space-y-6">
        <PageHeader
          icon={LayoutDashboard}
          title={t('dashboard.title', 'Dashboard')}
          description={t('dashboard.welcome', { name: user?.fullName || t('labels.user', { defaultValue: 'User' }) })}
          responsive
        />
        <Card className="shadow-sm">
          <CardContent className="pt-6">
            <EmptyState
              icon={AlertTriangle}
              title={t('dashboard.loadError', 'Failed to load dashboard')}
              description={t('dashboard.loadErrorDescription', 'There was an error loading the dashboard data. Please try again.')}
            />
            <div className="flex justify-center mt-4">
              <Button
                variant="outline"
                onClick={() => refetch()}
                className="cursor-pointer"
              >
                <RefreshCw className="h-4 w-4 mr-2" />
                {t('buttons.retry', 'Retry')}
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  // ─── Render ──────────────────────────────────────────────────────────

  return (
    <div className="container max-w-7xl py-6 space-y-6">
      <PageHeader
        icon={LayoutDashboard}
        title={t('dashboard.title', 'Dashboard')}
        description={t('dashboard.welcome', { name: user?.fullName || t('labels.user', { defaultValue: 'User' }) })}
        responsive
      />

      {/* ─── Row 1: KPI Metric Cards ──────────────────────────────────── */}
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
        <MetricCard
          title={t('dashboard.totalRevenue', 'Total Revenue')}
          value={formatCurrency(data?.revenue.totalRevenue ?? 0)}
          icon={DollarSign}
          trend={monthOverMonthRevenue.trend}
          trendValue={`${monthOverMonthRevenue.percent >= 0 ? '+' : ''}${monthOverMonthRevenue.percent.toFixed(1)}%`}
          trendLabel={t('dashboard.vsLastMonth', 'vs last month')}
          iconColor="text-emerald-600"
          iconBg="bg-emerald-100 dark:bg-emerald-900/30"
          isLoading={isLoading}
        />
        <MetricCard
          title={t('dashboard.ordersThisMonth', 'Orders This Month')}
          value={(data?.revenue.ordersThisMonth ?? 0).toLocaleString()}
          icon={ShoppingCart}
          iconColor="text-blue-600"
          iconBg="bg-blue-100 dark:bg-blue-900/30"
          isLoading={isLoading}
        />
        <MetricCard
          title={t('dashboard.averageOrderValue', 'Average Order Value')}
          value={formatCurrency(data?.revenue.averageOrderValue ?? 0)}
          icon={TrendingUp}
          iconColor="text-violet-600"
          iconBg="bg-violet-100 dark:bg-violet-900/30"
          isLoading={isLoading}
        />
        <MetricCard
          title={t('dashboard.ordersToday', 'Orders Today')}
          value={(data?.revenue.ordersToday ?? 0).toLocaleString()}
          icon={Package}
          iconColor="text-amber-600"
          iconBg="bg-amber-100 dark:bg-amber-900/30"
          isLoading={isLoading}
        />
      </div>

      {/* ─── Row 2: Charts ────────────────────────────────────────────── */}
      <div className="grid gap-6 grid-cols-1 lg:grid-cols-2">
        {/* Sales Over Time */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardHeader>
            <CardTitle className="text-lg">{t('dashboard.salesOverTime', 'Sales Over Time')}</CardTitle>
            <CardDescription>{t('dashboard.salesOverTimeDesc', 'Revenue and order trends for the last 30 days')}</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : salesChartData.length > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <AreaChart data={salesChartData} margin={{ top: 5, right: 10, left: 0, bottom: 0 }}>
                  <defs>
                    <linearGradient id="revenueGradient" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                  <XAxis
                    dataKey="dateLabel"
                    tick={{ fontSize: 11 }}
                    className="text-muted-foreground"
                    interval="preserveStartEnd"
                  />
                  <YAxis
                    tick={{ fontSize: 11 }}
                    className="text-muted-foreground"
                    tickFormatter={(v: number) => v >= 1_000_000 ? `${(v / 1_000_000).toFixed(0)}M` : v >= 1_000 ? `${(v / 1_000).toFixed(0)}K` : v.toString()}
                  />
                  <RechartsTooltip
                    contentStyle={{
                      borderRadius: '8px',
                      border: '1px solid hsl(var(--border))',
                      backgroundColor: 'hsl(var(--card))',
                      color: 'hsl(var(--card-foreground))',
                    }}
                    formatter={(value, name) => [
                      name === 'revenue' ? formatCurrency(Number(value ?? 0)) : Number(value ?? 0).toLocaleString(),
                      name === 'revenue' ? t('dashboard.revenue', 'Revenue') : t('dashboard.orders', 'Orders'),
                    ]}
                    labelFormatter={(label) => String(label)}
                  />
                  <Area
                    type="monotone"
                    dataKey="revenue"
                    stroke="hsl(var(--primary))"
                    fill="url(#revenueGradient)"
                    strokeWidth={2}
                    dot={false}
                    activeDot={{ r: 4 }}
                  />
                </AreaChart>
              </ResponsiveContainer>
            ) : (
              <EmptyState
                icon={TrendingUp}
                title={t('dashboard.noSalesData', 'No sales data')}
                description={t('dashboard.noSalesDataDesc', 'Sales data will appear here once orders are placed.')}
                className="border-0 rounded-none px-4 py-12"
              />
            )}
          </CardContent>
        </Card>

        {/* Order Status Distribution */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardHeader>
            <CardTitle className="text-lg">{t('dashboard.orderStatusDistribution', 'Order Status Distribution')}</CardTitle>
            <CardDescription>{t('dashboard.orderStatusDesc', 'Breakdown of orders by current status')}</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : orderStatusChartData.length > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <PieChart>
                  <Pie
                    data={orderStatusChartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={100}
                    paddingAngle={2}
                    dataKey="value"
                    nameKey="name"
                  >
                    {orderStatusChartData.map((entry) => (
                      <Cell
                        key={entry.key}
                        fill={ORDER_STATUS_CHART_COLORS[entry.key] ?? '#94a3b8'}
                      />
                    ))}
                  </Pie>
                  <RechartsTooltip
                    contentStyle={{
                      borderRadius: '8px',
                      border: '1px solid hsl(var(--border))',
                      backgroundColor: 'hsl(var(--card))',
                      color: 'hsl(var(--card-foreground))',
                    }}
                    formatter={(value) => [Number(value ?? 0).toLocaleString(), t('dashboard.orders', 'Orders')]}
                  />
                  <Legend
                    verticalAlign="bottom"
                    height={36}
                    formatter={(value: string) => <span className="text-xs text-muted-foreground">{value}</span>}
                  />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <EmptyState
                icon={ShoppingCart}
                title={t('dashboard.noOrderData', 'No order data')}
                description={t('dashboard.noOrderDataDesc', 'Order status data will appear here once orders are created.')}
                className="border-0 rounded-none px-4 py-12"
              />
            )}
          </CardContent>
        </Card>
      </div>

      {/* ─── Row 3: Tables ────────────────────────────────────────────── */}
      <div className="grid gap-6 grid-cols-1 lg:grid-cols-2">
        {/* Recent Orders */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
            <div>
              <CardTitle className="text-lg">{t('dashboard.recentOrders', 'Recent Orders')}</CardTitle>
              <CardDescription>{t('dashboard.recentOrdersDesc', 'Latest orders across all channels')}</CardDescription>
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="cursor-pointer text-muted-foreground hover:text-foreground"
              onClick={() => navigate('/portal/ecommerce/orders')}
            >
              {t('dashboard.viewAll', 'View All')}
              <ArrowRight className="h-4 w-4 ml-1" />
            </Button>
          </CardHeader>
          <CardContent>
            <div className="rounded-xl border border-border/50 overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('orders.orderNumber', 'Order #')}</TableHead>
                    <TableHead>{t('orders.customer', 'Customer')}</TableHead>
                    <TableHead className="text-right">{t('orders.grandTotal', 'Grand Total')}</TableHead>
                    <TableHead>{t('labels.status', 'Status')}</TableHead>
                    <TableHead>{t('labels.date', 'Date')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading ? (
                    <SkeletonRows columns={5} />
                  ) : data?.recentOrders?.length ? (
                    data.recentOrders.map((order) => (
                      <TableRow
                        key={order.orderId}
                        className="transition-colors hover:bg-muted/50 cursor-pointer"
                        onClick={() => navigate(`/portal/ecommerce/orders/${order.orderId}`)}
                      >
                        <TableCell className="font-medium font-mono text-sm">
                          {order.orderNumber}
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground truncate max-w-[150px]">
                          {order.customerEmail}
                        </TableCell>
                        <TableCell className="text-right font-medium text-sm">
                          {formatCurrency(order.grandTotal)}
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className={getOrderStatusColor(order.status as OrderStatus)}>
                            {t(`orders.status.${order.status.toLowerCase()}`, order.status)}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground whitespace-nowrap">
                          {formatDateTime(order.createdAt)}
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={5}>
                        <EmptyState
                          icon={ShoppingCart}
                          title={t('dashboard.noRecentOrders', 'No recent orders')}
                          size="sm"
                        />
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>

        {/* Top Selling Products */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
            <div>
              <CardTitle className="text-lg">{t('dashboard.topSellingProducts', 'Top Selling Products')}</CardTitle>
              <CardDescription>{t('dashboard.topSellingDesc', 'Best performing products by units sold')}</CardDescription>
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="cursor-pointer text-muted-foreground hover:text-foreground"
              onClick={() => navigate('/portal/ecommerce/products')}
            >
              {t('dashboard.viewAll', 'View All')}
              <ArrowRight className="h-4 w-4 ml-1" />
            </Button>
          </CardHeader>
          <CardContent>
            <div className="rounded-xl border border-border/50 overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('dashboard.productName', 'Product Name')}</TableHead>
                    <TableHead className="text-right">{t('dashboard.quantitySold', 'Qty Sold')}</TableHead>
                    <TableHead className="text-right">{t('dashboard.revenue', 'Revenue')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading ? (
                    <SkeletonRows columns={3} />
                  ) : data?.topSellingProducts?.length ? (
                    data.topSellingProducts.map((product) => (
                      <TableRow
                        key={product.productId}
                        className="transition-colors hover:bg-muted/50 cursor-pointer"
                        onClick={() => navigate(`/portal/ecommerce/products/${product.productId}`)}
                      >
                        <TableCell>
                          <div className="flex items-center gap-3">
                            {product.imageUrl ? (
                              <FilePreviewTrigger
                                file={{
                                  url: product.imageUrl,
                                  name: product.productName,
                                }}
                                thumbnailWidth={40}
                                thumbnailHeight={40}
                                className="rounded-lg"
                              />
                            ) : (
                              <div className="w-10 h-10 rounded-lg bg-muted flex items-center justify-center">
                                <Package className="h-4 w-4 text-muted-foreground" />
                              </div>
                            )}
                            <span className="font-medium text-sm">{product.productName}</span>
                          </div>
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {product.totalQuantitySold.toLocaleString()}
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {formatCurrency(product.totalRevenue)}
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={3}>
                        <EmptyState
                          icon={Package}
                          title={t('dashboard.noTopProducts', 'No sales data yet')}
                          size="sm"
                        />
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* ─── Row 4: Low Stock Alerts ──────────────────────────────────── */}
      {!isLoading && data?.lowStockProducts && data.lowStockProducts.length > 0 && (
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-amber-200 dark:border-amber-800/50">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
            <div className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-amber-500" />
              <div>
                <CardTitle className="text-lg">{t('dashboard.lowStockAlerts', 'Low Stock Alerts')}</CardTitle>
                <CardDescription>
                  {t('dashboard.lowStockDesc', '{{count}} products below stock threshold', { count: data.lowStockProducts.length })}
                </CardDescription>
              </div>
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="cursor-pointer text-muted-foreground hover:text-foreground"
              onClick={() => navigate('/portal/analytics/reports?tab=inventory')}
            >
              {t('dashboard.viewAll', 'View All')}
              <ArrowRight className="h-4 w-4 ml-1" />
            </Button>
          </CardHeader>
          <CardContent>
            <div className="rounded-xl border border-border/50 overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('dashboard.productName', 'Product Name')}</TableHead>
                    <TableHead>{t('labels.variant', 'Variant')}</TableHead>
                    <TableHead>{t('labels.sku', 'SKU')}</TableHead>
                    <TableHead className="text-right">{t('dashboard.currentStock', 'Current Stock')}</TableHead>
                    <TableHead className="text-right">{t('dashboard.threshold', 'Threshold')}</TableHead>
                    <TableHead>{t('labels.status', 'Status')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.lowStockProducts.map((product) => (
                    <TableRow
                      key={`${product.productId}-${product.variantId}`}
                      className="transition-colors hover:bg-muted/50 cursor-pointer"
                      onClick={() => navigate(`/portal/ecommerce/products/${product.productId}`)}
                    >
                      <TableCell className="font-medium text-sm">{product.productName}</TableCell>
                      <TableCell className="text-sm text-muted-foreground">{product.variantName}</TableCell>
                      <TableCell>
                        <span className="font-mono text-xs text-muted-foreground">{product.sku}</span>
                      </TableCell>
                      <TableCell className="text-right font-medium">
                        {product.stockQuantity}
                      </TableCell>
                      <TableCell className="text-right text-muted-foreground">
                        {product.lowStockThreshold}
                      </TableCell>
                      <TableCell>
                        {product.stockQuantity === 0 ? (
                          <Badge variant="outline" className={getStatusBadgeClasses('red')}>
                            {t('products.outOfStock', 'Out of Stock')}
                          </Badge>
                        ) : (
                          <Badge variant="outline" className={getStatusBadgeClasses('yellow')}>
                            {t('products.lowStock', 'Low Stock')}
                          </Badge>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

export default DashboardPage
