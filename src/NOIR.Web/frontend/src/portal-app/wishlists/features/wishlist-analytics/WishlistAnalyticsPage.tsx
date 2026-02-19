import { useTranslation } from 'react-i18next'
import {
  Heart,
  ListChecks,
  Package,
  TrendingUp,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import {
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  PageHeader,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { useWishlistAnalyticsQuery } from '@/portal-app/wishlists/queries'

export const WishlistAnalyticsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Wishlists')

  const { data: analytics, isLoading } = useWishlistAnalyticsQuery({ topCount: 10 })

  const totalWishlists = analytics?.totalWishlists ?? 0
  const totalItems = analytics?.totalWishlistItems ?? 0
  const topProducts = analytics?.topProducts ?? []

  const statsCards = [
    {
      title: t('wishlists.analytics.totalWishlists', 'Total Wishlists'),
      value: totalWishlists,
      icon: Heart,
      color: 'text-rose-500',
      bgColor: 'from-rose-500/20 to-rose-500/10',
      borderColor: 'border-rose-500/20',
    },
    {
      title: t('wishlists.analytics.totalItems', 'Total Items'),
      value: totalItems,
      icon: Package,
      color: 'text-blue-500',
      bgColor: 'from-blue-500/20 to-blue-500/10',
      borderColor: 'border-blue-500/20',
    },
    {
      title: t('wishlists.analytics.topProductsCount', 'Top Products Tracked'),
      value: topProducts.length,
      icon: TrendingUp,
      color: 'text-emerald-500',
      bgColor: 'from-emerald-500/20 to-emerald-500/10',
      borderColor: 'border-emerald-500/20',
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Heart}
        title={t('wishlists.analytics.title', 'Wishlist Analytics')}
        description={t('wishlists.analytics.description', 'Overview of wishlist activity and popular products')}
        responsive
      />

      {/* Stats Cards */}
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-3">
        {statsCards.map((stat) => (
          <Card
            key={stat.title}
            className="shadow-sm hover:shadow-lg transition-all duration-300"
          >
            <CardContent className="p-6">
              <div className="flex items-center gap-4">
                <div
                  className={`flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br ${stat.bgColor} border ${stat.borderColor} shadow-sm`}
                >
                  <stat.icon className={`h-6 w-6 ${stat.color}`} />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{stat.title}</p>
                  {isLoading ? (
                    <Skeleton className="h-8 w-16 mt-1" />
                  ) : (
                    <p className="text-2xl font-bold tracking-tight">{stat.value.toLocaleString()}</p>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Top Wishlisted Products */}
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-rose-500/20 to-rose-500/10 border border-rose-500/20">
              <ListChecks className="h-4.5 w-4.5 text-rose-500" />
            </div>
            <div>
              <CardTitle>{t('wishlists.analytics.topProducts', 'Top Wishlisted Products')}</CardTitle>
              <CardDescription>
                {t('wishlists.analytics.topProductsDescription', 'Most wishlisted products by your customers')}
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-12">#</TableHead>
                  <TableHead>{t('wishlists.analytics.productName', 'Product')}</TableHead>
                  <TableHead className="text-right">
                    {t('wishlists.analytics.timesWishlisted', 'Times Wishlisted')}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell><Skeleton className="h-4 w-6" /></TableCell>
                      <TableCell>
                        <div className="flex items-center gap-3">
                          <Skeleton className="h-10 w-10 rounded-lg" />
                          <Skeleton className="h-4 w-32" />
                        </div>
                      </TableCell>
                      <TableCell className="text-right"><Skeleton className="h-5 w-12 ml-auto rounded-full" /></TableCell>
                    </TableRow>
                  ))
                ) : topProducts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={3} className="p-0">
                      <EmptyState
                        icon={Heart}
                        title={t('wishlists.analytics.noProducts', 'No wishlisted products yet')}
                        description={t('wishlists.analytics.noProductsDescription', 'Wishlist analytics will appear here once customers start adding products to their wishlists.')}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  topProducts.map((product, index) => (
                    <TableRow key={product.productId} className="group transition-colors hover:bg-muted/50">
                      <TableCell>
                        <span className="text-sm text-muted-foreground font-mono">{index + 1}</span>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-3">
                          {product.productImage ? (
                            <img
                              src={product.productImage}
                              alt={product.productName}
                              className="h-10 w-10 rounded-lg object-cover border border-border/50"
                            />
                          ) : (
                            <div className="h-10 w-10 rounded-lg bg-muted flex items-center justify-center border border-border/50">
                              <Package className="h-4 w-4 text-muted-foreground" />
                            </div>
                          )}
                          <span className="font-medium text-sm">{product.productName}</span>
                        </div>
                      </TableCell>
                      <TableCell className="text-right">
                        <Badge variant="secondary" className="font-mono">
                          {product.wishlistCount}
                        </Badge>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

export default WishlistAnalyticsPage
