import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useAuthContext } from '@/contexts/AuthContext'
import { usePageContext } from '@/hooks/usePageContext'
import { formatCurrency } from '@/lib/utils/currency'
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
  Clock,
  CheckCircle2,
  Truck,
  XCircle,
  Eye,
  ExternalLink,
  BookOpen,
  Cpu,
  User,
} from 'lucide-react'
import { useDashboardMetricsQuery } from '@/portal-app/dashboard/queries'
import { getOrderStatusIconColor } from '@/portal-app/orders/utils/orderStatus'

const MetricCard = ({
  title,
  value,
  subtitle,
  icon: Icon,
  trend,
  trendLabel,
  iconColor = 'text-primary',
  iconBg = 'bg-primary/10',
}: {
  title: string
  value: string
  subtitle?: string
  icon: typeof DollarSign
  trend?: 'up' | 'down' | 'neutral'
  trendLabel?: string
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
        <div className={`p-3 rounded-xl ${iconBg}`}>
          <Icon className={`h-5 w-5 ${iconColor}`} />
        </div>
      </div>
    </CardContent>
  </Card>
)

const OrderStatusCard = ({ title, count, icon: Icon, color }: { title: string; count: number; icon: typeof Clock; color: string }) => (
  <div className="flex items-center gap-3 p-3 rounded-lg border border-border/50 hover:bg-muted/30 transition-colors">
    <div className={`p-2 rounded-lg ${color}`}>
      <Icon className="h-4 w-4" />
    </div>
    <div className="flex-1">
      <p className="text-sm text-muted-foreground">{title}</p>
    </div>
    <span className="text-lg font-semibold">{count}</span>
  </div>
)

const MetricSkeleton = () => (
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

  // Calculate month-over-month trend
  const monthTrend = revenue && revenue.revenueLastMonth > 0
    ? ((revenue.revenueThisMonth - revenue.revenueLastMonth) / revenue.revenueLastMonth * 100).toFixed(1)
    : null

  const monthTrendDirection = monthTrend
    ? Number(monthTrend) > 0 ? 'up' as const : Number(monthTrend) < 0 ? 'down' as const : 'neutral' as const
    : undefined

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <PageHeader
        icon={LayoutDashboard}
        title={t('dashboard.title', 'Dashboard')}
        description={t('dashboard.welcome', { name: user?.fullName || 'User' })}
        responsive
      />

      {/* Revenue KPIs */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {isLoading ? (
          <>
            <MetricSkeleton />
            <MetricSkeleton />
            <MetricSkeleton />
            <MetricSkeleton />
          </>
        ) : (
          <>
            <MetricCard
              title={t('dashboard.totalRevenue', 'Total Revenue')}
              value={formatCurrency(revenue?.totalRevenue ?? 0)}
              subtitle={t('dashboard.allTime', 'All time')}
              icon={DollarSign}
              iconColor="text-green-600"
              iconBg="bg-green-100 dark:bg-green-900/30"
            />
            <MetricCard
              title={t('dashboard.revenueThisMonth', 'This Month')}
              value={formatCurrency(revenue?.revenueThisMonth ?? 0)}
              icon={TrendingUp}
              trend={monthTrendDirection}
              trendLabel={monthTrend ? `${Number(monthTrend) > 0 ? '+' : ''}${monthTrend}% ${t('dashboard.vsLastMonth', 'vs last month')}` : undefined}
              iconColor="text-blue-600"
              iconBg="bg-blue-100 dark:bg-blue-900/30"
            />
            <MetricCard
              title={t('dashboard.totalOrders', 'Total Orders')}
              value={(revenue?.totalOrders ?? 0).toLocaleString()}
              subtitle={`${revenue?.ordersThisMonth ?? 0} ${t('dashboard.thisMonth', 'this month')}`}
              icon={ShoppingCart}
              iconColor="text-purple-600"
              iconBg="bg-purple-100 dark:bg-purple-900/30"
            />
            <MetricCard
              title={t('dashboard.avgOrderValue', 'Avg Order Value')}
              value={formatCurrency(revenue?.averageOrderValue ?? 0)}
              icon={DollarSign}
              iconColor="text-amber-600"
              iconBg="bg-amber-100 dark:bg-amber-900/30"
            />
          </>
        )}
      </div>

      {/* Order Status Overview + Quick Links */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Order Status Breakdown */}
        <Card className="lg:col-span-2 shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
          <CardHeader>
            <CardTitle className="text-lg">{t('dashboard.ordersByStatus', 'Orders by Status')}</CardTitle>
            <CardDescription>{t('dashboard.ordersByStatusDescription', 'Current order fulfillment pipeline')}</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                {[...Array(6)].map((_, i) => (
                  <Skeleton key={i} className="h-16 w-full rounded-lg" />
                ))}
              </div>
            ) : orderCounts ? (
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                <OrderStatusCard title={t('orders.status.pending', 'Pending')} count={orderCounts.pending} icon={Clock} color={getOrderStatusIconColor('Pending')} />
                <OrderStatusCard title={t('orders.status.confirmed', 'Confirmed')} count={orderCounts.confirmed} icon={CheckCircle2} color={getOrderStatusIconColor('Confirmed')} />
                <OrderStatusCard title={t('orders.status.processing', 'Processing')} count={orderCounts.processing} icon={Package} color={getOrderStatusIconColor('Processing')} />
                <OrderStatusCard title={t('orders.status.shipped', 'Shipped')} count={orderCounts.shipped} icon={Truck} color={getOrderStatusIconColor('Shipped')} />
                <OrderStatusCard title={t('orders.status.completed', 'Completed')} count={orderCounts.completed} icon={CheckCircle2} color={getOrderStatusIconColor('Completed')} />
                <OrderStatusCard title={t('orders.status.cancelled', 'Cancelled')} count={orderCounts.cancelled} icon={XCircle} color={getOrderStatusIconColor('Cancelled')} />
              </div>
            ) : null}
          </CardContent>
        </Card>

        {/* Quick Links */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
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

      {/* Bottom Row: Recent Orders, Top Products, Low Stock */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Recent Orders */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-border/50">
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
                  <Skeleton key={i} className="h-12 w-full rounded-lg" />
                ))}
              </div>
            ) : metrics?.recentOrders.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-6">
                {t('dashboard.noRecentOrders', 'No recent orders')}
              </p>
            ) : (
              <div className="space-y-2">
                {metrics?.recentOrders.map((order) => (
                  <div
                    key={order.orderId}
                    className="flex items-center justify-between p-2.5 rounded-lg hover:bg-muted/50 transition-colors cursor-pointer"
                    onClick={() => navigate(`/portal/ecommerce/orders/${order.orderId}`)}
                  >
                    <div className="flex flex-col min-w-0 flex-1 mr-3">
                      <span className="font-mono text-sm font-medium truncate">{order.orderNumber}</span>
                      <span className="text-xs text-muted-foreground truncate">{order.customerEmail}</span>
                    </div>
                    <div className="flex flex-col items-end">
                      <span className="text-sm font-medium">{formatCurrency(order.grandTotal)}</span>
                      <Badge variant="outline" className="text-[10px] px-1.5 py-0">
                        {t(`orders.status.${order.status.toLowerCase()}`, order.status)}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

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
                    <span className="text-sm font-medium">{formatCurrency(product.totalRevenue)}</span>
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
