import * as React from 'react'
import { format } from 'date-fns'
import { Calendar as CalendarIcon, X } from 'lucide-react'
import type { DateRange } from 'react-day-picker'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'
import { Calendar } from '../calendar/Calendar'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '../popover/Popover'

interface DateRangePickerProps {
  /** The selected date range */
  value?: DateRange
  /** Callback when the date range changes */
  onChange?: (range: DateRange | undefined) => void
  /** Placeholder text when no date is selected */
  placeholder?: string
  /** Additional className for the trigger button */
  className?: string
  /** Alignment of the popover */
  align?: 'start' | 'center' | 'end'
  /** Whether to show a clear button */
  showClear?: boolean
  /** Disable the picker */
  disabled?: boolean
  /** Number of months to display */
  numberOfMonths?: number
}

export function DateRangePicker({
  value,
  onChange,
  placeholder = 'Pick a date range',
  className,
  align = 'end', // Default to 'end' so popover extends left, avoiding right viewport overflow
  showClear = true,
  disabled = false,
  numberOfMonths = 2,
}: DateRangePickerProps) {
  const [open, setOpen] = React.useState(false)

  const handleSelect = (range: DateRange | undefined) => {
    onChange?.(range)
  }

  const handleClear = (e: React.MouseEvent) => {
    e.stopPropagation()
    onChange?.(undefined)
  }

  const formatDateRange = () => {
    if (!value?.from) return placeholder

    if (value.to) {
      return `${format(value.from, 'MMM d, yyyy')} - ${format(value.to, 'MMM d, yyyy')}`
    }

    return format(value.from, 'MMM d, yyyy')
  }

  const hasValue = value?.from !== undefined

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          disabled={disabled}
          className={cn(
            'justify-start text-left font-normal',
            !hasValue && 'text-muted-foreground',
            className
          )}
        >
          <CalendarIcon className="mr-2 h-4 w-4" />
          <span className="flex-1 truncate">{formatDateRange()}</span>
          {showClear && hasValue && (
            <X
              className="ml-2 h-4 w-4 shrink-0 opacity-50 hover:opacity-100"
              onClick={handleClear}
            />
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align={align} sideOffset={4} collisionPadding={16}>
        <Calendar
          initialFocus
          mode="range"
          defaultMonth={value?.from}
          selected={value}
          onSelect={handleSelect}
          numberOfMonths={numberOfMonths}
        />
        <div className="flex items-center justify-between border-t p-3">
          <div className="text-xs text-muted-foreground">
            {hasValue
              ? `${value.from ? format(value.from, 'PP') : ''} ${value.to ? ' → ' + format(value.to, 'PP') : ''}`
              : 'Select start and end dates'}
          </div>
          <div className="flex gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                onChange?.(undefined)
              }}
            >
              Clear
            </Button>
            <Button
              size="sm"
              onClick={() => setOpen(false)}
            >
              Apply
            </Button>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  )
}
