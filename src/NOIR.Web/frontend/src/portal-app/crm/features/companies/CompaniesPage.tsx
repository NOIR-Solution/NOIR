import { useState, useEffect, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Building2,
  EllipsisVertical,
  Eye,
  Loader2,
  Pencil,
  Plus,
  Search,
  Trash2,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
  Input,
  Pagination,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { useCompaniesQuery, useDeleteCompany } from '@/portal-app/crm/queries'
import type { GetCompaniesParams, CompanyListDto } from '@/types/crm'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { CompanyDialog } from './components/CompanyDialog'

export const CompaniesPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('CRM Companies')

  const canCreate = hasPermission(Permissions.CrmCompaniesCreate)
  const canUpdate = hasPermission(Permissions.CrmCompaniesUpdate)
  const canDelete = hasPermission(Permissions.CrmCompaniesDelete)

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [isFilterPending, startFilterTransition] = useTransition()
  const [params, setParams] = useState<GetCompaniesParams>({ page: 1, pageSize: 20 })

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-crm-company' })
  const [companyToDelete, setCompanyToDelete] = useState<CompanyListDto | null>(null)
  const deleteMutation = useDeleteCompany()

  useEffect(() => {
    setParams(prev => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  const queryParams = useMemo(() => ({
    ...params,
    search: deferredSearch || undefined,
  }), [params, deferredSearch])

  const { data: companiesResponse, isLoading: loading, error: queryError, refetch } = useCompaniesQuery(queryParams)
  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'CrmCompany',
    onCollectionUpdate: refetch,
  })

  const companies = companiesResponse?.items ?? []
  const { editItem: companyToEdit, openEdit, closeEdit } = useUrlEditDialog<CompanyListDto>(companies)
  const totalCount = companiesResponse?.totalCount ?? 0
  const totalPages = companiesResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  const handlePageChange = (page: number) => {
    startFilterTransition(() => {
      setParams(prev => ({ ...prev, page }))
    })
  }

  const handleViewCompany = (company: CompanyListDto) => {
    navigate(`/portal/crm/companies/${company.id}`)
  }

  const handleDelete = async () => {
    if (!companyToDelete) return
    try {
      await deleteMutation.mutateAsync(companyToDelete.id)
      toast.success(t('labels.deletedSuccessfully'))
      setCompanyToDelete(null)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.unknown'))
    }
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{t('crm.companies.title')}</h1>
          <p className="text-muted-foreground">{t('crm.companies.description')}</p>
        </div>
        {canCreate && (
          <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('crm.companies.create')}
          </Button>
        )}
      </div>

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('crm.companies.title')}</CardTitle>
              <CardDescription>
                {t('labels.showingCountOfTotal', { count: companies.length, total: totalCount })}
              </CardDescription>
            </div>
            <div className="relative flex-1 min-w-[200px]">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={t('crm.companies.searchPlaceholder')}
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="pl-9 h-9"
                aria-label={t('crm.companies.searchPlaceholder')}
              />
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">{error}</div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10" />
                  <TableHead>{t('crm.companies.name')}</TableHead>
                  <TableHead>{t('crm.companies.domain')}</TableHead>
                  <TableHead>{t('crm.companies.industry')}</TableHead>
                  <TableHead>{t('crm.companies.owner')}</TableHead>
                  <TableHead className="text-center">{t('crm.companies.contactsCount')}</TableHead>
                  <TableHead>{t('labels.created')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                    </TableRow>
                  ))
                ) : companies.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="p-0">
                      <EmptyState
                        icon={Building2}
                        title={t('crm.companies.noCompaniesFound')}
                        description={t('crm.companies.noCompaniesDescription')}
                        action={canCreate ? {
                          label: t('crm.companies.create'),
                          onClick: () => openCreate(),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  companies.map((company) => (
                    <TableRow
                      key={company.id}
                      className="group transition-colors hover:bg-muted/50 cursor-pointer"
                      onClick={() => handleViewCompany(company)}
                    >
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: company.name })}
                              onClick={(e) => e.stopPropagation()}
                            >
                              <EllipsisVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="start">
                            <DropdownMenuItem className="cursor-pointer" onClick={(e) => { e.stopPropagation(); handleViewCompany(company) }}>
                              <Eye className="h-4 w-4 mr-2" />
                              {t('labels.viewDetails')}
                            </DropdownMenuItem>
                            {canUpdate && (
                              <DropdownMenuItem className="cursor-pointer" onClick={(e) => { e.stopPropagation(); openEdit(company) }}>
                                <Pencil className="h-4 w-4 mr-2" />
                                {t('labels.edit')}
                              </DropdownMenuItem>
                            )}
                            {canDelete && (
                              <DropdownMenuItem className="text-destructive cursor-pointer" onClick={(e) => { e.stopPropagation(); setCompanyToDelete(company) }}>
                                <Trash2 className="h-4 w-4 mr-2" />
                                {t('labels.delete')}
                              </DropdownMenuItem>
                            )}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                      <TableCell><span className="font-medium text-sm">{company.name}</span></TableCell>
                      <TableCell><span className="text-sm text-muted-foreground">{company.domain || '-'}</span></TableCell>
                      <TableCell><span className="text-sm text-muted-foreground">{company.industry || '-'}</span></TableCell>
                      <TableCell><span className="text-sm text-muted-foreground">{company.ownerName || '-'}</span></TableCell>
                      <TableCell className="text-center"><Badge variant="secondary">{company.contactCount}</Badge></TableCell>
                      <TableCell><span className="text-sm text-muted-foreground">{formatDateTime(company.createdAt)}</span></TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {totalPages > 1 && (
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalItems={totalCount}
              pageSize={params.pageSize || 20}
              onPageChange={handlePageChange}
              showPageSizeSelector={false}
              className="mt-4"
            />
          )}
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
