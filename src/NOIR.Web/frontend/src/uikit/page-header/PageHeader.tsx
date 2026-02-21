import * as React from 'react'
import type { LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

interface PageHeaderProps {
  icon: LucideIcon
  title: string
  description?: string
  action?: React.ReactNode
  className?: string
  /** Enable responsive stacking on mobile (default: true) */
  responsive?: boolean
}

const PageHeader = React.forwardRef<HTMLDivElement, PageHeaderProps>(
  ({ icon: Icon, title, description, action, className, responsive = true }, ref) => {
    return (
      <div
        ref={ref}
        className={cn(
          responsive
            ? 'flex flex-col gap-4 md:flex-row md:items-center md:justify-between'
            : 'flex items-center justify-between',
          className
        )}
      >
        <div className="flex items-center gap-4">
          <div className="vt-page-icon flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-primary/20 to-primary/10 shadow-lg shadow-primary/20 backdrop-blur-sm border border-primary/20 transition-all duration-300 hover:shadow-xl hover:shadow-primary/30 hover:scale-105">
            <Icon className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="vt-page-title text-3xl font-bold tracking-tight bg-gradient-to-r from-foreground to-foreground/70 bg-clip-text text-transparent">
              {title}
            </h1>
            {description && (
              <p className="text-sm text-muted-foreground mt-1">{description}</p>
            )}
          </div>
        </div>
        {action && <div>{action}</div>}
      </div>
    )
  }
)

PageHeader.displayName = 'PageHeader'

export { PageHeader }
export type { PageHeaderProps }
