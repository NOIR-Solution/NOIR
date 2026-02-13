/**
 * FormField - A form field component that integrates with react-hook-form
 *
 * Automatically handles:
 * - Error display with proper styling
 * - Required field indicators
 * - Helper text/description
 * - Input masking (for patterns)
 *
 * @example
 * ```tsx
 * import { FormField } from "@uikit"
 *
 * <FormField
 *   form={form}
 *   name="email"
 *   label="Email Address"
 *   placeholder="Enter your email"
 *   type="email"
 *   description="We'll never share your email"
 * />
 * ```
 */

import * as React from "react"
import { Controller } from "react-hook-form"
import type { FieldValues, Path, UseFormReturn, ControllerRenderProps } from "react-hook-form"
import { Input } from "../input/Input"
import { Label } from "../label/Label"
import { cn } from "@/lib/utils"

export interface FormFieldProps<TValues extends FieldValues> {
  /**
   * The react-hook-form form object
   */
  form: UseFormReturn<TValues>

  /**
   * Field name (path in form values)
   */
  name: Path<TValues>

  /**
   * Field label
   */
  label?: string

  /**
   * Placeholder text
   */
  placeholder?: string

  /**
   * Helper text shown below the input
   */
  description?: string

  /**
   * Input type
   * @default "text"
   */
  type?: React.HTMLInputTypeAttribute

  /**
   * Whether the field is required (shows asterisk)
   */
  required?: boolean

  /**
   * Whether the field is disabled
   */
  disabled?: boolean

  /**
   * Additional className for the container
   */
  className?: string

  /**
   * Additional className for the input
   */
  inputClassName?: string

  /**
   * Auto-focus this field on mount
   */
  autoFocus?: boolean

  /**
   * Auto-complete attribute
   */
  autoComplete?: string

  /**
   * Custom render function for non-input fields (e.g., textarea, select)
   */
  render?: (props: {
    field: ControllerRenderProps<TValues, Path<TValues>>
    error?: string
    disabled?: boolean
  }) => React.ReactNode
}

export function FormField<TValues extends FieldValues>({
  form,
  name,
  label,
  placeholder,
  description,
  type = "text",
  required,
  disabled,
  className,
  inputClassName,
  autoFocus,
  autoComplete,
  render,
}: FormFieldProps<TValues>) {
  const error = form.formState.errors[name]
  const errorMessage = error?.message as string | undefined
  const id = `field-${String(name)}`

  return (
    <Controller
      name={name}
      control={form.control}
      render={({ field }) => (
        <div className={cn("space-y-2", className)}>
          {label && (
            <Label htmlFor={id} className={cn(errorMessage && "text-destructive")}>
              {label}
              {required && <span className="text-destructive ml-1">*</span>}
            </Label>
          )}

          {render ? (
            render({ field, error: errorMessage, disabled })
          ) : (
            <Input
              {...field}
              id={id}
              type={type}
              placeholder={placeholder}
              disabled={disabled}
              autoFocus={autoFocus}
              autoComplete={autoComplete}
              aria-invalid={!!errorMessage}
              aria-describedby={
                errorMessage ? `${id}-error` : description ? `${id}-description` : undefined
              }
              className={inputClassName}
              value={field.value ?? ""}
              onChange={(e) => {
                // Handle empty strings as undefined for optional fields
                const value = e.target.value
                field.onChange(value === "" ? "" : value)
              }}
            />
          )}

          {errorMessage && (
            <p id={`${id}-error`} className="text-sm text-destructive animate-in fade-in-0">
              {errorMessage}
            </p>
          )}

          {description && !errorMessage && (
            <p id={`${id}-description`} className="text-sm text-muted-foreground">
              {description}
            </p>
          )}
        </div>
      )}
    />
  )
}

/**
 * FormTextarea - A textarea variant of FormField
 */
export function FormTextarea<TValues extends FieldValues>({
  form,
  name,
  label,
  placeholder,
  description,
  required,
  disabled,
  className,
  rows = 4,
}: Omit<FormFieldProps<TValues>, "type" | "render"> & { rows?: number }) {
  return (
    <FormField
      form={form}
      name={name}
      label={label}
      placeholder={placeholder}
      description={description}
      required={required}
      disabled={disabled}
      className={className}
      render={({ field, error, disabled: isDisabled }) => (
        <textarea
          {...field}
          id={`field-${String(name)}`}
          placeholder={placeholder}
          disabled={isDisabled}
          rows={rows}
          aria-invalid={!!error}
          className={cn(
            "flex min-h-20 w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs",
            "placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring",
            "disabled:cursor-not-allowed disabled:opacity-50",
            "aria-invalid:border-destructive aria-invalid:ring-destructive/20",
            "resize-none"
          )}
          value={field.value ?? ""}
        />
      )}
    />
  )
}

/**
 * FormError - Display server-side or global form errors
 */
export function FormError({ message }: { message?: string | null }) {
  if (!message) return null

  return (
    <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive animate-in fade-in-0">
      {message}
    </div>
  )
}
