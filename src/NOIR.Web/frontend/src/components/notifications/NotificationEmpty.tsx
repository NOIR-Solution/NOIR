/**
 * NotificationEmpty Component
 *
 * Empty state for notification list
 */
import { Bell } from 'lucide-react'
import { cn } from '@/lib/utils'

interface NotificationEmptyProps {
  className?: string
}

export function NotificationEmpty({ className }: NotificationEmptyProps) {
  return (
    <div className={cn('flex flex-col items-center justify-center py-8 text-center', className)}>
      <div className="rounded-full bg-muted p-4 mb-4">
        <Bell className="size-8 text-muted-foreground" />
      </div>
      <h3 className="text-lg font-medium">No notifications</h3>
      <p className="text-sm text-muted-foreground mt-1">
        You're all caught up! Check back later.
      </p>
    </div>
  )
}
