import * as React from 'react'
import { Clock } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '../popover/Popover'

interface TimePickerProps {
  /** The selected time in HH:mm format */
  value?: string
  /** Callback when the time changes */
  onChange?: (time: string) => void
  /** Placeholder text when no time is selected */
  placeholder?: string
  /** Additional className for the trigger button */
  className?: string
  /** Time interval in minutes (default: 30) */
  interval?: number
  /** Disable the picker */
  disabled?: boolean
}

const generateTimeOptions = (interval: number = 30): string[] => {
  const times: string[] = []
  for (let hour = 0; hour < 24; hour++) {
    for (let minute = 0; minute < 60; minute += interval) {
      const h = hour.toString().padStart(2, '0')
      const m = minute.toString().padStart(2, '0')
      times.push(`${h}:${m}`)
    }
  }
  return times
}

const formatTime = (time: string): string => {
  const [hours, minutes] = time.split(':').map(Number)
  const period = hours >= 12 ? 'PM' : 'AM'
  const displayHours = hours % 12 || 12
  return `${displayHours}:${minutes.toString().padStart(2, '0')} ${period}`
}

export const TimePicker = ({
  value,
  onChange,
  placeholder = 'Select time',
  className,
  interval = 30,
  disabled = false,
}: TimePickerProps) => {
  const [open, setOpen] = React.useState(false)
  const timeOptions = React.useMemo(() => generateTimeOptions(interval), [interval])
  const selectedRef = React.useRef<HTMLButtonElement>(null)

  // Auto-scroll to selected time when popover opens
  React.useEffect(() => {
    if (open && value && selectedRef.current) {
      // Use setTimeout to ensure popover content is fully rendered
      const timeoutId = setTimeout(() => {
        selectedRef.current?.scrollIntoView({
          block: 'center',
          behavior: 'instant',
        })
      }, 0)
      return () => clearTimeout(timeoutId)
    }
  }, [open, value])

  const handleSelect = (time: string) => {
    onChange?.(time)
    setOpen(false)
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          disabled={disabled}
          className={cn(
            'w-full justify-start text-left font-normal h-10 cursor-pointer',
            !value && 'text-muted-foreground',
            className
          )}
        >
          <Clock className="mr-2 h-4 w-4 text-muted-foreground" />
          {value ? formatTime(value) : <span>{placeholder}</span>}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[200px] p-0" align="start" sideOffset={4}>
        <div className="h-[280px] overflow-y-auto p-2">
          {timeOptions.map((time) => {
            const isSelected = value === time
            return (
              <Button
                key={time}
                ref={isSelected ? selectedRef : undefined}
                variant={isSelected ? 'default' : 'ghost'}
                className="w-full justify-start font-normal mb-1 cursor-pointer"
                onClick={() => handleSelect(time)}
              >
                {formatTime(time)}
              </Button>
            )
          })}
        </div>
      </PopoverContent>
    </Popover>
  )
}
