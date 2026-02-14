import { useState, useEffect, useMemo } from 'react'
import { thumbHashToDataURL } from 'thumbhash'
import { cn } from '@/lib/utils'

interface ThumbHashImageProps {
  /** The actual image URL to load */
  src: string
  /** Base64-encoded ThumbHash for placeholder */
  thumbHash?: string | null
  /** Dominant color as hex (fallback if no thumbHash) */
  dominantColor?: string | null
  /** Alt text for the image */
  alt: string
  /** CSS class name */
  className?: string
  /** Image width */
  width?: number | string
  /** Image height */
  height?: number | string
  /** Loading strategy */
  loading?: 'lazy' | 'eager'
  /** Decoding strategy */
  decoding?: 'async' | 'sync' | 'auto'
  /** Object fit style */
  objectFit?: 'cover' | 'contain' | 'fill' | 'none' | 'scale-down'
  /** Srcset for responsive images */
  srcSet?: string
  /** Sizes attribute for responsive images */
  sizes?: string
  /** Callback when image loads */
  onLoad?: () => void
  /** Callback when image fails to load */
  onError?: () => void
}

/**
 * Image component with ThumbHash placeholder support.
 * Shows a blurred placeholder while the actual image loads,
 * then fades in the real image for a smooth experience.
 */
export const ThumbHashImage = ({
  src,
  thumbHash,
  dominantColor,
  alt,
  className,
  width,
  height,
  loading = 'lazy',
  decoding = 'async',
  objectFit = 'cover',
  srcSet,
  sizes,
  onLoad,
  onError,
}: ThumbHashImageProps) => {
  const [isLoaded, setIsLoaded] = useState(false)
  const [hasError, setHasError] = useState(false)

  // Decode ThumbHash to data URL
  const placeholderUrl = useMemo(() => {
    if (!thumbHash) return null

    try {
      // Decode base64 to Uint8Array
      const binaryString = atob(thumbHash)
      const bytes = new Uint8Array(binaryString.length)
      for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i)
      }
      return thumbHashToDataURL(bytes)
    } catch (error) {
      console.warn('Failed to decode ThumbHash:', error)
      return null
    }
  }, [thumbHash])

  // Reset state when src changes
  useEffect(() => {
    setIsLoaded(false)
    setHasError(false)
  }, [src])

  const handleLoad = () => {
    setIsLoaded(true)
    onLoad?.()
  }

  const handleError = () => {
    setHasError(true)
    onError?.()
  }

  // Determine background (placeholder or dominant color)
  const backgroundStyle = placeholderUrl
    ? { backgroundImage: `url(${placeholderUrl})`, backgroundSize: 'cover', backgroundPosition: 'center' }
    : dominantColor
      ? { backgroundColor: dominantColor }
      : { backgroundColor: '#e5e7eb' } // gray-200 fallback

  const objectFitClass = {
    cover: 'object-cover',
    contain: 'object-contain',
    fill: 'object-fill',
    none: 'object-none',
    'scale-down': 'object-scale-down',
  }[objectFit]

  return (
    <div
      className={cn('relative overflow-hidden', className)}
      style={{
        width: typeof width === 'number' ? `${width}px` : width,
        height: typeof height === 'number' ? `${height}px` : height,
        ...backgroundStyle,
      }}
    >
      {!hasError && (
        <img
          src={src}
          srcSet={srcSet}
          sizes={sizes}
          alt={alt}
          width={typeof width === 'number' ? width : undefined}
          height={typeof height === 'number' ? height : undefined}
          loading={loading}
          decoding={decoding}
          onLoad={handleLoad}
          onError={handleError}
          className={cn(
            'absolute inset-0 w-full h-full transition-opacity duration-300',
            objectFitClass,
            isLoaded ? 'opacity-100' : 'opacity-0'
          )}
        />
      )}

      {/* Error state */}
      {hasError && (
        <div className="absolute inset-0 flex items-center justify-center bg-gray-100">
          <span className="text-gray-400 text-sm">Failed to load</span>
        </div>
      )}
    </div>
  )
}

/**
 * Hook to decode ThumbHash to data URL.
 * Useful when you need the placeholder URL separately.
 */
export const useThumbHashUrl = (thumbHash: string | null | undefined): string | null => {
  return useMemo(() => {
    if (!thumbHash) return null

    try {
      const binaryString = atob(thumbHash)
      const bytes = new Uint8Array(binaryString.length)
      for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i)
      }
      return thumbHashToDataURL(bytes)
    } catch {
      return null
    }
  }, [thumbHash])
}

/**
 * Hook to get average color from ThumbHash.
 */
export const useThumbHashColor = (thumbHash: string | null | undefined): string | null => {
  return useMemo(() => {
    if (!thumbHash) return null

    try {
      const binaryString = atob(thumbHash)
      const bytes = new Uint8Array(binaryString.length)
      for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i)
      }

      // Extract average color from ThumbHash header
      // ThumbHash stores LQCA in first 4 bytes
      const l = (bytes[0] & 63) / 63.0
      const p = ((bytes[0] >> 6) | ((bytes[1] & 3) << 2)) / 31.0 - 0.5
      const q = ((bytes[1] >> 2) & 31) / 31.0 - 0.5
      // Convert from LPQ to RGB (simplified)
      const b = l - 2.0 / 3.0 * p
      const r = (3.0 * l - b + q) / 2.0
      const g = r - q

      const toHex = (v: number) => {
        const clamped = Math.max(0, Math.min(1, v))
        return Math.round(clamped * 255).toString(16).padStart(2, '0')
      }

      return `#${toHex(r)}${toHex(g)}${toHex(b)}`
    } catch {
      return null
    }
  }, [thumbHash])
}
