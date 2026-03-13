import { useState, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Crown,
  Eye,
  Loader2,
  Pencil,
  Plus,
  Trash2,
  TrendingUp,
  UserCheck,
  UserX,
  Users,
} from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, RowSelectionState, SortingState } from '@tanstack/react-table'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useServerTable, useSelectedIds } from '@/hooks/useServerTable'
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
  DropdownMenuSeparator,
  EmptyState,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'
import { useCustomersQuery, useCustomerStatsQuery, useBulkActivateCustomers, useBulkDeactivateCustomers, useBulkDeleteCustomers } from '@/portal-app/customers/queries'
import type { CustomerSegment, CustomerSummaryDto, CustomerTier } from '@/types/customer'
import { formatCurrency } from '@/lib/utils/currency'
import { CustomerFormDialog } from '../../components/CustomerFormDialog'
import { CustomerImportExport } from '../../components/CustomerImportExport'
import { DeleteCustomerDialog } from '../../components/DeleteCustomerDialog'
import { getSegmentBadgeClass, getTierBadgeClass } from '@/portal-app/customers/utils/customerStatus'

const CUSTOMER_SEGMENTS: CustomerSegment[] = ['New', 'Active', 'AtRisk', 'Dormant', 'Lost', 'VIP']
const CUSTOMER_TIERS: CustomerTier[] = ['Standard', 'Silver', 'Gold', 'Platinum', 'Diamond']

const ch = createColumnHelper<CustomerSummaryDto>()

export const CustomersPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  usePageContext('Customers')

  const canCreate = hasPermission(Permissions.CustomersCreate)
  const canUpdate = hasPermission(Permissions.CustomersUpdate)
  const canDelete = hasPermission(Permissions.CustomersDelete)
  const canManage = hasPermission(Permissions.CustomersManage)

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-customer' })
  const [customerToDelete, setCustomerToDelete] = useState<CustomerSummaryDto | null>(null)
  const [showBulkDeleteConfirm, setShowBulkDeleteConfirm] = useState(false)
  const [segmentFilter, setSegmentFilter] = useState<string>('all')
  const [tierFilter, setTierFilter] = useState<string>('all')
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})
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
  } = useTableParams({ defaultPageSize: 20 })

  const queryParams = useMemo(() => ({
    ...params,
    segment: segmentFilter !== 'all' ? segmentFilter as CustomerSegment : undefined,
    tier: tierFilter !== 'all' ? tierFilter as CustomerTier : undefined,
  }), [params, segmentFilter, tierFilter])

  const { data, isLoading, error: queryError, refetch: refresh } = useCustomersQuery(queryParams)
  const { data: stats } = useCustomerStatsQuery()

  const customers = data?.items ?? []
  const { editItem: customerToEdit, openEdit: openEditCustomer, closeEdit: closeEditCustomer } = useUrlEditDialog<CustomerSummaryDto>(customers)

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Customer',
    onCollectionUpdate: refresh,
  })

  const bulkActivateMutation = useBulkActivateCustomers()
  const bulkDeactivateMutation = useBulkDeactivateCustomers()
  const bulkDeleteMutation = useBulkDeleteCustomers()

  const vipCount = stats?.segmentDistribution.find(s => s.segment === 'VIP')?.count ?? 0
  const avgSpent = stats?.topSpenders && stats.topSpenders.length > 0
    ? stats.topSpenders.reduce((sum, c) => sum + c.totalSpent, 0) / stats.topSpenders.length
    : 0

  const handleSegmentFilter = (value: string) => startFilterTransition(() => { setSegmentFilter(value); setPage(1) })
  const handleTierFilter = (value: string) => startFilterTransition(() => { setTierFilter(value); setPage(1) })

  const selectedIds = useSelectedIds(rowSelection)
  const selectedCount = selectedIds.length

  const selectedInactiveCount = useMemo(
    () => customers.filter(c => rowSelection[c.id] && !c.isActive).length,
    [customers, rowSelection]
  )
  const selectedActiveCount = useMemo(
    () => customers.filter(c => rowSelection[c.id] && c.isActive).length,
    [customers, rowSelection]
  )

  const columns = useMemo((): ColumnDef<CustomerSummaryDto, unknown>[] => [
    createActionsColumn<CustomerSummaryDto>((customer) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => navigate(`/portal/ecommerce/customers/${customer.id}`)}>
          <Eye className="h-4 w-4 mr-2" />
          {t('labels.viewDetails', 'View Details')}
        </DropdownMenuItem>
        {canUpdate && (
          <DropdownMenuItem className="cursor-pointer" onClick={() => openEditCustomer(customer)}>
            <Pencil className="h-4 w-4 mr-2" />
            {t('labels.edit', 'Edit')}
          </DropdownMenuItem>
        )}
        {canDelete && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-destructive focus:text-destructive cursor-pointer"
              onClick={() => setCustomerToDelete(customer)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete', 'Delete')}
            </DropdownMenuItem>
          </>
        )}
      </>
    )),
    createSelectColumn<CustomerSummaryDto>(),
    ch.accessor('firstName', {
      id: 'name',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      cell: ({ row }) => (
        <span className="font-medium text-sm">
          {row.original.firstName} {row.original.lastName}
        </span>
      ),
    }) as ColumnDef<CustomerSummaryDto, unknown>,
    ch.accessor('email', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.email', 'Email')} />,
      enableSorting: false,
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue()}</span>,
    }) as ColumnDef<CustomerSummaryDto, unknown>,
    ch.accessor('phone', {
      header: t('labels.phone', 'Phone'),
      enableSorting: false,
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue() || '-'}</span>,
    }) as ColumnDef<CustomerSummaryDto, unknown>,
    ch.accessor('segment', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('customers.segmentLabel', 'Segment')} />,
      enableSorting: false,
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getSegmentBadgeClass(getValue())}>
          {t(`customers.segment.${getValue().toLowerCase()}`, getValue())}
        </Badge>
      ),
    }) as ColumnDef<CustomerSummaryDto, unknown>,
    ch.accessor('tier', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('customers.tierLabel', 'Tier')} />,
      enableSorting: false,
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getTierBadgeClass(getValue())}>
          {t(`customers.tier.${getValue().toLowerCase()}`, getValue())}
        </Badge>
      ),
    }) as ColumnDef<CustomerSummaryDto, unknown>,
    ch.accessor('totalOrders', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('customers.ordersLabel', 'Orders')} />,
      enableSorting: false,
      meta: { align: 'center', label: t('customers.ordersLabel', 'Orders') },
      size: 90,
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<CustomerSummaryDto, unknown>,
    ch.accessor('totalSpent', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('customers.totalSpent', 'Total Spent')} />,
      enableSorting: false,
      meta: { align: 'right', label: t('customers.totalSpent', 'Total Spent') },
      cell: ({ getValue }) => <span className="font-medium text-sm">{formatCurrency(getValue(), 'VND')}</span>,
    }) as ColumnDef<CustomerSummaryDto, unknown>,
    ch.accessor('loyaltyPoints', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('customers.loyaltyPoints', 'Points')} />,
      enableSorting: false,
      meta: { align: 'center', label: t('customers.loyaltyPoints', 'Points') },
      size: 80,
      cell: ({ getValue }) => <Badge variant="secondary">{getValue().toLocaleString()}</Badge>,
    }) as ColumnDef<CustomerSummaryDto, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canUpdate, canDelete])

  const tableData = useMemo(() => data?.items ?? [], [data?.items])

  const table = useServerTable({
    data: tableData,
    columns,
    rowCount: data?.totalCount ?? 0,
    columnVisibilityStorageKey: 'customers',
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: params.sorting as SortingState,
      rowSelection,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function'
        ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize })
        : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    onSortingChange: setSorting,
    onRowSelectionChange: setRowSelection,
    enableRowSelection: true,
    getRowId: (row) => row.id,
  })

  const onBulkActivate = () => {
    if (selectedCount === 0) return
    const inactiveIds = customers.filter(c => rowSelection[c.id] && !c.isActive).map(c => c.id)
    if (inactiveIds.length === 0) {
      toast.warning(t('customers.noInactiveSelected', 'No inactive customers selected'))
      return
    }
    startBulkTransition(async () => {
      try {
        const result = await bulkActivateMutation.mutateAsync(inactiveIds)
        if (result.failed > 0) {
          toast.warning(t('customers.bulkActivatePartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} activated, ${result.failed} failed` }))
        } else {
          toast.success(t('customers.bulkActivateSuccess', { count: result.success, defaultValue: `${result.success} customers activated` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('customers.bulkActivateFailed', 'Failed to activate customers'))
      }
      table.resetRowSelection()
    })
  }

  const onBulkDeactivate = () => {
    if (selectedCount === 0) return
    const activeIds = customers.filter(c => rowSelection[c.id] && c.isActive).map(c => c.id)
    if (activeIds.length === 0) {
      toast.warning(t('customers.noActiveSelected', 'No active customers selected'))
      return
    }
    startBulkTransition(async () => {
      try {
        const result = await bulkDeactivateMutation.mutateAsync(activeIds)
        if (result.failed > 0) {
          toast.warning(t('customers.bulkDeactivatePartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} deactivated, ${result.failed} failed` }))
        } else {
          toast.success(t('customers.bulkDeactivateSuccess', { count: result.success, defaultValue: `${result.success} customers deactivated` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('customers.bulkDeactivateFailed', 'Failed to deactivate customers'))
      }
      table.resetRowSelection()
    })
  }

  const onBulkDelete = () => {
    if (selectedCount === 0) return
    setShowBulkDeleteConfirm(true)
  }

  const handleBulkDeleteConfirm = () => {
    const selectedCustomerIds = [...selectedIds]
    startBulkTransition(async () => {
      try {
        const result = await bulkDeleteMutation.mutateAsync(selectedCustomerIds)
        if (result.failed > 0) {
          toast.warning(t('customers.bulkDeletePartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} deleted, ${result.failed} failed` }))
        } else {
          toast.success(t('customers.bulkDeleteSuccess', { count: result.success, defaultValue: `${result.success} customers deleted` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('customers.bulkDeleteFailed', 'Failed to delete customers'))
      }
      table.resetRowSelection()
      setShowBulkDeleteConfirm(false)
    })
  }

  if (queryError) {
    console.error(queryError)
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Users}
        title={t('customers.title', 'Customers')}
        description={t('customers.description', 'Manage your customer base and loyalty programs')}
        responsive
        action={
          <div className="flex items-center gap-2">
            <CustomerImportExport
              totalCount={data?.totalCount ?? 0}
              onImportComplete={() => {/* refetch handled by query invalidation */}}
            />
            {canCreate && (
              <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
                <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
                {t('customers.newCustomer', 'New Customer')}
              </Button>
            )}
          </div>
        }
      />

      {/* Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
                <Users className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t('customers.totalCustomers', 'Total Customers')}</p>
                <p className="text-2xl font-bold">{stats?.totalCustomers ?? 0}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-green-500/10 border border-green-500/20">
                <UserCheck className="h-5 w-5 text-green-600 dark:text-green-400" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t('customers.activeCustomers', 'Active Customers')}</p>
                <p className="text-2xl font-bold">{stats?.activeCustomers ?? 0}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-purple-500/10 border border-purple-500/20">
                <Crown className="h-5 w-5 text-purple-600 dark:text-purple-400" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t('customers.vipCustomers', 'VIP Customers')}</p>
                <p className="text-2xl font-bold">{vipCount}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-amber-500/10 border border-amber-500/20">
                <TrendingUp className="h-5 w-5 text-amber-600 dark:text-amber-400" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t('customers.avgSpend', 'Avg Top Spend')}</p>
                <p className="text-2xl font-bold">{formatCurrency(avgSpent, 'VND')}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('customers.allCustomers', 'All Customers')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('customers.searchPlaceholder', 'Search customers...')}
              isSearchStale={isSearchStale}
              onResetColumnVisibility={table.resetColumnVisibility}
              filterSlot={
                <>
                  <Select value={segmentFilter} onValueChange={handleSegmentFilter}>
                    <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('customers.filterBySegment', 'Filter by segment')}>
                      <SelectValue placeholder={t('customers.filterBySegment', 'Segment')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                      {CUSTOMER_SEGMENTS.map((segment) => (
                        <SelectItem key={segment} value={segment} className="cursor-pointer">
                          {t(`customers.segment.${segment.toLowerCase()}`, segment)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Select value={tierFilter} onValueChange={handleTierFilter}>
                    <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('customers.filterByTier', 'Filter by tier')}>
                      <SelectValue placeholder={t('customers.filterByTier', 'Tier')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                      {CUSTOMER_TIERS.map((tier) => (
                        <SelectItem key={tier} value={tier} className="cursor-pointer">
                          {t(`customers.tier.${tier.toLowerCase()}`, tier)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </>
              }
            />
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <BulkActionToolbar selectedCount={selectedCount} onClearSelection={() => table.resetRowSelection()}>
            {canManage && selectedInactiveCount > 0 && (
              <Button
                variant="outline"
                size="sm"
                onClick={onBulkActivate}
                disabled={isBulkPending}
                className="cursor-pointer text-emerald-600 border-emerald-200 hover:bg-emerald-50 dark:text-emerald-400 dark:border-emerald-800 dark:hover:bg-emerald-950"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <UserCheck className="h-4 w-4 mr-2" />}
                {t('customers.activateCount', { count: selectedInactiveCount, defaultValue: `Activate (${selectedInactiveCount})` })}
              </Button>
            )}
            {canManage && selectedActiveCount > 0 && (
              <Button
                variant="outline"
                size="sm"
                onClick={onBulkDeactivate}
                disabled={isBulkPending}
                className="cursor-pointer text-amber-600 border-amber-200 hover:bg-amber-50 dark:text-amber-400 dark:border-amber-800 dark:hover:bg-amber-950"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <UserX className="h-4 w-4 mr-2" />}
                {t('customers.deactivateCount', { count: selectedActiveCount, defaultValue: `Deactivate (${selectedActiveCount})` })}
              </Button>
            )}
            {canDelete && (
              <Button
                variant="outline"
                size="sm"
                onClick={onBulkDelete}
                disabled={isBulkPending}
                className="cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Trash2 className="h-4 w-4 mr-2" />}
                {t('customers.deleteCount', { count: selectedCount, defaultValue: `Delete (${selectedCount})` })}
              </Button>
            )}
          </BulkActionToolbar>

          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={selectedCount === 0 ? (customer) => navigate(`/portal/ecommerce/customers/${customer.id}`) : undefined}
            emptyState={
              <EmptyState
                icon={Users}
                title={t('customers.noCustomersFound', 'No customers found')}
                description={t('customers.noCustomersDescription', 'Get started by creating your first customer.')}
                action={canCreate ? {
                  label: t('customers.addCustomer', 'Add Customer'),
                  onClick: () => openCreate(),
                } : undefined}
              />
            }
          />

          <DataTablePagination table={table} showPageSizeSelector={false} />
        </CardContent>
      </Card>

      <CustomerFormDialog
        open={isCreateOpen || !!customerToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (customerToEdit) closeEditCustomer()
          }
        }}
        customer={customerToEdit}
      />

      <DeleteCustomerDialog
        open={!!customerToDelete}
        onOpenChange={(open) => !open && setCustomerToDelete(null)}
        customer={customerToDelete}
      />

      <Credenza open={showBulkDeleteConfirm} onOpenChange={setShowBulkDeleteConfirm}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('customers.bulkDeleteConfirmTitle', { count: selectedCount, defaultValue: `Delete ${selectedCount} customers` })}</CredenzaTitle>
                <CredenzaDescription>
                  {t('customers.bulkDeleteConfirmDescription', {
                    count: selectedCount,
                    defaultValue: `Are you sure you want to delete ${selectedCount} customers? This action cannot be undone.`,
                  })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button
              variant="outline"
              onClick={() => setShowBulkDeleteConfirm(false)}
              disabled={isBulkPending}
              className="cursor-pointer"
            >
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleBulkDeleteConfirm}
              disabled={isBulkPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {isBulkPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isBulkPending
                ? t('labels.deleting', 'Deleting...')
                : t('customers.deleteCount', { count: selectedCount, defaultValue: `Delete (${selectedCount})` })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default CustomersPage
