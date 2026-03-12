/**
 * MathBlock Component
 *
 * Renders math formulas using KaTeX. Supports both inline and display modes.
 * Uses katex.renderToString for synchronous rendering with memoization.
 */

import { useMemo } from 'react'
import katex from 'katex'
import 'katex/dist/katex.min.css'
import { cn } from '@/lib/utils'

export interface MathBlockProps {
  /** LaTeX formula string to render */
  formula: string
  /** Whether to render in display mode (block-level, centered). Default: false (inline) */
  displayMode?: boolean
  /** Additional CSS class names */
  className?: string
}

export const MathBlock = ({ formula, displayMode = false, className }: MathBlockProps) => {
  const rendered = useMemo(() => {
    if (!formula.trim()) {
      return { html: '', error: null }
    }

    try {
      const html = katex.renderToString(formula, {
        displayMode,
        throwOnError: false,
        output: 'html',
      })
      return { html, error: null }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to render formula'
      return { html: '', error: message }
    }
  }, [formula, displayMode])

  if (!formula.trim()) {
    return null
  }

  if (rendered.error) {
    return (
      <span
        className={cn(
          'font-mono text-sm text-destructive bg-destructive/5 rounded px-1.5 py-0.5',
          className
        )}
        title={rendered.error}
      >
        {formula}
      </span>
    )
  }

  if (displayMode) {
    return (
      <div
        className={cn('math-block overflow-x-auto', className)}
        tabIndex={0}
        role="region"
        aria-label="Mathematical formula"
        dangerouslySetInnerHTML={{ __html: rendered.html }}
      />
    )
  }

  return (
    <span
      className={cn('math-inline', className)}
      dangerouslySetInnerHTML={{ __html: rendered.html }}
    />
  )
}
