import { useMemo, useState, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Shield, Plus, Key, Edit, Trash2 } from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { useRowHighlight } from '@/hooks/useRowHighlight'
import { createActionsColumn, createFullAuditColumns } from '@/lib/table/columnHelpers'
import { useDelayedLoading } from '@/hooks/useDelayedLoading'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  Badge,
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'

import { CreateRoleDialog } from '../../components/roles/CreateRoleDialog'
import { EditRoleDialog } from '../../components/roles/EditRoleDialog'
import { DeleteRoleDialog } from '../../components/roles/DeleteRoleDialog'
import { PermissionsDialog } from '../../components/roles/PermissionsDialog'
import { useRolesQuery, useDeleteRoleMutation } from '@/portal-app/user-access/queries'
import type { RoleListItem } from '@/types'

const ch = createColumnHelper<RoleListItem>()

const SYSTEM_ROLE_DESCRIPTION_KEYS: Record<string, string> = {
  admin: 'roles.descriptions.admin',
  user: 'roles.descriptions.user',
}

export const RolesPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Roles')

  const canCreate = hasPermission(Permissions.RolesCreate)
  const canUpdate = hasPermission(Permissions.RolesUpdate)
  const canDelete = hasPermission(Permissions.RolesDelete)
  const canManagePermissions = hasPermission(Permissions.RolesManagePermissions)

  const { getRowAnimationClass, fadeOutRow } = useRowHighlight()

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-role' })
  const [roleToDelete, setRoleToDelete] = useState<RoleListItem | null>(null)
  const [roleForPermissions, setRoleForPermissions] = useState<RoleListItem | null>(null)
  const [typeFilter, setTypeFilter] = useState<string>('all')
  const [isFilterPendingTransition, startFilterTransition] = useTransition()

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
  } = useTableParams({ defaultPageSize: 10, tableKey: 'roles' })

  const { data, isLoading, isPlaceholderData, refetch: refresh } = useRolesQuery(params)
  const { editItem: roleToEdit, openEdit: openEditRole, closeEdit: closeEditRole } = useUrlEditDialog<RoleListItem>(data?.items)
  const deleteMutation = useDeleteRoleMutation()

  const isContentStale = useDelayedLoading(isSearchStale || isFilterPending || isFilterPendingTransition || isPlaceholderData)

  const handleTypeFilter = (value: string) => {
    startFilterTransition(() => setTypeFilter(value))
  }

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Role',
    onCollectionUpdate: refresh,
  })

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await fadeOutRow(id)
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('roles.deleteError', 'Failed to delete role')
      return { success: false, error: message }
    }
  }

  const columns = useMemo((): ColumnDef<RoleListItem, unknown>[] => [
    createActionsColumn<RoleListItem>((role) => (
      <>
        {canManagePermissions && (
          <DropdownMenuItem className="cursor-pointer" onClick={() => setRoleForPermissions(role)}>
            <Key className="mr-2 h-4 w-4" />
            {t('roles.managePermissions', 'Manage Permissions')}
          </DropdownMenuItem>
        )}
        {canUpdate && (
          <DropdownMenuItem className="cursor-pointer" onClick={() => openEditRole(role)}>
            <Edit className="mr-2 h-4 w-4" />
            {t('buttons.edit', 'Edit')}
          </DropdownMenuItem>
        )}
        {canDelete && !role.isSystemRole && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-destructive focus:text-destructive cursor-pointer"
              onClick={() => setRoleToDelete(role)}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              {t('buttons.delete', 'Delete')}
            </DropdownMenuItem>
          </>
        )}
      </>
    )),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('roles.columns.name', 'Name')} />,
      meta: { label: t('roles.columns.name', 'Name') },
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div
            className="w-8 h-8 rounded-full flex items-center justify-center shrink-0"
            style={{ backgroundColor: row.original.color || '#6b7280' }}
          >
            <Shield className="h-4 w-4 text-white" />
          </div>
          <div>
            <p className="font-medium">{row.original.name}</p>
            {row.original.parentRoleId && (
              <p className="text-xs text-muted-foreground">{t('roles.inheritsFrom', 'Inherits permissions')}</p>
            )}
          </div>
        </div>
      ),
    }) as ColumnDef<RoleListItem, unknown>,
    ch.accessor('description', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('roles.columns.description', 'Description')} />,
      meta: { label: t('roles.columns.description', 'Description') },
      cell: ({ row }) => {
        const descKey = row.original.isSystemRole
          ? SYSTEM_ROLE_DESCRIPTION_KEYS[row.original.name.toLowerCase()]
          : undefined
        const description = descKey ? t(descKey) : (row.original.description || '-')
        return <span className="text-muted-foreground line-clamp-2">{description}</span>
      },
    }) as ColumnDef<RoleListItem, unknown>,
    ch.accessor('permissionCount', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('roles.columns.permissions', 'Permissions')} />,
      enableSorting: false,
      meta: { align: 'center', label: t('roles.columns.permissions', 'Permissions') },
      size: 120,
      cell: ({ getValue }) => (
        <div className="flex items-center justify-center gap-1">
          <Key className="h-4 w-4 text-muted-foreground" />
          <span>{getValue()}</span>
        </div>
      ),
    }) as ColumnDef<RoleListItem, unknown>,
    ch.accessor('userCount', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('roles.columns.users', 'Users')} />,
      enableSorting: false,
      meta: { align: 'center', label: t('roles.columns.users', 'Users') },
      size: 90,
      cell: ({ getValue }) => <span>{getValue()}</span>,
    }) as ColumnDef<RoleListItem, unknown>,
    ch.accessor('isSystemRole', {
      id: 'type',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('roles.columns.type', 'Type')} />,
      meta: { align: 'center', label: t('roles.columns.type', 'Type') },
      size: 100,
      cell: ({ getValue }) => getValue() ? (
        <Badge variant="secondary">{t('roles.system', 'System')}</Badge>
      ) : (
        <Badge variant="outline">{t('roles.custom', 'Custom')}</Badge>
      ),
    }) as ColumnDef<RoleListItem, unknown>,
    ...createFullAuditColumns<RoleListItem>(t, formatDateTime),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canUpdate, canDelete, canManagePermissions, formatDateTime])

  const tableData = useMemo(() => {
    const items = data?.items ?? []
    if (typeFilter === 'all') return items
    return items.filter((r) => typeFilter === 'system' ? r.isSystemRole : !r.isSystemRole)
  }, [data?.items, typeFilter])

  const { table, settings, isCustomized, resetToDefault } = useEnterpriseTable({
    data: tableData,
    columns,
    tableKey: 'roles',
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
        icon={Shield}
        title={t('roles.title', 'Roles')}
        description={t('roles.description', 'Manage roles and permissions')}
        action={
          canCreate && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('roles.newRole', 'New Role')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('roles.listTitle', 'All Roles')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: tableData.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('roles.searchPlaceholder', 'Search roles...')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              filterSlot={
                <Select value={typeFilter} onValueChange={handleTypeFilter}>
                  <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('roles.filterByType', 'Filter by type')}>
                    <SelectValue placeholder={t('roles.filterByType', 'Type')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                    <SelectItem value="system" className="cursor-pointer">{t('roles.system', 'System')}</SelectItem>
                    <SelectItem value="custom" className="cursor-pointer">{t('roles.custom', 'Custom')}</SelectItem>
                  </SelectContent>
                </Select>
              }
            />
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isContentStale}
            onRowClick={canUpdate ? openEditRole : undefined}
            getRowAnimationClass={getRowAnimationClass}
            emptyState={
              <EmptyState
                icon={Shield}
                title={t('roles.noRoles', 'No roles found')}
                description={t('roles.noRolesDescription', 'Create a new role to get started.')}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      <CreateRoleDialog
        open={isCreateOpen}
        onOpenChange={onCreateOpenChange}
        onSuccess={refresh}
      />

      <EditRoleDialog
        role={roleToEdit}
        open={!!roleToEdit}
        onOpenChange={(open) => !open && closeEditRole()}
        onSuccess={refresh}
      />

      <DeleteRoleDialog
        role={roleToDelete}
        open={!!roleToDelete}
        onOpenChange={(open) => !open && setRoleToDelete(null)}
        onConfirm={handleDelete}
      />

      <PermissionsDialog
        role={roleForPermissions}
        open={!!roleForPermissions}
        onOpenChange={(open) => !open && setRoleForPermissions(null)}
        onSuccess={refresh}
      />
    </div>
  )
}

export default RolesPage
