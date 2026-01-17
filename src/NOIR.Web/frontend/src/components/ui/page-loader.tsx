import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Skeleton } from './skeleton'

interface PageLoaderProps {
  className?: string
  text?: string
  fullScreen?: boolean
}

/**
 * PageLoader - Full-page or container loading indicator
 * Use for page transitions and initial data loading
 */
export function PageLoader({ className, text, fullScreen = false }: PageLoaderProps) {
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

/**
 * PageSkeleton - Skeleton-based page loading placeholder
 * Use as Suspense fallback for lazy-loaded pages - better UX than spinner
 * Shows layout structure with skeleton placeholders
 */
export function PageSkeleton() {
  return (
    <div className="space-y-6">
      {/* Page header skeleton */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Skeleton className="h-10 w-10 rounded-lg" />
          <div className="space-y-2">
            <Skeleton className="h-8 w-[200px]" />
            <Skeleton className="h-4 w-[300px]" />
          </div>
        </div>
        <Skeleton className="h-10 w-[140px]" />
      </div>

      {/* Card skeleton */}
      <div className="rounded-lg border bg-card p-6 space-y-4">
        {/* Card header */}
        <div className="flex items-center justify-between pb-4">
          <div className="space-y-2">
            <Skeleton className="h-5 w-[120px]" />
            <Skeleton className="h-4 w-[180px]" />
          </div>
          <div className="flex items-center gap-2">
            <Skeleton className="h-10 w-[200px]" />
            <Skeleton className="h-10 w-[80px]" />
          </div>
        </div>

        {/* Table skeleton */}
        <div className="rounded-md border">
          {/* Table header */}
          <div className="border-b bg-muted/50 p-4">
            <div className="flex items-center gap-4">
              <Skeleton className="h-4 w-[100px]" />
              <Skeleton className="h-4 w-[150px]" />
              <Skeleton className="h-4 w-[80px]" />
              <Skeleton className="h-4 w-[100px]" />
              <Skeleton className="h-4 w-[60px] ml-auto" />
            </div>
          </div>
          {/* Table rows */}
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="border-b last:border-0 p-4">
              <div className="flex items-center gap-4">
                <Skeleton className="h-4 w-[100px]" />
                <Skeleton className="h-4 w-[150px]" />
                <Skeleton className="h-6 w-[70px] rounded-full" />
                <Skeleton className="h-4 w-[100px]" />
                <div className="flex gap-2 ml-auto">
                  <Skeleton className="h-8 w-8 rounded" />
                  <Skeleton className="h-8 w-8 rounded" />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
