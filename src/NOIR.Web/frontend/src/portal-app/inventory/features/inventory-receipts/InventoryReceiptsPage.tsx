import { useState, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Warehouse,
  CheckCircle2,
  XCircle,
  FileText,
  ArrowDownToLine,
  ArrowUpFromLine,
  MoreHorizontal,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
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
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  EmptyState,
  Label,
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

import {
  useInventoryReceiptsQuery,
  useConfirmInventoryReceiptMutation,
  useCancelInventoryReceiptMutation,
} from '@/portal-app/inventory/queries'
import type { GetInventoryReceiptsParams } from '@/types/inventory'
import type { InventoryReceiptType, InventoryReceiptStatus, InventoryReceiptSummaryDto } from '@/types/inventory'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'

const RECEIPT_TYPE_CONFIG: Record<InventoryReceiptType, { color: string; icon: typeof ArrowDownToLine }> = {
  StockIn: {
    color: 'bg-green-100 text-green-800 border-green-200 dark:bg-green-900/30 dark:text-green-400 dark:border-green-800',
    icon: ArrowDownToLine,
  },
  StockOut: {
    color: 'bg-orange-100 text-orange-800 border-orange-200 dark:bg-orange-900/30 dark:text-orange-400 dark:border-orange-800',
    icon: ArrowUpFromLine,
  },
}

const RECEIPT_STATUS_CONFIG: Record<InventoryReceiptStatus, { color: string; icon: typeof FileText }> = {
  Draft: {
    color: 'bg-gray-100 text-gray-800 border-gray-200 dark:bg-gray-900/30 dark:text-gray-400 dark:border-gray-800',
    icon: FileText,
  },
  Confirmed: {
    color: 'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400 dark:border-blue-800',
    icon: CheckCircle2,
  },
  Cancelled: {
    color: 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800',
    icon: XCircle,
  },
}

export const InventoryReceiptsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Inventory')

  const canWriteInventory = hasPermission(Permissions.InventoryWrite)
  const canManageInventory = hasPermission(Permissions.InventoryManage)

  const [params, setParams] = useState<GetInventoryReceiptsParams>({ page: 1, pageSize: 20 })
  const [typeFilter, setTypeFilter] = useState<string>('all')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()

  const queryParams = useMemo(() => ({
    ...params,
    type: typeFilter !== 'all' ? typeFilter as InventoryReceiptType : undefined,
    status: statusFilter !== 'all' ? statusFilter as InventoryReceiptStatus : undefined,
  }), [params, typeFilter, statusFilter])

  const { data: receiptsResponse, isLoading: loading, error: queryError } = useInventoryReceiptsQuery(queryParams)
  const confirmMutation = useConfirmInventoryReceiptMutation()
  const cancelMutation = useCancelInventoryReceiptMutation()
  const error = queryError?.message ?? null

  const receipts = receiptsResponse?.items ?? []
  const totalCount = receiptsResponse?.totalCount ?? 0
  const totalPages = receiptsResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  // Cancel dialog state
  const [receiptToCancel, setReceiptToCancel] = useState<InventoryReceiptSummaryDto | null>(null)
  const [cancelReason, setCancelReason] = useState('')

  const handleTypeFilter = (value: string) => {
    startFilterTransition(() => {
      setTypeFilter(value)
      setParams((prev) => ({ ...prev, page: 1 }))
    })
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

  const handleConfirm = async (receipt: InventoryReceiptSummaryDto) => {
    try {
      await confirmMutation.mutateAsync(receipt.id)
      toast.success(t('inventory.confirmSuccess', { receiptNumber: receipt.receiptNumber, defaultValue: `Receipt ${receipt.receiptNumber} confirmed` }))
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

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Warehouse}
        title={t('inventory.title', 'Inventory')}
        description={t('inventory.description', 'Manage stock receipts and inventory movements')}
        responsive
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle>{t('inventory.allReceipts', 'All Receipts')}</CardTitle>
              <CardDescription>
                {t('inventory.totalCount', { count: totalCount, defaultValue: `${totalCount} receipts total` })}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center justify-end gap-2">
              {/* Type Filter */}
              <Select value={typeFilter} onValueChange={handleTypeFilter}>
                <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('inventory.filterByType', 'Filter by type')}>
                  <SelectValue placeholder={t('inventory.type', 'Type')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                  <SelectItem value="StockIn" className="cursor-pointer">{t('inventory.type.stockIn', 'Stock In')}</SelectItem>
                  <SelectItem value="StockOut" className="cursor-pointer">{t('inventory.type.stockOut', 'Stock Out')}</SelectItem>
                </SelectContent>
              </Select>
              {/* Status Filter */}
              <Select value={statusFilter} onValueChange={handleStatusFilter}>
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
            </div>
          </div>
        </CardHeader>
        <CardContent className={isFilterPending ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
              {error}
            </div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('inventory.receiptNumber', 'Receipt #')}</TableHead>
                  <TableHead>{t('inventory.type', 'Type')}</TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead className="text-center">{t('inventory.items', 'Items')}</TableHead>
                  <TableHead className="text-right">{t('inventory.totalQuantity', 'Total Qty')}</TableHead>
                  <TableHead className="text-right">{t('inventory.totalCost', 'Total Cost')}</TableHead>
                  <TableHead>{t('labels.date', 'Date')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-12 ml-auto" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-24 ml-auto" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-8 w-8 rounded ml-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : receipts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} className="p-0">
                      <EmptyState
                        icon={Warehouse}
                        title={t('inventory.noReceiptsFound', 'No receipts found')}
                        description={t('inventory.noReceiptsDescription', 'Inventory receipts will appear here when stock movements are recorded.')}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  receipts.map((receipt) => {
                    const typeConfig = RECEIPT_TYPE_CONFIG[receipt.type]
                    const statusConfig = RECEIPT_STATUS_CONFIG[receipt.status]
                    const TypeIcon = typeConfig.icon
                    const StatusIcon = statusConfig.icon
                    const isDraft = receipt.status === 'Draft'

                    return (
                      <TableRow key={receipt.id} className="group transition-colors hover:bg-muted/50">
                        <TableCell>
                          <span className="font-mono font-medium text-sm">{receipt.receiptNumber}</span>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className={typeConfig.color}>
                            <TypeIcon className="h-3 w-3 mr-1.5" />
                            {t(`inventory.type.${receipt.type === 'StockIn' ? 'stockIn' : 'stockOut'}`)}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className={statusConfig.color}>
                            <StatusIcon className="h-3 w-3 mr-1.5" />
                            {t(`inventory.status.${receipt.status.toLowerCase()}`)}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-center">
                          <Badge variant="secondary">{receipt.itemCount}</Badge>
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {receipt.totalQuantity}
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {formatCurrency(receipt.totalCost)}
                        </TableCell>
                        <TableCell>
                          <span className="text-sm text-muted-foreground">
                            {formatDateTime(receipt.createdAt)}
                          </span>
                        </TableCell>
                        <TableCell className="text-right">
                          {isDraft && (canWriteInventory || canManageInventory) ? (
                            <DropdownMenu>
                              <DropdownMenuTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                                  aria-label={t('labels.actionsFor', { name: receipt.receiptNumber, defaultValue: `Actions for ${receipt.receiptNumber}` })}
                                >
                                  <MoreHorizontal className="h-4 w-4" />
                                </Button>
                              </DropdownMenuTrigger>
                              <DropdownMenuContent align="end">
                                {canWriteInventory && (
                                  <DropdownMenuItem
                                    className="cursor-pointer text-green-600 dark:text-green-400"
                                    onClick={() => handleConfirm(receipt)}
                                  >
                                    <CheckCircle2 className="h-4 w-4 mr-2" />
                                    {t('inventory.confirm', 'Confirm')}
                                  </DropdownMenuItem>
                                )}
                                {(canWriteInventory || canManageInventory) && (
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
                              </DropdownMenuContent>
                            </DropdownMenu>
                          ) : (
                            <span className="text-muted-foreground text-xs">-</span>
                          )}
                        </TableCell>
                      </TableRow>
                    )
                  })
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

      {/* Cancel Receipt Dialog */}
      <AlertDialog open={!!receiptToCancel} onOpenChange={(open) => { if (!open) { setReceiptToCancel(null); setCancelReason('') } }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <XCircle className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <AlertDialogTitle>{t('inventory.cancelReceiptTitle', 'Cancel Receipt')}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t('inventory.cancelReceiptDescription', {
                    receiptNumber: receiptToCancel?.receiptNumber,
                    defaultValue: `Are you sure you want to cancel receipt "${receiptToCancel?.receiptNumber}"? This action cannot be undone.`,
                  })}
                </AlertDialogDescription>
              </div>
            </div>
          </AlertDialogHeader>
          <div className="py-4">
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
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">{t('labels.cancel', 'Cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleCancel}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {t('inventory.cancelReceipt', 'Cancel Receipt')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

export default InventoryReceiptsPage
