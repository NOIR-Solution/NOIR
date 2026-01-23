/**
 * Media API Service
 *
 * Provides methods for uploading and managing media files.
 */
import { apiClient } from './apiClient'
import type { MediaFile, MediaUploadResult } from '@/types'

export type MediaFolder = 'blog' | 'content' | 'avatars' | 'branding'

/**
 * Upload a file to the media service
 */
export async function uploadMedia(
  file: File,
  folder: MediaFolder = 'content',
  entityId?: string
): Promise<MediaUploadResult> {
  const formData = new FormData()
  formData.append('file', file)

  const queryParams = new URLSearchParams()
  queryParams.append('folder', folder)
  if (entityId) {
    queryParams.append('entityId', entityId)
  }

  const response = await fetch(`/api/media/upload?${queryParams.toString()}`, {
    method: 'POST',
    body: formData,
    credentials: 'include',
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Upload failed' }))
    throw new Error(error.error || 'Upload failed')
  }

  return response.json()
}

/**
 * Get media file by ID
 */
export async function getMediaById(id: string): Promise<MediaFile> {
  return apiClient<MediaFile>(`/media/${id}`)
}

/**
 * Get media file by slug
 */
export async function getMediaBySlug(slug: string): Promise<MediaFile> {
  return apiClient<MediaFile>(`/media/by-slug/${slug}`)
}

/**
 * Get media file by short ID
 */
export async function getMediaByShortId(shortId: string): Promise<MediaFile> {
  return apiClient<MediaFile>(`/media/by-short-id/${shortId}`)
}

/**
 * Get media file by URL
 */
export async function getMediaByUrl(url: string): Promise<MediaFile> {
  const queryParams = new URLSearchParams()
  queryParams.append('url', url)
  return apiClient<MediaFile>(`/media/by-url?${queryParams.toString()}`)
}

/**
 * Batch get media files by IDs
 */
export async function getMediaByIds(ids: string[]): Promise<MediaFile[]> {
  return apiClient<MediaFile[]>('/media/batch/by-ids', {
    method: 'POST',
    body: JSON.stringify({ ids }),
  })
}

/**
 * Batch get media files by slugs
 */
export async function getMediaBySlugs(slugs: string[]): Promise<MediaFile[]> {
  return apiClient<MediaFile[]>('/media/batch/by-slugs', {
    method: 'POST',
    body: JSON.stringify({ slugs }),
  })
}

/**
 * Batch get media files by short IDs
 */
export async function getMediaByShortIds(shortIds: string[]): Promise<MediaFile[]> {
  return apiClient<MediaFile[]>('/media/batch/by-short-ids', {
    method: 'POST',
    body: JSON.stringify({ shortIds }),
  })
}
