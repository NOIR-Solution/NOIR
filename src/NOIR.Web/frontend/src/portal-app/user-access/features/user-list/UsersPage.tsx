import { useState, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Users, Plus } from 'lucide-react'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { usePageContext } from '@/hooks/usePageContext'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Input,
  PageHeader,
  Pagination,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'

import { UserTable } from '../../components/users/UserTable'
import { CreateUserDialog } from '../../components/users/CreateUserDialog'
import { EditUserDialog } from '../../components/users/EditUserDialog'
import { DeleteUserDialog } from '../../components/users/DeleteUserDialog'
import { AssignRolesDialog } from '../../components/users/AssignRolesDialog'
import {
  useUsersQuery,
  useAvailableRolesQuery,
  useLockUserMutation,
  useUnlockUserMutation,
  type UsersParams,
} from '@/portal-app/user-access/queries'
import type { UserListItem } from '@/types'

export const UsersPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Users')
  const [isFilterPending, startFilterTransition] = useTransition()
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [params, setParams] = useState<UsersParams>({ page: 1, pageSize: 10 })
  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
  const { data, isLoading: loading, error: queryError, refetch: refresh } = useUsersQuery(queryParams)
  const { data: availableRoles = [] } = useAvailableRolesQuery()
  const lockMutation = useLockUserMutation()
  const unlockMutation = useUnlockUserMutation()
  const error = queryError?.message ?? null

  const setPage = (page: number) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, page }))
  )
  const setRoleFilter = (role: string) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, role: role || undefined, page: 1 }))
  )
  const setLockedFilter = (isLocked: boolean | undefined) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, isLocked, page: 1 }))
  )

  const handleLock = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await lockMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to lock user'
      return { success: false, error: message }
    }
  }

  const handleUnlock = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await unlockMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to unlock user'
      return { success: false, error: message }
    }
  }

  // Permission checks
  const canCreateUsers = hasPermission(Permissions.UsersCreate)
  const canEditUsers = hasPermission(Permissions.UsersUpdate)
  const canDeleteUsers = hasPermission(Permissions.UsersDelete)
  const canAssignRoles = hasPermission(Permissions.UsersManageRoles)

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-user' })
  const { editItem: userToEdit, openEdit: openEditUser, closeEdit: closeEditUser } = useUrlEditDialog<UserListItem>(data?.items)
  const [userToDelete, setUserToDelete] = useState<UserListItem | null>(null)
  const [userForRoles, setUserForRoles] = useState<UserListItem | null>(null)

  const handleEditClick = (user: UserListItem) => {
    openEditUser(user)
  }

  const handleDeleteClick = (user: UserListItem) => {
    setUserToDelete(user)
  }

  const handleRolesClick = (user: UserListItem) => {
    setUserForRoles(user)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Users}
        title={t('users.title', 'Users')}
        description={t('users.description', 'Manage platform users and their roles')}
        action={
          canCreateUsers && (
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('users.createUser', 'Create User')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('users.listTitle', 'All Users')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {/* Search */}
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('users.searchPlaceholder', 'Search users...')}
                  value={searchInput}
                  onChange={(e) => { setSearchInput(e.target.value); setParams((prev) => ({ ...prev, page: 1 })) }}
                  className="pl-9 h-9"
                  aria-label={t('users.searchUsers', 'Search users')}
                />
              </div>
              {/* Role Filter */}
              <Select
                value={params.role || 'all'}
                onValueChange={(value) => setRoleFilter(value === 'all' ? '' : value)}
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
              {/* Status Filter */}
              <Select
                value={params.isLocked === undefined ? 'all' : params.isLocked ? 'locked' : 'active'}
                onValueChange={(value) => {
                  if (value === 'all') setLockedFilter(undefined)
                  else if (value === 'locked') setLockedFilter(true)
                  else setLockedFilter(false)
                }}
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
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <UserTable
            users={data?.items || []}
            onEdit={handleEditClick}
            onDelete={handleDeleteClick}
            onAssignRoles={handleRolesClick}
            loading={loading}
            canEdit={canEditUsers}
            canDelete={canDeleteUsers}
            canAssignRoles={canAssignRoles}
          />

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <Pagination
              currentPage={data.pageNumber}
              totalPages={data.totalPages}
              totalItems={data.totalCount}
              pageSize={params.pageSize || 10}
              onPageChange={setPage}
              showPageSizeSelector={false}
              className="mt-4"
            />
          )}
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
