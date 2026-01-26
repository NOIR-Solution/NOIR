import * as React from 'react'
import { format } from 'date-fns'
import { Calendar as CalendarIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'

interface DatePickerProps {
  /** The selected date */
  value?: Date
  /** Callback when the date changes */
  onChange?: (date: Date | undefined) => void
  /** Placeholder text when no date is selected */
  placeholder?: string
  /** Additional className for the trigger button */
  className?: string
  /** Disable dates before this date */
  minDate?: Date
  /** Disable dates after this date */
  maxDate?: Date
  /** Disable the picker */
  disabled?: boolean
}

export function DatePicker({
  value,
  onChange,
  placeholder = 'Select date',
  className,
  minDate,
  maxDate,
  disabled = false,
}: DatePickerProps) {
  const [open, setOpen] = React.useState(false)

  const handleSelect = (date: Date | undefined) => {
    onChange?.(date)
    setOpen(false)
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          disabled={disabled}
          className={cn(
            'w-full justify-start text-left font-normal h-10',
            !value && 'text-muted-foreground',
            className
          )}
        >
          <CalendarIcon className="mr-2 h-4 w-4 text-muted-foreground" />
          {value ? format(value, 'PPP') : <span>{placeholder}</span>}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start" sideOffset={4}>
        <Calendar
          mode="single"
          selected={value}
          onSelect={handleSelect}
          disabled={(date) => {
            if (minDate) {
              const min = new Date(minDate)
              min.setHours(0, 0, 0, 0)
              const check = new Date(date)
              check.setHours(0, 0, 0, 0)
              if (check < min) return true
            }
            if (maxDate) {
              const max = new Date(maxDate)
              max.setHours(23, 59, 59, 999)
              if (date > max) return true
            }
            return false
          }}
          initialFocus
        />
      </PopoverContent>
    </Popover>
  )
}
