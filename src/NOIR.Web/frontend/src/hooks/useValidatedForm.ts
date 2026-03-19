/**
 * useValidatedForm - Form handling with Zod validation + server error routing
 *
 * Features:
 * - Auto-validates using Zod schemas (synced with FluentValidation)
 * - Routes server errors: field-specific → inline, form-level → banner
 * - Tracks submitted fields for auto-dismissing FormErrorBanner
 * - Provides `requiredFields` for <Form> auto-asterisk
 *
 * @example
 * ```tsx
 * const { form, handleSubmit, serverErrors, dismissServerErrors, requiredFields } =
 *   useValidatedForm({
 *     schema: createBrandSchema(t),
 *     defaultValues: { name: '', slug: '' },
 *     onSubmit: async (data) => {
 *       await createBrand(data)
 *       onOpenChange(false)
 *     },
 *   })
 *
 * return (
 *   <Form {...form} requiredFields={requiredFields}>
 *     <form onSubmit={handleSubmit}>
 *       <FormErrorBanner errors={serverErrors} onDismiss={dismissServerErrors} />
 *       ...fields...
 *     </form>
 *   </Form>
 * )
 * ```
 */

import { useCallback, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import type { UseFormReturn, DefaultValues, Path, FieldValues, Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import type { z } from 'zod'
import { ApiError } from '@/services/apiClient'
import { getRequiredFields } from '@/lib/form'

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnyZodSchema = z.ZodObject<any>

export interface UseValidatedFormOptions<TValues extends FieldValues> {
  /**
   * Zod schema for validation (from schemas.generated.ts or factory)
   */
  schema: AnyZodSchema

  /**
   * Default form values
   */
  defaultValues?: DefaultValues<TValues>

  /**
   * Handler called on successful validation and submission
   */
  onSubmit: (data: TValues) => Promise<void> | void

  /**
   * Handler called when submission fails
   */
  onError?: (error: unknown) => void

  /**
   * Form mode for validation timing
   * @default "onBlur"
   */
  mode?: 'onBlur' | 'onChange' | 'onSubmit' | 'onTouched' | 'all'
}

export interface UseValidatedFormReturn<TValues extends FieldValues> {
  /** The react-hook-form form object */
  form: UseFormReturn<TValues>

  /** Submit handler to attach to form onSubmit */
  handleSubmit: (e?: React.BaseSyntheticEvent) => Promise<void>

  /** Whether form is currently submitting */
  isSubmitting: boolean

  /** Whether form has been modified */
  isDirty: boolean

  /** Whether all fields are valid */
  isValid: boolean

  /**
   * Form-level server errors to display in FormErrorBanner.
   * Excludes field-specific errors (those go inline via setError).
   */
  serverErrors: string[]

  /** Dismiss server errors banner */
  dismissServerErrors: () => void

  /** Set a server error from API response */
  setServerError: (error: string | null) => void

  /** Set error on a specific field (e.g., from server validation) */
  setFieldError: (field: Path<TValues>, message: string) => void

  /** Reset form to default values and clear all server errors */
  reset: () => void

  /**
   * Set of required field names from Zod schema.
   * Pass to <Form requiredFields={requiredFields}> for auto-asterisk.
   */
  requiredFields: Set<string>

  // Legacy compat
  /** @deprecated Use serverErrors[0] instead */
  serverError: string | null
  /** @deprecated Use dismissServerErrors instead */
  clearServerError: () => void
}

/**
 * A hook that provides type-safe form handling with Zod validation
 * and automatic server error routing.
 *
 * Error routing:
 * - ApiError with field errors → form.setError() (inline under field)
 * - ApiError without field errors → serverErrors[] (FormErrorBanner)
 * - Network/unknown errors → serverErrors[] (FormErrorBanner)
 */
export const useValidatedForm = <TValues extends FieldValues>({
  schema,
  defaultValues,
  onSubmit,
  onError,
  mode = 'onBlur',
}: UseValidatedFormOptions<TValues>): UseValidatedFormReturn<TValues> => {
  const { t } = useTranslation('common')
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

  // Use type assertion for resolver to avoid complex generic compatibility issues
  // The zodResolver correctly validates TValues at runtime
  const form = useForm<TValues>({
    resolver: zodResolver(schema) as unknown as Resolver<TValues>,
    defaultValues,
    mode,
    reValidateMode: 'onChange',
  })

  const handleSubmit = useCallback(
    async (e?: React.BaseSyntheticEvent) => {
      e?.preventDefault()
      setServerErrors([])

      await form.handleSubmit(async (data) => {
        try {
          await onSubmit(data)
        } catch (error) {
          const result = routeApiError(error, t)

          // Set field-specific errors inline
          if (result.fieldErrors.length > 0) {
            for (const { field, message } of result.fieldErrors) {
              form.setError(field as Path<TValues>, {
                type: 'server',
                message,
              })
            }
          }

          // Set form-level errors for banner
          if (result.formErrors.length > 0) {
            setServerErrors(result.formErrors)
          }

          onError?.(error)
        }
      })(e)
    },
    [form, onSubmit, onError, t],
  )

  const setFieldError = useCallback(
    (field: Path<TValues>, message: string) => {
      form.setError(field, { type: 'server', message })
    },
    [form],
  )

  const dismissServerErrors = useCallback(() => {
    setServerErrors([])
  }, [])

  const setServerError = useCallback((error: string | null) => {
    setServerErrors(error ? [error] : [])
  }, [])

  const reset = useCallback(() => {
    form.reset()
    setServerErrors([])
  }, [form])

  return {
    form,
    handleSubmit,
    isSubmitting: form.formState.isSubmitting,
    isDirty: form.formState.isDirty,
    isValid: form.formState.isValid,
    serverErrors,
    dismissServerErrors,
    setServerError,
    setFieldError,
    reset,
    requiredFields,
    // Legacy compat
    serverError: serverErrors[0] ?? null,
    clearServerError: dismissServerErrors,
  }
}

// ---------------------------------------------------------------------------
// Error routing utility
// ---------------------------------------------------------------------------

interface RoutedErrors {
  fieldErrors: Array<{ field: string; message: string }>
  formErrors: string[]
}

// Accept any callable translation function (compatible with i18next TFunction overloads)
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type TranslateFn = (...args: any[]) => any

/**
 * Routes an API error into field-specific and form-level errors.
 *
 * - ApiError with `.errors` → field errors (PascalCase → camelCase)
 * - ApiError without field errors → form error from message
 * - Generic Error → form error from message
 * - Unknown → generic fallback message
 */
const routeApiError = (error: unknown, t: TranslateFn): RoutedErrors => {
  const result: RoutedErrors = { fieldErrors: [], formErrors: [] }

  if (error instanceof ApiError) {
    // Route field-level validation errors (from ValidationProblemDetails.Errors)
    if (error.hasFieldErrors && error.errors) {
      for (const [field, messages] of Object.entries(error.errors)) {
        // Backend sends PascalCase property names, frontend uses camelCase
        const camelField = field.charAt(0).toLowerCase() + field.slice(1)
        if (messages.length > 0) {
          result.fieldErrors.push({ field: camelField, message: messages[0] })
        }
      }
    }

    // Always add the form-level message (detail or title from ProblemDetails)
    // This shows context in the banner even when there are also field errors
    const formMessage = error.message
    if (formMessage) {
      result.formErrors.push(formMessage)
    }
  } else if (error instanceof Error) {
    result.formErrors.push(error.message)
  } else {
    result.formErrors.push(t('errors.anErrorOccurred', 'An error occurred'))
  }

  return result
}
