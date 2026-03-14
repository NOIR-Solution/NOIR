import { useState, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  CheckCircle2,
  Eye,
  Loader2,
  Plus,
  ShoppingCart,
  XCircle,
} from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable, useSelectedIds } from '@/hooks/useEnterpriseTable'
import { createSelectColumn, createActionsColumn } from '@/lib/table/columnHelpers'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  EmptyState,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '@uikit'
import { useOrdersQuery, useBulkConfirmOrders, useBulkCancelOrders } from '@/portal-app/orders/queries'
import { ExportDropdownMenu } from '@/components/ExportDropdownMenu'
import { exportOrders } from '@/services/orders'
import type { OrderStatus, OrderSummaryDto } from '@/types/order'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { getOrderStatusColor, ORDER_STATUSES } from '@/portal-app/orders/utils/orderStatus'

const CANCELLABLE_STATUSES: OrderStatus[] = ['Pending', 'Confirmed', 'Processing']

const ch = createColumnHelper<OrderSummaryDto>()

export const OrdersPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { formatDateTime } = useRegionalSettings()
  const { hasPermission } = usePermissions()
  const canManageOrders = hasPermission(Permissions.OrdersManage)
  usePageContext('Orders')

  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [showBulkCancelConfirm, setShowBulkCancelConfirm] = useState(false)
  const [cancelReason, setCancelReason] = useState('')
  const [isBulkPending, startBulkTransition] = useTransition()
  const [isFilterPending, startFilterTransition] = useTransition()

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    setSorting,
    setPage,
    setPageSize,
    defaultPageSize,
  } = useTableParams({ defaultPageSize: 20, tableKey: 'orders' })

  const queryParams = useMemo(() => ({
    ...params,
    customerEmail: params.search,
    search: undefined,
    status: statusFilter !== 'all' ? statusFilter as OrderStatus : undefined,
  }), [params, statusFilter])

  const { data, isLoading, error: queryError, refetch: refresh } = useOrdersQuery(queryParams)

  const orders = data?.items ?? []

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Order',
    onCollectionUpdate: refresh,
  })

  const bulkConfirmMutation = useBulkConfirmOrders()
  const bulkCancelMutation = useBulkCancelOrders()

  const handleStatusFilter = (value: string) => startFilterTransition(() => { setStatusFilter(value); setPage(1) })

  const columns = useMemo((): ColumnDef<OrderSummaryDto, unknown>[] => [
    createActionsColumn<OrderSummaryDto>((order) => (
      <DropdownMenuItem className="cursor-pointer" onClick={() => navigate(`/portal/ecommerce/orders/${order.id}`)}>
        <Eye className="h-4 w-4 mr-2" />
        {t('labels.viewDetails', 'View Details')}
      </DropdownMenuItem>
    )),
    ...(canManageOrders ? [createSelectColumn<OrderSummaryDto>()] : []),
    ch.accessor('orderNumber', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('orders.orderNumber', 'Order #')} />,
      meta: { label: t('orders.orderNumber', 'Order #') },
      cell: ({ getValue }) => <span className="font-mono font-medium text-sm">{getValue()}</span>,
      size: 140,
    }) as ColumnDef<OrderSummaryDto, unknown>,
    ch.accessor('customerName', {
      id: 'customer',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.customer', 'Customer')} />,
      meta: { label: t('labels.customer', 'Customer') },
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="font-medium text-sm">{row.original.customerName || '-'}</span>
          <span className="text-xs text-muted-foreground">{row.original.customerEmail}</span>
        </div>
      ),
    }) as ColumnDef<OrderSummaryDto, unknown>,
    ch.accessor('status', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status', 'Status')} />,
      meta: { label: t('labels.status', 'Status') },
      size: 130,
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getOrderStatusColor(getValue())}>
          {t(`orders.status.${getValue().toLowerCase()}`, getValue())}
        </Badge>
      ),
    }) as ColumnDef<OrderSummaryDto, unknown>,
    ch.accessor('itemCount', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('orders.items', 'Items')} />,
      meta: { align: 'center', label: t('orders.items', 'Items') },
      size: 80,
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<OrderSummaryDto, unknown>,
    ch.accessor('grandTotal', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('orders.total', 'Total')} />,
      meta: { align: 'right', label: t('orders.total', 'Total') },
      cell: ({ row }) => (
        <span className="font-medium">{formatCurrency(row.original.grandTotal, row.original.currency)}</span>
      ),
    }) as ColumnDef<OrderSummaryDto, unknown>,
    ch.accessor('createdAt', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.date', 'Date')} />,
      meta: { label: t('labels.date', 'Date') },
      size: 160,
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{formatDateTime(getValue())}</span>,
    }) as ColumnDef<OrderSummaryDto, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canManageOrders, formatDateTime])

  const tableData = useMemo(() => data?.items ?? [], [data?.items])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: tableData,
    columns,
    tableKey: 'orders',
    rowCount: data?.totalCount ?? 0,
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: params.sorting as SortingState,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function'
        ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize })
        : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    onSortingChange: setSorting,
    enableRowSelection: canManageOrders,
    getRowId: (row) => row.id,
  })

  const currentRowSelection = table.getState().rowSelection
  const selectedIds = useSelectedIds(currentRowSelection)
  const selectedCount = selectedIds.length

  const selectedPendingCount = useMemo(
    () => orders.filter(o => currentRowSelection[o.id] && o.status === 'Pending').length,
    [orders, currentRowSelection]
  )
  const selectedCancellableCount = useMemo(
    () => orders.filter(o => currentRowSelection[o.id] && CANCELLABLE_STATUSES.includes(o.status)).length,
    [orders, currentRowSelection]
  )

  const onBulkConfirm = () => {
    if (selectedCount === 0) return
    const pendingIds = orders.filter(o => currentRowSelection[o.id] && o.status === 'Pending').map(o => o.id)
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
      table.resetRowSelection()
    })
  }

  const onBulkCancel = () => {
    if (selectedCount === 0) return
    if (selectedCancellableCount === 0) {
      toast.warning(t('orders.noCancellableOrdersSelected', 'No cancellable orders selected'))
      return
    }
    setShowBulkCancelConfirm(true)
  }

  const handleBulkCancelConfirm = () => {
    const cancellableIds = orders.filter(o => currentRowSelection[o.id] && CANCELLABLE_STATUSES.includes(o.status)).map(o => o.id)
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
      table.resetRowSelection()
      setShowBulkCancelConfirm(false)
      setCancelReason('')
    })
  }

  if (queryError) {
    console.error(queryError)
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

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('orders.allOrders', 'All Orders')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('orders.searchPlaceholder', 'Search by email...')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              density={settings.density}
              onDensityChange={setDensity}
              filterSlot={
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
              }
            />
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {canManageOrders && (
            <BulkActionToolbar selectedCount={selectedCount} onClearSelection={() => table.resetRowSelection()}>
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

          <DataTable
            table={table}
            density={settings.density}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={selectedCount === 0 ? (order) => navigate(`/portal/ecommerce/orders/${order.id}`) : undefined}
            emptyState={
              <EmptyState
                icon={ShoppingCart}
                title={t('orders.noOrdersFound', 'No orders found')}
                description={t('orders.noOrdersDescription', 'Orders will appear here when customers place them.')}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

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
