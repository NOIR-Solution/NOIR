import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useDelayedLoading } from '@/hooks/useDelayedLoading'
import {
  Contact,
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
import { createActionsColumn, createFullAuditColumns } from '@/lib/table/columnHelpers'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'
import { useContactsQuery, useDeleteContact } from '@/portal-app/crm/queries'
import type { ContactListDto, ContactSource } from '@/types/crm'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { ContactDialog } from './components/ContactDialog'

const CONTACT_SOURCES: ContactSource[] = ['Web', 'Referral', 'Social', 'Cold', 'Event', 'Other']

const ch = createColumnHelper<ContactListDto>()

export const ContactsPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('CRM Contacts')

  const { getRowAnimationClass, fadeOutRow } = useRowHighlight()

  const canCreate = hasPermission(Permissions.CrmContactsCreate)
  const canUpdate = hasPermission(Permissions.CrmContactsUpdate)
  const canDelete = hasPermission(Permissions.CrmContactsDelete)
  const showActions = canUpdate || canDelete

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    isFilterPending,
    setFilter,
    setSorting,
    setPage,
    setPageSize,
    defaultPageSize,
  } = useTableParams<{ source?: ContactSource }>({ defaultPageSize: 20, tableKey: 'crm-contacts' })

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-crm-contact' })
  const [contactToDelete, setContactToDelete] = useState<ContactListDto | null>(null)
  const deleteMutation = useDeleteContact()

  const { data: contactsResponse, isLoading, isPlaceholderData, error: queryError, refetch } = useContactsQuery(params)
  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'CrmContact',
    onCollectionUpdate: refetch,
  })

  const contacts = contactsResponse?.items ?? []
  const { editItem: contactToEdit, openEdit, closeEdit } = useUrlEditDialog<ContactListDto>(contacts)
  const isContentStale = useDelayedLoading(isSearchStale || isFilterPending || isPlaceholderData)
  const totalCount = contactsResponse?.totalCount ?? 0

  const handleSourceFilter = (value: string) => setFilter('source', value === 'all' ? undefined : (value as ContactSource))

  const handleViewContact = (contact: ContactListDto) => {
    navigate(`/portal/crm/contacts/${contact.id}`)
  }

  const handleDelete = async () => {
    if (!contactToDelete) return
    try {
      await fadeOutRow(contactToDelete.id)
      await deleteMutation.mutateAsync(contactToDelete.id)
      toast.success(t('labels.deletedSuccessfully'))
      setContactToDelete(null)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.unknown'))
    }
  }

  const getSourceBadgeColor = (source: ContactSource) => {
    const colorMap: Record<ContactSource, 'blue' | 'green' | 'purple' | 'gray' | 'amber'> = {
      Web: 'blue',
      Referral: 'green',
      Social: 'purple',
      Cold: 'gray',
      Event: 'amber',
      Other: 'gray',
    }
    return getStatusBadgeClasses(colorMap[source] || 'gray')
  }

  const columns = useMemo((): ColumnDef<ContactListDto, unknown>[] => [
    ...(showActions ? [
      createActionsColumn<ContactListDto>((contact) => (
        <>
          <DropdownMenuItem className="cursor-pointer" onClick={() => handleViewContact(contact)}>
            <Eye className="h-4 w-4 mr-2" />
            {t('labels.viewDetails')}
          </DropdownMenuItem>
          {canUpdate && (
            <DropdownMenuItem className="cursor-pointer" onClick={() => openEdit(contact)}>
              <Pencil className="h-4 w-4 mr-2" />
              {t('labels.edit')}
            </DropdownMenuItem>
          )}
          {canDelete && (
            <DropdownMenuItem
              className="text-destructive cursor-pointer"
              onClick={() => setContactToDelete(contact)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete')}
            </DropdownMenuItem>
          )}
        </>
      )),
    ] : []),
    ch.accessor((row) => `${row.firstName} ${row.lastName}`.trim(), {
      id: 'name',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name')} />,
      meta: { label: t('labels.name') },
      cell: ({ row }) => (
        <span className="font-medium text-sm">{row.original.firstName} {row.original.lastName}</span>
      ),
    }) as ColumnDef<ContactListDto, unknown>,
    ch.accessor('email', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.contacts.email')} />,
      meta: { label: t('crm.contacts.email') },
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue()}</span>,
    }) as ColumnDef<ContactListDto, unknown>,
    ch.accessor('phone', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.contacts.phone')} />,
      meta: { label: t('crm.contacts.phone') },
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue() || '-'}</span>,
    }) as ColumnDef<ContactListDto, unknown>,
    ch.accessor('companyName', {
      id: 'company',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.contacts.company')} />,
      meta: { label: t('crm.contacts.company') },
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue() || '-'}</span>,
    }) as ColumnDef<ContactListDto, unknown>,
    ch.accessor('source', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.contacts.source')} />,
      meta: { label: t('crm.contacts.source') },
      cell: ({ row }) => (
        <Badge variant="outline" className={getSourceBadgeColor(row.original.source)}>
          {t(`crm.sources.${row.original.source}`)}
        </Badge>
      ),
    }) as ColumnDef<ContactListDto, unknown>,
    ch.accessor('leadCount', {
      id: 'leadsCount',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('crm.contacts.leadsCount')} />,
      meta: { label: t('crm.contacts.leadsCount'), align: 'center' },
      enableSorting: false,
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<ContactListDto, unknown>,
    ...createFullAuditColumns<ContactListDto>(t, formatDateTime),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canUpdate, canDelete, showActions, formatDateTime])

  const { table, settings, isCustomized, resetToDefault } = useEnterpriseTable({
    data: contacts,
    columns,
    tableKey: 'crm-contacts',
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
        icon={Contact}
        title={t('crm.contacts.title')}
        description={t('crm.contacts.description')}
        responsive
        action={
          canCreate && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('crm.contacts.create')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('crm.contacts.title')}</CardTitle>
              <CardDescription>
                {contactsResponse ? t('labels.showingCountOfTotal', { count: contacts.length, total: totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('crm.contacts.searchPlaceholder')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              filterSlot={
                <Select value={params.filters.source ?? 'all'} onValueChange={handleSourceFilter}>
                  <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('crm.contacts.filterBySource')}>
                    <SelectValue placeholder={t('crm.contacts.filterBySource')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="cursor-pointer">{t('labels.all')}</SelectItem>
                    {CONTACT_SOURCES.map((source) => (
                      <SelectItem key={source} value={source} className="cursor-pointer">
                        {t(`crm.sources.${source}`)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              }
            />
          </div>
        </CardHeader>
        <CardContent className={isContentStale ? 'space-y-3 opacity-70 transition-opacity duration-200' : 'space-y-3 transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">{error}</div>
          )}

          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isContentStale}
            onRowClick={handleViewContact}
            getRowAnimationClass={getRowAnimationClass}
            emptyState={
              <EmptyState
                icon={Contact}
                title={t('crm.contacts.noContactsFound')}
                description={t('crm.contacts.noContactsDescription')}
                action={canCreate ? {
                  label: t('crm.contacts.create'),
                  onClick: () => openCreate(),
                } : undefined}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      <ContactDialog
        open={isCreateOpen || !!contactToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (contactToEdit) closeEdit()
          }
        }}
        contact={contactToEdit}
      />

      <Credenza open={!!contactToDelete} onOpenChange={(open) => !open && setContactToDelete(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('crm.contacts.deleteConfirmation')}</CredenzaTitle>
                <CredenzaDescription>{t('crm.contacts.deleteConfirmationDescription')}</CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setContactToDelete(null)} disabled={deleteMutation.isPending} className="cursor-pointer">
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

export default ContactsPage
