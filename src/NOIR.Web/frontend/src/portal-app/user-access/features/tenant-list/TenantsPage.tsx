import { useMemo, useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { Building, Plus, Edit, Trash2, KeyRound, Blocks } from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { useRowHighlight } from '@/hooks/useRowHighlight'
import { createActionsColumn, createFullAuditColumns } from '@/lib/table/columnHelpers'
import { useDelayedLoading } from '@/hooks/useDelayedLoading'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
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
} from '@uikit'

import { TenantStatusBadge } from '../../components/tenants/TenantStatusBadge'
import { CreateTenantDialog } from '../../components/tenants/CreateTenantDialog'
import { EditTenantDialog } from '../../components/tenants/EditTenantDialog'
import { DeleteTenantDialog } from '../../components/tenants/DeleteTenantDialog'
import { ResetAdminPasswordDialog } from '../../components/tenants/ResetAdminPasswordDialog'
import { useTenantsQuery, useDeleteTenantMutation } from '@/portal-app/user-access/queries'
import type { TenantListItem } from '@/types'

const ch = createColumnHelper<TenantListItem>()

export const TenantsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Tenants')
  const { formatDate, formatDateTime } = useRegionalSettings()

  const { getRowAnimationClass, fadeOutRow } = useRowHighlight()

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-tenant' })
  const [tenantToDelete, setTenantToDelete] = useState<TenantListItem | null>(null)
  const [tenantToResetPassword, setTenantToResetPassword] = useState<TenantListItem | null>(null)

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    isFilterPending,
    setSorting,
    setPage,
    setPageSize,
    defaultPageSize,
  } = useTableParams({ defaultPageSize: 10, tableKey: 'tenants' })

  const { data, isLoading, isPlaceholderData, refetch: refresh } = useTenantsQuery(params)

  const isContentStale = useDelayedLoading(isSearchStale || isFilterPending || isPlaceholderData)
  const deleteMutation = useDeleteTenantMutation()

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Tenant',
    onCollectionUpdate: refresh,
  })

  // URL-synced edit dialog state with tab support
  const [searchParams, setSearchParams] = useSearchParams()
  const editTenantId = searchParams.get('edit')
  const dialogTab = (searchParams.get('tab') as 'details' | 'modules') || 'details'
  const [editTenantItem, setEditTenantItem] = useState<TenantListItem | null>(null)

  useEffect(() => {
    if (editTenantId && data?.items) {
      const found = data.items.find((ten) => ten.id === editTenantId)
      setEditTenantItem(found ?? null)
    } else if (!editTenantId) {
      setEditTenantItem(null)
    }
  }, [editTenantId, data?.items])

  const handleEdit = (tenant: TenantListItem, tab: 'details' | 'modules' = 'details') => {
    setEditTenantItem(tenant)
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      next.set('edit', tenant.id)
      if (tab !== 'details') next.set('tab', tab)
      else next.delete('tab')
      return next
    }, { replace: true })
  }

  const handleDialogClose = () => {
    setEditTenantItem(null)
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      next.delete('edit')
      next.delete('tab')
      return next
    }, { replace: true })
  }

  const handleDialogTabChange = (tab: 'details' | 'modules') => {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      if (tab !== 'details') next.set('tab', tab)
      else next.delete('tab')
      return next
    }, { replace: true })
  }

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await fadeOutRow(id)
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('tenants.deleteError', 'Failed to delete tenant')
      return { success: false, error: message }
    }
  }

  const columns = useMemo((): ColumnDef<TenantListItem, unknown>[] => [
    createActionsColumn<TenantListItem>((tenant) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => handleEdit(tenant, 'details')}>
          <Edit className="mr-2 h-4 w-4" />
          {t('buttons.edit')}
        </DropdownMenuItem>
        <DropdownMenuItem className="cursor-pointer" onClick={() => handleEdit(tenant, 'modules')}>
          <Blocks className="mr-2 h-4 w-4" />
          {t('tenants.tabs.modules')}
        </DropdownMenuItem>
        <DropdownMenuItem className="cursor-pointer" onClick={() => setTenantToResetPassword(tenant)}>
          <KeyRound className="mr-2 h-4 w-4" />
          {t('tenants.resetAdminPassword')}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          className="text-destructive focus:text-destructive cursor-pointer"
          onClick={() => setTenantToDelete(tenant)}
        >
          <Trash2 className="mr-2 h-4 w-4" />
          {t('buttons.delete')}
        </DropdownMenuItem>
      </>
    )),
    ch.accessor('identifier', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('tenants.table.identifier')} />,
      meta: { label: t('tenants.table.identifier') },
      cell: ({ getValue }) => (
        <span className="font-mono text-sm">{getValue()}</span>
      ),
    }) as ColumnDef<TenantListItem, unknown>,
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('tenants.table.name')} />,
      meta: { label: t('tenants.table.name') },
      cell: ({ row }) => (
        <div>
          <span className="font-medium">{row.original.name || row.original.identifier}</span>
          <span className="block text-xs text-muted-foreground sm:hidden">{row.original.identifier}</span>
        </div>
      ),
    }) as ColumnDef<TenantListItem, unknown>,
    ch.accessor('isActive', {
      id: 'status',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status')} />,
      meta: { label: t('labels.status') },
      cell: ({ getValue }) => <TenantStatusBadge isActive={getValue()} />,
      size: 120,
    }) as ColumnDef<TenantListItem, unknown>,
    ...createFullAuditColumns<TenantListItem>(t, formatDateTime),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, formatDate, formatDateTime])

  const tableData = useMemo(() => data?.items ?? [], [data?.items])

  const { table, settings, isCustomized, resetToDefault } = useEnterpriseTable({
    data: tableData,
    columns,
    tableKey: 'tenants',
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
    getRowId: (row) => row.id,
  })


  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Building}
        title={t('tenants.title')}
        description={t('tenants.description')}
        action={
          <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('tenants.newTenant', 'New Tenant')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('tenants.listTitle')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('tenants.searchPlaceholder')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
            />
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isContentStale}
            onRowClick={(tenant) => handleEdit(tenant, 'details')}
            getRowAnimationClass={getRowAnimationClass}
            emptyState={
              <EmptyState
                icon={Building}
                title={t('tenants.noTenants', 'No tenants found')}
                description={t('tenants.noTenantsDescription', 'Create a new tenant to get started.')}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      <CreateTenantDialog
        open={isCreateOpen}
        onOpenChange={onCreateOpenChange}
        onSuccess={refresh}
      />

      <EditTenantDialog
        tenant={editTenantItem}
        open={!!editTenantId && !!editTenantItem}
        onOpenChange={(open) => !open && handleDialogClose()}
        onSuccess={refresh}
        activeTab={dialogTab}
        onTabChange={handleDialogTabChange}
      />

      <DeleteTenantDialog
        tenant={tenantToDelete}
        open={!!tenantToDelete}
        onOpenChange={(open) => !open && setTenantToDelete(null)}
        onConfirm={handleDelete}
      />

      <ResetAdminPasswordDialog
        tenant={tenantToResetPassword}
        open={!!tenantToResetPassword}
        onOpenChange={(open) => !open && setTenantToResetPassword(null)}
        onSuccess={refresh}
      />
    </div>
  )
}

export default TenantsPage
