import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Users, Filter, Plus } from 'lucide-react'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { usePageContext } from '@/hooks/usePageContext'
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
import { useUsers, useAvailableRoles } from '@/portal-app/user-access/states/useUsers'
import type { UserListItem } from '@/types'

export default function UsersPage() {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Users')
  const {
    data,
    loading,
    error,
    refresh,
    setPage,
    setSearch,
    setRoleFilter,
    setLockedFilter,
    handleLock,
    handleUnlock,
    params
  } = useUsers()
  const { roles: availableRoles } = useAvailableRoles()

  // Permission checks
  const canCreateUsers = hasPermission(Permissions.UsersCreate)
  const canEditUsers = hasPermission(Permissions.UsersUpdate)
  const canDeleteUsers = hasPermission(Permissions.UsersDelete)
  const canAssignRoles = hasPermission(Permissions.PermissionsAssign)

  const [searchInput, setSearchInput] = useState('')
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [userToEdit, setUserToEdit] = useState<UserListItem | null>(null)
  const [userToDelete, setUserToDelete] = useState<UserListItem | null>(null)
  const [userForRoles, setUserForRoles] = useState<UserListItem | null>(null)

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setSearch(searchInput)
  }

  const handleEditClick = (user: UserListItem) => {
    setUserToEdit(user)
  }

  const handleDeleteClick = (user: UserListItem) => {
    setUserToDelete(user)
  }

  const handleRolesClick = (user: UserListItem) => {
    setUserForRoles(user)
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <PageHeader
        icon={Users}
        title={t('users.title', 'Users')}
        description={t('users.description', 'Manage platform users and their roles')}
        action={
          canCreateUsers && (
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={() => setCreateDialogOpen(true)}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('users.createUser', 'Create User')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4 backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('users.listTitle', 'All Users')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
              {/* Role Filter */}
              <Select
                value={params.role || 'all'}
                onValueChange={(value) => setRoleFilter(value === 'all' ? '' : value)}
              >
                <SelectTrigger className="w-[140px] cursor-pointer" aria-label={t('users.filterByRole', 'Filter by role')}>
                  <Filter className="h-4 w-4 mr-2" />
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
                <SelectTrigger className="w-[120px] cursor-pointer" aria-label={t('users.filterByStatus', 'Filter by status')}>
                  <SelectValue placeholder={t('users.filterByStatus', 'Status')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                  <SelectItem value="active" className="cursor-pointer">{t('labels.active', 'Active')}</SelectItem>
                  <SelectItem value="locked" className="cursor-pointer">{t('users.locked', 'Locked')}</SelectItem>
                </SelectContent>
              </Select>

              {/* Search */}
              <form onSubmit={handleSearchSubmit} className="flex items-center gap-2">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    placeholder={t('users.searchPlaceholder', 'Search users...')}
                    value={searchInput}
                    onChange={(e) => setSearchInput(e.target.value)}
                    className="pl-10 w-full sm:w-64"
                    aria-label={t('users.searchUsers', 'Search users')}
                  />
                </div>
                <Button type="submit" variant="secondary">
                  {t('buttons.search', 'Search')}
                </Button>
              </form>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
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
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        onSuccess={refresh}
      />

      <EditUserDialog
        user={userToEdit}
        open={!!userToEdit}
        onOpenChange={(open) => !open && setUserToEdit(null)}
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
