import { Image, Video, Music, FileText, File, type LucideIcon } from 'lucide-react'

export type FileCategory = 'image' | 'video' | 'audio' | 'pdf' | 'unknown'

export interface PreviewFile {
  url: string
  name: string
  mimeType?: string
  thumbnailUrl?: string
  size?: number
}

const IMAGE_EXTENSIONS = new Set(['jpg', 'jpeg', 'png', 'gif', 'webp', 'avif', 'svg', 'bmp', 'ico'])
const VIDEO_EXTENSIONS = new Set(['mp4', 'webm', 'ogg', 'mov'])
const AUDIO_EXTENSIONS = new Set(['mp3', 'wav', 'flac', 'aac', 'm4a'])
const PDF_EXTENSIONS = new Set(['pdf'])

const MIME_PREFIX_MAP: Record<string, FileCategory> = {
  image: 'image',
  video: 'video',
  audio: 'audio',
}

export const getFileExtension = (url: string): string => {
  try {
    const pathname = new URL(url, 'https://placeholder.com').pathname
    const lastDot = pathname.lastIndexOf('.')
    if (lastDot === -1) return ''
    return pathname.slice(lastDot + 1).toLowerCase()
  } catch {
    return ''
  }
}

export const getFileCategory = (url: string, mimeType?: string): FileCategory => {
  // Priority: MIME type detection
  if (mimeType) {
    if (mimeType === 'application/pdf') return 'pdf'
    const prefix = mimeType.split('/')[0]
    if (prefix in MIME_PREFIX_MAP) return MIME_PREFIX_MAP[prefix]
  }

  // Fallback: URL extension detection
  const ext = getFileExtension(url)
  if (!ext) return 'unknown'
  if (IMAGE_EXTENSIONS.has(ext)) return 'image'
  if (VIDEO_EXTENSIONS.has(ext)) return 'video'
  if (AUDIO_EXTENSIONS.has(ext)) return 'audio'
  if (PDF_EXTENSIONS.has(ext)) return 'pdf'

  return 'unknown'
}

export const getFileIcon = (category: FileCategory): LucideIcon => {
  switch (category) {
    case 'image': return Image
    case 'video': return Video
    case 'audio': return Music
    case 'pdf': return FileText
    default: return File
  }
}

export const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`
}

export const isPreviewableInline = (category: FileCategory): boolean => category === 'image'

export const downloadFile = async (url: string, filename: string): Promise<void> => {
  try {
    const response = await fetch(url)
    const blob = await response.blob()
    const blobUrl = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = blobUrl
    link.download = filename
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(blobUrl)
  } catch {
    // Fallback: open in new tab
    window.open(url, '_blank')
  }
}
