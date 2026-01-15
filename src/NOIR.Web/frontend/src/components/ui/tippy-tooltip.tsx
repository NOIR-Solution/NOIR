import * as React from 'react'
import Tippy from '@tippyjs/react'
import type { TippyProps } from '@tippyjs/react'
import 'tippy.js/dist/tippy.css'
import 'tippy.js/animations/shift-away-subtle.css'
import { cn } from '@/lib/utils'

export interface TooltipProps extends Omit<TippyProps, 'content'> {
  /** The content to display in the tooltip */
  content: React.ReactNode
  /** The trigger element */
  children: React.ReactElement
  /** Additional className for the tooltip content wrapper */
  contentClassName?: string
}

/**
 * A beautiful tooltip component powered by Tippy.js
 * with smooth animations and clean styling.
 */
export function TippyTooltip({
  content,
  children,
  contentClassName,
  placement = 'top',
  animation = 'shift-away-subtle',
  duration = [200, 150],
  delay = [100, 0],
  interactive = false,
  arrow = true,
  ...props
}: TooltipProps) {
  return (
    <Tippy
      content={
        <div className={cn('text-sm', contentClassName)}>
          {content}
        </div>
      }
      placement={placement}
      animation={animation}
      duration={duration}
      delay={delay}
      interactive={interactive}
      arrow={arrow}
      theme="custom"
      {...props}
    >
      {children}
    </Tippy>
  )
}

/**
 * Rich content tooltip with header and list items
 * Perfect for search hints, feature explanations, etc.
 */
export interface RichTooltipProps extends Omit<TooltipProps, 'content'> {
  /** Title/header of the tooltip */
  title?: string
  /** List of items to display */
  items?: string[]
  /** Custom content (overrides title/items) */
  content?: React.ReactNode
}

export function RichTooltip({
  title,
  items,
  content,
  children,
  placement = 'bottom',
  interactive = true,
  ...props
}: RichTooltipProps) {
  const tooltipContent = content || (
    <div style={{ minWidth: '200px' }}>
      {/* Header - Primary theme color */}
      {title && (
        <div
          style={{
            padding: '10px 14px',
            background: 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)',
            fontWeight: 600,
            fontSize: '13px',
            color: '#ffffff',
            letterSpacing: '-0.01em',
          }}
        >
          {title}
        </div>
      )}
      {/* Content */}
      {items && items.length > 0 && (
        <ul
          style={{
            padding: '12px 14px',
            margin: 0,
            listStyle: 'none',
            fontSize: '13px',
            color: '#374151',
            lineHeight: 1.7,
          }}
        >
          {items.map((item, index) => (
            <li key={index} style={{ paddingLeft: '14px', position: 'relative' }}>
              <span style={{
                position: 'absolute',
                left: 0,
                color: '#3b82f6',
                fontWeight: 700,
              }}>•</span>
              {item}
            </li>
          ))}
        </ul>
      )}
    </div>
  )

  return (
    <TippyTooltip
      content={tooltipContent}
      placement={placement}
      interactive={interactive}
      {...props}
    >
      {children}
    </TippyTooltip>
  )
}

export default TippyTooltip
