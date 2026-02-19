import type { LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/uikit/button'

export interface ViewModeOption<T extends string = string> {
  value: T
  label: string
  icon: LucideIcon
  ariaLabel: string
}

export interface ViewModeToggleProps<T extends string = string> {
  options: ViewModeOption<T>[]
  value: T
  onChange: (value: T) => void
  className?: string
}

export const ViewModeToggle = <T extends string>({
  options,
  value,
  onChange,
  className,
}: ViewModeToggleProps<T>) => {
  return (
    <div className={cn('flex items-center gap-1 p-1 rounded-lg bg-muted border border-border/50', className)}>
      {options.map((option) => {
        const isActive = value === option.value
        const Icon = option.icon
        return (
          <Button
            key={option.value}
            variant={isActive ? 'default' : 'ghost'}
            size="sm"
            onClick={() => onChange(option.value)}
            className={cn(
              'cursor-pointer h-8 px-3 gap-1.5 transition-all duration-200',
              isActive
                ? 'shadow-sm font-medium'
                : 'text-muted-foreground hover:text-foreground',
            )}
            aria-label={option.ariaLabel}
            aria-pressed={isActive}
          >
            <Icon className="h-4 w-4" />
            <span className="text-xs hidden sm:inline">{option.label}</span>
          </Button>
        )
      })}
    </div>
  )
}
