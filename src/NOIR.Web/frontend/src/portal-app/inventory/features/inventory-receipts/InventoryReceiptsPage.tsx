import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Warehouse,
  CheckCircle2,
  Eye,
  Loader2,
  XCircle,
} from 'lucide-react'
import { toast } from 'sonner'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useTableParams } from '@/hooks/useTableParams'
import { useServerTable } from '@/hooks/useServerTable'
import { createActionsColumn } from '@/lib/table/columnHelpers'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
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
  DropdownMenuSeparator,
  EmptyState,
  Label,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '@uikit'

import {
  useInventoryReceiptsQuery,
  useConfirmInventoryReceiptMutation,
  useCancelInventoryReceiptMutation,
} from '@/portal-app/inventory/queries'
import type { InventoryReceiptType, InventoryReceiptStatus, InventoryReceiptSummaryDto } from '@/types/inventory'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { RECEIPT_TYPE_CONFIG, RECEIPT_STATUS_CONFIG } from './inventoryReceiptConfig'
import { InventoryReceiptDetailDialog } from './InventoryReceiptDetailDialog'

const ch = createColumnHelper<InventoryReceiptSummaryDto>()

export const InventoryReceiptsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Inventory')

  const canWriteInventory = hasPermission(Permissions.InventoryWrite)
  const canManageInventory = hasPermission(Permissions.InventoryManage)

  const [receiptToCancel, setReceiptToCancel] = useState<InventoryReceiptSummaryDto | null>(null)
  const [cancelReason, setCancelReason] = useState('')
  const [receiptToConfirm, setReceiptToConfirm] = useState<InventoryReceiptSummaryDto | null>(null)
  const [selectedReceiptId, setSelectedReceiptId] = useState<string | undefined>(undefined)

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    isFilterPending,
    setFilter,
    setPage,
    setPageSize,
  } = useTableParams<{ type?: InventoryReceiptType; status?: InventoryReceiptStatus }>({ defaultPageSize: 20 })

  const { data: receiptsResponse, isLoading, error: queryError, refetch } = useInventoryReceiptsQuery(params)
  const confirmMutation = useConfirmInventoryReceiptMutation()
  const cancelMutation = useCancelInventoryReceiptMutation()
  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'InventoryReceipt',
    onCollectionUpdate: refetch,
  })

  const receipts = receiptsResponse?.items ?? []

  const handleTypeFilter = (value: string) => setFilter('type', value === 'all' ? undefined : (value as InventoryReceiptType))
  const handleStatusFilter = (value: string) => setFilter('status', value === 'all' ? undefined : (value as InventoryReceiptStatus))

  const handleConfirm = (receipt: InventoryReceiptSummaryDto) => {
    setReceiptToConfirm(receipt)
  }

  const handleConfirmReceipt = async () => {
    if (!receiptToConfirm) return
    try {
      await confirmMutation.mutateAsync(receiptToConfirm.id)
      toast.success(t('inventory.confirmSuccess', { receiptNumber: receiptToConfirm.receiptNumber, defaultValue: `Receipt ${receiptToConfirm.receiptNumber} confirmed` }))
      setReceiptToConfirm(null)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('inventory.actionError', 'Failed to update receipt')
      toast.error(message)
    }
  }

  const handleCancel = async () => {
    if (!receiptToCancel) return
    try {
      await cancelMutation.mutateAsync({ id: receiptToCancel.id, reason: cancelReason || undefined })
      toast.success(t('inventory.cancelSuccess', { receiptNumber: receiptToCancel.receiptNumber, defaultValue: `Receipt ${receiptToCancel.receiptNumber} cancelled` }))
      setReceiptToCancel(null)
      setCancelReason('')
    } catch (err) {
      const message = err instanceof Error ? err.message : t('inventory.actionError', 'Failed to update receipt')
      toast.error(message)
    }
  }

  const columns = useMemo((): ColumnDef<InventoryReceiptSummaryDto, unknown>[] => [
    createActionsColumn<InventoryReceiptSummaryDto>((receipt) => {
      const isDraft = receipt.status === 'Draft'
      return (
        <>
          <DropdownMenuItem
            className="cursor-pointer"
            onClick={() => setSelectedReceiptId(receipt.id)}
          >
            <Eye className="h-4 w-4 mr-2" />
            {t('labels.viewDetails', 'View Details')}
          </DropdownMenuItem>
          {isDraft && canWriteInventory && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="cursor-pointer text-green-600 dark:text-green-400"
                onClick={() => handleConfirm(receipt)}
              >
                <CheckCircle2 className="h-4 w-4 mr-2" />
                {t('inventory.confirm', 'Confirm')}
              </DropdownMenuItem>
            </>
          )}
          {isDraft && (canWriteInventory || canManageInventory) && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="cursor-pointer text-destructive focus:text-destructive"
                onClick={() => setReceiptToCancel(receipt)}
              >
                <XCircle className="h-4 w-4 mr-2" />
                {t('inventory.cancel', 'Cancel')}
              </DropdownMenuItem>
            </>
          )}
        </>
      )
    }),
    ch.accessor('receiptNumber', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('inventory.receiptNumber', 'Receipt #')} />,
      meta: { label: t('inventory.receiptNumber', 'Receipt #') },
      enableSorting: false,
      cell: ({ getValue }) => <span className="font-mono font-medium text-sm">{getValue()}</span>,
    }) as ColumnDef<InventoryReceiptSummaryDto, unknown>,
    ch.accessor('type', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.type', 'Type')} />,
      meta: { label: t('labels.type', 'Type') },
      enableSorting: false,
      cell: ({ row }) => {
        const typeConfig = RECEIPT_TYPE_CONFIG[row.original.type]
        const TypeIcon = typeConfig.icon
        return (
          <Badge variant="outline" className={typeConfig.color}>
            <TypeIcon className="h-3 w-3 mr-1.5" />
            {t(`inventory.type.${typeConfig.label}`)}
          </Badge>
        )
      },
    }) as ColumnDef<InventoryReceiptSummaryDto, unknown>,
    ch.accessor('status', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status', 'Status')} />,
      meta: { label: t('labels.status', 'Status') },
      enableSorting: false,
      cell: ({ row }) => {
        const statusConfig = RECEIPT_STATUS_CONFIG[row.original.status]
        const StatusIcon = statusConfig.icon
        return (
          <Badge variant="outline" className={statusConfig.color}>
            <StatusIcon className="h-3 w-3 mr-1.5" />
            {t(`inventory.status.${statusConfig.label}`)}
          </Badge>
        )
      },
    }) as ColumnDef<InventoryReceiptSummaryDto, unknown>,
    ch.accessor('itemCount', {
      id: 'items',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('inventory.items', 'Items')} />,
      meta: { label: t('inventory.items', 'Items'), align: 'center' },
      enableSorting: false,
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<InventoryReceiptSummaryDto, unknown>,
    ch.accessor('totalQuantity', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('inventory.totalQuantity', 'Total Qty')} />,
      meta: { label: t('inventory.totalQuantity', 'Total Qty'), align: 'right' },
      enableSorting: false,
      cell: ({ getValue }) => <span className="font-medium">{getValue()}</span>,
    }) as ColumnDef<InventoryReceiptSummaryDto, unknown>,
    ch.accessor('totalCost', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('inventory.totalCost', 'Total Cost')} />,
      meta: { label: t('inventory.totalCost', 'Total Cost'), align: 'right' },
      enableSorting: false,
      cell: ({ row }) => <span className="font-medium">{formatCurrency(row.original.totalCost)}</span>,
    }) as ColumnDef<InventoryReceiptSummaryDto, unknown>,
    ch.accessor('createdAt', {
      id: 'date',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.date', 'Date')} />,
      meta: { label: t('labels.date', 'Date') },
      enableSorting: false,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatDateTime(row.original.createdAt)}
        </span>
      ),
    }) as ColumnDef<InventoryReceiptSummaryDto, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canWriteInventory, canManageInventory])

  const table = useServerTable({
    data: receipts,
    columns,
    rowCount: receiptsResponse?.totalCount ?? 0,
    columnVisibilityStorageKey: 'inventory-receipts',
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: [],
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function'
        ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize })
        : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    getRowId: (row) => row.id,
  })

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Warehouse}
        title={t('inventory.title', 'Inventory')}
        description={t('inventory.description', 'Manage stock receipts and inventory movements')}
        responsive
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('inventory.allReceipts', 'All Receipts')}</CardTitle>
              <CardDescription>
                {receiptsResponse ? t('labels.showingCountOfTotal', { count: receipts.length, total: receiptsResponse.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('inventory.searchPlaceholder')}
              isSearchStale={isSearchStale}
              onResetColumnVisibility={table.resetColumnVisibility}
              filterSlot={
                <>
                  <Select value={params.filters.type ?? 'all'} onValueChange={handleTypeFilter}>
                    <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('inventory.filterByType', 'Filter by type')}>
                      <SelectValue placeholder={t('labels.type', 'Type')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                      <SelectItem value="StockIn" className="cursor-pointer">{t('inventory.type.stockIn', 'Stock In')}</SelectItem>
                      <SelectItem value="StockOut" className="cursor-pointer">{t('inventory.type.stockOut', 'Stock Out')}</SelectItem>
                    </SelectContent>
                  </Select>
                  <Select value={params.filters.status ?? 'all'} onValueChange={handleStatusFilter}>
                    <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('inventory.filterByStatus', 'Filter by status')}>
                      <SelectValue placeholder={t('labels.status', 'Status')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                      <SelectItem value="Draft" className="cursor-pointer">{t('inventory.status.draft', 'Draft')}</SelectItem>
                      <SelectItem value="Confirmed" className="cursor-pointer">{t('inventory.status.confirmed', 'Confirmed')}</SelectItem>
                      <SelectItem value="Cancelled" className="cursor-pointer">{t('inventory.status.cancelled', 'Cancelled')}</SelectItem>
                    </SelectContent>
                  </Select>
                </>
              }
            />
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={(receipt) => setSelectedReceiptId(receipt.id)}
            emptyState={
              <EmptyState
                icon={Warehouse}
                title={t('inventory.noReceiptsFound', 'No receipts found')}
                description={t('inventory.noReceiptsDescription', 'Inventory receipts will appear here when stock movements are recorded.')}
              />
            }
          />

          <DataTablePagination table={table} showPageSizeSelector={false} />
        </CardContent>
      </Card>

      {/* Confirm Receipt Dialog */}
      <Credenza open={!!receiptToConfirm} onOpenChange={(open: boolean) => { if (!open) setReceiptToConfirm(null) }}>
        <CredenzaContent>
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-green-500/10 border border-green-500/20">
                <CheckCircle2 className="h-5 w-5 text-green-600 dark:text-green-400" />
              </div>
              <div>
                <CredenzaTitle>{t('inventory.confirmReceiptTitle', 'Confirm Receipt')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('inventory.confirmReceiptDescription', 'Are you sure you want to confirm receipt "{{receiptNumber}}"? This action is irreversible and inventory quantities will be updated.', { receiptNumber: receiptToConfirm?.receiptNumber })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setReceiptToConfirm(null)} disabled={confirmMutation.isPending} className="cursor-pointer">
              {t('buttons.cancel', 'Cancel')}
            </Button>
            <Button
              onClick={handleConfirmReceipt}
              disabled={confirmMutation.isPending}
              className="cursor-pointer"
            >
              {confirmMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {confirmMutation.isPending ? t('labels.confirming', 'Confirming...') : t('inventory.confirm', 'Confirm')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Cancel Receipt Dialog */}
      <Credenza open={!!receiptToCancel} onOpenChange={(open: boolean) => { if (!open) { setReceiptToCancel(null); setCancelReason('') } }}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <XCircle className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('inventory.cancelReceiptTitle', 'Cancel Receipt')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('inventory.cancelReceiptDescription', {
                    receiptNumber: receiptToCancel?.receiptNumber,
                    defaultValue: `Are you sure you want to cancel receipt "${receiptToCancel?.receiptNumber}"? This action cannot be undone.`,
                  })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody>
            <div className="py-2">
              <div className="space-y-2">
                <Label htmlFor="cancelReason">{t('inventory.reasonOptional', 'Reason (optional)')}</Label>
                <Textarea
                  id="cancelReason"
                  value={cancelReason}
                  onChange={(e) => setCancelReason(e.target.value)}
                  placeholder={t('inventory.cancelReasonPlaceholder', 'Enter cancellation reason...')}
                  rows={3}
                />
              </div>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => { setReceiptToCancel(null); setCancelReason('') }} disabled={cancelMutation.isPending} className="cursor-pointer">
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleCancel}
              disabled={cancelMutation.isPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {cancelMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {cancelMutation.isPending ? t('labels.cancelling', 'Cancelling...') : t('inventory.cancelReceipt', 'Cancel Receipt')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      <InventoryReceiptDetailDialog
        receiptId={selectedReceiptId}
        open={!!selectedReceiptId}
        onOpenChange={(open) => { if (!open) setSelectedReceiptId(undefined) }}
      />
    </div>
  )
}

export default InventoryReceiptsPage
