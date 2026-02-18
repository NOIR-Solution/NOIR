/**
 * Review types matching backend DTOs.
 */

export type ReviewStatus = 'Pending' | 'Approved' | 'Rejected'

export type ReviewMediaType = 'Image' | 'Video'

export interface ReviewDto {
  id: string
  productId: string
  productName?: string | null
  userId: string
  userName?: string | null
  rating: number
  title?: string | null
  content: string
  status: ReviewStatus
  isVerifiedPurchase: boolean
  helpfulVotes: number
  notHelpfulVotes: number
  adminResponse?: string | null
  adminRespondedAt?: string | null
  mediaUrls: string[]
  createdAt: string
}

export interface ReviewDetailDto {
  id: string
  productId: string
  productName?: string | null
  userId: string
  userName?: string | null
  orderId?: string | null
  rating: number
  title?: string | null
  content: string
  status: ReviewStatus
  isVerifiedPurchase: boolean
  helpfulVotes: number
  notHelpfulVotes: number
  adminResponse?: string | null
  adminRespondedAt?: string | null
  media: ReviewMediaDto[]
  createdAt: string
  modifiedAt?: string | null
}

export interface ReviewMediaDto {
  id: string
  mediaUrl: string
  mediaType: ReviewMediaType
  displayOrder: number
}

export interface ReviewStatsDto {
  averageRating: number
  totalReviews: number
  ratingDistribution: Record<number, number>
}

export interface ReviewPagedResult {
  items: ReviewDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}
