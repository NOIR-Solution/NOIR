import { useState, useDeferredValue, useMemo, useEffect, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import {
  ChevronLeft,
  ChevronRight,
  Filter,
  MoreHorizontal,
  Pencil,
  Percent,
  Play,
  Plus,
  Pause,
  Search,
  Trash2,
  X,
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
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  EmptyState,
  Input,
  PageHeader,
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
import { usePromotionsQuery, useActivatePromotionMutation, useDeactivatePromotionMutation } from '@/portal-app/promotions/queries'
import type { GetPromotionsParams } from '@/services/promotions'
import type { PromotionDto, PromotionStatus, PromotionType } from '@/types/promotion'
import { PromotionFormDialog } from '../../components/PromotionFormDialog'
import { DeletePromotionDialog } from '../../components/DeletePromotionDialog'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { toast } from 'sonner'

// ============================================================================
// Constants
// ============================================================================

const PROMOTION_STATUSES: PromotionStatus[] = ['Draft', 'Active', 'Scheduled', 'Expired', 'Cancelled']
const PROMOTION_TYPES: PromotionType[] = ['VoucherCode', 'FlashSale', 'BundleDeal', 'FreeShipping']

const getStatusBadgeVariant = (status: PromotionStatus): 'default' | 'secondary' | 'destructive' | 'outline' => {
  switch (status) {
    case 'Active':
      return 'default'
    case 'Draft':
    case 'Scheduled':
      return 'secondary'
    case 'Expired':
      return 'outline'
    case 'Cancelled':
      return 'destructive'
    default:
      return 'secondary'
  }
}

const getStatusColor = (status: PromotionStatus): string => {
  switch (status) {
    case 'Active':
      return 'bg-emerald-500/10 text-emerald-600 border-emerald-500/20'
    case 'Draft':
      return 'bg-gray-500/10 text-gray-600 border-gray-500/20'
    case 'Scheduled':
      return 'bg-blue-500/10 text-blue-600 border-blue-500/20'
    case 'Expired':
      return 'bg-orange-500/10 text-orange-600 border-orange-500/20'
    case 'Cancelled':
      return 'bg-red-500/10 text-red-600 border-red-500/20'
    default:
      return ''
  }
}

const formatDiscountValue = (dto: PromotionDto): string => {
  switch (dto.discountType) {
    case 'Percentage':
      return `${dto.discountValue}%`
    case 'FixedAmount':
      return `${dto.discountValue.toLocaleString()}`
    case 'FreeShipping':
      return 'Free Shipping'
    case 'BuyXGetY':
      return 'Buy X Get Y'
    default:
      return String(dto.discountValue)
  }
}

// ============================================================================
// Component
// ============================================================================

export const PromotionsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Promotions')

  // Permission checks
  const canWrite = hasPermission(Permissions.PromotionsWrite)
  const canDelete = hasPermission(Permissions.PromotionsDelete)

  // Search state with React 19 deferred value
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch

  // Filter state with transitions
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [typeFilter, setTypeFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()

  // Pagination state
  const [params, setParams] = useState<GetPromotionsParams>({ page: 1, pageSize: 20 })

  // Dialog state
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [promotionToEdit, setPromotionToEdit] = useState<PromotionDto | null>(null)
  const [promotionToDelete, setPromotionToDelete] = useState<PromotionDto | null>(null)

  // Reset page when search changes
  useEffect(() => {
    setParams(prev => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  // Build query params
  const queryParams = useMemo(() => ({
    ...params,
    search: deferredSearch || undefined,
    status: statusFilter !== 'all' ? statusFilter as PromotionStatus : undefined,
    promotionType: typeFilter !== 'all' ? typeFilter as PromotionType : undefined,
  }), [params, deferredSearch, statusFilter, typeFilter])

  // Queries and mutations
  const { data: promotionsResponse, isLoading: loading, error: queryError, refetch: refresh } = usePromotionsQuery(queryParams)
  const activateMutation = useActivatePromotionMutation()
  const deactivateMutation = useDeactivatePromotionMutation()
  const error = queryError?.message ?? null

  const promotions = promotionsResponse?.items ?? []
  const totalCount = promotionsResponse?.totalCount ?? 0
  const totalPages = promotionsResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  // Handlers
  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
  }

  const handleStatusFilter = (value: string) => {
    startFilterTransition(() => {
      setStatusFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handleTypeFilter = (value: string) => {
    startFilterTransition(() => {
      setTypeFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handlePageChange = (page: number) => {
    startFilterTransition(() => {
      setParams(prev => ({ ...prev, page }))
    })
  }

  const handleActivate = async (promotion: PromotionDto) => {
    try {
      await activateMutation.mutateAsync(promotion.id)
      toast.success(t('promotions.activateSuccess', 'Promotion activated successfully'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('promotions.activateError', 'Failed to activate promotion')
      toast.error(message)
    }
  }

  const handleDeactivate = async (promotion: PromotionDto) => {
    try {
      await deactivateMutation.mutateAsync(promotion.id)
      toast.success(t('promotions.deactivateSuccess', 'Promotion deactivated successfully'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('promotions.deactivateError', 'Failed to deactivate promotion')
      toast.error(message)
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Percent}
        title={t('promotions.title', 'Promotions')}
        description={t('promotions.description', 'Manage promotions, vouchers, and discount campaigns')}
        responsive
        action={
          canWrite && (
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300 cursor-pointer" onClick={() => setShowCreateDialog(true)}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('promotions.newPromotion', 'New Promotion')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('promotions.allPromotions', 'All Promotions')}</CardTitle>
              <CardDescription>
                {t('promotions.totalCount', { count: totalCount, defaultValue: `${totalCount} promotions total` })}
              </CardDescription>
            </div>
            <div className="flex items-center gap-3 flex-wrap">
              {/* Status Filter */}
              <Select value={statusFilter} onValueChange={handleStatusFilter}>
                <SelectTrigger className="w-[150px] cursor-pointer">
                  <Filter className="h-4 w-4 mr-2" />
                  <SelectValue placeholder={t('promotions.filterByStatus', 'Filter status')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                  {PROMOTION_STATUSES.map((status) => (
                    <SelectItem key={status} value={status} className="cursor-pointer">
                      {t(`promotions.status.${status.toLowerCase()}`, status)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {/* Type Filter */}
              <Select value={typeFilter} onValueChange={handleTypeFilter}>
                <SelectTrigger className="w-[160px] cursor-pointer">
                  <Filter className="h-4 w-4 mr-2" />
                  <SelectValue placeholder={t('promotions.filterByType', 'Filter type')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('labels.all', 'All')}</SelectItem>
                  {PROMOTION_TYPES.map((type) => (
                    <SelectItem key={type} value={type} className="cursor-pointer">
                      {t(`promotions.type.${type.toLowerCase()}`, type)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {/* Search */}
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('promotions.searchPlaceholder', 'Search promotions...')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-10 w-full sm:w-48"
                  aria-label={t('promotions.searchPromotions', 'Search promotions')}
                />
                {searchInput && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="absolute right-1 top-1/2 -translate-y-1/2 h-6 w-6 cursor-pointer"
                    onClick={() => setSearchInput('')}
                    aria-label={t('labels.clearSearch', 'Clear search')}
                  >
                    <X className="h-3.5 w-3.5" />
                  </Button>
                )}
              </div>
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
                  <TableHead className="w-[20%]">{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('promotions.code', 'Code')}</TableHead>
                  <TableHead>{t('promotions.type.label', 'Type')}</TableHead>
                  <TableHead>{t('promotions.discount', 'Discount')}</TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead>{t('promotions.startDate', 'Start Date')}</TableHead>
                  <TableHead>{t('promotions.endDate', 'End Date')}</TableHead>
                  <TableHead className="text-center">{t('promotions.usage', 'Usage')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-24 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-12 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-8 w-8 rounded ml-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : promotions.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} className="p-0">
                      <EmptyState
                        icon={Percent}
                        title={t('promotions.noPromotionsFound', 'No promotions found')}
                        description={t('promotions.noPromotionsDescription', 'Get started by creating your first promotion.')}
                        action={canWrite ? {
                          label: t('promotions.addPromotion', 'Add Promotion'),
                          onClick: () => setShowCreateDialog(true),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  promotions.map((promotion) => (
                    <TableRow key={promotion.id} className="group transition-colors hover:bg-muted/50">
                      <TableCell>
                        <span className="font-medium">{promotion.name}</span>
                        {promotion.description && (
                          <p className="text-sm text-muted-foreground line-clamp-1 mt-0.5">
                            {promotion.description}
                          </p>
                        )}
                      </TableCell>
                      <TableCell>
                        <code className="text-sm bg-muted px-1.5 py-0.5 rounded font-mono">
                          {promotion.code}
                        </code>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline">
                          {t(`promotions.type.${promotion.promotionType.toLowerCase()}`, promotion.promotionType)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <span className="font-medium text-sm">
                          {formatDiscountValue(promotion)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Badge variant={getStatusBadgeVariant(promotion.status)} className={getStatusColor(promotion.status)}>
                          {t(`promotions.status.${promotion.status.toLowerCase()}`, promotion.status)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">
                          {formatDateTime(promotion.startDate)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">
                          {formatDateTime(promotion.endDate)}
                        </span>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">
                          {promotion.currentUsageCount}
                          {promotion.usageLimitTotal != null ? `/${promotion.usageLimitTotal}` : ''}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: promotion.name, defaultValue: `Actions for ${promotion.name}` })}
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            {canWrite && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={() => setPromotionToEdit(promotion)}
                              >
                                <Pencil className="h-4 w-4 mr-2" />
                                {t('labels.edit', 'Edit')}
                              </DropdownMenuItem>
                            )}
                            {canWrite && promotion.status !== 'Active' && promotion.status !== 'Expired' && promotion.status !== 'Cancelled' && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={() => handleActivate(promotion)}
                              >
                                <Play className="h-4 w-4 mr-2" />
                                {t('promotions.activate', 'Activate')}
                              </DropdownMenuItem>
                            )}
                            {canWrite && promotion.status === 'Active' && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={() => handleDeactivate(promotion)}
                              >
                                <Pause className="h-4 w-4 mr-2" />
                                {t('promotions.deactivate', 'Deactivate')}
                              </DropdownMenuItem>
                            )}
                            {canDelete && (
                              <>
                                <DropdownMenuSeparator />
                                <DropdownMenuItem
                                  className="text-destructive cursor-pointer"
                                  onClick={() => setPromotionToDelete(promotion)}
                                >
                                  <Trash2 className="h-4 w-4 mr-2" />
                                  {t('labels.delete', 'Delete')}
                                </DropdownMenuItem>
                              </>
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
            <div className="flex items-center justify-between mt-4">
              <p className="text-sm text-muted-foreground">
                {t('labels.pageOf', { current: currentPage, total: totalPages, defaultValue: `Page ${currentPage} of ${totalPages}` })}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="cursor-pointer"
                  disabled={currentPage <= 1}
                  onClick={() => handlePageChange(currentPage - 1)}
                  aria-label={t('labels.previousPage', 'Previous page')}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  className="cursor-pointer"
                  disabled={currentPage >= totalPages}
                  onClick={() => handlePageChange(currentPage + 1)}
                  aria-label={t('labels.nextPage', 'Next page')}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Promotion Dialog */}
      <PromotionFormDialog
        open={showCreateDialog || !!promotionToEdit}
        onOpenChange={(open) => {
          if (!open) {
            setShowCreateDialog(false)
            setPromotionToEdit(null)
          }
        }}
        promotion={promotionToEdit}
        onSuccess={() => refresh()}
      />

      {/* Delete Confirmation Dialog */}
      <DeletePromotionDialog
        promotion={promotionToDelete}
        onOpenChange={(open) => !open && setPromotionToDelete(null)}
        onSuccess={() => refresh()}
      />
    </div>
  )
}

export default PromotionsPage
