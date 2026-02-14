import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { motion, AnimatePresence } from 'framer-motion'
import { CheckCircle2, Circle, Sparkles, ChevronRight, X } from 'lucide-react'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Progress } from '@uikit'

import { cn } from '@/lib/utils'
import {
  useOnboarding,
  ONBOARDING_ITEMS,
  type OnboardingItemId,
} from '@/hooks/useOnboarding'

interface ChecklistItemProps {
  id: OnboardingItemId
  label: string
  description: string
  href: string
  completed: boolean
  onComplete: () => void
}

const ChecklistItem = ({
  label,
  description,
  href,
  completed,
  onComplete,
}: ChecklistItemProps) => {
  return (
    <motion.div
      layout
      initial={{ opacity: 0, x: -10 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: 10 }}
      className={cn(
        'group flex items-center gap-3 p-3 rounded-lg transition-colors',
        completed
          ? 'bg-muted/30'
          : 'bg-muted/50 hover:bg-muted cursor-pointer'
      )}
    >
      <button
        onClick={onComplete}
        className={cn(
          'flex-shrink-0 transition-transform',
          !completed && 'hover:scale-110'
        )}
        aria-label={completed ? `${label} completed` : `Mark ${label} as complete`}
      >
        {completed ? (
          <CheckCircle2 className="h-5 w-5 text-green-600" />
        ) : (
          <Circle className="h-5 w-5 text-muted-foreground group-hover:text-primary" />
        )}
      </button>

      <div className="flex-1 min-w-0">
        <ViewTransitionLink
          to={href}
          className={cn(
            'text-sm font-medium transition-colors',
            completed
              ? 'text-muted-foreground line-through'
              : 'text-foreground hover:text-primary'
          )}
        >
          {label}
        </ViewTransitionLink>
        <p className="text-xs text-muted-foreground truncate">{description}</p>
      </div>

      {!completed && (
        <ViewTransitionLink
          to={href}
          className="flex-shrink-0 opacity-0 group-hover:opacity-100 transition-opacity"
        >
          <ChevronRight className="h-4 w-4 text-muted-foreground" />
        </ViewTransitionLink>
      )}
    </motion.div>
  )
}

interface OnboardingChecklistProps {
  className?: string
  /** Allow dismissing the checklist */
  dismissible?: boolean
  /** Callback when dismissed */
  onDismiss?: () => void
}

export const OnboardingChecklist = ({
  className,
  dismissible = true,
  onDismiss,
}: OnboardingChecklistProps) => {
  const { progress, completeItem, isItemCompleted, shouldShowChecklist } =
    useOnboarding()

  // Don't show if onboarding is complete
  if (!shouldShowChecklist) return null

  return (
    <Card
      className={cn(
        'border-primary/20 bg-gradient-to-br from-primary/5 to-background',
        'shadow-sm hover:shadow-md transition-shadow duration-300',
        className
      )}
    >
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-2">
            <div className="p-1.5 rounded-lg bg-primary/10">
              <Sparkles className="h-4 w-4 text-primary" />
            </div>
            <div>
              <CardTitle className="text-base">Complete Your Setup</CardTitle>
              <CardDescription className="text-xs">
                {progress.completed} of {progress.total} tasks completed
              </CardDescription>
            </div>
          </div>
          {dismissible && onDismiss && (
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-muted-foreground hover:text-foreground"
              onClick={onDismiss}
              aria-label="Dismiss onboarding checklist"
            >
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>

        {/* Progress bar */}
        <div className="mt-3">
          <Progress value={progress.percentage} className="h-2" />
        </div>
      </CardHeader>

      <CardContent className="pt-0">
        <div className="space-y-2">
          <AnimatePresence mode="popLayout">
            {ONBOARDING_ITEMS.map((item) => (
              <ChecklistItem
                key={item.id}
                id={item.id}
                label={item.label}
                description={item.description}
                href={item.href}
                completed={isItemCompleted(item.id)}
                onComplete={() => completeItem(item.id)}
              />
            ))}
          </AnimatePresence>
        </div>

        {/* Completion message */}
        {progress.percentage === 100 && (
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className="mt-4 p-3 rounded-lg bg-green-500/10 border border-green-500/20 text-center"
          >
            <p className="text-sm font-medium text-green-600">
              All done! You're all set up.
            </p>
          </motion.div>
        )}
      </CardContent>
    </Card>
  )
}
