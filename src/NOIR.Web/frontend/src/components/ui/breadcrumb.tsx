import * as React from 'react'
import { Link } from 'react-router-dom'
import { ChevronRight, Home } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface BreadcrumbItem {
  label: string
  href?: string
  icon?: LucideIcon
}

interface BreadcrumbProps {
  items: BreadcrumbItem[]
  className?: string
  /** Show home icon for first item if it has an href */
  showHomeIcon?: boolean
}

const Breadcrumb = React.forwardRef<HTMLElement, BreadcrumbProps>(
  ({ items, className, showHomeIcon = true }, ref) => {
    if (!items || items.length === 0) return null

    return (
      <nav
        ref={ref}
        aria-label="Breadcrumb"
        className={cn('flex items-center text-sm', className)}
      >
        <ol className="flex items-center gap-1.5">
          {items.map((item, index) => {
            const isLast = index === items.length - 1
            const isFirst = index === 0
            const Icon = item.icon

            return (
              <li key={`${item.label}-${index}`} className="flex items-center gap-1.5">
                {index > 0 && (
                  <ChevronRight className="h-3.5 w-3.5 text-muted-foreground/60 flex-shrink-0" />
                )}

                {item.href && !isLast ? (
                  <Link
                    to={item.href}
                    className={cn(
                      'flex items-center gap-1.5 text-muted-foreground hover:text-foreground transition-colors cursor-pointer',
                      'hover:underline underline-offset-4'
                    )}
                  >
                    {isFirst && showHomeIcon && !Icon && (
                      <Home className="h-3.5 w-3.5" />
                    )}
                    {Icon && <Icon className="h-3.5 w-3.5" />}
                    <span>{item.label}</span>
                  </Link>
                ) : (
                  <span
                    className={cn(
                      'flex items-center gap-1.5',
                      isLast
                        ? 'text-foreground font-medium'
                        : 'text-muted-foreground'
                    )}
                    aria-current={isLast ? 'page' : undefined}
                  >
                    {Icon && <Icon className="h-3.5 w-3.5" />}
                    <span className="truncate max-w-[200px]">{item.label}</span>
                  </span>
                )}
              </li>
            )
          })}
        </ol>
      </nav>
    )
  }
)

Breadcrumb.displayName = 'Breadcrumb'

export { Breadcrumb }
