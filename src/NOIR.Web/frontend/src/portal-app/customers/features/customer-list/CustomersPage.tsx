import { useState, useEffect, useDeferredValue, useMemo, useTransition, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Eye,
  EllipsisVertical,
  Loader2,
  Pencil,
  Plus,
  Search,
  Trash2,
  Users,
  Crown,
  TrendingUp,
  UserCheck,
  UserX,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { useSelection } from '@/hooks/useSelection'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Checkbox,
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
  PageHeader,
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
import { useCustomersQuery, useCustomerStatsQuery, useBulkActivateCustomers, useBulkDeactivateCustomers, useBulkDeleteCustomers } from '@/portal-app/customers/queries'
import type { GetCustomersParams } from '@/services/customers'
import type { CustomerSegment, CustomerSummaryDto, CustomerTier } from '@/types/customer'
import { formatCurrency } from '@/lib/utils/currency'
import { CustomerFormDialog } from '../../components/CustomerFormDialog'
import { CustomerImportExport } from '../../components/CustomerImportExport'
import { DeleteCustomerDialog } from '../../components/DeleteCustomerDialog'
import { getSegmentBadgeClass, getTierBadgeClass } from '@/portal-app/customers/utils/customerStatus'

const CUSTOMER_SEGMENTS: CustomerSegment[] = ['New', 'Active', 'AtRisk', 'Dormant', 'Lost', 'VIP']
const CUSTOMER_TIERS: CustomerTier[] = ['Standard', 'Silver', 'Gold', 'Platinum', 'Diamond']

export const CustomersPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  usePageContext('Customers')

  const canCreate = hasPermission(Permissions.CustomersCreate)
  const canUpdate = hasPermission(Permissions.CustomersUpdate)
  const canDelete = hasPermission(Permissions.CustomersDelete)
  const canManage = hasPermission(Permissions.CustomersManage)

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [segmentFilter, setSegmentFilter] = useState<string>('all')
  const [tierFilter, setTierFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()
  const [params, setParams] = useState<GetCustomersParams>({ page: 1, pageSize: 20 })

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-customer' })
  const [customerToDelete, setCustomerToDelete] = useState<CustomerSummaryDto | null>(null)
  const [showBulkDeleteConfirm, setShowBulkDeleteConfirm] = useState(false)
  const [isBulkPending, startBulkTransition] = useTransition()

  useEffect(() => {
    setParams(prev => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  const queryParams = useMemo(() => ({
    ...params,
    search: deferredSearch || undefined,
    segment: segmentFilter !== 'all' ? segmentFilter as CustomerSegment : undefined,
    tier: tierFilter !== 'all' ? tierFilter as CustomerTier : undefined,
  }), [params, deferredSearch, segmentFilter, tierFilter])

  const { data: customersResponse, isLoading: loading, error: queryError, refetch } = useCustomersQuery(queryParams)
  const { data: stats } = useCustomerStatsQuery()
  const error = queryError?.message ?? null

  const customers = customersResponse?.items ?? []
  const { editItem: customerToEdit, openEdit: openEditCustomer, closeEdit: closeEditCustomer } = useUrlEditDialog<CustomerSummaryDto>(customers)
  const totalCount = customersResponse?.totalCount ?? 0
  const totalPages = customersResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  const { selectedIds, setSelectedIds, handleSelectAll, handleSelectNone, handleToggleSelect, isAllSelected } = useSelection(customers)

  const handleCollectionUpdate = useCallback(() => {
    if (selectedIds.size === 0) refetch()
  }, [selectedIds.size, refetch])

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Customer',
    onCollectionUpdate: handleCollectionUpdate,
  })

  // Bulk mutation hooks
  const bulkActivateMutation = useBulkActivateCustomers()
  const bulkDeactivateMutation = useBulkDeactivateCustomers()
  const bulkDeleteMutation = useBulkDeleteCustomers()

  // Computed counts
  const selectedActiveCount = customers.filter(c => selectedIds.has(c.id) && c.isActive).length
  const selectedInactiveCount = customers.filter(c => selectedIds.has(c.id) && !c.isActive).length

  const vipCount = stats?.segmentDistribution.find(s => s.segment === 'VIP')?.count ?? 0
  const avgSpent = stats?.topSpenders && stats.topSpenders.length > 0
    ? stats.topSpenders.reduce((sum, c) => sum + c.totalSpent, 0) / stats.topSpenders.length
    : 0

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
  }

  const handleSegmentFilter = (value: string) => {
    startFilterTransition(() => {
      setSegmentFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handleTierFilter = (value: string) => {
    startFilterTransition(() => {
      setTierFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handlePageChange = (page: number) => {
    startFilterTransition(() => {
      setParams(prev => ({ ...prev, page }))
    })
  }

  const handleViewCustomer = (customer: CustomerSummaryDto) => {
    navigate(`/portal/ecommerce/customers/${customer.id}`)
  }

  // Bulk action handlers
  const onBulkActivate = () => {
    if (selectedIds.size === 0) return
    const inactiveIds = customers.filter(c => selectedIds.has(c.id) && !c.isActive).map(c => c.id)
    if (inactiveIds.length === 0) {
      toast.warning(t('customers.noInactiveSelected', 'No inactive customers selected'))
      return
    }
    startBulkTransition(async () => {
      try {
        const result = await bulkActivateMutation.mutateAsync(inactiveIds)
        if (result.failed > 0) {
          toast.warning(t('customers.bulkActivatePartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} activated, ${result.failed} failed` }))
        } else {
          toast.success(t('customers.bulkActivateSuccess', { count: result.success, defaultValue: `${result.success} customers activated` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('customers.bulkActivateFailed', 'Failed to activate customers'))
      }
      setSelectedIds(new Set())
    })
  }

  const onBulkDeactivate = () => {
    if (selectedIds.size === 0) return
    const activeIds = customers.filter(c => selectedIds.has(c.id) && c.isActive).map(c => c.id)
    if (activeIds.length === 0) {
      toast.warning(t('customers.noActiveSelected', 'No active customers selected'))
      return
    }
    startBulkTransition(async () => {
      try {
        const result = await bulkDeactivateMutation.mutateAsync(activeIds)
        if (result.failed > 0) {
          toast.warning(t('customers.bulkDeactivatePartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} deactivated, ${result.failed} failed` }))
        } else {
          toast.success(t('customers.bulkDeactivateSuccess', { count: result.success, defaultValue: `${result.success} customers deactivated` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('customers.bulkDeactivateFailed', 'Failed to deactivate customers'))
      }
      setSelectedIds(new Set())
    })
  }

  const onBulkDelete = () => {
    if (selectedIds.size === 0) return
    setShowBulkDeleteConfirm(true)
  }

  const handleBulkDeleteConfirm = () => {
    const selectedCustomerIds = Array.from(selectedIds)
    startBulkTransition(async () => {
      try {
        const result = await bulkDeleteMutation.mutateAsync(selectedCustomerIds)
        if (result.failed > 0) {
          toast.warning(t('customers.bulkDeletePartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} deleted, ${result.failed} failed` }))
        } else {
          toast.success(t('customers.bulkDeleteSuccess', { count: result.success, defaultValue: `${result.success} customers deleted` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('customers.bulkDeleteFailed', 'Failed to delete customers'))
      }
      setSelectedIds(new Set())
      setShowBulkDeleteConfirm(false)
    })
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Users}
        title={t('customers.title', 'Customers')}
        description={t('customers.description', 'Manage your customer base and loyalty programs')}
        responsive
        action={
          <div className="flex items-center gap-2">
            <CustomerImportExport
              totalCount={totalCount}
              onImportComplete={() => {/* refetch handled by query invalidation */}}
            />
            {canCreate && (
              <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
                <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
                {t('customers.newCustomer', 'New Customer')}
              </Button>
            )}
          </div>
        }
      />

      {/* Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
                <Users className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t('customers.totalCustomers', 'Total Customers')}</p>
                <p className="text-2xl font-bold">{stats?.totalCustomers ?? 0}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-green-500/10 border border-green-500/20">
                <UserCheck className="h-5 w-5 text-green-600 dark:text-green-400" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t('customers.activeCustomers', 'Active Customers')}</p>
                <p className="text-2xl font-bold">{stats?.activeCustomers ?? 0}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-purple-500/10 border border-purple-500/20">
                <Crown className="h-5 w-5 text-purple-600 dark:text-purple-400" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t('customers.vipCustomers', 'VIP Customers')}</p>
                <p className="text-2xl font-bold">{vipCount}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-amber-500/10 border border-amber-500/20">
                <TrendingUp className="h-5 w-5 text-amber-600 dark:text-amber-400" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t('customers.avgSpend', 'Avg Top Spend')}</p>
                <p className="text-2xl font-bold">{formatCurrency(avgSpent, 'VND')}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Customer List */}
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('customers.allCustomers', 'All Customers')}</CardTitle>
              <CardDescription>
                {t('labels.showingCountOfTotal', { count: customers.length, total: totalCount })}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {/* Search */}
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('customers.searchPlaceholder', 'Search customers...')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-9 h-9"
                  aria-label={t('customers.searchCustomers', 'Search customers')}
                />
              </div>
              {/* Segment Filter */}
              <Select value={segmentFilter} onValueChange={handleSegmentFilter}>
                <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('customers.filterBySegment', 'Filter by segment')}>
                  <SelectValue placeholder={t('customers.filterBySegment', 'Segment')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                  {CUSTOMER_SEGMENTS.map((segment) => (
                    <SelectItem key={segment} value={segment} className="cursor-pointer">
                      {t(`customers.segment.${segment.toLowerCase()}`, segment)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {/* Tier Filter */}
              <Select value={tierFilter} onValueChange={handleTierFilter}>
                <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('customers.filterByTier', 'Filter by tier')}>
                  <SelectValue placeholder={t('customers.filterByTier', 'Tier')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                  {CUSTOMER_TIERS.map((tier) => (
                    <SelectItem key={tier} value={tier} className="cursor-pointer">
                      {t(`customers.tier.${tier.toLowerCase()}`, tier)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          {/* Bulk Action Toolbar */}
          <BulkActionToolbar selectedCount={selectedIds.size} onClearSelection={handleSelectNone}>
            {canManage && selectedInactiveCount > 0 && (
              <Button
                variant="outline"
                size="sm"
                onClick={onBulkActivate}
                disabled={isBulkPending}
                className="cursor-pointer text-emerald-600 border-emerald-200 hover:bg-emerald-50 dark:text-emerald-400 dark:border-emerald-800 dark:hover:bg-emerald-950"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <UserCheck className="h-4 w-4 mr-2" />}
                {t('customers.activateCount', { count: selectedInactiveCount, defaultValue: `Activate (${selectedInactiveCount})` })}
              </Button>
            )}
            {canManage && selectedActiveCount > 0 && (
              <Button
                variant="outline"
                size="sm"
                onClick={onBulkDeactivate}
                disabled={isBulkPending}
                className="cursor-pointer text-amber-600 border-amber-200 hover:bg-amber-50 dark:text-amber-400 dark:border-amber-800 dark:hover:bg-amber-950"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <UserX className="h-4 w-4 mr-2" />}
                {t('customers.deactivateCount', { count: selectedActiveCount, defaultValue: `Deactivate (${selectedActiveCount})` })}
              </Button>
            )}
            {canDelete && (
              <Button
                variant="outline"
                size="sm"
                onClick={onBulkDelete}
                disabled={isBulkPending}
                className="cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Trash2 className="h-4 w-4 mr-2" />}
                {t('customers.deleteCount', { count: selectedIds.size, defaultValue: `Delete (${selectedIds.size})` })}
              </Button>
            )}
          </BulkActionToolbar>

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                  <TableHead className="w-[40px]">
                    <Checkbox
                      checked={isAllSelected}
                      onCheckedChange={(checked) => {
                        if (checked) handleSelectAll()
                        else handleSelectNone()
                      }}
                      aria-label={t('labels.selectAll', 'Select all')}
                      className="cursor-pointer"
                    />
                  </TableHead>
                  <TableHead>{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('labels.email', 'Email')}</TableHead>
                  <TableHead>{t('labels.phone', 'Phone')}</TableHead>
                  <TableHead>{t('customers.segmentLabel', 'Segment')}</TableHead>
                  <TableHead>{t('customers.tierLabel', 'Tier')}</TableHead>
                  <TableHead className="text-center">{t('customers.ordersLabel', 'Orders')}</TableHead>
                  <TableHead className="text-right">{t('customers.totalSpent', 'Total Spent')}</TableHead>
                  <TableHead className="text-center">{t('customers.loyaltyPoints', 'Points')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-4 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-40" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-24 ml-auto" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-12 mx-auto rounded-full" /></TableCell>
                    </TableRow>
                  ))
                ) : customers.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={10} className="p-0">
                      <EmptyState
                        icon={Users}
                        title={t('customers.noCustomersFound', 'No customers found')}
                        description={t('customers.noCustomersDescription', 'Get started by creating your first customer.')}
                        action={canCreate ? {
                          label: t('customers.addCustomer', 'Add Customer'),
                          onClick: () => openCreate(),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  customers.map((customer) => (
                    <TableRow
                      key={customer.id}
                      className={`group transition-colors hover:bg-muted/50 ${selectedIds.size === 0 ? 'cursor-pointer' : ''} ${selectedIds.has(customer.id) ? 'bg-primary/5' : ''}`}
                      onClick={() => { if (selectedIds.size === 0) handleViewCustomer(customer) }}
                    >
                      <TableCell className="sticky left-0 z-10 bg-background">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: `${customer.firstName} ${customer.lastName}`, defaultValue: `Actions for ${customer.firstName} ${customer.lastName}` })}
                              onClick={(e) => e.stopPropagation()}
                            >
                              <EllipsisVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="start">
                            <DropdownMenuItem
                              className="cursor-pointer"
                              onClick={(e) => {
                                e.stopPropagation()
                                handleViewCustomer(customer)
                              }}
                            >
                              <Eye className="h-4 w-4 mr-2" />
                              {t('labels.viewDetails', 'View Details')}
                            </DropdownMenuItem>
                            {canUpdate && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={(e) => {
                                  e.stopPropagation()
                                  openEditCustomer(customer)
                                }}
                              >
                                <Pencil className="h-4 w-4 mr-2" />
                                {t('labels.edit', 'Edit')}
                              </DropdownMenuItem>
                            )}
                            {canDelete && (
                              <DropdownMenuItem
                                className="text-destructive cursor-pointer"
                                onClick={(e) => {
                                  e.stopPropagation()
                                  setCustomerToDelete(customer)
                                }}
                              >
                                <Trash2 className="h-4 w-4 mr-2" />
                                {t('labels.delete', 'Delete')}
                              </DropdownMenuItem>
                            )}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                      <TableCell onClick={(e) => e.stopPropagation()}>
                        <Checkbox
                          checked={selectedIds.has(customer.id)}
                          onCheckedChange={() => handleToggleSelect(customer.id)}
                          aria-label={t('labels.selectItem', { name: `${customer.firstName} ${customer.lastName}`, defaultValue: `Select ${customer.firstName} ${customer.lastName}` })}
                          className="cursor-pointer"
                        />
                      </TableCell>
                      <TableCell>
                        <span className="font-medium text-sm">
                          {customer.firstName} {customer.lastName}
                        </span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">{customer.email}</span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">{customer.phone || '-'}</span>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className={getSegmentBadgeClass(customer.segment)}>
                          {t(`customers.segment.${customer.segment.toLowerCase()}`, customer.segment)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className={getTierBadgeClass(customer.tier)}>
                          {t(`customers.tier.${customer.tier.toLowerCase()}`, customer.tier)}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">{customer.totalOrders}</Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <span className="font-medium text-sm">{formatCurrency(customer.totalSpent, 'VND')}</span>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">{customer.loyaltyPoints.toLocaleString()}</Badge>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
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

      {/* Create/Edit Customer Dialog */}
      <CustomerFormDialog
        open={isCreateOpen || !!customerToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (customerToEdit) closeEditCustomer()
          }
        }}
        customer={customerToEdit}
      />

      {/* Delete Confirmation Dialog */}
      <DeleteCustomerDialog
        open={!!customerToDelete}
        onOpenChange={(open) => !open && setCustomerToDelete(null)}
        customer={customerToDelete}
      />

      {/* Bulk Delete Confirmation Dialog */}
      <Credenza open={showBulkDeleteConfirm} onOpenChange={setShowBulkDeleteConfirm}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('customers.bulkDeleteConfirmTitle', { count: selectedIds.size, defaultValue: `Delete ${selectedIds.size} customers` })}</CredenzaTitle>
                <CredenzaDescription>
                  {t('customers.bulkDeleteConfirmDescription', {
                    count: selectedIds.size,
                    defaultValue: `Are you sure you want to delete ${selectedIds.size} customers? This action cannot be undone.`,
                  })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button
              variant="outline"
              onClick={() => setShowBulkDeleteConfirm(false)}
              disabled={isBulkPending}
              className="cursor-pointer"
            >
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleBulkDeleteConfirm}
              disabled={isBulkPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {isBulkPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isBulkPending
                ? t('labels.deleting', 'Deleting...')
                : t('customers.deleteCount', { count: selectedIds.size, defaultValue: `Delete (${selectedIds.size})` })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default CustomersPage
