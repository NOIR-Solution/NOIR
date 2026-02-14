import { useState, useCallback } from 'react'
import Lightbox from 'yet-another-react-lightbox'
import Zoom from 'yet-another-react-lightbox/plugins/zoom'
import 'yet-another-react-lightbox/styles.css'
import { cn } from '@/lib/utils'
import { Image as ImageIcon } from 'lucide-react'

interface ImageLightboxProps {
  /** The image source URL (used for lightbox full view) */
  src: string
  /** Thumbnail URL for the small preview (defaults to src if not provided) */
  thumbnailSrc?: string
  /** Alt text for the image */
  alt?: string
  /** Thumbnail width (default: 48px) */
  thumbnailWidth?: number | string
  /** Thumbnail height (default: 48px) */
  thumbnailHeight?: number | string
  /** Additional className for the thumbnail container */
  className?: string
  /** Additional className for the thumbnail image */
  imageClassName?: string
  /** Placeholder to show when image fails to load */
  fallback?: React.ReactNode
  /** Show placeholder icon when no src */
  showPlaceholder?: boolean
}

/**
 * A clickable thumbnail that opens a lightbox to view the full image.
 * Supports zoom functionality for detailed viewing.
 */
export const ImageLightbox = ({
  src,
  thumbnailSrc,
  alt = '',
  thumbnailWidth = 48,
  thumbnailHeight = 48,
  className,
  imageClassName,
  fallback,
  showPlaceholder = true,
}: ImageLightboxProps) => {
  // Use thumbnailSrc for display, src for lightbox full view
  const displaySrc = thumbnailSrc || src
  const [open, setOpen] = useState(false)
  const [imageError, setImageError] = useState(false)

  const handleImageError = useCallback(() => {
    setImageError(true)
  }, [])

  const handleClick = useCallback((e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (src && !imageError) {
      setOpen(true)
    }
  }, [src, imageError])

  // Default fallback placeholder
  const defaultFallback = (
    <div
      className={cn(
        'bg-muted rounded-md flex items-center justify-center flex-shrink-0',
        className
      )}
      style={{
        width: typeof thumbnailWidth === 'number' ? `${thumbnailWidth}px` : thumbnailWidth,
        height: typeof thumbnailHeight === 'number' ? `${thumbnailHeight}px` : thumbnailHeight,
      }}
    >
      <ImageIcon className="w-5 h-5 text-muted-foreground" />
    </div>
  )

  // Show fallback if no src, image error, or showPlaceholder is true
  if (!src || imageError) {
    if (fallback) return <>{fallback}</>
    if (showPlaceholder) return defaultFallback
    return null
  }

  return (
    <>
      <button
        type="button"
        onClick={handleClick}
        className={cn(
          'cursor-pointer focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 rounded-md transition-all hover:opacity-80 hover:ring-2 hover:ring-primary/50 flex-shrink-0',
          className
        )}
        style={{
          width: typeof thumbnailWidth === 'number' ? `${thumbnailWidth}px` : thumbnailWidth,
          height: typeof thumbnailHeight === 'number' ? `${thumbnailHeight}px` : thumbnailHeight,
        }}
        title="Click to view full image"
      >
        <img
          src={displaySrc}
          alt={alt}
          className={cn('w-full h-full object-cover rounded-md', imageClassName)}
          onError={handleImageError}
        />
      </button>

      <Lightbox
        open={open}
        close={() => setOpen(false)}
        slides={[{ src, alt }]}
        plugins={[Zoom]}
        controller={{
          closeOnBackdropClick: true,
        }}
        zoom={{
          maxZoomPixelRatio: 3,
          zoomInMultiplier: 2,
          doubleTapDelay: 300,
          doubleClickDelay: 300,
          doubleClickMaxStops: 2,
          keyboardMoveDistance: 50,
          wheelZoomDistanceFactor: 100,
          pinchZoomDistanceFactor: 100,
          scrollToZoom: true,
        }}
        carousel={{
          finite: true,
        }}
        render={{
          buttonPrev: () => null,
          buttonNext: () => null,
        }}
        styles={{
          container: { backgroundColor: 'rgba(0, 0, 0, 0.9)' },
        }}
      />
    </>
  )
}
