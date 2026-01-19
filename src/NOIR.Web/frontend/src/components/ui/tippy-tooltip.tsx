import * as React from 'react'
import Tippy from '@tippyjs/react'
import type { TippyProps } from '@tippyjs/react'
// CSS imports moved to index.css for reliable loading
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
 *
 * Uses primary color background with white text by default.
 */
export function TippyTooltip({
  content,
  children,
  contentClassName,
  placement = 'right',
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
        <span className={cn(contentClassName)}>
          {content}
        </span>
      }
      placement={placement}
      animation={animation}
      duration={duration}
      delay={delay}
      interactive={interactive}
      arrow={arrow}
      theme="custom"
      appendTo={() => document.body}
      {...props}
    >
      {children}
    </Tippy>
  )
}

/**
 * Rich content tooltip with header and list items
 * Perfect for search hints, feature explanations, etc.
 *
 * Uses a white/light background with primary colored header.
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
            background: 'hsl(var(--primary))',
            fontWeight: 600,
            fontSize: '13px',
            color: 'hsl(var(--primary-foreground))',
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
            color: 'hsl(var(--foreground))',
            lineHeight: 1.7,
          }}
        >
          {items.map((item, index) => (
            <li key={index} style={{ paddingLeft: '14px', position: 'relative' }}>
              <span style={{
                position: 'absolute',
                left: 0,
                color: 'hsl(var(--primary))',
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
    <Tippy
      content={tooltipContent}
      placement={placement}
      animation="shift-away-subtle"
      duration={[200, 150]}
      delay={[100, 0]}
      interactive={interactive}
      arrow={true}
      theme="custom rich"
      appendTo={() => document.body}
      {...props}
    >
      {children}
    </Tippy>
  )
}

export default TippyTooltip
