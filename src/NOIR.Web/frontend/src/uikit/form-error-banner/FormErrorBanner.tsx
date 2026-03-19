import { useCallback } from 'react'
import { AlertCircle, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'

export interface FormErrorBannerProps {
  /**
   * List of error messages to display.
   * When empty or undefined, the banner is hidden.
   */
  errors?: string[]

  /**
   * Callback when user clicks the dismiss (X) button.
   */
  onDismiss?: () => void

  /**
   * Optional title for the error banner.
   * @default "Unable to save"
   */
  title?: string

  /**
   * Additional CSS classes on the banner wrapper.
   */
  className?: string
}

/**
 * FormErrorBanner — displays form-level / server-side validation errors
 * at the top of a form.
 *
 * Design:
 * - Destructive alert style with red border + background
 * - Dismiss button (X) to manually close
 * - Supports multiple error messages as a bullet list
 * - Auto-hides when `errors` is empty
 *
 * Placement: inside <CredenzaBody> or <form>, BEFORE the first <FormField>.
 *
 * @example
 * ```tsx
 * <FormErrorBanner
 *   errors={serverErrors}
 *   onDismiss={() => setServerErrors([])}
 *   title={t('validation.unableToSave')}
 * />
 * ```
 */
export const FormErrorBanner = ({ errors, onDismiss, title, className }: FormErrorBannerProps) => {
  const handleDismiss = useCallback(() => {
    onDismiss?.()
  }, [onDismiss])

  if (!errors || errors.length === 0) return null

  return (
    <div
      role="alert"
      aria-live="assertive"
      className={cn(
        'relative rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive',
        className,
      )}
    >
      <div className="flex items-start gap-3">
        <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" />
        <div className="flex-1 min-w-0">
          {title && (
            <p className="font-medium mb-1">{title}</p>
          )}
          {errors.length === 1 ? (
            <p>{errors[0]}</p>
          ) : (
            <ul className="list-disc pl-4 space-y-0.5">
              {errors.map((error, i) => (
                <li key={i}>{error}</li>
              ))}
            </ul>
          )}
        </div>
        {onDismiss && (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="h-6 w-6 shrink-0 text-destructive hover:text-destructive hover:bg-destructive/10 cursor-pointer"
            onClick={handleDismiss}
            aria-label="Dismiss"
          >
            <X className="h-3.5 w-3.5" />
          </Button>
        )}
      </div>
    </div>
  )
}
