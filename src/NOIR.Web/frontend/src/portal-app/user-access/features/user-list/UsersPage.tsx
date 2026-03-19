import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { Users, Plus, Edit, Trash2, Shield, Lock, LockOpen, ShieldCheck, Activity } from 'lucide-react'
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
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import {
  useUsersQuery,
  useAvailableRolesQuery,
  useLockUserMutation,
  useUnlockUserMutation,
} from '@/portal-app/user-access/queries'
import type { UserListItem } from '@/types'
import { CreateUserDialog } from '../../components/users/CreateUserDialog'
import { EditUserDialog } from '../../components/users/EditUserDialog'
import { DeleteUserDialog } from '../../components/users/DeleteUserDialog'
import { AssignRolesDialog } from '../../components/users/AssignRolesDialog'

const ch = createColumnHelper<UserListItem>()

export const UsersPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Users')

  const canCreateUsers = hasPermission(Permissions.UsersCreate)
  const canEditUsers = hasPermission(Permissions.UsersUpdate)
  const canDeleteUsers = hasPermission(Permissions.UsersDelete)
  const canAssignRoles = hasPermission(Permissions.UsersManageRoles)
  const showActions = canEditUsers || canDeleteUsers || canAssignRoles

  const { getRowAnimationClass } = useRowHighlight()

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-user' })
  const [userToDelete, setUserToDelete] = useState<UserListItem | null>(null)
  const [userForRoles, setUserForRoles] = useState<UserListItem | null>(null)

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    isFilterPending,
    setFilter,
    setPage,
    setPageSize,
    setSorting,
    defaultPageSize,
  } = useTableParams<{ role?: string; isLocked?: boolean }>({ defaultPageSize: 10, tableKey: 'users' })

  const { data, isLoading, isPlaceholderData, error: queryError, refetch: refresh } = useUsersQuery(params)
  const { data: availableRoles = [] } = useAvailableRolesQuery()
  const lockMutation = useLockUserMutation()
  const unlockMutation = useUnlockUserMutation()
  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'User',
    onCollectionUpdate: refresh,
  })

  const handleLock = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await lockMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('users.lockError', 'Failed to lock user')
      return { success: false, error: message }
    }
  }

  const handleUnlock = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await unlockMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('users.unlockError', 'Failed to unlock user')
      return { success: false, error: message }
    }
  }

  const isContentStale = useDelayedLoading(isSearchStale || isFilterPending || isPlaceholderData)

  const setRoleFilter = (value: string) => setFilter('role', value === 'all' ? undefined : value)
  const setLockedFilter = (value: string) => {
    const isLocked = value === 'all' ? undefined : value === 'locked'
    setFilter('isLocked', isLocked)
  }

  const handleViewActivity = (user: UserListItem) => {
    navigate(`/portal/activity-timeline?userId=${encodeURIComponent(user.id)}&userEmail=${encodeURIComponent(user.email)}`)
  }

  const getInitials = (user: UserListItem) => {
    if (user.displayName) return user.displayName.charAt(0).toUpperCase()
    return user.email.charAt(0).toUpperCase()
  }

  const columns = useMemo((): ColumnDef<UserListItem, unknown>[] => [
    ...(showActions ? [
      createActionsColumn<UserListItem>((user) => (
        <>
          {canAssignRoles && (
            <DropdownMenuItem className="cursor-pointer" onClick={() => setUserForRoles(user)}>
              <Shield className="mr-2 h-4 w-4" />
              {t('users.assignRoles', 'Assign Roles')}
            </DropdownMenuItem>
          )}
          {canEditUsers && (
            <DropdownMenuItem className="cursor-pointer" onClick={() => openEditUser(user)}>
              <Edit className="mr-2 h-4 w-4" />
              {t('buttons.edit', 'Edit')}
            </DropdownMenuItem>
          )}
          <DropdownMenuItem className="cursor-pointer" onClick={() => handleViewActivity(user)}>
            <Activity className="mr-2 h-4 w-4" />
            {t('users.viewActivity', 'View Activity')}
          </DropdownMenuItem>
          {canDeleteUsers && (canAssignRoles || canEditUsers) && <DropdownMenuSeparator />}
          {canDeleteUsers && (
            user.isSystemUser ? (
              <DropdownMenuItem disabled className="text-muted-foreground">
                <ShieldCheck className="mr-2 h-4 w-4" />
                {t('users.protectedSystemUser', 'Protected (System User)')}
              </DropdownMenuItem>
            ) : (
              <DropdownMenuItem
                className="text-destructive focus:text-destructive cursor-pointer"
                onClick={() => setUserToDelete(user)}
              >
                <Trash2 className="mr-2 h-4 w-4" />
                {user.isLocked ? t('users.unlock', 'Unlock') : t('users.lock', 'Lock')}
              </DropdownMenuItem>
            )
          )}
        </>
      )),
    ] : []),
    ch.accessor((row) => row.displayName || row.email, {
      id: 'user',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('users.columns.user', 'User')} />,
      meta: { label: t('users.columns.user', 'User') },
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
            <span className="text-primary text-sm font-medium">{getInitials(row.original)}</span>
          </div>
          <div className="flex items-center gap-2">
            <p className="font-medium">{row.original.displayName || row.original.email.split('@')[0]}</p>
            {row.original.isSystemUser && (
              <Badge variant="secondary" className="gap-1 text-xs">
                <ShieldCheck className="h-3 w-3" />
                {t('users.systemUser', 'System')}
              </Badge>
            )}
          </div>
        </div>
      ),
    }) as ColumnDef<UserListItem, unknown>,
    ch.accessor('email', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('users.columns.email', 'Email')} />,
      meta: { label: t('users.columns.email', 'Email') },
      cell: ({ getValue }) => <span className="text-muted-foreground">{getValue()}</span>,
    }) as ColumnDef<UserListItem, unknown>,
    ch.accessor('roles', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('users.columns.roles', 'Roles')} />,
      meta: { label: t('users.columns.roles', 'Roles') },
      enableSorting: false,
      cell: ({ row }) => (
        <div className="flex flex-wrap gap-1">
          {row.original.roles.length > 0 ? (
            row.original.roles.slice(0, 3).map((role) => (
              <Badge key={role} variant="outline" className="text-xs">{role}</Badge>
            ))
          ) : (
            <span className="text-muted-foreground text-sm">-</span>
          )}
          {row.original.roles.length > 3 && (
            <Badge variant="secondary" className="text-xs">+{row.original.roles.length - 3}</Badge>
          )}
        </div>
      ),
    }) as ColumnDef<UserListItem, unknown>,
    ch.accessor('isLocked', {
      id: 'status',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('users.columns.status', 'Status')} />,
      meta: { label: t('users.columns.status', 'Status'), align: 'center' },
      cell: ({ getValue }) => (
        getValue() ? (
          <Badge variant="outline" className={`gap-1 ${getStatusBadgeClasses('red')}`}>
            <Lock className="h-3 w-3" />
            {t('users.locked', 'Locked')}
          </Badge>
        ) : (
          <Badge variant="outline" className={`gap-1 ${getStatusBadgeClasses('green')}`}>
            <LockOpen className="h-3 w-3" />
            {t('labels.active', 'Active')}
          </Badge>
        )
      ),
    }) as ColumnDef<UserListItem, unknown>,
    ...createFullAuditColumns<UserListItem>(t, formatDateTime),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canEditUsers, canDeleteUsers, canAssignRoles, showActions, formatDateTime])

  const users = data?.items ?? []
  const { editItem: userToEdit, openEdit: openEditUser, closeEdit: closeEditUser } = useUrlEditDialog<UserListItem>(users)

  const { table, settings, isCustomized, resetToDefault } = useEnterpriseTable({
    data: users,
    columns,
    tableKey: 'users',
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

  if (queryError) {
    console.error(queryError)
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Users}
        title={t('users.title', 'Users')}
        description={t('users.description', 'Manage platform users and their roles')}
        action={
          canCreateUsers && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('users.newUser', 'New User')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('users.listTitle', 'All Users')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('users.searchPlaceholder', 'Search users...')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              filterSlot={
                <>
                  <Select
                    value={params.filters.role || 'all'}
                    onValueChange={setRoleFilter}
                  >
                    <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('users.filterByRole', 'Filter by role')}>
                      <SelectValue placeholder={t('users.filterByRole', 'Filter by role')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                      {availableRoles.map((role) => (
                        <SelectItem key={role.id} value={role.name} className="cursor-pointer">
                          {role.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Select
                    value={params.filters.isLocked === undefined ? 'all' : params.filters.isLocked ? 'locked' : 'active'}
                    onValueChange={setLockedFilter}
                  >
                    <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('users.filterByStatus', 'Filter by status')}>
                      <SelectValue placeholder={t('users.filterByStatus', 'Status')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                      <SelectItem value="active" className="cursor-pointer">{t('labels.active', 'Active')}</SelectItem>
                      <SelectItem value="locked" className="cursor-pointer">{t('users.locked', 'Locked')}</SelectItem>
                    </SelectContent>
                  </Select>
                </>
              }
            />
          </div>
        </CardHeader>
        <CardContent className={isContentStale ? 'space-y-3 opacity-70 transition-opacity duration-200' : 'space-y-3 transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isContentStale}
            onRowClick={canEditUsers ? openEditUser : undefined}
            getRowAnimationClass={getRowAnimationClass}
            emptyState={
              <EmptyState
                icon={Users}
                title={t('users.noUsers', 'No users found')}
                description={t('users.noUsersDescription', 'No users match your current filters.')}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      <CreateUserDialog
        open={isCreateOpen}
        onOpenChange={onCreateOpenChange}
        onSuccess={refresh}
      />

      <EditUserDialog
        user={userToEdit}
        open={!!userToEdit}
        onOpenChange={(open) => !open && closeEditUser()}
        onSuccess={refresh}
      />

      <DeleteUserDialog
        user={userToDelete}
        open={!!userToDelete}
        onOpenChange={(open) => !open && setUserToDelete(null)}
        onConfirm={userToDelete?.isLocked ? handleUnlock : handleLock}
      />

      <AssignRolesDialog
        user={userForRoles}
        open={!!userForRoles}
        onOpenChange={(open) => !open && setUserForRoles(null)}
        onSuccess={refresh}
      />
    </div>
  )
}

export default UsersPage
