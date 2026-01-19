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

// Upload result from media upload endpoint
export interface MediaUploadResult {
  success: boolean
  error?: string
  slug?: string
  defaultUrl?: string
  location?: string // TinyMCE compatibility alias for defaultUrl
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
