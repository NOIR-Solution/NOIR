import { useState, useEffect, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Calendar,
  Eye,
  MoreHorizontal,
  Plus,
  Search,
  ShoppingCart,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
  Input,
  PageHeader,
  Pagination,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { useOrdersQuery } from '@/portal-app/orders/queries'
import type { GetOrdersParams } from '@/services/orders'
import type { OrderStatus, OrderSummaryDto } from '@/types/order'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { getOrderStatusColor, ORDER_STATUSES } from '@/portal-app/orders/utils/orderStatus'

export const OrdersPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { formatDateTime } = useRegionalSettings()
  const { hasPermission } = usePermissions()
  const canManageOrders = hasPermission(Permissions.OrdersManage)
  usePageContext('Orders')

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()
  const [params, setParams] = useState<GetOrdersParams>({ page: 1, pageSize: 20 })

  // Reset page to 1 when deferred search settles
  useEffect(() => {
    setParams(prev => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  const queryParams = useMemo(() => ({
    ...params,
    customerEmail: deferredSearch || undefined,
    status: statusFilter !== 'all' ? statusFilter as OrderStatus : undefined,
  }), [params, deferredSearch, statusFilter])

  const { data: ordersResponse, isLoading: loading, error: queryError } = useOrdersQuery(queryParams)
  const error = queryError?.message ?? null

  const orders = ordersResponse?.items ?? []
  const totalCount = ordersResponse?.totalCount ?? 0
  const totalPages = ordersResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
  }

  const handleStatusFilter = (value: string) => {
    startFilterTransition(() => {
      setStatusFilter(value)
      setParams((prev) => ({ ...prev, page: 1 }))
    })
  }

  const handlePageChange = (page: number) => {
    startFilterTransition(() => {
      setParams((prev) => ({ ...prev, page }))
    })
  }

  const handleViewOrder = (order: OrderSummaryDto) => {
    navigate(`/portal/ecommerce/orders/${order.id}`)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={ShoppingCart}
        title={t('orders.title', 'Orders')}
        description={t('orders.description', 'Manage customer orders and track fulfillment')}
        responsive
        action={
          canManageOrders ? (
            <Button
              className="group shadow-lg hover:shadow-xl transition-all duration-300 cursor-pointer"
              onClick={() => navigate('/portal/ecommerce/orders/create')}
            >
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('orders.createNew', 'Create Order')}
            </Button>
          ) : undefined
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('orders.allOrders', 'All Orders')}</CardTitle>
              <CardDescription>
                {t('orders.totalCount', { count: totalCount, defaultValue: `${totalCount} orders total` })}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {/* Search */}
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('orders.searchPlaceholder', 'Search by email...')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-9 h-9"
                  aria-label={t('orders.searchOrders', 'Search orders')}
                />
              </div>
              {/* Status Filter */}
              <Select value={statusFilter} onValueChange={handleStatusFilter}>
                <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('orders.filterByStatus', 'Filter by status')}>
                  <SelectValue placeholder={t('orders.filterByStatus', 'Filter status')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                  {ORDER_STATUSES.map((status) => (
                    <SelectItem key={status} value={status} className="cursor-pointer">
                      {t(`orders.status.${status.toLowerCase()}`, status)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                  <TableHead>{t('orders.orderNumber', 'Order #')}</TableHead>
                  <TableHead>{t('labels.customer', 'Customer')}</TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead className="text-center">{t('orders.items', 'Items')}</TableHead>
                  <TableHead className="text-right">{t('orders.total', 'Total')}</TableHead>
                  <TableHead>
                    <Calendar className="h-4 w-4 inline mr-1" />
                    {t('labels.date', 'Date')}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-24 ml-auto" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                    </TableRow>
                  ))
                ) : orders.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="p-0">
                      <EmptyState
                        icon={ShoppingCart}
                        title={t('orders.noOrdersFound', 'No orders found')}
                        description={t('orders.noOrdersDescription', 'Orders will appear here when customers place them.')}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  orders.map((order) => (
                    <TableRow
                      key={order.id}
                      className="group transition-colors hover:bg-muted/50 cursor-pointer"
                      onClick={() => handleViewOrder(order)}
                    >
                      <TableCell className="sticky left-0 z-10 bg-background" onClick={(e) => e.stopPropagation()}>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: order.orderNumber, defaultValue: `Actions for ${order.orderNumber}` })}
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem className="cursor-pointer" onClick={() => handleViewOrder(order)}>
                              <Eye className="h-4 w-4 mr-2" />
                              {t('labels.viewDetails')}
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                      <TableCell>
                        <span className="font-mono font-medium text-sm">{order.orderNumber}</span>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-col">
                          <span className="font-medium text-sm">{order.customerName || '-'}</span>
                          <span className="text-xs text-muted-foreground">{order.customerEmail}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className={getOrderStatusColor(order.status)}>
                          {t(`orders.status.${order.status.toLowerCase()}`, order.status)}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">{order.itemCount}</Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <span className="font-medium">
                          {formatCurrency(order.grandTotal, order.currency)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">
                          {formatDateTime(order.createdAt)}
                        </span>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalItems={totalCount}
              pageSize={params.pageSize || 20}
              onPageChange={handlePageChange}
              showPageSizeSelector={false}
              className="mt-4"
            />
          )}
        </CardContent>
      </Card>
    </div>
  )
}

export default OrdersPage
