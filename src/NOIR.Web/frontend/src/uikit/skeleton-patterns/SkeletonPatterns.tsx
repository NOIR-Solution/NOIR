import { Skeleton } from '../skeleton/Skeleton'
import { Card, CardContent, CardHeader } from '../card/Card'
import { TableCell, TableRow } from '../table/Table'
import { cn } from '@/lib/utils'

/**
 * TableRowSkeleton - Skeleton for table rows
 */
interface TableRowSkeletonProps {
  columns?: number
  className?: string
}

export function TableRowSkeleton({ columns = 5, className }: TableRowSkeletonProps) {
  return (
    <TableRow className={cn('animate-pulse', className)}>
      {Array.from({ length: columns }).map((_, i) => (
        <TableCell key={i}>
          <Skeleton className="h-4 w-full" />
        </TableCell>
      ))}
    </TableRow>
  )
}

/**
 * TableSkeleton - Multiple skeleton rows for tables
 */
interface TableSkeletonProps {
  rows?: number
  columns?: number
  className?: string
}

export function TableSkeleton({ rows = 5, columns = 5, className }: TableSkeletonProps) {
  return (
    <>
      {Array.from({ length: rows }).map((_, i) => (
        <TableRowSkeleton key={i} columns={columns} className={className} />
      ))}
    </>
  )
}

/**
 * CardGridSkeleton - Skeleton for card grid layouts
 */
interface CardGridSkeletonProps {
  count?: number
  columns?: 1 | 2 | 3 | 4
  showImage?: boolean
  className?: string
}

export function CardGridSkeleton({
  count = 6,
  columns = 3,
  showImage = true,
  className,
}: CardGridSkeletonProps) {
  const gridCols = {
    1: 'grid-cols-1',
    2: 'grid-cols-1 sm:grid-cols-2',
    3: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
    4: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4',
  }

  return (
    <div className={cn('grid gap-4', gridCols[columns], className)}>
      {Array.from({ length: count }).map((_, i) => (
        <Card key={i} className="animate-pulse overflow-hidden">
          {showImage && <Skeleton className="h-40 w-full rounded-none" />}
          <CardContent className="p-4 space-y-3">
            <Skeleton className="h-5 w-3/4" />
            <Skeleton className="h-4 w-1/2" />
            <div className="flex gap-2 pt-2">
              <Skeleton className="h-6 w-16 rounded-full" />
              <Skeleton className="h-6 w-16 rounded-full" />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}

/**
 * PageHeaderSkeleton - Skeleton for page headers
 */
export function PageHeaderSkeleton({ className }: { className?: string }) {
  return (
    <div className={cn('flex items-center gap-4 animate-pulse', className)}>
      <Skeleton className="h-12 w-12 rounded-2xl" />
      <div className="space-y-2">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-4 w-64" />
      </div>
    </div>
  )
}

/**
 * FormSkeleton - Skeleton for form layouts
 */
interface FormSkeletonProps {
  fields?: number
  className?: string
}

export function FormSkeleton({ fields = 4, className }: FormSkeletonProps) {
  return (
    <div className={cn('space-y-6 animate-pulse', className)}>
      {Array.from({ length: fields }).map((_, i) => (
        <div key={i} className="space-y-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-10 w-full" />
        </div>
      ))}
      <div className="flex gap-3 pt-4">
        <Skeleton className="h-10 w-24" />
        <Skeleton className="h-10 w-24" />
      </div>
    </div>
  )
}

/**
 * StatCardSkeleton - Skeleton for stat/metric cards
 */
export function StatCardSkeleton({ className }: { className?: string }) {
  return (
    <Card className={cn('animate-pulse', className)}>
      <CardHeader className="pb-2">
        <Skeleton className="h-4 w-24" />
      </CardHeader>
      <CardContent>
        <Skeleton className="h-8 w-16 mb-1" />
        <Skeleton className="h-3 w-20" />
      </CardContent>
    </Card>
  )
}

/**
 * StatGridSkeleton - Multiple stat card skeletons
 */
interface StatGridSkeletonProps {
  count?: number
  className?: string
}

export function StatGridSkeleton({ count = 4, className }: StatGridSkeletonProps) {
  return (
    <div className={cn('grid grid-cols-2 lg:grid-cols-4 gap-4', className)}>
      {Array.from({ length: count }).map((_, i) => (
        <StatCardSkeleton key={i} />
      ))}
    </div>
  )
}

/**
 * ListItemSkeleton - Skeleton for list items
 */
interface ListItemSkeletonProps {
  showAvatar?: boolean
  showActions?: boolean
  className?: string
}

export function ListItemSkeleton({
  showAvatar = true,
  showActions = true,
  className,
}: ListItemSkeletonProps) {
  return (
    <div className={cn('flex items-center gap-4 p-4 animate-pulse', className)}>
      {showAvatar && <Skeleton className="h-10 w-10 rounded-full flex-shrink-0" />}
      <div className="flex-1 space-y-2">
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-3 w-1/2" />
      </div>
      {showActions && (
        <div className="flex gap-2 flex-shrink-0">
          <Skeleton className="h-8 w-8 rounded" />
          <Skeleton className="h-8 w-8 rounded" />
        </div>
      )}
    </div>
  )
}

/**
 * ListSkeleton - Multiple list item skeletons
 */
interface ListSkeletonProps {
  count?: number
  showAvatar?: boolean
  showActions?: boolean
  className?: string
}

export function ListSkeleton({
  count = 5,
  showAvatar = true,
  showActions = true,
  className,
}: ListSkeletonProps) {
  return (
    <div className={cn('divide-y', className)}>
      {Array.from({ length: count }).map((_, i) => (
        <ListItemSkeleton
          key={i}
          showAvatar={showAvatar}
          showActions={showActions}
        />
      ))}
    </div>
  )
}

/**
 * DetailPageSkeleton - Full page skeleton for detail views
 */
export function DetailPageSkeleton({ className }: { className?: string }) {
  return (
    <div className={cn('space-y-6 animate-pulse', className)}>
      {/* Header */}
      <div className="flex items-center gap-4">
        <Skeleton className="h-12 w-12 rounded-xl" />
        <div className="space-y-2">
          <Skeleton className="h-7 w-48" />
          <Skeleton className="h-4 w-32" />
        </div>
      </div>

      {/* Content cards */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <CardHeader>
              <Skeleton className="h-5 w-32" />
            </CardHeader>
            <CardContent className="space-y-4">
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-5/6" />
              <Skeleton className="h-4 w-4/6" />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <Skeleton className="h-5 w-24" />
            </CardHeader>
            <CardContent className="space-y-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="flex justify-between">
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-32" />
                </div>
              ))}
            </CardContent>
          </Card>
        </div>
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <Skeleton className="h-5 w-20" />
            </CardHeader>
            <CardContent className="space-y-3">
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-3/4" />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
