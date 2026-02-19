import { useState, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Building } from 'lucide-react'
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

import { TenantTable } from '../../components/tenants/TenantTable'
import { CreateTenantDialog } from '../../components/tenants/CreateTenantDialog'
import { EditTenantDialog } from '../../components/tenants/EditTenantDialog'
import { DeleteTenantDialog } from '../../components/tenants/DeleteTenantDialog'
import { ResetAdminPasswordDialog } from '../../components/tenants/ResetAdminPasswordDialog'
import { useTenantsQuery, useDeleteTenantMutation } from '@/portal-app/user-access/queries'
import type { GetTenantsParams } from '@/services/tenants'
import type { TenantListItem } from '@/types'

export const TenantsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Tenants')
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [isPaginationPending, startPaginationTransition] = useTransition()
  const [params, setParams] = useState<GetTenantsParams>({ pageNumber: 1, pageSize: 10 })

  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
  const { data, isLoading: loading, error: queryError, refetch: refresh } = useTenantsQuery(queryParams)
  const deleteMutation = useDeleteTenantMutation()
  const error = queryError?.message ?? null

  const setPage = (page: number) => startPaginationTransition(() =>
    setParams((prev) => ({ ...prev, pageNumber: page }))
  )
  const [tenantToEdit, setTenantToEdit] = useState<TenantListItem | null>(null)
  const [tenantToDelete, setTenantToDelete] = useState<TenantListItem | null>(null)
  const [tenantToResetPassword, setTenantToResetPassword] = useState<TenantListItem | null>(null)

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete tenant'
      return { success: false, error: message }
    }
  }

  const handleEditClick = (tenant: TenantListItem) => {
    setTenantToEdit(tenant)
  }

  const handleDeleteClick = (tenant: TenantListItem) => {
    setTenantToDelete(tenant)
  }

  const handleResetPasswordClick = (tenant: TenantListItem) => {
    setTenantToResetPassword(tenant)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Building}
        title={t('tenants.title')}
        description={t('tenants.description')}
        action={<CreateTenantDialog onSuccess={refresh} />}
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('tenants.listTitle')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showing') + ' ' + data.items.length + ' ' + t('labels.of') + ' ' + data.totalCount + ' ' + t('labels.items') : ''}
              </CardDescription>
            </div>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={t('tenants.searchPlaceholder')}
                value={searchInput}
                onChange={(e) => { setSearchInput(e.target.value); setParams((prev) => ({ ...prev, pageNumber: 1 })) }}
                className="pl-10 w-full sm:w-64"
                aria-label={t('tenants.searchTenants', 'Search tenants')}
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

          <TenantTable
            tenants={data?.items || []}
            onEdit={handleEditClick}
            onDelete={handleDeleteClick}
            onResetPassword={handleResetPasswordClick}
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

      <EditTenantDialog
        tenant={tenantToEdit}
        open={!!tenantToEdit}
        onOpenChange={(open) => !open && setTenantToEdit(null)}
        onSuccess={refresh}
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
