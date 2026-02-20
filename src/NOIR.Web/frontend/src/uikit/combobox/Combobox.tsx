import * as React from 'react'
import { Check, ChevronsUpDown, Search, ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '../popover/Popover'
import { Input } from '../input/Input'

export interface ComboboxOption {
  value: string
  label: string
  description?: string
}

interface ComboboxProps {
  options: ComboboxOption[]
  value?: string
  onValueChange?: (value: string) => void
  placeholder?: string
  searchPlaceholder?: string
  emptyText?: string
  className?: string
  disabled?: boolean
  /** Text to show count, e.g., "banks" will show "24 banks" */
  countLabel?: string
}

export const Combobox = ({
  options,
  value,
  onValueChange,
  placeholder = 'Select option...',
  searchPlaceholder = 'Search...',
  emptyText = 'No results found.',
  className,
  disabled,
  countLabel,
}: ComboboxProps) => {
  const [open, setOpen] = React.useState(false)
  const [search, setSearch] = React.useState('')
  const listRef = React.useRef<HTMLDivElement>(null)
  const [canScrollDown, setCanScrollDown] = React.useState(false)

  const selectedOption = React.useMemo(
    () => options.find((opt) => opt.value === value),
    [options, value]
  )

  const filteredOptions = React.useMemo(() => {
    if (!search) return options
    const searchLower = search.toLowerCase()
    return options.filter(
      (opt) =>
        opt.label.toLowerCase().includes(searchLower) ||
        opt.value.toLowerCase().includes(searchLower) ||
        opt.description?.toLowerCase().includes(searchLower)
    )
  }, [options, search])

  // Check if list is scrollable
  React.useEffect(() => {
    const checkScroll = () => {
      if (listRef.current) {
        const { scrollHeight, clientHeight, scrollTop } = listRef.current
        setCanScrollDown(scrollHeight > clientHeight && scrollTop < scrollHeight - clientHeight - 10)
      }
    }

    // Check on open and after render
    if (open) {
      setTimeout(checkScroll, 50)
    }
  }, [open, filteredOptions])

  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const { scrollHeight, clientHeight, scrollTop } = e.currentTarget
    setCanScrollDown(scrollTop < scrollHeight - clientHeight - 10)
  }

  // Handle mouse wheel scrolling explicitly (Radix Popover can block wheel events)
  const handleWheel = (e: React.WheelEvent<HTMLDivElement>) => {
    const container = listRef.current
    if (!container) return

    // Prevent the event from bubbling to prevent Popover from closing
    e.stopPropagation()

    // Manually scroll the container
    container.scrollTop += e.deltaY

    // Update scroll indicator state
    const { scrollHeight, clientHeight, scrollTop } = container
    setCanScrollDown(scrollTop + e.deltaY < scrollHeight - clientHeight - 10)
  }

  const handleSelect = (optionValue: string) => {
    onValueChange?.(optionValue)
    setOpen(false)
    setSearch('')
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className={cn(
            'w-full justify-between font-normal',
            !value && 'text-muted-foreground',
            className
          )}
          disabled={disabled}
        >
          <span className="truncate">
            {selectedOption ? selectedOption.label : placeholder}
          </span>
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
        {/* Search header with count */}
        <div className="flex items-center border-b px-3">
          <Search className="mr-2 h-4 w-4 shrink-0 opacity-50" />
          <Input
            placeholder={searchPlaceholder}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="h-10 border-0 bg-transparent px-0 focus-visible:ring-0 focus-visible:ring-offset-0"
          />
          {countLabel && !search && (
            <span className="text-xs text-muted-foreground whitespace-nowrap ml-2">
              {options.length} {countLabel}
            </span>
          )}
          {search && (
            <span className="text-xs text-muted-foreground whitespace-nowrap ml-2">
              {filteredOptions.length} found
            </span>
          )}
        </div>

        {/* Scrollable list with visible scrollbar */}
        <div className="relative">
          <div
            ref={listRef}
            onScroll={handleScroll}
            onWheel={handleWheel}
            className="max-h-[280px] overflow-y-auto overscroll-contain"
            style={{ scrollbarWidth: 'thin', scrollbarColor: 'var(--border) transparent' }}
          >
            {filteredOptions.length === 0 ? (
              <div className="py-6 text-center text-sm text-muted-foreground">
                {emptyText}
              </div>
            ) : (
              <div className="p-1">
                {filteredOptions.map((option) => (
                  <div
                    key={option.value}
                    onClick={() => handleSelect(option.value)}
                    className={cn(
                      'relative flex cursor-pointer select-none items-start rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent hover:text-accent-foreground',
                      value === option.value && 'bg-accent text-accent-foreground'
                    )}
                  >
                    <Check
                      className={cn(
                        'mr-2 h-4 w-4 mt-0.5 shrink-0',
                        value === option.value ? 'opacity-100' : 'opacity-0'
                      )}
                    />
                    <div className="flex flex-col gap-0.5 min-w-0">
                      <span className="font-medium">{option.label}</span>
                      {option.description && (
                        <span className="text-xs text-muted-foreground leading-tight">
                          {option.description}
                        </span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Scroll indicator */}
          {canScrollDown && filteredOptions.length > 5 && (
            <div className="absolute bottom-0 left-0 right-0 flex justify-center py-1 bg-gradient-to-t from-popover to-transparent pointer-events-none">
              <ChevronDown className="h-4 w-4 text-muted-foreground animate-bounce" />
            </div>
          )}
        </div>
      </PopoverContent>
    </Popover>
  )
}
