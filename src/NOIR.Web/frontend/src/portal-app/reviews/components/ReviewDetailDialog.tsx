import { useTranslation } from 'react-i18next'
import {
  CheckCircle2,
  ExternalLink,
  MessageSquare,
  ShieldCheck,
  Star,
  ThumbsDown,
  ThumbsUp,
  XCircle,
} from 'lucide-react'
import {
  Badge,
  Button,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaHeader,
  CredenzaTitle,
  Separator,
  Skeleton,
} from '@uikit'
import { useReviewQuery } from '@/portal-app/reviews/queries'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import type { ReviewStatus } from '@/types/review'

interface ReviewDetailDialogProps {
  reviewId: string | undefined
  open: boolean
  onOpenChange: (open: boolean) => void
  onApprove: (id: string) => void
  onReject: (id: string) => void
  onRespond: (id: string) => void
}

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

const StarRating = ({ rating, size = 'md' }: { rating: number; size?: 'sm' | 'md' }) => {
  const sizeClass = size === 'sm' ? 'h-3.5 w-3.5' : 'h-5 w-5'
  return (
    <div className="flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((star) => (
        <Star
          key={star}
          className={`${sizeClass} ${
            star <= rating
              ? 'fill-yellow-400 text-yellow-400'
              : 'fill-muted text-muted-foreground/30'
          }`}
        />
      ))}
    </div>
  )
}

export const ReviewDetailDialog = ({
  reviewId,
  open,
  onOpenChange,
  onApprove,
  onReject,
  onRespond,
}: ReviewDetailDialogProps) => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()
  const { data: review, isLoading } = useReviewQuery(reviewId)

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[600px]">
        <CredenzaHeader>
          <CredenzaTitle>{t('reviews.reviewDetail', 'Review Detail')}</CredenzaTitle>
          <CredenzaDescription>
            {t('reviews.reviewDetailDescription', 'View full review details and take moderation actions.')}
          </CredenzaDescription>
        </CredenzaHeader>

        <CredenzaBody>
          {isLoading ? (
            <div className="space-y-4">
              <Skeleton className="h-6 w-48" />
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-20 w-full" />
            </div>
          ) : review ? (
            <div className="space-y-5">
              {/* Header: Rating + Status */}
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <StarRating rating={review.rating} />
                  <span className="text-sm text-muted-foreground">
                    {review.rating}/5
                  </span>
                </div>
                <Badge variant="outline" className={getStatusColor(review.status)}>
                  {t(`reviews.status.${review.status.toLowerCase()}`, review.status)}
                </Badge>
              </div>

              {/* Product + Customer Info */}
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-muted-foreground">
                    {t('reviews.product', 'Product')}
                  </span>
                  <p className="font-medium mt-0.5">{review.productName || '-'}</p>
                </div>
                <div>
                  <span className="text-muted-foreground">
                    {t('reviews.customer', 'Customer')}
                  </span>
                  <div className="flex items-center gap-1.5 mt-0.5">
                    <span className="font-medium">{review.userName || '-'}</span>
                    {review.isVerifiedPurchase && (
                      <ShieldCheck
                        className="h-3.5 w-3.5 text-blue-500"
                        aria-label={t('reviews.verifiedPurchase', 'Verified purchase')}
                      />
                    )}
                  </div>
                </div>
                <div>
                  <span className="text-muted-foreground">{t('labels.date', 'Date')}</span>
                  <p className="font-medium mt-0.5">{formatDateTime(review.createdAt)}</p>
                </div>
                {review.orderId && (
                  <div>
                    <span className="text-muted-foreground">
                      {t('reviews.orderId', 'Order')}
                    </span>
                    <p className="font-medium mt-0.5 flex items-center gap-1">
                      <span className="font-mono text-xs">{review.orderId.slice(0, 8)}...</span>
                      <ExternalLink className="h-3 w-3" />
                    </p>
                  </div>
                )}
              </div>

              <Separator />

              {/* Review Content */}
              <div>
                {review.title && (
                  <h4 className="font-semibold text-base mb-2">{review.title}</h4>
                )}
                <p className="text-sm leading-relaxed whitespace-pre-wrap">{review.content}</p>
              </div>

              {/* Media Gallery */}
              {review.media.length > 0 && (
                <div>
                  <span className="text-sm font-medium text-muted-foreground mb-2 block">
                    {t('reviews.media', 'Media')} ({review.media.length})
                  </span>
                  <div className="grid grid-cols-4 gap-2">
                    {review.media.map((item) => (
                      <div
                        key={item.id}
                        className="aspect-square rounded-lg border border-border/50 overflow-hidden bg-muted"
                      >
                        {item.mediaType === 'Image' ? (
                          <img
                            src={item.mediaUrl}
                            alt={t('reviews.reviewMedia', 'Review media')}
                            className="w-full h-full object-cover"
                          />
                        ) : (
                          <video
                            src={item.mediaUrl}
                            className="w-full h-full object-cover"
                            controls
                          />
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Helpfulness Votes */}
              <div className="flex items-center gap-4 text-sm text-muted-foreground">
                <div className="flex items-center gap-1">
                  <ThumbsUp className="h-3.5 w-3.5" />
                  <span>
                    {review.helpfulVotes} {t('reviews.helpful', 'helpful')}
                  </span>
                </div>
                <div className="flex items-center gap-1">
                  <ThumbsDown className="h-3.5 w-3.5" />
                  <span>
                    {review.notHelpfulVotes} {t('reviews.notHelpful', 'not helpful')}
                  </span>
                </div>
              </div>

              {/* Admin Response */}
              {review.adminResponse && (
                <>
                  <Separator />
                  <div className="bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-900 rounded-lg p-4">
                    <div className="flex items-center gap-2 mb-2">
                      <MessageSquare className="h-4 w-4 text-blue-600" />
                      <span className="text-sm font-medium text-blue-700 dark:text-blue-400">
                        {t('reviews.adminResponse', 'Admin Response')}
                      </span>
                      {review.adminRespondedAt && (
                        <span className="text-xs text-blue-500">
                          {formatDateTime(review.adminRespondedAt)}
                        </span>
                      )}
                    </div>
                    <p className="text-sm whitespace-pre-wrap">{review.adminResponse}</p>
                  </div>
                </>
              )}

              <Separator />

              {/* Actions */}
              <div className="flex items-center justify-end gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="cursor-pointer"
                  onClick={() => {
                    onRespond(review.id)
                    onOpenChange(false)
                  }}
                >
                  <MessageSquare className="h-4 w-4 mr-1" />
                  {review.adminResponse
                    ? t('reviews.editResponse', 'Edit Response')
                    : t('reviews.addResponse', 'Add Response')}
                </Button>
                {review.status === 'Pending' && (
                  <>
                    <Button
                      variant="outline"
                      size="sm"
                      className="cursor-pointer text-green-600 hover:text-green-700 hover:bg-green-50 dark:hover:bg-green-950/30"
                      onClick={() => {
                        onApprove(review.id)
                        onOpenChange(false)
                      }}
                    >
                      <CheckCircle2 className="h-4 w-4 mr-1" />
                      {t('reviews.approve', 'Approve')}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      className="cursor-pointer text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30"
                      onClick={() => {
                        onReject(review.id)
                        onOpenChange(false)
                      }}
                    >
                      <XCircle className="h-4 w-4 mr-1" />
                      {t('reviews.reject', 'Reject')}
                    </Button>
                  </>
                )}
              </div>
            </div>
          ) : (
            <div className="py-8 text-center text-muted-foreground">
              {t('reviews.reviewNotFound', 'Review not found')}
            </div>
          )}
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}
