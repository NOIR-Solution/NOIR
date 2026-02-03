import * as React from 'react'
import { cn } from '@/lib/utils'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'

export interface InlineEditInputProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'onChange'> {
  /** Error message to display in tooltip */
  error?: string
  /** Whether the field has an error */
  hasError?: boolean
  /** Callback when value changes */
  onChange?: (value: string) => void
  /** Callback when Enter is pressed (immediate save) */
  onEnterPress?: () => void
  /** Callback when Escape is pressed (revert) */
  onEscapePress?: () => void
  /** Custom class for the input wrapper */
  wrapperClassName?: string
  /** Alignment for numeric inputs */
  align?: 'left' | 'center' | 'right'
}

const InlineEditInput = React.forwardRef<HTMLInputElement, InlineEditInputProps>(
  (
    {
      className,
      wrapperClassName,
      error,
      hasError,
      onChange,
      onEnterPress,
      onEscapePress,
      align = 'left',
      type = 'text',
      ...props
    },
    ref
  ) => {
    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      onChange?.(e.target.value)
    }

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') {
        e.preventDefault()
        onEnterPress?.()
      } else if (e.key === 'Escape') {
        e.preventDefault()
        onEscapePress?.()
      }
    }

    const showError = hasError || !!error
    const alignmentClass = {
      left: 'text-left',
      center: 'text-center',
      right: 'text-right',
    }[align]

    const input = (
      <input
        type={type}
        className={cn(
          'flex h-8 w-full rounded-md border bg-transparent px-2 py-1 text-sm transition-colors',
          'file:border-0 file:bg-transparent file:text-sm file:font-medium',
          'placeholder:text-muted-foreground',
          'focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring',
          'disabled:cursor-not-allowed disabled:opacity-50',
          showError
            ? 'border-destructive focus-visible:ring-destructive'
            : 'border-input hover:border-muted-foreground/50',
          alignmentClass,
          // Hide spinner for number inputs
          type === 'number' && '[appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none',
          className
        )}
        ref={ref}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        {...props}
      />
    )

    // If there's an error, wrap in tooltip
    if (error) {
      return (
        <TooltipProvider delayDuration={0}>
          <Tooltip>
            <TooltipTrigger asChild>
              <div className={cn('relative', wrapperClassName)}>
                {input}
              </div>
            </TooltipTrigger>
            <TooltipContent
              side="top"
              className="bg-destructive text-destructive-foreground text-xs"
            >
              {error}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      )
    }

    return (
      <div className={cn('relative', wrapperClassName)}>
        {input}
      </div>
    )
  }
)
InlineEditInput.displayName = 'InlineEditInput'

export { InlineEditInput }
