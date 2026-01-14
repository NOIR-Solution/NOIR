/**
 * useValidatedForm - A wrapper around react-hook-form with Zod validation
 *
 * This hook provides type-safe form handling with validation rules
 * that are automatically synced with the backend FluentValidation rules.
 *
 * @example
 * ```tsx
 * import { useValidatedForm } from "@/hooks/useValidatedForm"
 * import { createTenantSchema } from "@/validation/schemas.generated"
 *
 * function CreateTenantForm() {
 *   const { form, handleSubmit, isSubmitting } = useValidatedForm({
 *     schema: createTenantSchema,
 *     defaultValues: { identifier: "", name: "" },
 *     onSubmit: async (data) => {
 *       await api.post("/api/tenants", data)
 *     },
 *   })
 *
 *   return (
 *     <form onSubmit={handleSubmit}>
 *       <FormField form={form} name="identifier" label="Identifier" />
 *       <FormField form={form} name="name" label="Name" />
 *       <button type="submit" disabled={isSubmitting}>Create</button>
 *     </form>
 *   )
 * }
 * ```
 */

import { useCallback, useState } from "react"
import { useForm } from "react-hook-form"
import type { UseFormReturn, DefaultValues, Path, FieldValues, Resolver } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import type { z } from "zod"

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnyZodObject = z.ZodObject<any>

export interface UseValidatedFormOptions<TValues extends FieldValues> {
  /**
   * Zod schema for validation (from schemas.generated.ts)
   */
  schema: AnyZodObject

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
  mode?: "onBlur" | "onChange" | "onSubmit" | "onTouched" | "all"
}

export interface UseValidatedFormReturn<TValues extends FieldValues> {
  /**
   * The react-hook-form form object
   */
  form: UseFormReturn<TValues>

  /**
   * Submit handler to attach to form onSubmit
   */
  handleSubmit: (e?: React.BaseSyntheticEvent) => Promise<void>

  /**
   * Whether form is currently submitting
   */
  isSubmitting: boolean

  /**
   * Whether form has been modified
   */
  isDirty: boolean

  /**
   * Whether all fields are valid
   */
  isValid: boolean

  /**
   * Server-side error message (set by onError handler)
   */
  serverError: string | null

  /**
   * Clear server-side error
   */
  clearServerError: () => void

  /**
   * Set a server error from API response
   */
  setServerError: (error: string | null) => void

  /**
   * Set error on a specific field (e.g., from server validation)
   */
  setFieldError: (field: Path<TValues>, message: string) => void

  /**
   * Reset form to default values
   */
  reset: () => void
}

/**
 * A hook that provides type-safe form handling with Zod validation.
 *
 * Features:
 * - Auto-validates using Zod schemas generated from FluentValidation
 * - Tracks submission state
 * - Handles server-side errors
 * - Provides utilities for field-level error setting
 */
export function useValidatedForm<TValues extends FieldValues>({
  schema,
  defaultValues,
  onSubmit,
  onError,
  mode = "onBlur",
}: UseValidatedFormOptions<TValues>): UseValidatedFormReturn<TValues> {
  const [serverError, setServerError] = useState<string | null>(null)

  // Use type assertion for resolver to avoid complex generic compatibility issues
  // The zodResolver correctly validates TValues at runtime
  const form = useForm<TValues>({
    resolver: zodResolver(schema) as unknown as Resolver<TValues>,
    defaultValues,
    mode,
  })

  const handleSubmit = useCallback(
    async (e?: React.BaseSyntheticEvent) => {
      e?.preventDefault()
      setServerError(null)

      await form.handleSubmit(async (data) => {
        try {
          await onSubmit(data)
        } catch (error) {
          // Handle API error responses
          if (error instanceof Error) {
            setServerError(error.message)
          } else if (typeof error === "object" && error !== null) {
            // Handle ProblemDetails response
            const problemDetails = error as { detail?: string; title?: string; errors?: Record<string, string[]> }

            if (problemDetails.errors) {
              // Map field errors from server
              Object.entries(problemDetails.errors).forEach(([field, messages]) => {
                const fieldName = field.charAt(0).toLowerCase() + field.slice(1)
                if (messages.length > 0) {
                  form.setError(fieldName as Path<TValues>, {
                    type: "server",
                    message: messages[0],
                  })
                }
              })
            }

            setServerError(problemDetails.detail || problemDetails.title || "An error occurred")
          }

          onError?.(error)
        }
      })(e)
    },
    [form, onSubmit, onError]
  )

  const setFieldError = useCallback(
    (field: Path<TValues>, message: string) => {
      form.setError(field, { type: "server", message })
    },
    [form]
  )

  const clearServerError = useCallback(() => {
    setServerError(null)
  }, [])

  const reset = useCallback(() => {
    form.reset()
    setServerError(null)
  }, [form])

  return {
    form,
    handleSubmit,
    isSubmitting: form.formState.isSubmitting,
    isDirty: form.formState.isDirty,
    isValid: form.formState.isValid,
    serverError,
    clearServerError,
    setServerError,
    setFieldError,
    reset,
  }
}
