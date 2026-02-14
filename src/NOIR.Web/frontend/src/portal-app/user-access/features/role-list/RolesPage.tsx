import { useState, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Shield } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Input,
  PageHeader,
  Pagination,
} from '@uikit'

import { RoleTable } from '../../components/roles/RoleTable'
import { CreateRoleDialog } from '../../components/roles/CreateRoleDialog'
import { EditRoleDialog } from '../../components/roles/EditRoleDialog'
import { DeleteRoleDialog } from '../../components/roles/DeleteRoleDialog'
import { PermissionsDialog } from '../../components/roles/PermissionsDialog'
import { useRolesQuery, useDeleteRoleMutation, type RolesParams } from '@/portal-app/user-access/queries'
import type { RoleListItem } from '@/types'

export const RolesPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Roles')
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [isPaginationPending, startPaginationTransition] = useTransition()
  const [params, setParams] = useState<RolesParams>({ page: 1, pageSize: 10 })

  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
  const { data, isLoading: loading, error: queryError, refetch: refresh } = useRolesQuery(queryParams)
  const deleteMutation = useDeleteRoleMutation()
  const error = queryError?.message ?? null

  const setPage = (page: number) => startPaginationTransition(() =>
    setParams((prev) => ({ ...prev, page }))
  )
  const [roleToEdit, setRoleToEdit] = useState<RoleListItem | null>(null)
  const [roleToDelete, setRoleToDelete] = useState<RoleListItem | null>(null)
  const [roleForPermissions, setRoleForPermissions] = useState<RoleListItem | null>(null)

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete role'
      return { success: false, error: message }
    }
  }

  const handleEditClick = (role: RoleListItem) => {
    setRoleToEdit(role)
  }

  const handleDeleteClick = (role: RoleListItem) => {
    setRoleToDelete(role)
  }

  const handlePermissionsClick = (role: RoleListItem) => {
    setRoleForPermissions(role)
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <PageHeader
        icon={Shield}
        title={t('roles.title', 'Roles')}
        description={t('roles.description', 'Manage roles and permissions')}
        action={<CreateRoleDialog onSuccess={refresh} />}
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4 backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('roles.listTitle', 'All Roles')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={t('roles.searchPlaceholder', 'Search roles...')}
                value={searchInput}
                onChange={(e) => { setSearchInput(e.target.value); setParams((prev) => ({ ...prev, page: 1 })) }}
                className="pl-10 w-full sm:w-64"
                aria-label={t('roles.searchRoles', 'Search roles')}
              />
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isPaginationPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
              {error}
            </div>
          )}

          <RoleTable
            roles={data?.items || []}
            onEdit={handleEditClick}
            onDelete={handleDeleteClick}
            onPermissions={handlePermissionsClick}
            loading={loading}
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

      <EditRoleDialog
        role={roleToEdit}
        open={!!roleToEdit}
        onOpenChange={(open) => !open && setRoleToEdit(null)}
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
