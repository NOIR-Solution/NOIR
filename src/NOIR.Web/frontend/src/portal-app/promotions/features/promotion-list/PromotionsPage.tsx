import { useState, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Percent, Plus, Eye, Play, Pause, Trash2 } from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useServerTable } from '@/hooks/useServerTable'
import { createActionsColumn } from '@/lib/table/columnHelpers'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  DropdownMenuSeparator,
  EmptyState,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { usePromotionsQuery, useActivatePromotionMutation, useDeactivatePromotionMutation } from '@/portal-app/promotions/queries'
import type { PromotionDto, PromotionStatus, PromotionType } from '@/types/promotion'
import { PromotionFormDialog } from '../../components/PromotionFormDialog'
import { DeletePromotionDialog } from '../../components/DeletePromotionDialog'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { toast } from 'sonner'

const PROMOTION_STATUSES: PromotionStatus[] = ['Draft', 'Active', 'Scheduled', 'Expired', 'Cancelled']
const PROMOTION_TYPES: PromotionType[] = ['VoucherCode', 'FlashSale', 'BundleDeal', 'FreeShipping']

const statusBadgeColors: Record<PromotionStatus, 'green' | 'gray' | 'blue' | 'orange' | 'red'> = {
  Active: 'green',
  Draft: 'gray',
  Scheduled: 'blue',
  Expired: 'orange',
  Cancelled: 'red',
}

const formatDiscountValue = (dto: PromotionDto, t: (...args: unknown[]) => string): string => {
  switch (dto.discountType) {
    case 'Percentage': return `${dto.discountValue}%`
    case 'FixedAmount': return `${dto.discountValue.toLocaleString()}`
    case 'FreeShipping': return t('promotions.discountType.freeShipping', 'Free Shipping')
    case 'BuyXGetY': return t('promotions.discountType.buyXGetY', 'Buy X Get Y')
    default: return String(dto.discountValue)
  }
}

const ch = createColumnHelper<PromotionDto>()

export const PromotionsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Promotions')

  const canWrite = hasPermission(Permissions.PromotionsWrite)
  const canDelete = hasPermission(Permissions.PromotionsDelete)

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-promotion' })
  const [promotionToDelete, setPromotionToDelete] = useState<PromotionDto | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [typeFilter, setTypeFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    setSorting,
    setPage,
    setPageSize,
  } = useTableParams({ defaultPageSize: 20 })

  const queryParams = useMemo(() => ({
    ...params,
    status: statusFilter !== 'all' ? statusFilter as PromotionStatus : undefined,
    promotionType: typeFilter !== 'all' ? typeFilter as PromotionType : undefined,
  }), [params, statusFilter, typeFilter])

  const { data, isLoading, error: queryError, refetch: refresh } = usePromotionsQuery(queryParams)
  const activateMutation = useActivatePromotionMutation()
  const deactivateMutation = useDeactivatePromotionMutation()

  const promotions = data?.items ?? []
  const { editItem: promotionToEdit, openEdit: openEditPromotion, closeEdit: closeEditPromotion } = useUrlEditDialog<PromotionDto>(promotions)

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Promotion',
    onCollectionUpdate: refresh,
  })

  const handleStatusFilter = (value: string) => startFilterTransition(() => { setStatusFilter(value); setPage(1) })
  const handleTypeFilter = (value: string) => startFilterTransition(() => { setTypeFilter(value); setPage(1) })

  const handleActivate = async (promotion: PromotionDto) => {
    try {
      await activateMutation.mutateAsync(promotion.id)
      toast.success(t('promotions.activateSuccess', 'Promotion activated successfully'))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('promotions.activateError', 'Failed to activate promotion'))
    }
  }

  const handleDeactivate = async (promotion: PromotionDto) => {
    try {
      await deactivateMutation.mutateAsync(promotion.id)
      toast.success(t('promotions.deactivateSuccess', 'Promotion deactivated successfully'))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('promotions.deactivateError', 'Failed to deactivate promotion'))
    }
  }

  const columns = useMemo((): ColumnDef<PromotionDto, unknown>[] => [
    createActionsColumn<PromotionDto>((promotion) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => openEditPromotion(promotion)}>
          <Eye className="h-4 w-4 mr-2" />
          {canWrite ? t('labels.edit', 'Edit') : t('labels.viewDetails', 'View Details')}
        </DropdownMenuItem>
        {canWrite && promotion.status !== 'Active' && promotion.status !== 'Expired' && promotion.status !== 'Cancelled' && (
          <DropdownMenuItem className="cursor-pointer" onClick={() => handleActivate(promotion)}>
            <Play className="h-4 w-4 mr-2" />
            {t('promotions.activate', 'Activate')}
          </DropdownMenuItem>
        )}
        {canWrite && promotion.status === 'Active' && (
          <DropdownMenuItem className="cursor-pointer" onClick={() => handleDeactivate(promotion)}>
            <Pause className="h-4 w-4 mr-2" />
            {t('promotions.deactivate', 'Deactivate')}
          </DropdownMenuItem>
        )}
        {canDelete && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-destructive focus:text-destructive cursor-pointer"
              onClick={() => setPromotionToDelete(promotion)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete', 'Delete')}
            </DropdownMenuItem>
          </>
        )}
      </>
    )),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      cell: ({ row }) => (
        <div>
          <span className="font-medium">{row.original.name}</span>
          {row.original.description && (
            <p className="text-sm text-muted-foreground line-clamp-1 mt-0.5">{row.original.description}</p>
          )}
        </div>
      ),
    }) as ColumnDef<PromotionDto, unknown>,
    ch.accessor('code', {
      header: t('promotions.code', 'Code'),
      enableSorting: false,
      cell: ({ getValue }) => (
        <code className="text-sm bg-muted px-1.5 py-0.5 rounded font-mono">{getValue()}</code>
      ),
    }) as ColumnDef<PromotionDto, unknown>,
    ch.accessor('promotionType', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('promotions.type.label', 'Type')} />,
      enableSorting: false,
      meta: { label: t('promotions.type.label', 'Type') },
      cell: ({ getValue }) => (
        <Badge variant="outline">{t(`promotions.type.${getValue().toLowerCase()}`, getValue())}</Badge>
      ),
    }) as ColumnDef<PromotionDto, unknown>,
    ch.accessor('discountValue', {
      id: 'discount',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('promotions.discount', 'Discount')} />,
      enableSorting: false,
      meta: { label: t('promotions.discount', 'Discount') },
      cell: ({ row }) => (
        <span className="font-medium text-sm">{formatDiscountValue(row.original, t as unknown as (...args: unknown[]) => string)}</span>
      ),
    }) as ColumnDef<PromotionDto, unknown>,
    ch.accessor('status', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status', 'Status')} />,
      enableSorting: false,
      size: 120,
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getStatusBadgeClasses(statusBadgeColors[getValue()])}>
          {t(`promotions.status.${getValue().toLowerCase()}`, getValue())}
        </Badge>
      ),
    }) as ColumnDef<PromotionDto, unknown>,
    ch.accessor('startDate', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('promotions.startDate', 'Start Date')} />,
      enableSorting: false,
      meta: { label: t('promotions.startDate', 'Start Date') },
      size: 150,
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{formatDateTime(getValue())}</span>,
    }) as ColumnDef<PromotionDto, unknown>,
    ch.accessor('endDate', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('promotions.endDate', 'End Date')} />,
      enableSorting: false,
      meta: { label: t('promotions.endDate', 'End Date') },
      size: 150,
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{formatDateTime(getValue())}</span>,
    }) as ColumnDef<PromotionDto, unknown>,
    ch.accessor('currentUsageCount', {
      id: 'usage',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('promotions.usage', 'Usage')} />,
      enableSorting: false,
      meta: { align: 'center', label: t('promotions.usage', 'Usage') },
      size: 90,
      cell: ({ row }) => (
        <Badge variant="secondary">
          {row.original.currentUsageCount}
          {row.original.usageLimitTotal != null ? `/${row.original.usageLimitTotal}` : ''}
        </Badge>
      ),
    }) as ColumnDef<PromotionDto, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canWrite, canDelete, formatDateTime])

  const tableData = useMemo(() => data?.items ?? [], [data?.items])

  const table = useServerTable({
    data: tableData,
    columns,
    rowCount: data?.totalCount ?? 0,
    columnVisibilityStorageKey: 'promotions',
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
    getRowId: (row) => row.id,
  })

  if (queryError) {
    console.error(queryError)
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Percent}
        title={t('promotions.title', 'Promotions')}
        description={t('promotions.description', 'Manage promotions, vouchers, and discount campaigns')}
        action={
          canWrite && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('promotions.newPromotion', 'New Promotion')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">{t('promotions.allPromotions', 'All Promotions')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <DataTableToolbar
            table={table}
            searchInput={searchInput}
            onSearchChange={setSearchInput}
            searchPlaceholder={t('promotions.searchPlaceholder', 'Search promotions...')}
            isSearchStale={isSearchStale}
            onResetColumnVisibility={table.resetColumnVisibility}
            filterSlot={
              <>
                <Select value={statusFilter} onValueChange={handleStatusFilter}>
                  <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('promotions.filterByStatus', 'Filter by status')}>
                    <SelectValue placeholder={t('promotions.filterByStatus', 'Filter status')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                    {PROMOTION_STATUSES.map((status) => (
                      <SelectItem key={status} value={status} className="cursor-pointer">
                        {t(`promotions.status.${status.toLowerCase()}`, status)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Select value={typeFilter} onValueChange={handleTypeFilter}>
                  <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('promotions.filterByType', 'Filter by type')}>
                    <SelectValue placeholder={t('promotions.filterByType', 'Filter type')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                    {PROMOTION_TYPES.map((type) => (
                      <SelectItem key={type} value={type} className="cursor-pointer">
                        {t(`promotions.type.${type.toLowerCase()}`, type)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </>
            }
          />

          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={openEditPromotion}
            emptyState={
              <EmptyState
                icon={Percent}
                title={t('promotions.noPromotionsFound', 'No promotions found')}
                description={t('promotions.noPromotionsDescription', 'Get started by creating your first promotion.')}
                action={canWrite ? {
                  label: t('promotions.addPromotion', 'Add Promotion'),
                  onClick: () => openCreate(),
                } : undefined}
              />
            }
          />

          <DataTablePagination table={table} showPageSizeSelector={false} />
        </CardContent>
      </Card>

      <PromotionFormDialog
        open={isCreateOpen || !!promotionToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (promotionToEdit) closeEditPromotion()
          }
        }}
        promotion={promotionToEdit}
        onSuccess={() => refresh()}
      />

      <DeletePromotionDialog
        promotion={promotionToDelete}
        onOpenChange={(open) => !open && setPromotionToDelete(null)}
        onSuccess={() => refresh()}
      />
    </div>
  )
}

export default PromotionsPage
