import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Building } from 'lucide-react'
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
} from '@uikit'

import { TenantTable } from './components/TenantTable'
import { CreateTenantDialog } from './components/CreateTenantDialog'
import { EditTenantDialog } from './components/EditTenantDialog'
import { DeleteTenantDialog } from './components/DeleteTenantDialog'
import { ResetAdminPasswordDialog } from './components/ResetAdminPasswordDialog'
import { useTenants } from '@/hooks/useTenants'
import type { TenantListItem } from '@/types'

export default function TenantsPage() {
  const { t } = useTranslation('common')
  usePageContext('Tenants')
  const { data, loading, error, refresh, setPage, setSearch, handleDelete, params } = useTenants()

  const [searchInput, setSearchInput] = useState('')
  const [tenantToEdit, setTenantToEdit] = useState<TenantListItem | null>(null)
  const [tenantToDelete, setTenantToDelete] = useState<TenantListItem | null>(null)
  const [tenantToResetPassword, setTenantToResetPassword] = useState<TenantListItem | null>(null)

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setSearch(searchInput)
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
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <PageHeader
        icon={Building}
        title={t('tenants.title')}
        description={t('tenants.description')}
        action={<CreateTenantDialog onSuccess={refresh} />}
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4 backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('tenants.listTitle')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showing') + ' ' + data.items.length + ' ' + t('labels.of') + ' ' + data.totalCount + ' ' + t('labels.items') : ''}
              </CardDescription>
            </div>
            <form onSubmit={handleSearchSubmit} className="flex items-center gap-2">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('tenants.searchPlaceholder')}
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  className="pl-10 w-full sm:w-64"
                  aria-label={t('tenants.searchTenants', 'Search tenants')}
                />
              </div>
              <Button type="submit" variant="secondary">
                {t('buttons.search')}
              </Button>
            </form>
          </div>
        </CardHeader>
        <CardContent>
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
