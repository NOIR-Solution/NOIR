import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { UsersRound, Plus, Eye, Trash2, Loader2 } from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
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
  EmptyState,
  PageHeader,
} from '@uikit'

import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useCustomerGroupsQuery, useDeleteCustomerGroupMutation } from '@/portal-app/customer-groups/queries'
import { CustomerGroupDialog } from '../../components/CustomerGroupDialog'
import type { CustomerGroupListItem } from '@/types/customerGroup'

import { toast } from 'sonner'

const ch = createColumnHelper<CustomerGroupListItem>()

export const CustomerGroupsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('CustomerGroups')

  const canCreateGroups = hasPermission(Permissions.CustomerGroupsCreate)
  const canUpdateGroups = hasPermission(Permissions.CustomerGroupsUpdate)
  const canDeleteGroups = hasPermission(Permissions.CustomerGroupsDelete)
  const showActions = canUpdateGroups || canDeleteGroups

  const { params, searchInput, setSearchInput, isSearchStale, isFilterPending, setSorting, setPage, setPageSize, defaultPageSize } = useTableParams({ defaultPageSize: 20, tableKey: 'customer-groups' })
  const { data: groupsResponse, isLoading, error: queryError, refetch: refresh } = useCustomerGroupsQuery(params)
  const deleteMutation = useDeleteCustomerGroupMutation()

  const groups = groupsResponse?.items ?? []
  const { editItem: groupToEdit, openEdit: openEditGroup, closeEdit: closeEditGroup } = useUrlEditDialog<CustomerGroupListItem>(groups)
  const [groupToDelete, setGroupToDelete] = useState<CustomerGroupListItem | null>(null)
  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-group' })

  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'CustomerGroup',
    onCollectionUpdate: refresh,
  })

  const handleDelete = async () => {
    if (!groupToDelete) return
    try {
      await deleteMutation.mutateAsync(groupToDelete.id)
      toast.success(t('customerGroups.deleteSuccess', 'Customer group deleted successfully'))
      setGroupToDelete(null)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('customerGroups.deleteError', 'Failed to delete customer group')
      toast.error(message)
    }
  }

  const columns = useMemo((): ColumnDef<CustomerGroupListItem, unknown>[] => [
    ...(showActions ? [
      createActionsColumn<CustomerGroupListItem>((group) => (
        <>
          <DropdownMenuItem className="cursor-pointer" onClick={() => openEditGroup(group)}>
            <Eye className="h-4 w-4 mr-2" />
            {canUpdateGroups ? t('labels.edit', 'Edit') : t('labels.viewDetails', 'View Details')}
          </DropdownMenuItem>
          {canDeleteGroups && (
            <DropdownMenuItem
              className="text-destructive cursor-pointer"
              onClick={() => setGroupToDelete(group)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete', 'Delete')}
            </DropdownMenuItem>
          )}
        </>
      )),
    ] : []),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      meta: { label: t('labels.name', 'Name') },
      cell: ({ row }) => (
        <div>
          <span className="font-medium">{row.original.name}</span>
          {row.original.description && (
            <p className="text-sm text-muted-foreground line-clamp-1 mt-0.5">
              {row.original.description}
            </p>
          )}
        </div>
      ),
    }) as ColumnDef<CustomerGroupListItem, unknown>,
    ch.accessor('slug', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.slug', 'Slug')} />,
      meta: { label: t('labels.slug', 'Slug') },
      cell: ({ getValue }) => <code className="text-sm bg-muted px-1.5 py-0.5 rounded">{getValue()}</code>,
    }) as ColumnDef<CustomerGroupListItem, unknown>,
    ch.accessor('isActive', {
      id: 'status',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status', 'Status')} />,
      meta: { label: t('labels.status', 'Status') },
      cell: ({ row }) => (
        <Badge variant="outline" className={getStatusBadgeClasses(row.original.isActive ? 'green' : 'gray')}>
          {row.original.isActive ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
        </Badge>
      ),
    }) as ColumnDef<CustomerGroupListItem, unknown>,
    ch.accessor('memberCount', {
      id: 'members',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('customerGroups.members', 'Members')} />,
      meta: { label: t('customerGroups.members', 'Members'), align: 'center' },
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<CustomerGroupListItem, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canUpdateGroups, canDeleteGroups, showActions])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: groups,
    columns,
    rowCount: groupsResponse?.totalCount ?? 0,
    tableKey: 'customer-groups',
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: params.sorting as SortingState,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function' ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize }) : updater
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
        icon={UsersRound}
        title={t('customerGroups.title', 'Customer Groups')}
        description={t('customerGroups.description', 'Manage customer groups for segmentation and targeting')}
        responsive
        action={
          canCreateGroups && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('customerGroups.newGroup', 'New Group')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('customerGroups.allGroups', 'All Groups')}</CardTitle>
              <CardDescription>
                {groupsResponse ? t('labels.showingCountOfTotal', { count: groups.length, total: groupsResponse.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('customerGroups.searchPlaceholder', 'Search groups...')}
              isSearchStale={isSearchStale || isFilterPending}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              density={settings.density}
              onDensityChange={setDensity}
            />
          </div>
        </CardHeader>
        <CardContent className={`space-y-3 ${(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}`}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <DataTable
            table={table}
            density={settings.density}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={openEditGroup}
            emptyState={
              <EmptyState
                icon={UsersRound}
                title={t('customerGroups.noGroupsFound', 'No customer groups found')}
                description={t('customerGroups.noGroupsDescription', 'Get started by creating your first customer group.')}
                action={canCreateGroups ? { label: t('customerGroups.addGroup', 'Add Group'), onClick: () => openCreate() } : undefined}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      <CustomerGroupDialog
        open={isCreateOpen || !!groupToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (groupToEdit) closeEditGroup()
          }
        }}
        group={groupToEdit}
        onSuccess={() => refresh()}
      />

      <Credenza open={!!groupToDelete} onOpenChange={(open) => !open && setGroupToDelete(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('customerGroups.deleteTitle', 'Delete Customer Group')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('customerGroups.deleteDescription', {
                    name: groupToDelete?.name,
                    defaultValue: `Are you sure you want to delete "${groupToDelete?.name}"? This action cannot be undone.`,
                  })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setGroupToDelete(null)} disabled={deleteMutation.isPending} className="cursor-pointer">
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {deleteMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {deleteMutation.isPending ? t('labels.deleting', 'Deleting...') : t('labels.delete', 'Delete')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default CustomerGroupsPage
