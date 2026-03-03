import { useState, useEffect, useDeferredValue, useMemo, useTransition, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Calendar,
  CheckCircle2,
  Eye,
  Loader2,
  MoreHorizontal,
  Plus,
  Search,
  ShoppingCart,
  XCircle,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { useSelection } from '@/hooks/useSelection'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Checkbox,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
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
  Textarea,
} from '@uikit'
import { useOrdersQuery, useBulkConfirmOrders, useBulkCancelOrders } from '@/portal-app/orders/queries'
import { ExportDropdownMenu } from '@/components/ExportDropdownMenu'
import { exportOrders } from '@/services/orders'
import type { GetOrdersParams } from '@/services/orders'
import type { OrderStatus, OrderSummaryDto } from '@/types/order'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { getOrderStatusColor, ORDER_STATUSES } from '@/portal-app/orders/utils/orderStatus'

const CANCELLABLE_STATUSES: OrderStatus[] = ['Pending', 'Confirmed', 'Processing']

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

  // Bulk action state
  const [showBulkCancelConfirm, setShowBulkCancelConfirm] = useState(false)
  const [cancelReason, setCancelReason] = useState('')
  const [isBulkPending, startBulkTransition] = useTransition()

  // Reset page to 1 when deferred search settles
  useEffect(() => {
    setParams(prev => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  const queryParams = useMemo(() => ({
    ...params,
    customerEmail: deferredSearch || undefined,
    status: statusFilter !== 'all' ? statusFilter as OrderStatus : undefined,
  }), [params, deferredSearch, statusFilter])

  const { data: ordersResponse, isLoading: loading, error: queryError, refetch } = useOrdersQuery(queryParams)
  const error = queryError?.message ?? null

  const orders = ordersResponse?.items ?? []
  const totalCount = ordersResponse?.totalCount ?? 0
  const totalPages = ordersResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  const { selectedIds, setSelectedIds, handleSelectAll, handleSelectNone, handleToggleSelect, isAllSelected } = useSelection(orders)

  const handleCollectionUpdate = useCallback(() => {
    if (selectedIds.size === 0) refetch()
  }, [selectedIds.size, refetch])

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Order',
    onCollectionUpdate: handleCollectionUpdate,
  })

  // Bulk mutation hooks
  const bulkConfirmMutation = useBulkConfirmOrders()
  const bulkCancelMutation = useBulkCancelOrders()

  // Computed counts
  const selectedPendingCount = orders.filter(o => selectedIds.has(o.id) && o.status === 'Pending').length
  const selectedCancellableCount = orders.filter(o => selectedIds.has(o.id) && CANCELLABLE_STATUSES.includes(o.status)).length

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

  // Bulk action handlers
  const onBulkConfirm = () => {
    if (selectedIds.size === 0) return
    const pendingIds = orders.filter(o => selectedIds.has(o.id) && o.status === 'Pending').map(o => o.id)
    if (pendingIds.length === 0) {
      toast.warning(t('orders.noPendingOrdersSelected', 'No pending orders selected'))
      return
    }
    startBulkTransition(async () => {
      try {
        const result = await bulkConfirmMutation.mutateAsync(pendingIds)
        if (result.failed > 0) {
          toast.warning(t('orders.bulkConfirmPartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} confirmed, ${result.failed} failed` }))
        } else {
          toast.success(t('orders.bulkConfirmSuccess', { count: result.success, defaultValue: `${result.success} orders confirmed` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('orders.bulkConfirmFailed', 'Failed to confirm orders'))
      }
      setSelectedIds(new Set())
    })
  }

  const onBulkCancel = () => {
    if (selectedIds.size === 0) return
    if (selectedCancellableCount === 0) {
      toast.warning(t('orders.noCancellableOrdersSelected', 'No cancellable orders selected'))
      return
    }
    setShowBulkCancelConfirm(true)
  }

  const handleBulkCancelConfirm = () => {
    const cancellableIds = orders.filter(o => selectedIds.has(o.id) && CANCELLABLE_STATUSES.includes(o.status)).map(o => o.id)
    startBulkTransition(async () => {
      try {
        const result = await bulkCancelMutation.mutateAsync({ ids: cancellableIds, reason: cancelReason || undefined })
        if (result.failed > 0) {
          toast.warning(t('orders.bulkCancelPartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} cancelled, ${result.failed} failed` }))
        } else {
          toast.success(t('orders.bulkCancelSuccess', { count: result.success, defaultValue: `${result.success} orders cancelled` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('orders.bulkCancelFailed', 'Failed to cancel orders'))
      }
      setSelectedIds(new Set())
      setShowBulkCancelConfirm(false)
      setCancelReason('')
    })
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={ShoppingCart}
        title={t('orders.title', 'Orders')}
        description={t('orders.description', 'Manage customer orders and track fulfillment')}
        responsive
        action={
          <div className="flex items-center gap-2">
            <ExportDropdownMenu onExport={(format) => exportOrders({ format })} />
            {canManageOrders && (
              <Button
                className="group transition-all duration-300 cursor-pointer"
                onClick={() => navigate('/portal/ecommerce/orders/create')}
              >
                <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
                {t('orders.newOrder', 'New Order')}
              </Button>
            )}
          </div>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('orders.allOrders', 'All Orders')}</CardTitle>
              <CardDescription>
                {t('labels.showingCountOfTotal', { count: orders.length, total: totalCount })}
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

          {/* Bulk Action Toolbar */}
          {canManageOrders && (
            <BulkActionToolbar selectedCount={selectedIds.size} onClearSelection={handleSelectNone}>
              {selectedPendingCount > 0 && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={onBulkConfirm}
                  disabled={isBulkPending}
                  className="cursor-pointer text-emerald-600 border-emerald-200 hover:bg-emerald-50 dark:text-emerald-400 dark:border-emerald-800 dark:hover:bg-emerald-950"
                >
                  {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <CheckCircle2 className="h-4 w-4 mr-2" />}
                  {t('orders.confirmCount', { count: selectedPendingCount, defaultValue: `Confirm (${selectedPendingCount})` })}
                </Button>
              )}
              {selectedCancellableCount > 0 && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={onBulkCancel}
                  disabled={isBulkPending}
                  className="cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
                >
                  {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <XCircle className="h-4 w-4 mr-2" />}
                  {t('orders.cancelCount', { count: selectedCancellableCount, defaultValue: `Cancel (${selectedCancellableCount})` })}
                </Button>
              )}
            </BulkActionToolbar>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                  {canManageOrders && (
                    <TableHead className="w-[40px]">
                      <Checkbox
                        checked={isAllSelected}
                        onCheckedChange={(checked) => {
                          if (checked) handleSelectAll()
                          else handleSelectNone()
                        }}
                        aria-label={t('labels.selectAll', 'Select all')}
                        className="cursor-pointer"
                      />
                    </TableHead>
                  )}
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
                      {canManageOrders && <TableCell><Skeleton className="h-4 w-4 rounded" /></TableCell>}
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
                    <TableCell colSpan={canManageOrders ? 8 : 7} className="p-0">
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
                      className={`group transition-colors hover:bg-muted/50 ${selectedIds.size === 0 ? 'cursor-pointer' : ''} ${selectedIds.has(order.id) ? 'bg-primary/5' : ''}`}
                      onClick={() => { if (selectedIds.size === 0) handleViewOrder(order) }}
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
                      {canManageOrders && (
                        <TableCell onClick={(e) => e.stopPropagation()}>
                          <Checkbox
                            checked={selectedIds.has(order.id)}
                            onCheckedChange={() => handleToggleSelect(order.id)}
                            aria-label={t('labels.selectItem', { name: order.orderNumber, defaultValue: `Select ${order.orderNumber}` })}
                            className="cursor-pointer"
                          />
                        </TableCell>
                      )}
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

      {/* Bulk Cancel Confirmation Dialog */}
      <Credenza open={showBulkCancelConfirm} onOpenChange={setShowBulkCancelConfirm}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <XCircle className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('orders.bulkCancelConfirmTitle', { count: selectedCancellableCount, defaultValue: `Cancel ${selectedCancellableCount} orders` })}</CredenzaTitle>
                <CredenzaDescription>
                  {t('orders.bulkCancelConfirmDescription', {
                    count: selectedCancellableCount,
                    defaultValue: `Are you sure you want to cancel ${selectedCancellableCount} orders?`,
                  })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody>
            <div className="space-y-4">
              <div>
                <label className="text-sm font-medium" htmlFor="cancel-reason">
                  {t('orders.cancelReason', 'Cancellation reason (optional)')}
                </label>
                <Textarea
                  id="cancel-reason"
                  value={cancelReason}
                  onChange={(e) => setCancelReason(e.target.value)}
                  placeholder={t('orders.cancelReasonPlaceholder', 'Enter reason for cancellation...')}
                  className="mt-1.5"
                  rows={3}
                />
              </div>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button
              variant="outline"
              onClick={() => { setShowBulkCancelConfirm(false); setCancelReason('') }}
              disabled={isBulkPending}
              className="cursor-pointer"
            >
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleBulkCancelConfirm}
              disabled={isBulkPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {isBulkPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isBulkPending
                ? t('labels.cancelling', 'Cancelling...')
                : t('orders.cancelCount', { count: selectedCancellableCount, defaultValue: `Cancel (${selectedCancellableCount})` })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default OrdersPage
