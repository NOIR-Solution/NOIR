/**
 * NotificationEmpty Component
 *
 * Animated empty state for notification list with engaging illustration
 */
import { motion } from 'framer-motion'
import { Bell, BellOff, Mail, MessageSquare } from 'lucide-react'
import { cn } from '@/lib/utils'

interface NotificationEmptyProps {
  className?: string
}

export function NotificationEmpty({ className }: NotificationEmptyProps) {
  return (
    <div className={cn('flex flex-col items-center justify-center py-12 px-6', className)}>
      {/* Animated illustration with 3 icons */}
      <div className="flex justify-center isolate mb-6">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1, type: 'spring', stiffness: 300, damping: 30 }}
          className="bg-background size-12 grid place-items-center rounded-xl relative left-2.5 top-1.5 -rotate-6 shadow-lg ring-1 ring-border"
        >
          <Mail className="size-6 text-muted-foreground" />
        </motion.div>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2, type: 'spring', stiffness: 300, damping: 30 }}
          className="bg-background size-12 grid place-items-center rounded-xl relative z-10 shadow-lg ring-1 ring-border"
        >
          <BellOff className="size-6 text-muted-foreground" />
        </motion.div>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.3, type: 'spring', stiffness: 300, damping: 30 }}
          className="bg-background size-12 grid place-items-center rounded-xl relative right-2.5 top-1.5 rotate-6 shadow-lg ring-1 ring-border"
        >
          <MessageSquare className="size-6 text-muted-foreground" />
        </motion.div>
      </div>

      <motion.h3
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.4 }}
        className="text-base font-semibold text-foreground mb-1.5"
      >
        All caught up!
      </motion.h3>
      <motion.p
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.5 }}
        className="text-sm text-muted-foreground text-center max-w-[240px]"
      >
        You don't have any notifications right now. We'll let you know when something arrives.
      </motion.p>
    </div>
  )
}
