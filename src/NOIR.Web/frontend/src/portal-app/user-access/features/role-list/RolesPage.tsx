import { useState, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Shield, Plus } from 'lucide-react'
import { Button } from '@uikit'
import { usePageContext } from '@/hooks/usePageContext'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
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

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-role' })

  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
  const { data, isLoading: loading, error: queryError, refetch: refresh } = useRolesQuery(queryParams)
  const deleteMutation = useDeleteRoleMutation()
  const error = queryError?.message ?? null

  const setPage = (page: number) => startPaginationTransition(() =>
    setParams((prev) => ({ ...prev, page }))
  )
  const { editItem: roleToEdit, openEdit: openEditRole, closeEdit: closeEditRole } = useUrlEditDialog<RoleListItem>(data?.items)
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
    openEditRole(role)
  }

  const handleDeleteClick = (role: RoleListItem) => {
    setRoleToDelete(role)
  }

  const handlePermissionsClick = (role: RoleListItem) => {
    setRoleForPermissions(role)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Shield}
        title={t('roles.title', 'Roles')}
        description={t('roles.description', 'Manage roles and permissions')}
        action={
          <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('roles.create', 'Create Role')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('roles.listTitle', 'All Roles')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('roles.searchPlaceholder', 'Search roles...')}
                  value={searchInput}
                  onChange={(e) => { setSearchInput(e.target.value); setParams((prev) => ({ ...prev, page: 1 })) }}
                  className="pl-9 h-9"
                  aria-label={t('roles.searchRoles', 'Search roles')}
                />
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isPaginationPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
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
