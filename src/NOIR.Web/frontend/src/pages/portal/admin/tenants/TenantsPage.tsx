import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Search } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { TenantTable } from './components/TenantTable'
import { CreateTenantDialog } from './components/CreateTenantDialog'
import { EditTenantDialog } from './components/EditTenantDialog'
import { DeleteTenantDialog } from './components/DeleteTenantDialog'
import { useTenants } from '@/hooks/useTenants'
import type { TenantListItem } from '@/types'

export default function TenantsPage() {
  const { t } = useTranslation('common')
  const { data, loading, error, refresh, setPage, setSearch, handleDelete, params } = useTenants()

  const [searchInput, setSearchInput] = useState('')
  const [tenantToEdit, setTenantToEdit] = useState<TenantListItem | null>(null)
  const [tenantToDelete, setTenantToDelete] = useState<TenantListItem | null>(null)

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

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('tenants.title')}</h1>
          <p className="text-muted-foreground">{t('tenants.description')}</p>
        </div>
        <CreateTenantDialog onSuccess={refresh} />
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>{t('tenants.listTitle')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showing') + ' ' + data.items.length + ' ' + t('labels.of') + ' ' + data.totalCount + ' ' + t('labels.items') : ''}
              </CardDescription>
            </div>
            <form onSubmit={handleSearchSubmit} className="flex items-center space-x-2">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('tenants.searchPlaceholder')}
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  className="pl-10 w-64"
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
            loading={loading}
          />

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <p className="text-sm text-muted-foreground">
                {t('labels.page')} {data.pageNumber} {t('labels.of')} {data.totalPages}
              </p>
              <div className="flex items-center space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(params.pageNumber! - 1)}
                  disabled={!data.hasPreviousPage}
                >
                  {t('buttons.previous')}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(params.pageNumber! + 1)}
                  disabled={!data.hasNextPage}
                >
                  {t('buttons.next')}
                </Button>
              </div>
            </div>
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
    </div>
  )
}
