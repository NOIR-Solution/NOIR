import { Loader2, RefreshCw } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Skeleton } from './skeleton'

/**
 * Loading Components - Standardized loading states for NOIR
 *
 * Best Practices Applied:
 * - Skeletons for content loading (pages, tables, lists, dialogs) - reduces perceived wait time
 * - Loader2 spinner for button/inline actions - fast feedback for interactions
 * - RefreshCw for refresh operations - indicates data refresh vs initial load
 *
 * References:
 * - Nielsen Norman Group: Skeleton screens reduce perceived loading time
 * - Facebook, LinkedIn, Instagram all use skeleton-first approach
 */

// =============================================================================
// SPINNER COMPONENTS - For buttons and inline actions
// =============================================================================

interface SpinnerProps {
  className?: string
  size?: 'xs' | 'sm' | 'md' | 'lg'
}

const spinnerSizes = {
  xs: 'h-3 w-3',
  sm: 'h-4 w-4',
  md: 'h-5 w-5',
  lg: 'h-6 w-6',
}

/**
 * Spinner - Standardized inline loading spinner using Loader2
 * Use for: Button loading states, inline actions, small loading indicators
 */
export function Spinner({ className, size = 'sm' }: SpinnerProps) {
  return (
    <Loader2
      className={cn('animate-spin', spinnerSizes[size], className)}
    />
  )
}

/**
 * RefreshSpinner - For refresh operations
 * Use for: Refresh buttons, data reload indicators
 */
export function RefreshSpinner({ className, size = 'sm', isRefreshing = false }: SpinnerProps & { isRefreshing?: boolean }) {
  return (
    <RefreshCw
      className={cn(spinnerSizes[size], isRefreshing && 'animate-spin', className)}
    />
  )
}

/**
 * ButtonSpinner - Spinner with proper spacing for button content
 * Use for: Inside buttons during form submission
 */
export function ButtonSpinner({ className }: { className?: string }) {
  return <Loader2 className={cn('mr-2 h-4 w-4 animate-spin', className)} />
}

// =============================================================================
// PAGE/CONTAINER LOADING - For full page or container loading states
// =============================================================================

interface PageSpinnerProps {
  className?: string
  text?: string
  fullScreen?: boolean
}

/**
 * PageSpinner - Full page or container loading indicator
 * Use for: Initial page load, route transitions, authentication checks
 * Note: Prefer skeleton loading for content areas when possible
 */
export function PageSpinner({ className, text, fullScreen = false }: PageSpinnerProps) {
  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center gap-3',
        fullScreen ? 'fixed inset-0 bg-background/80 backdrop-blur-sm z-50' : 'py-12',
        className
      )}
    >
      <Loader2 className="h-8 w-8 animate-spin text-primary" />
      {text && <p className="text-sm text-muted-foreground">{text}</p>}
    </div>
  )
}

// =============================================================================
// SKELETON COMPONENTS - For content placeholder loading
// =============================================================================

/**
 * TableRowSkeleton - Skeleton for table rows
 * Use for: Table loading states (UserTable, RoleTable, TenantTable)
 */
export function TableRowSkeleton({
  columns = 4,
  rows = 5,
  showAvatar = false,
}: {
  columns?: number
  rows?: number
  showAvatar?: boolean
}) {
  return (
    <div className="space-y-3">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="flex items-center space-x-4">
          {showAvatar && <Skeleton className="h-10 w-10 rounded-full" />}
          <div className="flex-1 space-y-2">
            <Skeleton className="h-4 w-[200px]" />
            <Skeleton className="h-3 w-[150px]" />
          </div>
          {Array.from({ length: columns - (showAvatar ? 2 : 1) }).map((_, j) => (
            <Skeleton key={j} className="h-4 w-[100px]" />
          ))}
        </div>
      ))}
    </div>
  )
}

/**
 * CardSkeleton - Skeleton for card content
 * Use for: Card loading states, preference cards, settings panels
 */
export function CardSkeleton({
  showIcon = true,
  lines = 2,
}: {
  showIcon?: boolean
  lines?: number
}) {
  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        {showIcon && <Skeleton className="h-10 w-10 rounded-lg" />}
        <div className="flex-1 space-y-2">
          <Skeleton className="h-4 w-[140px]" />
          <Skeleton className="h-3 w-[200px]" />
        </div>
      </div>
      {lines > 0 && (
        <div className="space-y-2 pt-2">
          {Array.from({ length: lines }).map((_, i) => (
            <Skeleton key={i} className="h-4 w-full" />
          ))}
        </div>
      )}
    </div>
  )
}

/**
 * TimelineEntrySkeleton - Skeleton for timeline/activity entries
 * Use for: Activity timeline, notification lists
 */
export function TimelineEntrySkeleton({
  count = 5,
}: {
  count?: number
}) {
  return (
    <>
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="flex gap-4 mb-4">
          <Skeleton className="h-10 w-10 rounded-full flex-shrink-0" />
          <div className="flex-1 space-y-2 p-4 border rounded-lg">
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-3 w-1/2" />
          </div>
        </div>
      ))}
    </>
  )
}

/**
 * ListItemSkeleton - Skeleton for list items
 * Use for: Dropdown lists, notification lists, log entries
 */
export function ListItemSkeleton({
  count = 3,
  showIcon = true,
}: {
  count?: number
  showIcon?: boolean
}) {
  return (
    <div className="space-y-2">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="flex items-center gap-3 p-2">
          {showIcon && <Skeleton className="h-8 w-8 rounded-full" />}
          <div className="flex-1 space-y-1">
            <Skeleton className="h-4 w-[180px]" />
            <Skeleton className="h-3 w-[120px]" />
          </div>
        </div>
      ))}
    </div>
  )
}

/**
 * FormSkeleton - Skeleton for form fields
 * Use for: Dialog form loading, settings form loading
 */
export function FormSkeleton({
  fields = 3,
}: {
  fields?: number
}) {
  return (
    <div className="space-y-4">
      {Array.from({ length: fields }).map((_, i) => (
        <div key={i} className="space-y-2">
          <Skeleton className="h-4 w-[80px]" />
          <Skeleton className="h-10 w-full" />
        </div>
      ))}
    </div>
  )
}

/**
 * InlineContentSkeleton - Inline skeleton for text content
 * Use for: Loading text within paragraphs, stats, counters
 */
export function InlineContentSkeleton({
  width = 'w-[60px]',
}: {
  width?: string
}) {
  return <Skeleton className={cn('h-4 inline-block', width)} />
}
