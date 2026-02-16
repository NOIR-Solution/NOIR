import { useState, useRef, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Image as ImageIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Popover, PopoverContent, PopoverTrigger } from '../popover/Popover'
import { Tooltip, TooltipContent, TooltipTrigger } from '../tooltip/Tooltip'
import { FilePreviewModal } from './FilePreviewModal'
import { getFileCategory, getFileIcon, isPreviewableInline, type PreviewFile } from './file-preview.utils'

interface FilePreviewTriggerProps {
  file: PreviewFile
  files?: PreviewFile[]
  index?: number
  thumbnailWidth?: number | string
  thumbnailHeight?: number | string
  className?: string
  imageClassName?: string
  showHoverPreview?: boolean
  fallback?: React.ReactNode
  children?: React.ReactNode
}

export const FilePreviewTrigger = ({
  file,
  files,
  index = 0,
  thumbnailWidth = 48,
  thumbnailHeight = 48,
  className,
  imageClassName,
  showHoverPreview = true,
  fallback,
  children,
}: FilePreviewTriggerProps) => {
  const { t } = useTranslation('common')
  const [modalOpen, setModalOpen] = useState(false)
  const [hoverOpen, setHoverOpen] = useState(false)
  const [imageError, setImageError] = useState(false)
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const category = getFileCategory(file.url, file.mimeType)
  const isImage = isPreviewableInline(category)
  const allFiles = files ?? [file]
  const displaySrc = file.thumbnailUrl || file.url

  const widthStyle = typeof thumbnailWidth === 'number' ? `${thumbnailWidth}px` : thumbnailWidth
  const heightStyle = typeof thumbnailHeight === 'number' ? `${thumbnailHeight}px` : thumbnailHeight

  const showPopover = useCallback(() => {
    if (closeTimer.current) {
      clearTimeout(closeTimer.current)
      closeTimer.current = null
    }
    setHoverOpen(true)
  }, [])

  const scheduleClose = useCallback(() => {
    closeTimer.current = setTimeout(() => setHoverOpen(false), 150)
  }, [])

  const handleClick = useCallback((e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setHoverOpen(false)
    setModalOpen(true)
  }, [])

  const handleImageError = useCallback(() => {
    setImageError(true)
  }, [])

  const ariaLabel = t('filePreview.openPreview', 'Open preview for {{name}}', { name: file.name })
  const hoverLabel = t('filePreview.clickToPreview', 'Click to preview')

  // Fallback placeholder
  const defaultFallback = (
    <div
      className={cn(
        'bg-muted rounded-md flex items-center justify-center flex-shrink-0',
        className,
      )}
      style={{ width: widthStyle, height: heightStyle }}
    >
      <ImageIcon className="w-5 h-5 text-muted-foreground" />
    </div>
  )

  // No URL or image failed — show fallback
  if (!file.url || (isImage && imageError)) {
    return fallback ? <>{fallback}</> : defaultFallback
  }

  // Render the thumbnail content
  const renderThumbnail = () => {
    if (children) return children

    if (isImage) {
      return (
        <img
          src={displaySrc}
          alt={file.name}
          className={cn('w-full h-full object-cover rounded-md', imageClassName)}
          onError={handleImageError}
        />
      )
    }

    // Non-image: icon thumbnail
    const FileIcon = getFileIcon(category)
    return (
      <div
        className={cn(
          'bg-muted rounded-md flex items-center justify-center w-full h-full',
          imageClassName,
        )}
      >
        <FileIcon className="w-5 h-5 text-muted-foreground" />
      </div>
    )
  }

  const triggerButton = (
    <button
      type="button"
      onClick={handleClick}
      className={cn(
        'cursor-pointer focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 rounded-md transition-all hover:opacity-80 hover:ring-2 hover:ring-primary/50 flex-shrink-0',
        className,
      )}
      style={{ width: widthStyle, height: heightStyle }}
      aria-label={ariaLabel}
    >
      {renderThumbnail()}
    </button>
  )

  // Image with hover preview using Popover (ColorPopover pattern)
  const wrappedTrigger = isImage && showHoverPreview ? (
    <Popover open={hoverOpen} onOpenChange={setHoverOpen}>
      <PopoverTrigger asChild>
        <div
          onMouseEnter={showPopover}
          onMouseLeave={scheduleClose}
        >
          {triggerButton}
        </div>
      </PopoverTrigger>
      <PopoverContent
        className="w-auto p-1"
        side="top"
        align="start"
        onMouseEnter={showPopover}
        onMouseLeave={scheduleClose}
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <img
          src={file.url}
          alt={file.name}
          className="max-w-[240px] max-h-[240px] rounded object-contain"
          onError={handleImageError}
        />
      </PopoverContent>
    </Popover>
  ) : !isImage ? (
    <Tooltip>
      <TooltipTrigger asChild>
        {triggerButton}
      </TooltipTrigger>
      <TooltipContent>
        {hoverLabel}
      </TooltipContent>
    </Tooltip>
  ) : (
    triggerButton
  )

  return (
    <>
      {wrappedTrigger}
      <FilePreviewModal
        open={modalOpen}
        onOpenChange={setModalOpen}
        files={allFiles}
        initialIndex={index}
      />
    </>
  )
}
