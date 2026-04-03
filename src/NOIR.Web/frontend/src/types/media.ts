/**
 * Media File Types
 */

// Media variant for responsive images
export interface MediaVariant {
  variant: string
  format: string
  url: string
  width: number
  height: number
  sizeBytes: number
}

// Full media file details
export interface MediaFile {
  id: string
  shortId: string
  slug: string
  originalFileName: string
  folder: string
  defaultUrl: string
  thumbHash?: string
  dominantColor?: string
  width: number
  height: number
  aspectRatio: number
  format: string
  mimeType: string
  sizeBytes: number
  hasTransparency: boolean
  altText?: string
  variants: MediaVariant[]
  srcsets: Record<string, string>
  createdAt: string
}

// List DTO (lightweight, for grid/list views)
export interface MediaFileListItem {
  id: string
  shortId: string
  slug: string
  originalFileName: string
  folder: string
  defaultUrl: string
  thumbHash?: string
  dominantColor?: string
  width: number
  height: number
  format: string
  mimeType: string
  sizeBytes: number
  altText?: string
  createdAt: string
  modifiedByName?: string
}

// Paginated result
export interface PagedMediaResult {
  items: MediaFileListItem[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

// Query params
export interface MediaFilesParams {
  search?: string
  fileType?: string
  folder?: string
  orderBy?: string
  isDescending?: boolean
  page?: number
  pageSize?: number
}

// Bulk operation result
export interface BulkMediaOperationResult {
  success: number
  failed: number
  errors: { mediaFileId: string; fileName?: string; message: string }[]
}

// Upload result from media upload endpoint
export interface MediaUploadResult {
  success: boolean
  error?: string
  slug?: string
  defaultUrl?: string
  location?: string
  thumbHash?: string
  dominantColor?: string
  metadata?: {
    width: number
    height: number
    format: string
    mimeType: string
    sizeBytes: number
    hasTransparency: boolean
  }
  variants?: MediaVariant[]
  srcsets?: Record<string, string>
  processingTimeMs?: number
  mediaFileId?: string
  shortId?: string
}
