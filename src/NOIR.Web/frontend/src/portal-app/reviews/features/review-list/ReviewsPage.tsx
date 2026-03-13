import { useState, useMemo, useTransition, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { CheckCircle2, Eye, MessageSquare, ShieldCheck, Star, XCircle } from 'lucide-react'
import { toast } from 'sonner'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, RowSelectionState, SortingState } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlTab } from '@/hooks/useUrlTab'
import { useTableParams } from '@/hooks/useTableParams'
import { useServerTable, useSelectedIds } from '@/hooks/useServerTable'
import { createSelectColumn, createActionsColumn } from '@/lib/table/columnHelpers'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  DropdownMenuSeparator,
  EmptyState,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import { useReviewsQuery, useApproveReview, useRejectReview, useBulkApprove, useBulkReject } from '@/portal-app/reviews/queries'
import type { ReviewDto, ReviewStatus } from '@/types/review'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { ReviewDetailDialog } from '@/portal-app/reviews/components/ReviewDetailDialog'
import { RejectReviewDialog } from '@/portal-app/reviews/components/RejectReviewDialog'
import { AdminResponseDialog } from '@/portal-app/reviews/components/AdminResponseDialog'
import { getReviewStatusColor } from '@/portal-app/reviews/utils/reviewStatus'

const REVIEW_STATUSES: ReviewStatus[] = ['Pending', 'Approved', 'Rejected']

const RATING_OPTIONS = [1, 2, 3, 4, 5]

const StarRating = ({ rating }: { rating: number }) => (
  <div className="flex items-center gap-0.5">
    {[1, 2, 3, 4, 5].map((star) => (
      <Star key={star} className={`h-3.5 w-3.5 ${star <= rating ? 'fill-yellow-400 text-yellow-400' : 'fill-muted text-muted-foreground/30'}`} />
    ))}
  </div>
)

const ch = createColumnHelper<ReviewDto>()

export const ReviewsPage = () => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Reviews')

  // Tab (status) state
  const { activeTab, handleTabChange: setUrlTab, isPending: isTabPending } = useUrlTab({ defaultTab: 'all' })

  // Rating filter
  const [ratingFilter, setRatingFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()

  // Table params (search, pagination, sorting)
  const { params, searchInput, setSearchInput, isSearchStale, setSorting, setPage, setPageSize } = useTableParams({ defaultPageSize: 20 })

  // Row selection
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})

  // Dialog state
  const [detailReviewId, setDetailReviewId] = useState<string | undefined>()
  const [rejectReviewId, setRejectReviewId] = useState<string | undefined>()
  const [responseReviewId, setResponseReviewId] = useState<string | undefined>()

  // Query params
  const queryParams = useMemo(() => ({
    ...params,
    status: activeTab !== 'all' ? activeTab as ReviewStatus : undefined,
    rating: ratingFilter !== 'all' ? Number(ratingFilter) : undefined,
  }), [params, activeTab, ratingFilter])

  const {
    data,
    isLoading,
    refetch,
  } = useReviewsQuery(queryParams)

  const tableData = useMemo(() => data?.items ?? [], [data?.items])

  // Mutations
  const approveMutation = useApproveReview()
  const rejectMutation = useRejectReview()
  const bulkApproveMutation = useBulkApprove()
  const bulkRejectMutation = useBulkReject()

  // Handlers that don't depend on table
  const handleApprove = async (id: string) => {
    try {
      await approveMutation.mutateAsync(id)
      toast.success(t('reviews.approveSuccess', 'Review approved successfully'))
    } catch {
      toast.error(t('reviews.approveFailed', 'Failed to approve review'))
    }
  }

  const handleRejectConfirm = async (reason?: string) => {
    if (!rejectReviewId) return
    try {
      await rejectMutation.mutateAsync({ id: rejectReviewId, reason })
      toast.success(t('reviews.rejectSuccess', 'Review rejected successfully'))
      setRejectReviewId(undefined)
    } catch {
      toast.error(t('reviews.rejectFailed', 'Failed to reject review'))
    }
  }

  const handleAdminResponseSubmit = () => {
    setResponseReviewId(undefined)
  }

  const handleRatingFilter = (value: string) => {
    startFilterTransition(() => {
      setRatingFilter(value)
      setPage(1)
    })
  }

  // Columns
  const columns = useMemo((): ColumnDef<ReviewDto, unknown>[] => [
    createActionsColumn<ReviewDto>((review) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => setDetailReviewId(review.id)}>
          <Eye className="h-4 w-4 mr-2" />
          {t('reviews.viewDetails', 'View Details')}
        </DropdownMenuItem>
        <DropdownMenuItem className="cursor-pointer" onClick={() => setResponseReviewId(review.id)}>
          <MessageSquare className="h-4 w-4 mr-2" />
          {t('reviews.respond', 'Respond')}
        </DropdownMenuItem>
        {review.status === 'Pending' && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem className="cursor-pointer text-green-600" onClick={() => handleApprove(review.id)}>
              <CheckCircle2 className="h-4 w-4 mr-2" />
              {t('reviews.approve', 'Approve')}
            </DropdownMenuItem>
            <DropdownMenuItem className="cursor-pointer text-destructive" onClick={() => setRejectReviewId(review.id)}>
              <XCircle className="h-4 w-4 mr-2" />
              {t('reviews.reject', 'Reject')}
            </DropdownMenuItem>
          </>
        )}
      </>
    )),
    createSelectColumn<ReviewDto>(),
    ch.accessor('productName', {
      header: t('reviews.product', 'Product'),
      enableSorting: false,
      cell: ({ getValue }) => <span className="font-medium text-sm truncate max-w-[180px] block">{getValue() || '-'}</span>,
    }) as ColumnDef<ReviewDto, unknown>,
    ch.accessor('userName', {
      header: t('reviews.customer', 'Customer'),
      enableSorting: false,
      cell: ({ row }) => (
        <div className="flex items-center gap-1.5">
          <span className="text-sm">{row.original.userName || '-'}</span>
          {row.original.isVerifiedPurchase && (
            <ShieldCheck className="h-3.5 w-3.5 text-blue-500" aria-label={t('reviews.verifiedPurchase', 'Verified purchase')} />
          )}
        </div>
      ),
    }) as ColumnDef<ReviewDto, unknown>,
    ch.accessor('rating', {
      header: t('reviews.rating', 'Rating'),
      enableSorting: false,
      size: 110,
      cell: ({ getValue }) => <StarRating rating={getValue()} />,
    }) as ColumnDef<ReviewDto, unknown>,
    ch.accessor('title', {
      header: t('reviews.reviewTitle', 'Title'),
      enableSorting: false,
      cell: ({ getValue }) => <span className="text-sm truncate max-w-[200px] block">{getValue() || '-'}</span>,
    }) as ColumnDef<ReviewDto, unknown>,
    ch.accessor('status', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status', 'Status')} />,
      enableSorting: false,
      size: 110,
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getReviewStatusColor(getValue())}>
          {t(`reviews.status.${getValue().toLowerCase()}`, getValue())}
        </Badge>
      ),
    }) as ColumnDef<ReviewDto, unknown>,
    ch.accessor('createdAt', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.date', 'Date')} />,
      enableSorting: false,
      size: 150,
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{formatDateTime(getValue())}</span>,
    }) as ColumnDef<ReviewDto, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, formatDateTime])

  // Table instance
  const table = useServerTable({
    data: tableData,
    columns,
    rowCount: data?.totalCount ?? 0,
    enableRowSelection: true,
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: params.sorting as SortingState,
      rowSelection,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function'
        ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize })
        : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    onSortingChange: setSorting,
    onRowSelectionChange: setRowSelection,
    getRowId: (row) => row.id,
  })

  // Derived selection state (after table)
  const selectedIds = useSelectedIds(rowSelection)
  const selectedCount = selectedIds.length

  // Handlers that depend on table
  const handleTabChange = (value: string) => {
    setUrlTab(value)
    setPage(1)
    table.resetRowSelection()
  }

  const handleBulkApprove = async () => {
    if (selectedCount === 0) return
    try {
      await bulkApproveMutation.mutateAsync(selectedIds)
      toast.success(t('reviews.bulkApproveSuccess', { count: selectedCount, defaultValue: `${selectedCount} reviews approved` }))
      table.resetRowSelection()
    } catch {
      toast.error(t('reviews.bulkApproveFailed', 'Failed to approve selected reviews'))
    }
  }

  const handleBulkReject = async (reason?: string) => {
    if (selectedCount === 0) return
    try {
      await bulkRejectMutation.mutateAsync({ reviewIds: selectedIds, reason })
      toast.success(t('reviews.bulkRejectSuccess', { count: selectedCount, defaultValue: `${selectedCount} reviews rejected` }))
      table.resetRowSelection()
      setRejectReviewId(undefined)
    } catch {
      toast.error(t('reviews.bulkRejectFailed', 'Failed to reject selected reviews'))
    }
  }

  const handleCollectionUpdate = useCallback(() => {
    if (selectedCount === 0) refetch()
  }, [selectedCount, refetch])

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Review',
    onCollectionUpdate: handleCollectionUpdate,
  })

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Star}
        title={t('reviews.title', 'Reviews')}
        description={t('reviews.description', 'Moderate customer reviews and manage ratings')}
        responsive
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="flex flex-col gap-4">
            {/* Tabs */}
            <Tabs value={activeTab} onValueChange={handleTabChange}>
              <TabsList>
                <TabsTrigger value="all" className="cursor-pointer">
                  {t('labels.all', 'All')}
                </TabsTrigger>
                {REVIEW_STATUSES.map((status) => (
                  <TabsTrigger key={status} value={status} className="cursor-pointer">
                    {t(`reviews.status.${status.toLowerCase()}`, status)}
                  </TabsTrigger>
                ))}
              </TabsList>
              <TabsContent value="all" forceMount className="hidden" />
              {REVIEW_STATUSES.map((status) => (
                <TabsContent key={status} value={status} forceMount className="hidden" />
              ))}
            </Tabs>

            <div>
              <CardTitle className="text-lg">{t('reviews.allReviews', 'All Reviews')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('reviews.searchPlaceholder', 'Search reviews...')}
              isSearchStale={isSearchStale || isFilterPending || isTabPending}
              onResetColumnVisibility={table.resetColumnVisibility}
              filterSlot={
                <Select value={ratingFilter} onValueChange={handleRatingFilter}>
                  <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('reviews.filterByRating', 'Filter rating')}>
                    <SelectValue placeholder={t('reviews.filterByRating', 'Filter rating')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="cursor-pointer">
                      {t('reviews.allRatings', 'All ratings')}
                    </SelectItem>
                    {RATING_OPTIONS.map((rating) => (
                      <SelectItem key={rating} value={rating.toString()} className="cursor-pointer">
                        {rating} {rating === 1 ? t('reviews.star', 'star') : t('reviews.stars', 'stars')}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              }
            />
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <BulkActionToolbar
            selectedCount={selectedCount}
            onClearSelection={() => table.resetRowSelection()}
          >
            <Button
              variant="outline"
              size="sm"
              className="cursor-pointer text-green-600 hover:text-green-700 hover:bg-green-50 dark:hover:bg-green-950/30"
              onClick={handleBulkApprove}
              disabled={bulkApproveMutation.isPending}
            >
              <CheckCircle2 className="h-4 w-4 mr-1" />
              {t('reviews.approveSelected', 'Approve selected')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="cursor-pointer text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30"
              onClick={() => setRejectReviewId('bulk')}
              disabled={bulkRejectMutation.isPending}
            >
              <XCircle className="h-4 w-4 mr-1" />
              {t('reviews.rejectSelected', 'Reject selected')}
            </Button>
          </BulkActionToolbar>

          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending || isTabPending}
            onRowClick={selectedCount === 0 ? (review) => setDetailReviewId(review.id) : undefined}
            emptyState={
              <EmptyState
                icon={Star}
                title={t('reviews.noReviewsFound', 'No reviews found')}
                description={t('reviews.noReviewsDescription', 'Reviews will appear here when customers submit them.')}
                className="border-0 rounded-none px-4 py-12"
              />
            }
          />

          <DataTablePagination table={table} />
        </CardContent>
      </Card>

      {/* Dialogs */}
      <ReviewDetailDialog
        reviewId={detailReviewId}
        open={!!detailReviewId}
        onOpenChange={(open) => {
          if (!open) setDetailReviewId(undefined)
        }}
        onApprove={handleApprove}
        onReject={setRejectReviewId}
        onRespond={setResponseReviewId}
      />

      <RejectReviewDialog
        open={!!rejectReviewId}
        onOpenChange={(open) => {
          if (!open) setRejectReviewId(undefined)
        }}
        onConfirm={(reason) => {
          if (rejectReviewId === 'bulk') {
            handleBulkReject(reason)
          } else {
            handleRejectConfirm(reason)
          }
        }}
        isBulk={rejectReviewId === 'bulk'}
        count={rejectReviewId === 'bulk' ? selectedCount : undefined}
      />

      <AdminResponseDialog
        reviewId={responseReviewId}
        open={!!responseReviewId}
        onOpenChange={(open) => {
          if (!open) setResponseReviewId(undefined)
        }}
        onSuccess={handleAdminResponseSubmit}
      />
    </div>
  )
}

export default ReviewsPage
