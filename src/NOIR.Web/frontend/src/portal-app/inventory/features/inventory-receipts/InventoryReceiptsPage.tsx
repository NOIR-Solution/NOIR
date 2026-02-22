import { useState, useMemo, useDeferredValue, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Warehouse,
  CheckCircle2,
  Eye,
  Loader2,
  Search,
  XCircle,
  EllipsisVertical,
} from 'lucide-react'
import { toast } from 'sonner'
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
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  EmptyState,
  Input,
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
import { RECEIPT_TYPE_CONFIG, RECEIPT_STATUS_CONFIG } from './inventoryReceiptConfig'
import { InventoryReceiptDetailDialog } from './InventoryReceiptDetailDialog'

export const InventoryReceiptsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Inventory')

  const canWriteInventory = hasPermission(Permissions.InventoryWrite)
  const canManageInventory = hasPermission(Permissions.InventoryManage)

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [params, setParams] = useState<GetInventoryReceiptsParams>({ page: 1, pageSize: 20 })
  const [typeFilter, setTypeFilter] = useState<string>('all')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()

  const queryParams = useMemo(() => ({
    ...params,
    search: deferredSearch || undefined,
    type: typeFilter !== 'all' ? typeFilter as InventoryReceiptType : undefined,
    status: statusFilter !== 'all' ? statusFilter as InventoryReceiptStatus : undefined,
  }), [params, deferredSearch, typeFilter, statusFilter])

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

  // Confirm dialog state
  const [receiptToConfirm, setReceiptToConfirm] = useState<InventoryReceiptSummaryDto | null>(null)

  // Detail dialog state
  const [selectedReceiptId, setSelectedReceiptId] = useState<string | undefined>(undefined)

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
              <CardTitle className="text-lg">{t('inventory.allReceipts', 'All Receipts')}</CardTitle>
              <CardDescription>
                {t('inventory.totalCount', { count: totalCount, defaultValue: `${totalCount} receipts total` })}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {/* Search */}
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground pointer-events-none" />
                <Input
                  placeholder={t('inventory.searchPlaceholder')}
                  value={searchInput}
                  onChange={(e) => { setSearchInput(e.target.value); setParams((prev) => ({ ...prev, page: 1 })) }}
                  className="pl-9 h-9"
                  aria-label={t('inventory.searchPlaceholder')}
                />
              </div>
              {/* Type Filter */}
              <Select value={typeFilter} onValueChange={handleTypeFilter}>
                <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('inventory.filterByType', 'Filter by type')}>
                  <SelectValue placeholder={t('labels.type', 'Type')} />
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
                  <TableHead>{t('inventory.receiptNumber', 'Receipt #')}</TableHead>
                  <TableHead>{t('labels.type', 'Type')}</TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead className="text-center">{t('inventory.items', 'Items')}</TableHead>
                  <TableHead className="text-right">{t('inventory.totalQuantity', 'Total Qty')}</TableHead>
                  <TableHead className="text-right">{t('inventory.totalCost', 'Total Cost')}</TableHead>
                  <TableHead>{t('labels.date', 'Date')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-12 ml-auto" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-24 ml-auto" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-28" /></TableCell>
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
                      <TableRow
                        key={receipt.id}
                        className="group transition-colors hover:bg-muted/50 cursor-pointer"
                        onClick={() => setSelectedReceiptId(receipt.id)}
                      >
                        <TableCell className="sticky left-0 z-10 bg-background" onClick={(e) => e.stopPropagation()}>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button
                                variant="ghost"
                                size="sm"
                                className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                                aria-label={t('labels.actionsFor', { name: receipt.receiptNumber, defaultValue: `Actions for ${receipt.receiptNumber}` })}
                              >
                                <EllipsisVertical className="h-4 w-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="start">
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
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </TableCell>
                        <TableCell>
                          <span className="font-mono font-medium text-sm">{receipt.receiptNumber}</span>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className={typeConfig.color}>
                            <TypeIcon className="h-3 w-3 mr-1.5" />
                            {t(`inventory.type.${typeConfig.label}`)}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className={statusConfig.color}>
                            <StatusIcon className="h-3 w-3 mr-1.5" />
                            {t(`inventory.status.${statusConfig.label}`)}
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

      {/* Receipt Detail Dialog */}
      <InventoryReceiptDetailDialog
        receiptId={selectedReceiptId}
        open={!!selectedReceiptId}
        onOpenChange={(open) => { if (!open) setSelectedReceiptId(undefined) }}
      />
    </div>
  )
}

export default InventoryReceiptsPage
