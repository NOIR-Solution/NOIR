import * as React from 'react'
import * as LabelPrimitive from '@radix-ui/react-label'
import { Slot } from '@radix-ui/react-slot'
import {
  Controller,
  FormProvider,
  useFormContext,
} from 'react-hook-form'
import type {
  ControllerProps,
  FieldPath,
  FieldValues,
} from 'react-hook-form'

import { cn } from '@/lib/utils'
import { Label } from '../label/Label'

// ---------------------------------------------------------------------------
// Schema context — provides the set of required field names to all children.
// Populated by <Form requiredFields={...}> and consumed by <FormLabel>.
// ---------------------------------------------------------------------------

const FormSchemaContext = React.createContext<Set<string>>(new Set())

/**
 * Hook to access the set of required field names from the nearest <Form>.
 * Returns an empty Set when no schema is provided (backward-compatible).
 */
export const useFormRequiredFields = () => React.useContext(FormSchemaContext)

// ---------------------------------------------------------------------------
// Form — wraps react-hook-form FormProvider + optional schema context
// ---------------------------------------------------------------------------

export interface FormProps {
  /** Set of required field names — pass `getRequiredFields(schema)` for auto-asterisk on FormLabel */
  requiredFields?: Set<string>
  children?: React.ReactNode
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  [key: string]: any
}

const Form = ({ requiredFields, ...props }: FormProps) => {
  const stableEmpty = React.useMemo(() => new Set<string>(), [])
  return (
    <FormSchemaContext.Provider value={requiredFields ?? stableEmpty}>
      {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
      <FormProvider {...(props as any)} />
    </FormSchemaContext.Provider>
  )
}

// ---------------------------------------------------------------------------
// FormField
// ---------------------------------------------------------------------------

type FormFieldContextValue<
  TFieldValues extends FieldValues = FieldValues,
  TName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>
> = {
  name: TName
}

const FormFieldContext = React.createContext<FormFieldContextValue>(
  {} as FormFieldContextValue
)

const FormField = <
  TFieldValues extends FieldValues = FieldValues,
  TName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>
>({
  ...props
}: ControllerProps<TFieldValues, TName>) => {
  return (
    <FormFieldContext.Provider value={{ name: props.name }}>
      <Controller {...props} />
    </FormFieldContext.Provider>
  )
}

// ---------------------------------------------------------------------------
// useFormField
// ---------------------------------------------------------------------------

const useFormField = () => {
  const fieldContext = React.useContext(FormFieldContext)
  const itemContext = React.useContext(FormItemContext)
  const { getFieldState, formState } = useFormContext()

  const fieldState = getFieldState(fieldContext.name, formState)

  if (!fieldContext) {
    throw new Error('useFormField should be used within <FormField>')
  }

  const { id, isFocused } = itemContext

  return {
    id,
    name: fieldContext.name,
    formItemId: `${id}-form-item`,
    formDescriptionId: `${id}-form-item-description`,
    formMessageId: `${id}-form-item-message`,
    isFocused,
    ...fieldState,
  }
}

// ---------------------------------------------------------------------------
// FormItem — tracks focus state for "reward early, punish late" UX
//
// Errors and red borders are suppressed while the field is focused so the
// user can type freely to fix mistakes without seeing distracting inline
// errors. Errors appear (or re-appear) only after the user leaves the field.
// ---------------------------------------------------------------------------

type FormItemContextValue = {
  id: string
  isFocused: boolean
}

const FormItemContext = React.createContext<FormItemContextValue>(
  { id: '', isFocused: false }
)

const FormItem = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, onFocus, onBlur, ...props }, ref) => {
  const id = React.useId()
  const [isFocused, setIsFocused] = React.useState(false)

  // Use a timer to debounce blur so that focus shifts between children
  // (e.g. show/hide password button, Radix Select popover close) don't
  // produce a brief error flash.
  const blurTimer = React.useRef<ReturnType<typeof setTimeout>>(undefined)

  const handleFocusCapture = React.useCallback(() => {
    clearTimeout(blurTimer.current)
    setIsFocused(true)
  }, [])

  const handleBlurCapture = React.useCallback(() => {
    blurTimer.current = setTimeout(() => setIsFocused(false), 0)
  }, [])

  React.useEffect(() => {
    return () => clearTimeout(blurTimer.current)
  }, [])

  return (
    <FormItemContext.Provider value={{ id, isFocused }}>
      <div
        ref={ref}
        className={cn('space-y-2', className)}
        onFocusCapture={handleFocusCapture}
        onBlurCapture={handleBlurCapture}
        {...props}
      />
    </FormItemContext.Provider>
  )
})
FormItem.displayName = 'FormItem'

// ---------------------------------------------------------------------------
// FormLabel — auto-shows red asterisk for required fields
// ---------------------------------------------------------------------------

const FormLabel = React.forwardRef<
  React.ElementRef<typeof LabelPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof LabelPrimitive.Root>
>(({ className, children, ...props }, ref) => {
  const { error, formItemId, name, isFocused, isDirty } = useFormField()
  const { formState } = useFormContext()
  const requiredFields = React.useContext(FormSchemaContext)
  const isRequired = requiredFields.has(name)

  // Same two gates as FormMessage/FormControl: only show error color when
  // the user has meaningfully interacted AND is not currently focused
  const isServerError = error && typeof error === 'object' && 'type' in error && error.type === 'server'
  const hasBeenValidated = isServerError || isDirty || formState.isSubmitted
  const showErrorColor = !!error && !isFocused && hasBeenValidated

  return (
    <Label
      ref={ref}
      className={cn(showErrorColor && 'text-destructive', 'gap-0.5', className)}
      htmlFor={formItemId}
      {...props}
    >
      {children}
      {isRequired && (
        <span className="text-destructive" aria-hidden="true">*</span>
      )}
    </Label>
  )
})
FormLabel.displayName = 'FormLabel'

// ---------------------------------------------------------------------------
// FormControl
// ---------------------------------------------------------------------------

const FormControl = React.forwardRef<
  React.ElementRef<typeof Slot>,
  React.ComponentPropsWithoutRef<typeof Slot>
>(({ ...props }, ref) => {
  const { error, formItemId, formDescriptionId, formMessageId, isDirty, isFocused } = useFormField()
  const { formState } = useFormContext()

  const isServerError = error && typeof error === 'object' && 'type' in error && error.type === 'server'
  // Show red border only when: field is not focused AND (dirty, submitted, or server error)
  const shouldShowInvalid = !!error && !isFocused && (isServerError || isDirty || formState.isSubmitted)

  return (
    <Slot
      ref={ref}
      id={formItemId}
      aria-describedby={
        !error
          ? `${formDescriptionId}`
          : `${formDescriptionId} ${formMessageId}`
      }
      aria-invalid={shouldShowInvalid}
      {...props}
    />
  )
})
FormControl.displayName = 'FormControl'

// ---------------------------------------------------------------------------
// FormDescription
// ---------------------------------------------------------------------------

const FormDescription = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => {
  const { formDescriptionId } = useFormField()

  return (
    <p
      ref={ref}
      id={formDescriptionId}
      className={cn('text-sm text-muted-foreground', className)}
      {...props}
    />
  )
})
FormDescription.displayName = 'FormDescription'

// ---------------------------------------------------------------------------
// FormMessage — shows error after field interaction, hidden while focused
//
// "Reward early, punish late":
//   - User focuses + types → no error while actively editing (isFocused=true)
//   - User blurs with invalid value → error shown (isFocused=false, isDirty=true)
//   - User focuses + blurs WITHOUT editing → no error (isDirty=false)
//   - User clicks Submit → all errors shown when field loses focus
//   - Server errors (type="server") → shown immediately, hidden while focused
// ---------------------------------------------------------------------------

const FormMessage = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(({ className, children, ...props }, ref) => {
  const { error, formMessageId, isDirty, isFocused } = useFormField()
  const { formState } = useFormContext()

  const isServerError = error && typeof error === 'object' && 'type' in error && error.type === 'server'

  // Gate 1: Should this error be visible at all (has the user "earned" seeing it)?
  const hasBeenValidated = isServerError || isDirty || formState.isSubmitted
  // Gate 2: Is the user currently focused on this field?
  const shouldShow = hasBeenValidated && !isFocused

  const body = error ? String(error?.message) : children

  if (!body || !shouldShow) {
    return null
  }

  return (
    <p
      ref={ref}
      id={formMessageId}
      // Accessibility: role="alert" + aria-live="assertive" ensure screen readers
      // announce validation errors immediately (WCAG 3.3.1, 4.1.3)
      role="alert"
      aria-live="assertive"
      className={cn('text-sm font-medium text-destructive', className)}
      {...props}
    >
      {body}
    </p>
  )
})
FormMessage.displayName = 'FormMessage'

export {
  useFormField,
  Form,
  FormItem,
  FormLabel,
  FormControl,
  FormDescription,
  FormMessage,
  FormField,
}
