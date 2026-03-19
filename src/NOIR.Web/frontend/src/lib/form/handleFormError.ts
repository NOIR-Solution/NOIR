import type { FieldValues, Path, UseFormReturn } from 'react-hook-form'
import { ApiError } from '@/services/apiClient'

// Accept any callable translation function (compatible with i18next TFunction overloads)
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type TranslateFn = (...args: any[]) => any

/**
 * Routes a caught API error to the appropriate display mechanism:
 * - Field-specific errors → form.setError() (inline under field)
 * - Form-level message → setServerErrors() (FormErrorBanner at top of form)
 *
 * Usage in form submit catch block:
 * ```tsx
 * catch (err) {
 *   handleFormError(err, form, setServerErrors, t)
 * }
 * ```
 *
 * This replaces `toast.error()` for form validation errors.
 * Toast should ONLY be used for network errors or success messages.
 */
export const handleFormError = <TValues extends FieldValues>(
  err: unknown,
  form: UseFormReturn<TValues>,
  setServerErrors: (errors: string[]) => void,
  t: TranslateFn,
): void => {
  const errors: string[] = []

  if (err instanceof ApiError) {
    // Route field-specific validation errors inline
    if (err.hasFieldErrors && err.errors) {
      for (const [field, messages] of Object.entries(err.errors)) {
        // Backend sends PascalCase, frontend uses camelCase
        const camelField = field.charAt(0).toLowerCase() + field.slice(1)
        if (messages.length > 0) {
          form.setError(camelField as Path<TValues>, {
            type: 'server',
            message: messages[0],
          })
        }
      }
    }

    // Always show the form-level message in banner
    if (err.message) {
      errors.push(err.message)
    }
  } else if (err instanceof Error) {
    errors.push(err.message)
  } else {
    errors.push(t('errors.anErrorOccurred', 'An error occurred'))
  }

  if (errors.length > 0) {
    setServerErrors(errors)
  }
}
