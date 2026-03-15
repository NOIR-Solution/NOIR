import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Building2,
  Eye,
  Loader2,
  Pencil,
  Plus,
  Trash2,
} from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { useRowHighlight } from '@/hooks/useRowHighlight'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { createActionsColumn } from '@/lib/table/columnHelpers'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  EmptyState,
  PageHeader,
} from '@uikit'
import { useCompaniesQuery, useDeleteCompany } from '@/portal-app/crm/queries'
import type { CompanyListDto } from '@/types/crm'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { CompanyDialog } from './components/CompanyDialog'

const ch = createColumnHelper<CompanyListDto>()

export const CompaniesPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('CRM Companies')

  const { getRowAnimationClass, fadeOutRow } = useRowHighlight()

  const canCreate = hasPermission(Permissions.CrmCompaniesCreate)
  const canUpdate = hasPermission(Permissions.CrmCompaniesUpdate)
  const canDelete = hasPermission(Permissions.CrmCompaniesDelete)
  const showActions = canUpdate || canDelete

  const { params, searchInput, setSearchInput, isSearchStale, isFilterPending, setSorting, setPage, setPageSize, defaultPageSize } = useTableParams({ defaultPageSize: 20, tableKey: 'crm-companies' })

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-crm-company' })
  const [companyToDelete, setCompanyToDelete] = useState<CompanyListDto | null>(null)
  const deleteMutation = useDeleteCompany()

  const { data: companiesResponse, isLoading, error: queryError, refetch } = useCompaniesQuery(params)
  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'CrmCompany',
    onCollectionUpdate: refetch,
  })

  const companies = companiesResponse?.items ?? []
  const { editItem: companyToEdit, openEdit, closeEdit } = useUrlEditDialog<CompanyListDto>(companies)
  const totalCount = companiesResponse?.totalCount ?? 0

  const handleViewCompany = (company: CompanyListDto) => {
    navigate(`/portal/crm/companies/${company.id}`)
  }

  const handleDelete = async () => {
    if (!companyToDelete) return
    try {
      await fadeOutRow(companyToDelete.id)
      await deleteMutation.mutateAsync(companyToDelete.id)
      toast.success(t('labels.deletedSuccessfully'))
      setCompanyToDelete(null)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.unknown'))
    }
  }

  const columns = useMemo((): ColumnDef<CompanyListDto, unknown>[] => [
    ...(showActions ? [
      createActionsColumn<CompanyListDto>((company) => (
        <>
          <DropdownMenuItem className="cursor-pointer" onClick={() => handleViewCompany(company)}>
            <Eye className="h-4 w-4 mr-2" />
            {t('labels.viewDetails')}
          </DropdownMenuItem>
          {canUpdate && (
            <DropdownMenuItem className="cursor-pointer" onClick={() => openEdit(company)}>
              <Pencil className="h-4 w-4 mr-2" />
              {t('labels.edit')}
            </DropdownMenuItem>
          )}
          {canDelete && (
            <DropdownMenuItem
              className="text-destructive cursor-pointer"
              onClick={() => setCompanyToDelete(company)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete')}
            </DropdownMenuItem>
          )}
        </>
      )),
    ] : []),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.companies.name')} />,
      meta: { label: t('crm.companies.name') },
      cell: ({ getValue }) => <span className="font-medium text-sm">{getValue()}</span>,
    }) as ColumnDef<CompanyListDto, unknown>,
    ch.accessor('domain', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.companies.domain')} />,
      meta: { label: t('crm.companies.domain') },
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue() || '-'}</span>,
    }) as ColumnDef<CompanyListDto, unknown>,
    ch.accessor('industry', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.companies.industry')} />,
      meta: { label: t('crm.companies.industry') },
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue() || '-'}</span>,
    }) as ColumnDef<CompanyListDto, unknown>,
    ch.accessor('ownerName', {
      id: 'owner',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.companies.owner')} />,
      meta: { label: t('crm.companies.owner') },
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue() || '-'}</span>,
    }) as ColumnDef<CompanyListDto, unknown>,
    ch.accessor('contactCount', {
      id: 'contactCount',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.companies.contactsCount')} />,
      meta: { label: t('crm.companies.contactsCount'), align: 'center' },
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<CompanyListDto, unknown>,
    ch.accessor('createdAt', {
      id: 'created',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.created')} />,
      meta: { label: t('labels.created') },
      cell: ({ getValue }) => (
        <span className="text-sm text-muted-foreground">{formatDateTime(getValue())}</span>
      ),
    }) as ColumnDef<CompanyListDto, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canUpdate, canDelete, showActions])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: companies,
    columns,
    tableKey: 'crm-companies',
    rowCount: totalCount,
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: params.sorting as SortingState,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function' ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize }) : updater
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
        icon={Building2}
        title={t('crm.companies.title')}
        description={t('crm.companies.description')}
        responsive
        action={
          canCreate && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('crm.companies.create')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('crm.companies.title')}</CardTitle>
              <CardDescription>
                {companiesResponse ? t('labels.showingCountOfTotal', { count: companies.length, total: totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('crm.companies.searchPlaceholder')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              density={settings.density}
              onDensityChange={setDensity}
            />
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'space-y-3 opacity-70 transition-opacity duration-200' : 'space-y-3 transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">{error}</div>
          )}

          <DataTable
            table={table}
            density={settings.density}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={handleViewCompany}
            getRowAnimationClass={getRowAnimationClass}
            emptyState={
              <EmptyState
                icon={Building2}
                title={t('crm.companies.noCompaniesFound')}
                description={t('crm.companies.noCompaniesDescription')}
                action={canCreate ? {
                  label: t('crm.companies.create'),
                  onClick: () => openCreate(),
                } : undefined}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      <CompanyDialog
        open={isCreateOpen || !!companyToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (companyToEdit) closeEdit()
          }
        }}
        company={companyToEdit}
      />

      <Credenza open={!!companyToDelete} onOpenChange={(open) => !open && setCompanyToDelete(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('crm.companies.deleteConfirmation')}</CredenzaTitle>
                <CredenzaDescription>{t('crm.companies.deleteConfirmationDescription')}</CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setCompanyToDelete(null)} disabled={deleteMutation.isPending} className="cursor-pointer">
              {t('labels.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {deleteMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('labels.delete')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default CompaniesPage
