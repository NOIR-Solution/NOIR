import * as React from 'react'
import { cn } from '@/lib/utils'

interface AvatarProps extends React.HTMLAttributes<HTMLSpanElement> {
  src?: string
  alt?: string
  fallback?: string
  size?: 'sm' | 'md' | 'lg'
}

const sizeClasses = {
  sm: 'h-8 w-8 text-xs',
  md: 'h-10 w-10 text-sm',
  lg: 'h-12 w-12 text-base',
}

const Avatar = React.forwardRef<HTMLSpanElement, AvatarProps>(
  ({ className, src, alt, fallback, size = 'md', ...props }, ref) => {
    const [imageError, setImageError] = React.useState(false)
    const showFallback = !src || imageError

    // Generate initials from fallback or alt
    const initials = React.useMemo(() => {
      const text = fallback || alt || ''
      const parts = text.split(/[\s@]+/).filter(Boolean)
      if (parts.length >= 2) {
        return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
      }
      return text.slice(0, 2).toUpperCase() || '?'
    }, [fallback, alt])

    return (
      <span
        ref={ref}
        className={cn(
          'relative flex shrink-0 overflow-hidden rounded-full',
          sizeClasses[size],
          className
        )}
        {...props}
      >
        {!showFallback ? (
          <img
            src={src}
            alt={alt}
            className="aspect-square h-full w-full object-cover"
            onError={() => setImageError(true)}
          />
        ) : (
          <span
            className={cn(
              'flex h-full w-full items-center justify-center rounded-full',
              'bg-primary/10 text-primary font-medium'
            )}
          >
            {initials}
          </span>
        )}
      </span>
    )
  }
)
Avatar.displayName = 'Avatar'

export { Avatar }
export type { AvatarProps }
