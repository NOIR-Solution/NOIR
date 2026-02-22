import { useState, useDeferredValue, useMemo, useEffect, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { Search, Building, Plus } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { useUrlDialog } from '@/hooks/useUrlDialog'
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
  const [params, setParams] = useState<GetTenantsParams>({ page: 1, pageSize: 10 })

  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
  const { data, isLoading: loading, error: queryError, refetch: refresh } = useTenantsQuery(queryParams)
  const deleteMutation = useDeleteTenantMutation()
  const error = queryError?.message ?? null

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-tenant' })

  const setPage = (page: number) => startPaginationTransition(() =>
    setParams((prev) => ({ ...prev, page }))
  )
  // URL-synced dialog state: ?edit=tenantId&tab=modules
  const [searchParams, setSearchParams] = useSearchParams()
  const editTenantId = searchParams.get('edit')
  const dialogTab = (searchParams.get('tab') as 'details' | 'modules') || 'details'
  const [editTenantItem, setEditTenantItem] = useState<TenantListItem | null>(null)

  // When data loads and we have an edit param, find the tenant
  useEffect(() => {
    if (editTenantId && data?.items) {
      const found = data.items.find(t => t.id === editTenantId)
      setEditTenantItem(found ?? null)
    } else if (!editTenantId) {
      setEditTenantItem(null)
    }
  }, [editTenantId, data?.items])

  const handleEdit = (tenant: TenantListItem, tab: 'details' | 'modules' = 'details') => {
    setEditTenantItem(tenant)
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      next.set('edit', tenant.id)
      if (tab !== 'details') next.set('tab', tab)
      else next.delete('tab')
      return next
    }, { replace: true })
  }

  const handleDialogClose = () => {
    setEditTenantItem(null)
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      next.delete('edit')
      next.delete('tab')
      return next
    }, { replace: true })
  }

  const handleDialogTabChange = (tab: 'details' | 'modules') => {
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      if (tab !== 'details') next.set('tab', tab)
      else next.delete('tab')
      return next
    }, { replace: true })
  }

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

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Building}
        title={t('tenants.title')}
        description={t('tenants.description')}
        action={
          <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('tenants.createNew')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('tenants.listTitle')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('tenants.searchPlaceholder')}
                  value={searchInput}
                  onChange={(e) => { setSearchInput(e.target.value); setParams((prev) => ({ ...prev, page: 1 })) }}
                  className="pl-9 h-9"
                  aria-label={t('tenants.searchTenants', 'Search tenants')}
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

          <TenantTable
            tenants={data?.items || []}
            onEdit={(t) => handleEdit(t, 'details')}
            onEditModules={(t) => handleEdit(t, 'modules')}
            onDelete={setTenantToDelete}
            onResetPassword={setTenantToResetPassword}
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
