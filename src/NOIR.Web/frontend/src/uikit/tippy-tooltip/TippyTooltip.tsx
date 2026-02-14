import * as React from 'react'
import { Tooltip, TooltipTrigger, TooltipContent } from '../tooltip/Tooltip'
import { cn } from '@/lib/utils'

export interface TooltipProps {
  /** The content to display in the tooltip */
  content: React.ReactNode
  /** The trigger element */
  children: React.ReactElement
  /** Additional className for the tooltip content wrapper */
  contentClassName?: string
  /** Placement of the tooltip */
  placement?: 'top' | 'right' | 'bottom' | 'left'
  /** Delay before showing (ms) - array [show, hide] or single number */
  delay?: number | [number, number]
  /** Whether tooltip should stay open when hovering over it */
  interactive?: boolean
  /** Callback when tooltip opens */
  onShow?: () => void
  /** Theme (ignored - for API compatibility) */
  theme?: string
}

/**
 * A beautiful tooltip component powered by Radix UI
 * with smooth animations and clean styling.
 * React 19 compatible.
 *
 * Uses primary color background with white text by default.
 */
export const TippyTooltip = ({
  content,
  children,
  contentClassName,
  placement = 'right',
  delay = 100,
  onShow,
}: TooltipProps) => {
  const delayDuration = Array.isArray(delay) ? delay[0] : delay

  return (
    <Tooltip
      delayDuration={delayDuration}
      onOpenChange={(open) => {
        if (open && onShow) {
          onShow()
        }
      }}
    >
      <TooltipTrigger asChild>
        {children}
      </TooltipTrigger>
      <TooltipContent side={placement} className={cn(contentClassName)}>
        {content}
      </TooltipContent>
    </Tooltip>
  )
}

/**
 * Rich content tooltip with header and list items
 * Perfect for search hints, feature explanations, etc.
 * React 19 compatible.
 *
 * Uses a card-style background with primary colored header.
 */
export interface RichTooltipProps extends Omit<TooltipProps, 'content'> {
  /** Title/header of the tooltip */
  title?: string
  /** List of items to display */
  items?: string[]
  /** Custom content (overrides title/items) */
  content?: React.ReactNode
}

export const RichTooltip = ({
  title,
  items,
  content,
  children,
  placement = 'bottom',
  delay = 100,
}: RichTooltipProps) => {
  const delayDuration = Array.isArray(delay) ? delay[0] : delay

  const tooltipContent = content || (
    <div className="min-w-[200px]">
      {/* Header - Primary theme color */}
      {title && (
        <div className="bg-primary text-primary-foreground px-3.5 py-2.5 font-semibold text-[13px] tracking-tight -mx-3 -mt-1.5 mb-1.5 rounded-t-md">
          {title}
        </div>
      )}
      {/* Content */}
      {items && items.length > 0 && (
        <ul className="space-y-0.5 text-[13px] leading-relaxed">
          {items.map((item, index) => (
            <li key={index} className="flex items-start gap-2">
              <span className="text-primary font-bold mt-0.5">•</span>
              <span>{item}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )

  return (
    <Tooltip delayDuration={delayDuration}>
      <TooltipTrigger asChild>
        {children}
      </TooltipTrigger>
      <TooltipContent
        side={placement}
        className="bg-card text-card-foreground border-border max-w-sm"
      >
        {tooltipContent}
      </TooltipContent>
    </Tooltip>
  )
}
