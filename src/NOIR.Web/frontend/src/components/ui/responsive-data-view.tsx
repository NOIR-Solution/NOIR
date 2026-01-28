import * as React from 'react'
import { useIsMobile } from '@/hooks/useMediaQuery'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

interface ResponsiveDataViewProps<T> {
  data: T[]
  /** Render function for desktop table view */
  renderTable: () => React.ReactNode
  /** Render function for each item in mobile card view */
  renderCard: (item: T, index: number) => React.ReactNode
  /** Loading state */
  loading?: boolean
  /** Number of skeleton cards to show when loading */
  skeletonCount?: number
  /** Custom className for the container */
  className?: string
  /** Force a specific view mode (overrides responsive behavior) */
  forceView?: 'table' | 'cards'
  /** Gap between cards in mobile view */
  cardGap?: 'sm' | 'md' | 'lg'
}

/**
 * ResponsiveDataView - Switches between table and card views based on screen size
 * - Mobile (<768px): Shows cards
 * - Desktop (>=768px): Shows table
 */
export function ResponsiveDataView<T>({
  data,
  renderTable,
  renderCard,
  loading = false,
  skeletonCount = 3,
  className,
  forceView,
  cardGap = 'md',
}: ResponsiveDataViewProps<T>) {
  const isMobile = useIsMobile()
  const showCards = forceView === 'cards' || (forceView !== 'table' && isMobile)

  const gapClasses = {
    sm: 'gap-2',
    md: 'gap-3',
    lg: 'gap-4',
  }

  if (loading) {
    if (showCards) {
      return (
        <div className={cn('space-y-3', className)}>
          {Array.from({ length: skeletonCount }).map((_, i) => (
            <Card key={i} className="animate-pulse">
              <CardContent className="p-4 space-y-3">
                <Skeleton className="h-5 w-3/4" />
                <Skeleton className="h-4 w-1/2" />
                <div className="flex gap-2">
                  <Skeleton className="h-8 w-16" />
                  <Skeleton className="h-8 w-16" />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )
    }
    return <div className={className}>{renderTable()}</div>
  }

  if (showCards) {
    return (
      <div className={cn('flex flex-col', gapClasses[cardGap], className)}>
        {data.map((item, index) => (
          <React.Fragment key={index}>
            {renderCard(item, index)}
          </React.Fragment>
        ))}
      </div>
    )
  }

  return <div className={className}>{renderTable()}</div>
}

/**
 * MobileCard - Pre-styled card for mobile data views
 */
interface MobileCardProps {
  children: React.ReactNode
  className?: string
  onClick?: () => void
}

export function MobileCard({ children, className, onClick }: MobileCardProps) {
  return (
    <Card
      className={cn(
        'shadow-sm hover:shadow-md transition-shadow duration-200',
        onClick && 'cursor-pointer active:scale-[0.99]',
        className
      )}
      onClick={onClick}
    >
      <CardContent className="p-4">{children}</CardContent>
    </Card>
  )
}

/**
 * MobileCardHeader - Header section for mobile cards
 */
interface MobileCardHeaderProps {
  title: React.ReactNode
  subtitle?: React.ReactNode
  badge?: React.ReactNode
  avatar?: React.ReactNode
}

export function MobileCardHeader({
  title,
  subtitle,
  badge,
  avatar,
}: MobileCardHeaderProps) {
  return (
    <div className="flex items-start gap-3">
      {avatar && <div className="flex-shrink-0">{avatar}</div>}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <h3 className="font-medium text-foreground truncate">{title}</h3>
          {badge}
        </div>
        {subtitle && (
          <p className="text-sm text-muted-foreground truncate mt-0.5">
            {subtitle}
          </p>
        )}
      </div>
    </div>
  )
}

/**
 * MobileCardActions - Action buttons for mobile cards
 */
interface MobileCardActionsProps {
  children: React.ReactNode
  className?: string
}

export function MobileCardActions({ children, className }: MobileCardActionsProps) {
  return (
    <div className={cn('flex items-center gap-2 mt-3 pt-3 border-t', className)}>
      {children}
    </div>
  )
}

/**
 * MobileCardField - Key-value field display for mobile cards
 */
interface MobileCardFieldProps {
  label: string
  value: React.ReactNode
  className?: string
}

export function MobileCardField({ label, value, className }: MobileCardFieldProps) {
  return (
    <div className={cn('flex justify-between items-center text-sm', className)}>
      <span className="text-muted-foreground">{label}</span>
      <span className="text-foreground font-medium">{value}</span>
    </div>
  )
}
