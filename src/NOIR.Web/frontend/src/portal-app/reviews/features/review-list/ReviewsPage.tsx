import { useState, useEffect, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import {
  CheckCircle2,
  Eye,
  MessageSquare,
  Search,
  ShieldCheck,
  Star,
  XCircle,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Checkbox,
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
  Tabs,
  TabsList,
  TabsTrigger,
} from '@uikit'
import { useReviewsQuery } from '@/portal-app/reviews/queries'
import {
  useApproveReview,
  useRejectReview,
  useBulkApprove,
  useBulkReject,
} from '@/portal-app/reviews/queries'
import type { GetReviewsParams } from '@/services/reviews'
import type { ReviewDto, ReviewStatus } from '@/types/review'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { ReviewDetailDialog } from '@/portal-app/reviews/components/ReviewDetailDialog'
import { RejectReviewDialog } from '@/portal-app/reviews/components/RejectReviewDialog'
import { AdminResponseDialog } from '@/portal-app/reviews/components/AdminResponseDialog'

const REVIEW_STATUSES: ReviewStatus[] = ['Pending', 'Approved', 'Rejected']

const RATING_OPTIONS = [1, 2, 3, 4, 5]

const getStatusColor = (status: ReviewStatus): string => {
  switch (status) {
    case 'Pending':
      return 'bg-yellow-100 text-yellow-800 border-yellow-300 dark:bg-yellow-900/30 dark:text-yellow-400 dark:border-yellow-700'
    case 'Approved':
      return 'bg-green-100 text-green-800 border-green-300 dark:bg-green-900/30 dark:text-green-400 dark:border-green-700'
    case 'Rejected':
      return 'bg-red-100 text-red-800 border-red-300 dark:bg-red-900/30 dark:text-red-400 dark:border-red-700'
    default:
      return ''
  }
}

const StarRating = ({ rating }: { rating: number }) => (
  <div className="flex items-center gap-0.5">
    {[1, 2, 3, 4, 5].map((star) => (
      <Star
        key={star}
        className={`h-3.5 w-3.5 ${
          star <= rating
            ? 'fill-yellow-400 text-yellow-400'
            : 'fill-muted text-muted-foreground/30'
        }`}
      />
    ))}
  </div>
)

export const ReviewsPage = () => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Reviews')

  // Search state
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch

  // Filter state
  const [activeTab, setActiveTab] = useState<string>('all')
  const [ratingFilter, setRatingFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()
  const [params, setParams] = useState<GetReviewsParams>({ page: 1, pageSize: 20 })

  // Selection state
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  // Dialog state
  const [detailReviewId, setDetailReviewId] = useState<string | undefined>()
  const [rejectReviewId, setRejectReviewId] = useState<string | undefined>()
  const [responseReviewId, setResponseReviewId] = useState<string | undefined>()

  // Reset page on search change
  useEffect(() => {
    setParams((prev) => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  const queryParams = useMemo(
    () => ({
      ...params,
      search: deferredSearch || undefined,
      status: activeTab !== 'all' ? (activeTab as ReviewStatus) : undefined,
      rating: ratingFilter !== 'all' ? Number(ratingFilter) : undefined,
    }),
    [params, deferredSearch, activeTab, ratingFilter],
  )

  const {
    data: reviewsResponse,
    isLoading: loading,
    error: queryError,
  } = useReviewsQuery(queryParams)
  const error = queryError?.message ?? null

  const reviews = reviewsResponse?.items ?? []
  const totalCount = reviewsResponse?.totalCount ?? 0
  const totalPages = reviewsResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  // Mutations
  const approveMutation = useApproveReview()
  const rejectMutation = useRejectReview()
  const bulkApproveMutation = useBulkApprove()
  const bulkRejectMutation = useBulkReject()

  // Handlers
  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
  }

  const handleTabChange = (value: string) => {
    startFilterTransition(() => {
      setActiveTab(value)
      setParams((prev) => ({ ...prev, page: 1 }))
      setSelectedIds(new Set())
    })
  }

  const handleRatingFilter = (value: string) => {
    startFilterTransition(() => {
      setRatingFilter(value)
      setParams((prev) => ({ ...prev, page: 1 }))
    })
  }

  const handlePageChange = (page: number) => {
    startFilterTransition(() => {
      setParams((prev) => ({ ...prev, page }))
    })
  }

  const handleToggleSelect = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  const handleSelectAll = () => {
    if (selectedIds.size === reviews.length) {
      setSelectedIds(new Set())
    } else {
      setSelectedIds(new Set(reviews.map((r) => r.id)))
    }
  }

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

  const handleBulkApprove = async () => {
    if (selectedIds.size === 0) return
    try {
      await bulkApproveMutation.mutateAsync(Array.from(selectedIds))
      toast.success(
        t('reviews.bulkApproveSuccess', {
          count: selectedIds.size,
          defaultValue: `${selectedIds.size} reviews approved`,
        }),
      )
      setSelectedIds(new Set())
    } catch {
      toast.error(t('reviews.bulkApproveFailed', 'Failed to approve selected reviews'))
    }
  }

  const handleBulkReject = async (reason?: string) => {
    if (selectedIds.size === 0) return
    try {
      await bulkRejectMutation.mutateAsync({
        reviewIds: Array.from(selectedIds),
        reason,
      })
      toast.success(
        t('reviews.bulkRejectSuccess', {
          count: selectedIds.size,
          defaultValue: `${selectedIds.size} reviews rejected`,
        }),
      )
      setSelectedIds(new Set())
      setRejectReviewId(undefined)
    } catch {
      toast.error(t('reviews.bulkRejectFailed', 'Failed to reject selected reviews'))
    }
  }

  const handleAdminResponseSubmit = () => {
    setResponseReviewId(undefined)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Star}
        title={t('reviews.title', 'Reviews')}
        description={t('reviews.description', 'Moderate customer reviews and manage ratings')}
        responsive
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
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
            </Tabs>

            <div className="space-y-3">
              <div>
                <CardTitle>{t('reviews.allReviews', 'All Reviews')}</CardTitle>
                <CardDescription>
                  {t('reviews.totalCount', {
                    count: totalCount,
                    defaultValue: `${totalCount} reviews total`,
                  })}
                </CardDescription>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                {/* Search */}
                <div className="relative flex-1 min-w-[200px]">
                  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    placeholder={t('reviews.searchPlaceholder', 'Search reviews...')}
                    value={searchInput}
                    onChange={handleSearchChange}
                    className="pl-9 h-9"
                    aria-label={t('reviews.searchReviews', 'Search reviews')}
                  />
                </div>
                {/* Rating Filter */}
                <Select value={ratingFilter} onValueChange={handleRatingFilter}>
                  <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('reviews.filterByRating', 'Filter rating')}>
                    <SelectValue placeholder={t('reviews.filterByRating', 'Filter rating')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="cursor-pointer">
                      {t('reviews.allRatings', 'All ratings')}
                    </SelectItem>
                    {RATING_OPTIONS.map((rating) => (
                      <SelectItem
                        key={rating}
                        value={rating.toString()}
                        className="cursor-pointer"
                      >
                        {rating} {rating === 1
                          ? t('reviews.star', 'star')
                          : t('reviews.stars', 'stars')}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            {/* Bulk Actions Toolbar */}
            {selectedIds.size > 0 && (
              <div className="flex items-center gap-3 p-3 bg-muted/50 rounded-lg border border-border/50 animate-in fade-in-0 slide-in-from-top-2 duration-200">
                <span className="text-sm font-medium">
                  {t('reviews.selectedCount', {
                    count: selectedIds.size,
                    defaultValue: `${selectedIds.size} selected`,
                  })}
                </span>
                <div className="flex items-center gap-2 ml-auto">
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
                  <Button
                    variant="ghost"
                    size="sm"
                    className="cursor-pointer"
                    onClick={() => setSelectedIds(new Set())}
                  >
                    {t('labels.clearSelection', 'Clear')}
                  </Button>
                </div>
              </div>
            )}
          </div>
        </CardHeader>
        <CardContent
          className={
            isSearchStale || isFilterPending
              ? 'opacity-70 transition-opacity duration-200'
              : 'transition-opacity duration-200'
          }
        >
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">{error}</div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-12">
                    <Checkbox
                      checked={reviews.length > 0 && selectedIds.size === reviews.length}
                      onCheckedChange={handleSelectAll}
                      aria-label={t('reviews.selectAll', 'Select all reviews')}
                      className="cursor-pointer"
                    />
                  </TableHead>
                  <TableHead>{t('reviews.product', 'Product')}</TableHead>
                  <TableHead>{t('reviews.customer', 'Customer')}</TableHead>
                  <TableHead>{t('reviews.rating', 'Rating')}</TableHead>
                  <TableHead>{t('reviews.reviewTitle', 'Title')}</TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead>{t('labels.date', 'Date')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell>
                        <Skeleton className="h-4 w-4" />
                      </TableCell>
                      <TableCell>
                        <Skeleton className="h-4 w-28" />
                      </TableCell>
                      <TableCell>
                        <Skeleton className="h-4 w-24" />
                      </TableCell>
                      <TableCell>
                        <Skeleton className="h-4 w-20" />
                      </TableCell>
                      <TableCell>
                        <Skeleton className="h-4 w-32" />
                      </TableCell>
                      <TableCell>
                        <Skeleton className="h-5 w-20 rounded-full" />
                      </TableCell>
                      <TableCell>
                        <Skeleton className="h-4 w-28" />
                      </TableCell>
                      <TableCell className="text-right">
                        <Skeleton className="h-8 w-24 ml-auto" />
                      </TableCell>
                    </TableRow>
                  ))
                ) : reviews.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} className="p-0">
                      <EmptyState
                        icon={Star}
                        title={t('reviews.noReviewsFound', 'No reviews found')}
                        description={t(
                          'reviews.noReviewsDescription',
                          'Reviews will appear here when customers submit them.',
                        )}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  reviews.map((review) => (
                    <ReviewTableRow
                      key={review.id}
                      review={review}
                      isSelected={selectedIds.has(review.id)}
                      onToggleSelect={handleToggleSelect}
                      onViewDetail={setDetailReviewId}
                      onApprove={handleApprove}
                      onReject={setRejectReviewId}
                      onRespond={setResponseReviewId}
                      formatDateTime={formatDateTime}
                      t={t}
                    />
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
        count={rejectReviewId === 'bulk' ? selectedIds.size : undefined}
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

// Extracted row component
interface ReviewTableRowProps {
  review: ReviewDto
  isSelected: boolean
  onToggleSelect: (id: string) => void
  onViewDetail: (id: string) => void
  onApprove: (id: string) => void
  onReject: (id: string) => void
  onRespond: (id: string) => void
  formatDateTime: (date: Date | string) => string
  t: ReturnType<typeof useTranslation<'common'>>['t']
}

const ReviewTableRow = ({
  review,
  isSelected,
  onToggleSelect,
  onViewDetail,
  onApprove,
  onReject,
  onRespond,
  formatDateTime,
  t,
}: ReviewTableRowProps) => (
  <TableRow className="group transition-colors hover:bg-muted/50">
    <TableCell>
      <Checkbox
        checked={isSelected}
        onCheckedChange={() => onToggleSelect(review.id)}
        aria-label={t('reviews.selectReview', {
          title: review.title || review.id,
          defaultValue: `Select review ${review.title || review.id}`,
        })}
        className="cursor-pointer"
      />
    </TableCell>
    <TableCell>
      <div className="flex flex-col">
        <span className="font-medium text-sm truncate max-w-[180px]">
          {review.productName || '-'}
        </span>
      </div>
    </TableCell>
    <TableCell>
      <div className="flex items-center gap-1.5">
        <span className="text-sm">{review.userName || '-'}</span>
        {review.isVerifiedPurchase && (
          <ShieldCheck
            className="h-3.5 w-3.5 text-blue-500"
            aria-label={t('reviews.verifiedPurchase', 'Verified purchase')}
          />
        )}
      </div>
    </TableCell>
    <TableCell>
      <StarRating rating={review.rating} />
    </TableCell>
    <TableCell>
      <span className="text-sm truncate max-w-[200px] block">{review.title || '-'}</span>
    </TableCell>
    <TableCell>
      <Badge variant="outline" className={getStatusColor(review.status)}>
        {t(`reviews.status.${review.status.toLowerCase()}`, review.status)}
      </Badge>
    </TableCell>
    <TableCell>
      <span className="text-sm text-muted-foreground">{formatDateTime(review.createdAt)}</span>
    </TableCell>
    <TableCell className="text-right">
      <div className="flex items-center justify-end gap-1">
        <Button
          variant="ghost"
          size="sm"
          className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
          onClick={() => onViewDetail(review.id)}
          aria-label={t('reviews.viewReview', {
            title: review.title || review.id,
            defaultValue: `View review ${review.title || review.id}`,
          })}
        >
          <Eye className="h-4 w-4" />
        </Button>
        {review.status === 'Pending' && (
          <>
            <Button
              variant="ghost"
              size="sm"
              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 text-green-600 hover:bg-green-50 hover:text-green-700 dark:hover:bg-green-950/30"
              onClick={() => onApprove(review.id)}
              aria-label={t('reviews.approveReview', {
                title: review.title || review.id,
                defaultValue: `Approve review ${review.title || review.id}`,
              })}
            >
              <CheckCircle2 className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 text-red-600 hover:bg-red-50 hover:text-red-700 dark:hover:bg-red-950/30"
              onClick={() => onReject(review.id)}
              aria-label={t('reviews.rejectReview', {
                title: review.title || review.id,
                defaultValue: `Reject review ${review.title || review.id}`,
              })}
            >
              <XCircle className="h-4 w-4" />
            </Button>
          </>
        )}
        <Button
          variant="ghost"
          size="sm"
          className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-blue-50 hover:text-blue-600 dark:hover:bg-blue-950/30"
          onClick={() => onRespond(review.id)}
          aria-label={t('reviews.respondToReview', {
            title: review.title || review.id,
            defaultValue: `Respond to review ${review.title || review.id}`,
          })}
        >
          <MessageSquare className="h-4 w-4" />
        </Button>
      </div>
    </TableCell>
  </TableRow>
)

export default ReviewsPage
