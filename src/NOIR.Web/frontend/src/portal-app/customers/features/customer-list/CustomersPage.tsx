import { useState, useEffect, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Eye,
  MoreHorizontal,
  Pencil,
  Plus,
  Search,
  Trash2,
  Users,
  Crown,
  TrendingUp,
  UserCheck,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
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
import { useCustomersQuery, useCustomerStatsQuery } from '@/portal-app/customers/queries'
import type { GetCustomersParams } from '@/services/customers'
import type { CustomerSegment, CustomerSummaryDto, CustomerTier } from '@/types/customer'
import { formatCurrency } from '@/lib/utils/currency'
import { CustomerFormDialog } from '../../components/CustomerFormDialog'
import { DeleteCustomerDialog } from '../../components/DeleteCustomerDialog'

const CUSTOMER_SEGMENTS: CustomerSegment[] = ['New', 'Active', 'AtRisk', 'Dormant', 'Lost', 'VIP']
const CUSTOMER_TIERS: CustomerTier[] = ['Standard', 'Silver', 'Gold', 'Platinum', 'Diamond']

const getSegmentBadgeClass = (segment: CustomerSegment): string => {
  switch (segment) {
    case 'New':
      return 'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400 dark:border-blue-800'
    case 'Active':
      return 'bg-green-100 text-green-800 border-green-200 dark:bg-green-900/30 dark:text-green-400 dark:border-green-800'
    case 'VIP':
      return 'bg-purple-100 text-purple-800 border-purple-200 dark:bg-purple-900/30 dark:text-purple-400 dark:border-purple-800'
    case 'AtRisk':
      return 'bg-orange-100 text-orange-800 border-orange-200 dark:bg-orange-900/30 dark:text-orange-400 dark:border-orange-800'
    case 'Dormant':
      return 'bg-yellow-100 text-yellow-800 border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-400 dark:border-yellow-800'
    case 'Lost':
      return 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800'
    default:
      return 'bg-gray-100 text-gray-800 border-gray-200 dark:bg-gray-900/30 dark:text-gray-400 dark:border-gray-800'
  }
}

const getTierBadgeClass = (tier: CustomerTier): string => {
  switch (tier) {
    case 'Standard':
      return 'bg-slate-100 text-slate-700 border-slate-200 dark:bg-slate-900/30 dark:text-slate-400 dark:border-slate-800'
    case 'Silver':
      return 'bg-gray-100 text-gray-700 border-gray-300 dark:bg-gray-900/30 dark:text-gray-300 dark:border-gray-700'
    case 'Gold':
      return 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400 dark:border-amber-800'
    case 'Platinum':
      return 'bg-cyan-100 text-cyan-800 border-cyan-200 dark:bg-cyan-900/30 dark:text-cyan-400 dark:border-cyan-800'
    case 'Diamond':
      return 'bg-violet-100 text-violet-800 border-violet-200 dark:bg-violet-900/30 dark:text-violet-400 dark:border-violet-800'
    default:
      return 'bg-gray-100 text-gray-800 border-gray-200'
  }
}

export const CustomersPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  usePageContext('Customers')

  const canCreate = hasPermission(Permissions.CustomersCreate)
  const canUpdate = hasPermission(Permissions.CustomersUpdate)
  const canDelete = hasPermission(Permissions.CustomersDelete)

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [segmentFilter, setSegmentFilter] = useState<string>('all')
  const [tierFilter, setTierFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()
  const [params, setParams] = useState<GetCustomersParams>({ page: 1, pageSize: 20 })

  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [customerToEdit, setCustomerToEdit] = useState<CustomerSummaryDto | null>(null)
  const [customerToDelete, setCustomerToDelete] = useState<CustomerSummaryDto | null>(null)

  useEffect(() => {
    setParams(prev => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  const queryParams = useMemo(() => ({
    ...params,
    search: deferredSearch || undefined,
    segment: segmentFilter !== 'all' ? segmentFilter as CustomerSegment : undefined,
    tier: tierFilter !== 'all' ? tierFilter as CustomerTier : undefined,
  }), [params, deferredSearch, segmentFilter, tierFilter])

  const { data: customersResponse, isLoading: loading, error: queryError } = useCustomersQuery(queryParams)
  const { data: stats } = useCustomerStatsQuery()
  const error = queryError?.message ?? null

  const customers = customersResponse?.items ?? []
  const totalCount = customersResponse?.totalCount ?? 0
  const totalPages = customersResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

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

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Users}
        title={t('customers.title', 'Customers')}
        description={t('customers.description', 'Manage your customer base and loyalty programs')}
        responsive
        action={
          canCreate && (
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300 cursor-pointer" onClick={() => setShowCreateDialog(true)}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('customers.newCustomer', 'New Customer')}
            </Button>
          )
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
              <CardTitle>{t('customers.allCustomers', 'All Customers')}</CardTitle>
              <CardDescription>
                {t('customers.totalCount', { count: totalCount, defaultValue: `${totalCount} customers total` })}
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
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
              {error}
            </div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('labels.email', 'Email')}</TableHead>
                  <TableHead>{t('labels.phone', 'Phone')}</TableHead>
                  <TableHead>{t('customers.segmentLabel', 'Segment')}</TableHead>
                  <TableHead>{t('customers.tierLabel', 'Tier')}</TableHead>
                  <TableHead className="text-center">{t('customers.ordersLabel', 'Orders')}</TableHead>
                  <TableHead className="text-right">{t('customers.totalSpent', 'Total Spent')}</TableHead>
                  <TableHead className="text-center">{t('customers.loyaltyPoints', 'Points')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-40" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-24 ml-auto" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-12 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-8 w-8 rounded ml-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : customers.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} className="p-0">
                      <EmptyState
                        icon={Users}
                        title={t('customers.noCustomersFound', 'No customers found')}
                        description={t('customers.noCustomersDescription', 'Get started by creating your first customer.')}
                        action={canCreate ? {
                          label: t('customers.addCustomer', 'Add Customer'),
                          onClick: () => setShowCreateDialog(true),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  customers.map((customer) => (
                    <TableRow
                      key={customer.id}
                      className="group transition-colors hover:bg-muted/50 cursor-pointer"
                      onClick={() => handleViewCustomer(customer)}
                    >
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
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: `${customer.firstName} ${customer.lastName}`, defaultValue: `Actions for ${customer.firstName} ${customer.lastName}` })}
                              onClick={(e) => e.stopPropagation()}
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
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
                                  setCustomerToEdit(customer)
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
        open={showCreateDialog || !!customerToEdit}
        onOpenChange={(open) => {
          if (!open) {
            setShowCreateDialog(false)
            setCustomerToEdit(null)
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
    </div>
  )
}

export default CustomersPage
