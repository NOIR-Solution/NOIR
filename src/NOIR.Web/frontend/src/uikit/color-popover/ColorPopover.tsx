import { useState, useRef, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Copy, Check } from 'lucide-react'
import { Button } from '../button/Button'
import { Popover, PopoverContent, PopoverTrigger } from '../popover/Popover'
import { cn } from '@/lib/utils'

interface ColorPopoverProps {
  color: string
  size?: 'sm' | 'md'
  className?: string
}

export const ColorPopover = ({ color, size = 'sm', className }: ColorPopoverProps) => {
  const { t } = useTranslation('common')
  const [copied, setCopied] = useState(false)
  const [open, setOpen] = useState(false)
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const handleCopy = useCallback(() => {
    navigator.clipboard.writeText(color)
    setCopied(true)
    setTimeout(() => setCopied(false), 1500)
  }, [color])

  const showPopover = useCallback(() => {
    if (closeTimer.current) {
      clearTimeout(closeTimer.current)
      closeTimer.current = null
    }
    setOpen(true)
  }, [])

  const scheduleClose = useCallback(() => {
    closeTimer.current = setTimeout(() => setOpen(false), 150)
  }, [])

  const sizeClasses = size === 'sm' ? 'w-4 h-4' : 'w-5 h-5'

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          className={cn(
            sizeClasses,
            'rounded-full shrink-0 cursor-pointer ring-1 ring-border hover:ring-2 hover:ring-primary/50 transition-all duration-200',
            className,
          )}
          style={{ backgroundColor: color }}
          aria-label={t('labels.colorValue', { defaultValue: 'Color: {{color}}', color })}
          onMouseEnter={showPopover}
          onMouseLeave={scheduleClose}
        />
      </PopoverTrigger>
      <PopoverContent
        className="w-auto p-2"
        side="top"
        align="start"
        onMouseEnter={showPopover}
        onMouseLeave={scheduleClose}
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <div className="flex items-center gap-2">
          <div
            className="w-6 h-6 rounded border border-border shrink-0"
            style={{ backgroundColor: color }}
          />
          <code className="text-xs bg-muted px-1.5 py-0.5 rounded select-all">
            {color}
          </code>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6 shrink-0 cursor-pointer"
            onClick={handleCopy}
            aria-label={t('buttons.copyColorCode', 'Copy color code')}
          >
            {copied ? (
              <Check className="h-3 w-3 text-green-500" />
            ) : (
              <Copy className="h-3 w-3 text-muted-foreground" />
            )}
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  )
}
