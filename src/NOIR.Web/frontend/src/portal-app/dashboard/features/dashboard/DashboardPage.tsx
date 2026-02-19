import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useAuthContext } from '@/contexts/AuthContext'
import { usePageContext } from '@/hooks/usePageContext'
import { formatCurrency, formatVndAbbreviated } from '@/lib/utils/currency'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Badge,
  Button,
  PageHeader,
  Skeleton,
} from '@uikit'
import {
  LayoutDashboard,
  DollarSign,
  ShoppingCart,
  TrendingUp,
  Package,
  AlertTriangle,
  ArrowUpRight,
  ArrowDownRight,
  Eye,
  ExternalLink,
  BookOpen,
  Cpu,
  User,
} from 'lucide-react'
import { useDashboardMetricsQuery } from '@/portal-app/dashboard/queries'

import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { Payload } from 'recharts/types/component/DefaultTooltipContent'

/* ──────────────── KPI Card ──────────────── */

const KpiCard = ({
  title,
  value,
  subtitle,
  icon: Icon,
  trend,
  trendLabel,
  iconColor = 'text-primary',
  iconBg = 'bg-primary/10',
  sparklineData,
  sparklineId,
}: {
  title: string
  value: string
  subtitle?: string
  icon: typeof DollarSign
  trend?: 'up' | 'down' | 'neutral'
  trendLabel?: string
  iconColor?: string
  iconBg?: string
  sparklineData?: { value: number }[]
  sparklineId?: string
}) => (
  <Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
    <CardContent className="pt-6">
      <div className="flex items-start justify-between">
        <div className="space-y-1.5 flex-1 min-w-0">
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          <p className="text-2xl font-bold font-mono tabular-nums">{value}</p>
          {subtitle && (
            <p className="text-xs text-muted-foreground">{subtitle}</p>
          )}
          {trendLabel && (
            <div className="flex items-center gap-1 text-xs">
              {trend === 'up' && <ArrowUpRight className="h-3 w-3 text-green-600" />}
              {trend === 'down' && <ArrowDownRight className="h-3 w-3 text-red-600" />}
              <span className={trend === 'up' ? 'text-green-600' : trend === 'down' ? 'text-red-600' : 'text-muted-foreground'}>
                {trendLabel}
              </span>
            </div>
          )}
        </div>
        <div className="flex flex-col items-end gap-2">
          <div className={`p-3 rounded-xl ${iconBg}`}>
            <Icon className={`h-5 w-5 ${iconColor}`} />
          </div>
          {sparklineData && sparklineData.length > 1 && sparklineId && (
            <div className="h-8 w-20">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={sparklineData} margin={{ top: 0, right: 0, left: 0, bottom: 0 }}>
                  <defs>
                    <linearGradient id={`sparkGrad-${sparklineId}`} x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor={trend === 'down' ? '#ef4444' : '#22c55e'} stopOpacity={0.3} />
                      <stop offset="100%" stopColor={trend === 'down' ? '#ef4444' : '#22c55e'} stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <Area
                    type="monotone"
                    dataKey="value"
                    stroke={trend === 'down' ? '#ef4444' : '#22c55e'}
                    strokeWidth={1.5}
                    fill={`url(#sparkGrad-${sparklineId})`}
                  />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          )}
        </div>
      </div>
    </CardContent>
  </Card>
)

/* ──────────────── Skeletons ──────────────── */

const KpiSkeleton = () => (
  <Card className="shadow-sm">
    <CardContent className="pt-6">
      <div className="flex items-start justify-between">
        <div className="space-y-3">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-8 w-32" />
          <Skeleton className="h-3 w-20" />
        </div>
        <Skeleton className="h-11 w-11 rounded-xl" />
      </div>
    </CardContent>
  </Card>
)

const ChartSkeleton = ({ className }: { className?: string }) => (
  <Card className={`shadow-sm ${className ?? ''}`}>
    <CardHeader>
      <Skeleton className="h-5 w-32" />
      <Skeleton className="h-4 w-48" />
    </CardHeader>
    <CardContent>
      <Skeleton className="h-48 w-full rounded-lg" />
    </CardContent>
  </Card>
)

/* ──────────────── Custom Chart Tooltip ──────────────── */

interface ChartTooltipProps {
  active?: boolean
  payload?: ReadonlyArray<Payload<number, string>>
  label?: string | number
}

const RevenueTooltip = ({ active, payload, label }: ChartTooltipProps) => {
  if (!active || !payload?.length) return null
  return (
    <div className="rounded-lg border bg-background p-3 shadow-md">
      <p className="text-xs text-muted-foreground mb-1">{label}</p>
      <p className="text-sm font-semibold font-mono tabular-nums">{formatCurrency(payload[0].value ?? 0)}</p>
    </div>
  )
}

const OrderBarTooltip = ({ active, payload, label }: ChartTooltipProps) => {
  if (!active || !payload?.length) return null
  return (
    <div className="rounded-lg border bg-background p-3 shadow-md">
      <p className="text-xs text-muted-foreground mb-1">{label}</p>
      <p className="text-sm font-semibold font-mono tabular-nums">{payload[0].value}</p>
    </div>
  )
}

/* ──────────────── Main Page ──────────────── */

export const DashboardPage = () => {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()
  const navigate = useNavigate()
  usePageContext('Dashboard')

  const { data: metrics, isLoading } = useDashboardMetricsQuery({
    topProducts: 5,
    lowStockThreshold: 10,
    recentOrders: 5,
    salesDays: 30,
  })

  const revenue = metrics?.revenue
  const orderCounts = metrics?.orderCounts

  // Month-over-month trend
  const monthTrend = revenue && revenue.revenueLastMonth > 0
    ? ((revenue.revenueThisMonth - revenue.revenueLastMonth) / revenue.revenueLastMonth * 100).toFixed(1)
    : null

  const monthTrendDirection = monthTrend
    ? Number(monthTrend) > 0 ? 'up' as const : Number(monthTrend) < 0 ? 'down' as const : 'neutral' as const
    : undefined

  // Sparkline data from salesOverTime
  const sparklineData = metrics?.salesOverTime?.map(d => ({ value: d.revenue })) ?? []

  // Chart data
  const revenueChartData = metrics?.salesOverTime?.map(d => ({
    date: new Date(d.date).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' }),
    revenue: d.revenue,
    orders: d.orderCount,
  })) ?? []

  // Order status bar chart data
  const orderStatusBarData = orderCounts ? [
    { name: t('orders.status.pending', 'Pending'), count: orderCounts.pending, fill: '#f59e0b' },
    { name: t('orders.status.confirmed', 'Confirmed'), count: orderCounts.confirmed, fill: '#3b82f6' },
    { name: t('orders.status.processing', 'Processing'), count: orderCounts.processing, fill: '#6366f1' },
    { name: t('orders.status.shipped', 'Shipped'), count: orderCounts.shipped, fill: '#8b5cf6' },
    { name: t('orders.status.delivered', 'Delivered'), count: orderCounts.delivered, fill: '#10b981' },
    { name: t('orders.status.completed', 'Completed'), count: orderCounts.completed, fill: '#22c55e' },
    { name: t('orders.status.cancelled', 'Cancelled'), count: orderCounts.cancelled, fill: '#ef4444' },
    { name: t('orders.status.returned', 'Returned'), count: orderCounts.returned, fill: '#f43f5e' },
    { name: t('orders.status.refunded', 'Refunded'), count: orderCounts.refunded, fill: '#f97316' },
  ].filter(d => d.count > 0) : []

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <PageHeader
        icon={LayoutDashboard}
        title={t('dashboard.title', 'Dashboard')}
        description={t('dashboard.welcome', { name: user?.fullName || 'User' })}
        responsive
      />

      {/* ── Row 1: KPI Cards ── */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {isLoading ? (
          <>
            <KpiSkeleton />
            <KpiSkeleton />
            <KpiSkeleton />
            <KpiSkeleton />
          </>
        ) : (
          <>
            <KpiCard
              title={t('dashboard.totalRevenue', 'Total Revenue')}
              value={formatVndAbbreviated(revenue?.totalRevenue ?? 0)}
              subtitle={t('dashboard.allTime', 'All time')}
              icon={DollarSign}
              iconColor="text-green-600"
              iconBg="bg-green-100 dark:bg-green-900/30"
              sparklineData={sparklineData}
              sparklineId="totalRevenue"
              trend={monthTrendDirection}
            />
            <KpiCard
              title={t('dashboard.revenueThisMonth', 'This Month')}
              value={formatVndAbbreviated(revenue?.revenueThisMonth ?? 0)}
              icon={TrendingUp}
              trend={monthTrendDirection}
              trendLabel={monthTrend ? `${Number(monthTrend) > 0 ? '+' : ''}${monthTrend}% ${t('dashboard.vsLastMonth', 'vs last month')}` : undefined}
              iconColor="text-blue-600"
              iconBg="bg-blue-100 dark:bg-blue-900/30"
            />
            <KpiCard
              title={t('dashboard.totalOrders', 'Total Orders')}
              value={(revenue?.totalOrders ?? 0).toLocaleString()}
              subtitle={`${revenue?.ordersThisMonth ?? 0} ${t('dashboard.thisMonth', 'this month')}`}
              icon={ShoppingCart}
              iconColor="text-purple-600"
              iconBg="bg-purple-100 dark:bg-purple-900/30"
            />
            <KpiCard
              title={t('dashboard.avgOrderValue', 'Avg Order Value')}
              value={formatVndAbbreviated(revenue?.averageOrderValue ?? 0)}
              icon={DollarSign}
              iconColor="text-amber-600"
              iconBg="bg-amber-100 dark:bg-amber-900/30"
            />
          </>
        )}
      </div>

      {/* ── Row 2: Revenue Area Chart (60%) + Order Status Bar Chart (40%) ── */}
      <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">
        {isLoading ? (
          <>
            <ChartSkeleton className="lg:col-span-3" />
            <ChartSkeleton className="lg:col-span-2" />
          </>
        ) : (
          <>
            {/* Revenue Area Chart */}
            <Card className="lg:col-span-3 shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
              <CardHeader>
                <CardTitle className="text-lg">{t('dashboard.revenueOverTime', 'Revenue Over Time')}</CardTitle>
                <CardDescription>{t('dashboard.last30Days', 'Last 30 days')}</CardDescription>
              </CardHeader>
              <CardContent>
                {revenueChartData.length > 0 ? (
                  <div className="h-64">
                    <ResponsiveContainer width="100%" height="100%">
                      <AreaChart data={revenueChartData} margin={{ top: 5, right: 10, left: 10, bottom: 0 }}>
                        <defs>
                          <linearGradient id="revenueGradient" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stopColor="hsl(var(--primary))" stopOpacity={0.3} />
                            <stop offset="100%" stopColor="hsl(var(--primary))" stopOpacity={0.02} />
                          </linearGradient>
                        </defs>
                        <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                        <XAxis
                          dataKey="date"
                          tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
                          tickLine={false}
                          axisLine={false}
                          interval="preserveStartEnd"
                        />
                        <YAxis
                          tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
                          tickLine={false}
                          axisLine={false}
                          tickFormatter={(v: number) => formatVndAbbreviated(v)}
                        />
                        <Tooltip content={<RevenueTooltip />} />
                        <Area
                          type="monotone"
                          dataKey="revenue"
                          stroke="hsl(var(--primary))"
                          strokeWidth={2}
                          fill="url(#revenueGradient)"
                        />
                      </AreaChart>
                    </ResponsiveContainer>
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground text-center py-16">{t('dashboard.noSalesData', 'No sales data available')}</p>
                )}
              </CardContent>
            </Card>

            {/* Order Status Bar Chart */}
            <Card className="lg:col-span-2 shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
              <CardHeader>
                <CardTitle className="text-lg">{t('dashboard.ordersByStatus', 'Orders by Status')}</CardTitle>
                <CardDescription>{t('dashboard.ordersByStatusDescription', 'Current order fulfillment pipeline')}</CardDescription>
              </CardHeader>
              <CardContent>
                {orderStatusBarData.length > 0 ? (
                  <div className="h-64">
                    <ResponsiveContainer width="100%" height="100%">
                      <BarChart data={orderStatusBarData} layout="vertical" margin={{ top: 0, right: 10, left: 0, bottom: 0 }}>
                        <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="hsl(var(--border))" />
                        <XAxis
                          type="number"
                          tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
                          tickLine={false}
                          axisLine={false}
                          allowDecimals={false}
                        />
                        <YAxis
                          type="category"
                          dataKey="name"
                          tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
                          tickLine={false}
                          axisLine={false}
                          width={80}
                        />
                        <Tooltip content={<OrderBarTooltip />} />
                        <Bar dataKey="count" radius={[0, 4, 4, 0]} maxBarSize={24} />
                      </BarChart>
                    </ResponsiveContainer>
                  </div>
                ) : (
                  <div className="flex items-center justify-center py-16">
                    <p className="text-sm text-muted-foreground">{t('dashboard.noOrdersYet', 'No orders yet')}</p>
                  </div>
                )}
              </CardContent>
            </Card>
          </>
        )}
      </div>

      {/* ── Row 3: Recent Orders (60%) + Quick Links / Activity (40%) ── */}
      <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">
        {/* Recent Orders */}
        <Card className="lg:col-span-3 shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg">{t('dashboard.recentOrders', 'Recent Orders')}</CardTitle>
                <CardDescription>{t('dashboard.latestOrderActivity', 'Latest order activity')}</CardDescription>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="cursor-pointer text-sm text-primary hover:underline flex items-center gap-1 h-auto p-0"
                onClick={() => navigate('/portal/ecommerce/orders')}
              >
                {t('dashboard.viewAll', 'View all')}
                <Eye className="h-3 w-3" />
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="space-y-3">
                {[...Array(5)].map((_, i) => (
                  <Skeleton key={i} className="h-14 w-full rounded-lg" />
                ))}
              </div>
            ) : metrics?.recentOrders.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-6">
                {t('dashboard.noRecentOrders', 'No recent orders')}
              </p>
            ) : (
              <div className="rounded-lg border border-border/50 overflow-hidden">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/40">
                      <th className="text-left font-medium text-muted-foreground px-4 py-2.5">{t('orders.orderNumber', 'Order #')}</th>
                      <th className="text-left font-medium text-muted-foreground px-4 py-2.5">{t('orders.customer', 'Customer')}</th>
                      <th className="text-right font-medium text-muted-foreground px-4 py-2.5">{t('orders.total', 'Total')}</th>
                      <th className="text-center font-medium text-muted-foreground px-4 py-2.5">{t('labels.status', 'Status')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {metrics?.recentOrders.map((order) => (
                      <tr
                        key={order.orderId}
                        className="border-b last:border-0 hover:bg-muted/30 transition-colors cursor-pointer"
                        onClick={() => navigate(`/portal/ecommerce/orders/${order.orderId}`)}
                      >
                        <td className="px-4 py-3 font-mono text-sm font-medium">{order.orderNumber}</td>
                        <td className="px-4 py-3 text-muted-foreground truncate max-w-[180px]">{order.customerEmail}</td>
                        <td className="px-4 py-3 text-right font-mono font-medium tabular-nums">{formatCurrency(order.grandTotal)}</td>
                        <td className="px-4 py-3 text-center">
                          <Badge variant="outline" className="text-[10px] px-1.5 py-0">
                            {t(`orders.status.${order.status.toLowerCase()}`, order.status)}
                          </Badge>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Quick Links */}
        <Card className="lg:col-span-2 shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
          <CardHeader>
            <CardTitle className="text-lg">{t('dashboard.quickLinks', 'Quick Links')}</CardTitle>
            <CardDescription>{t('dashboard.quickLinksDescription', 'Useful tools and resources')}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <a
              href="/api/docs"
              target="_blank"
              rel="noopener noreferrer"
              className="group flex items-center justify-between rounded-lg border border-border p-3 hover:bg-accent hover:border-blue-600/30 transition-colors cursor-pointer"
            >
              <div className="flex items-center gap-3">
                <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-blue-600/10">
                  <BookOpen className="h-4 w-4 text-blue-600" />
                </div>
                <span className="text-sm font-medium text-foreground group-hover:text-blue-600 transition-colors">{t('dashboard.apiDocs', 'API Documentation')}</span>
              </div>
              <ExternalLink className="h-4 w-4 text-muted-foreground group-hover:text-blue-600 transition-colors" />
            </a>
            <a
              href="/hangfire"
              target="_blank"
              rel="noopener noreferrer"
              className="group flex items-center justify-between rounded-lg border border-border p-3 hover:bg-accent hover:border-cyan-600/30 transition-colors cursor-pointer"
            >
              <div className="flex items-center gap-3">
                <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-cyan-600/10">
                  <Cpu className="h-4 w-4 text-cyan-600" />
                </div>
                <span className="text-sm font-medium text-foreground group-hover:text-cyan-600 transition-colors">{t('dashboard.hangfire', 'Background Jobs')}</span>
              </div>
              <ExternalLink className="h-4 w-4 text-muted-foreground group-hover:text-cyan-600 transition-colors" />
            </a>
            <div className="rounded-lg border border-border p-3">
              <div className="flex items-center gap-3 mb-3">
                <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-teal-600/10">
                  <User className="h-4 w-4 text-teal-600" />
                </div>
                <p className="text-sm font-medium text-foreground">{t('dashboard.yourProfile', 'Your Profile')}</p>
              </div>
              <div className="space-y-1 text-sm pl-11">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('profile.email', 'Email')}:</span>
                  <span className="font-medium text-foreground">{user?.email}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('profile.tenant', 'Tenant')}:</span>
                  <span className="font-medium text-foreground">{user?.tenantId ?? t('profile.platform', 'Platform')}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('profile.roles', 'Roles')}:</span>
                  <span className="font-medium text-foreground">{user?.roles?.join(', ') || 'None'}</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* ── Row 4: Top Products (50%) + Low Stock Alerts (50%) ── */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Top Selling Products */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg">{t('dashboard.topProducts', 'Top Products')}</CardTitle>
                <CardDescription>{t('dashboard.bestSellingItems', 'Best selling items')}</CardDescription>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="cursor-pointer text-sm text-primary hover:underline flex items-center gap-1 h-auto p-0"
                onClick={() => navigate('/portal/ecommerce/products')}
              >
                {t('dashboard.viewAll', 'View all')}
                <Eye className="h-3 w-3" />
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="space-y-3">
                {[...Array(5)].map((_, i) => (
                  <Skeleton key={i} className="h-12 w-full rounded-lg" />
                ))}
              </div>
            ) : metrics?.topSellingProducts.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-6">
                {t('dashboard.noTopProducts', 'No sales data yet')}
              </p>
            ) : (
              <div className="space-y-2">
                {metrics?.topSellingProducts.map((product, index) => (
                  <div
                    key={product.productId}
                    className="flex items-center gap-3 p-2.5 rounded-lg hover:bg-muted/50 transition-colors"
                  >
                    <div className="flex items-center justify-center w-7 h-7 rounded-full bg-muted text-xs font-bold text-muted-foreground">
                      {index + 1}
                    </div>
                    {product.imageUrl ? (
                      <img src={product.imageUrl} alt={product.productName} className="h-8 w-8 rounded-md object-cover" />
                    ) : (
                      <div className="h-8 w-8 rounded-md bg-muted flex items-center justify-center">
                        <Package className="h-4 w-4 text-muted-foreground" />
                      </div>
                    )}
                    <div className="flex flex-col min-w-0 flex-1">
                      <span className="text-sm font-medium truncate">{product.productName}</span>
                      <span className="text-xs text-muted-foreground">{product.totalQuantitySold} {t('dashboard.sold', 'sold')}</span>
                    </div>
                    <span className="text-sm font-medium font-mono tabular-nums">{formatVndAbbreviated(product.totalRevenue)}</span>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Low Stock Products */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg flex items-center gap-2">
                  <AlertTriangle className="h-4 w-4 text-amber-500" />
                  {t('dashboard.lowStock', 'Low Stock')}
                </CardTitle>
                <CardDescription>{t('dashboard.lowStockDescription', 'Products needing replenishment')}</CardDescription>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="cursor-pointer text-sm text-primary hover:underline flex items-center gap-1 h-auto p-0"
                onClick={() => navigate('/portal/ecommerce/inventory')}
              >
                {t('dashboard.viewAll', 'View all')}
                <Eye className="h-3 w-3" />
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="space-y-3">
                {[...Array(5)].map((_, i) => (
                  <Skeleton key={i} className="h-12 w-full rounded-lg" />
                ))}
              </div>
            ) : metrics?.lowStockProducts.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-6">
                {t('dashboard.noLowStock', 'All products are well stocked')}
              </p>
            ) : (
              <div className="space-y-2">
                {metrics?.lowStockProducts.map((product) => (
                  <div
                    key={`${product.productId}-${product.variantId}`}
                    className="flex items-center justify-between p-2.5 rounded-lg hover:bg-muted/50 transition-colors"
                  >
                    <div className="flex flex-col min-w-0 flex-1 mr-3">
                      <span className="text-sm font-medium truncate">{product.productName}</span>
                      <span className="text-xs text-muted-foreground truncate">
                        {product.variantName}
                        {product.sku && ` (${product.sku})`}
                      </span>
                    </div>
                    <Badge
                      variant={product.stockQuantity === 0 ? 'destructive' : 'secondary'}
                      className={product.stockQuantity === 0
                        ? ''
                        : 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400'
                      }
                    >
                      {product.stockQuantity} {t('dashboard.left', 'left')}
                    </Badge>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

export default DashboardPage
