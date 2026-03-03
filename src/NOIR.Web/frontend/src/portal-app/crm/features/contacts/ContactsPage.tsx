import { useState, useEffect, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Contact,
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { useContactsQuery, useDeleteContact } from '@/portal-app/crm/queries'
import type { GetContactsParams, ContactListDto, ContactSource } from '@/types/crm'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { ContactDialog } from './components/ContactDialog'

const CONTACT_SOURCES: ContactSource[] = ['Web', 'Referral', 'Social', 'Cold', 'Event', 'Other']

export const ContactsPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  usePageContext('CRM Contacts')

  const canCreate = hasPermission(Permissions.CrmContactsCreate)
  const canUpdate = hasPermission(Permissions.CrmContactsUpdate)
  const canDelete = hasPermission(Permissions.CrmContactsDelete)

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [sourceFilter, setSourceFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()
  const [params, setParams] = useState<GetContactsParams>({ page: 1, pageSize: 20 })

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-crm-contact' })
  const [contactToDelete, setContactToDelete] = useState<ContactListDto | null>(null)
  const deleteMutation = useDeleteContact()

  useEffect(() => {
    setParams(prev => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  const queryParams = useMemo(() => ({
    ...params,
    search: deferredSearch || undefined,
    source: sourceFilter !== 'all' ? sourceFilter as ContactSource : undefined,
  }), [params, deferredSearch, sourceFilter])

  const { data: contactsResponse, isLoading: loading, error: queryError, refetch } = useContactsQuery(queryParams)
  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'CrmContact',
    onCollectionUpdate: refetch,
  })

  const contacts = contactsResponse?.items ?? []
  const { editItem: contactToEdit, openEdit, closeEdit } = useUrlEditDialog<ContactListDto>(contacts)
  const totalCount = contactsResponse?.totalCount ?? 0
  const totalPages = contactsResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  const handleSourceFilter = (value: string) => {
    startFilterTransition(() => {
      setSourceFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handlePageChange = (page: number) => {
    startFilterTransition(() => {
      setParams(prev => ({ ...prev, page }))
    })
  }

  const handleViewContact = (contact: ContactListDto) => {
    navigate(`/portal/crm/contacts/${contact.id}`)
  }

  const handleDelete = async () => {
    if (!contactToDelete) return
    try {
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

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{t('crm.contacts.title')}</h1>
          <p className="text-muted-foreground">{t('crm.contacts.description')}</p>
        </div>
        {canCreate && (
          <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('crm.contacts.create')}
          </Button>
        )}
      </div>

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('crm.contacts.title')}</CardTitle>
              <CardDescription>
                {t('labels.showingCountOfTotal', { count: contacts.length, total: totalCount })}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('crm.contacts.searchPlaceholder')}
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  className="pl-9 h-9"
                  aria-label={t('crm.contacts.searchPlaceholder')}
                />
              </div>
              <Select value={sourceFilter} onValueChange={handleSourceFilter}>
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
                  <TableHead>{t('labels.name')}</TableHead>
                  <TableHead>{t('crm.contacts.email')}</TableHead>
                  <TableHead>{t('crm.contacts.phone')}</TableHead>
                  <TableHead>{t('crm.contacts.company')}</TableHead>
                  <TableHead>{t('crm.contacts.source')}</TableHead>
                  <TableHead className="text-center">{t('crm.contacts.leadsCount')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-40" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : contacts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="p-0">
                      <EmptyState
                        icon={Contact}
                        title={t('crm.contacts.noContactsFound')}
                        description={t('crm.contacts.noContactsDescription')}
                        action={canCreate ? {
                          label: t('crm.contacts.create'),
                          onClick: () => openCreate(),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  contacts.map((contact) => (
                    <TableRow
                      key={contact.id}
                      className="group transition-colors hover:bg-muted/50 cursor-pointer"
                      onClick={() => handleViewContact(contact)}
                    >
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: `${contact.firstName} ${contact.lastName}` })}
                              onClick={(e) => e.stopPropagation()}
                            >
                              <EllipsisVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="start">
                            <DropdownMenuItem
                              className="cursor-pointer"
                              onClick={(e) => { e.stopPropagation(); handleViewContact(contact) }}
                            >
                              <Eye className="h-4 w-4 mr-2" />
                              {t('labels.viewDetails')}
                            </DropdownMenuItem>
                            {canUpdate && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={(e) => { e.stopPropagation(); openEdit(contact) }}
                              >
                                <Pencil className="h-4 w-4 mr-2" />
                                {t('labels.edit')}
                              </DropdownMenuItem>
                            )}
                            {canDelete && (
                              <DropdownMenuItem
                                className="text-destructive cursor-pointer"
                                onClick={(e) => { e.stopPropagation(); setContactToDelete(contact) }}
                              >
                                <Trash2 className="h-4 w-4 mr-2" />
                                {t('labels.delete')}
                              </DropdownMenuItem>
                            )}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                      <TableCell>
                        <span className="font-medium text-sm">{contact.firstName} {contact.lastName}</span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">{contact.email}</span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">{contact.phone || '-'}</span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">{contact.companyName || '-'}</span>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className={getSourceBadgeColor(contact.source)}>
                          {t(`crm.sources.${contact.source}`)}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">{contact.leadCount}</Badge>
                      </TableCell>
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

      {/* Create/Edit Contact Dialog */}
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

      {/* Delete Confirmation Dialog */}
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
