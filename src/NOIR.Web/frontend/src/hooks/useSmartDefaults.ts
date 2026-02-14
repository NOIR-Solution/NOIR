import { useEffect, useRef, useCallback } from 'react'
import {
  type UseFormReturn,
  type Path,
  type FieldValues,
  type PathValue,
} from 'react-hook-form'
import { generateSKU } from '@/lib/utils/sku'

/**
 * Generate a URL-friendly slug from a string
 */
const generateSlug = (text: string): string => {
  return text
    .toLowerCase()
    .trim()
    .replace(/[^\w\s-]/g, '') // Remove special characters
    .replace(/[\s_-]+/g, '-') // Replace spaces and underscores with hyphens
    .replace(/^-+|-+$/g, '') // Remove leading/trailing hyphens
}

/**
 * Debounce utility for duplicate checking
 */
const debounce = <T extends (...args: Parameters<T>) => void>(
  func: T,
  wait: number
): T => {
  let timeout: ReturnType<typeof setTimeout> | null = null

  return ((...args: Parameters<T>) => {
    if (timeout) clearTimeout(timeout)
    timeout = setTimeout(() => func(...args), wait)
  }) as T
}

interface UseSmartDefaultsOptions<T extends FieldValues> {
  /** React Hook Form instance */
  form: UseFormReturn<T>
  /** Field to watch for changes (source of auto-generation) */
  sourceField: Path<T>
  /** Target fields to auto-populate */
  targetFields: {
    /** Auto-generate slug from source */
    slug?: Path<T>
    /** Auto-generate SKU from source */
    sku?: Path<T>
  }
  /** Category code for SKU generation */
  categoryCode?: string
  /** Whether this is an edit form (skip auto-generation on mount) */
  isEditing?: boolean
  /** Function to check for duplicate values */
  checkDuplicate?: (field: string, value: string) => Promise<boolean>
  /** Callback when duplicate is found */
  onDuplicateFound?: (field: string, value: string) => void
}

/**
 * Hook for auto-generating form field values
 *
 * Automatically generates slugs and SKUs from a source field,
 * with optional duplicate detection.
 *
 * @example
 * useSmartDefaults({
 *   form,
 *   sourceField: 'name',
 *   targetFields: { slug: 'slug', sku: 'sku' },
 *   isEditing: Boolean(productId),
 * })
 */
export const useSmartDefaults = <T extends FieldValues>({
  form,
  sourceField,
  targetFields,
  categoryCode,
  isEditing = false,
  checkDuplicate,
  onDuplicateFound,
}: UseSmartDefaultsOptions<T>) => {
  const initialMount = useRef(true)
  const userModified = useRef<Set<string>>(new Set())

  // Watch source field for changes
  const sourceValue = form.watch(sourceField)

  // Track user modifications to target fields
  useEffect(() => {
    const subscription = form.watch((_value, { name }) => {
      if (name && Object.values(targetFields).includes(name as Path<T>)) {
        // User manually modified this field
        userModified.current.add(name)
      }
    })
    return () => subscription.unsubscribe()
  }, [form, targetFields])

  // Debounced duplicate check
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const debouncedCheck = useCallback(
    debounce(async (field: string, value: string) => {
      if (checkDuplicate && value) {
        const isDuplicate = await checkDuplicate(field, value)
        if (isDuplicate) {
          onDuplicateFound?.(field, value)
        }
      }
    }, 500),
    [checkDuplicate, onDuplicateFound]
  )

  useEffect(() => {
    // Skip on initial mount for edit mode
    if (isEditing && initialMount.current) {
      initialMount.current = false
      return
    }
    initialMount.current = false

    if (!sourceValue || typeof sourceValue !== 'string') return

    // Auto-generate slug
    if (targetFields.slug && !userModified.current.has(targetFields.slug)) {
      const currentSlug = form.getValues(targetFields.slug)
      // Only update if empty or not manually modified
      if (!currentSlug || (!isEditing && !userModified.current.has(targetFields.slug))) {
        const newSlug = generateSlug(sourceValue)
        form.setValue(targetFields.slug, newSlug as PathValue<T, Path<T>>, {
          shouldValidate: false,
        })
        debouncedCheck('slug', newSlug)
      }
    }

    // Auto-generate SKU
    if (targetFields.sku && !userModified.current.has(targetFields.sku)) {
      const currentSku = form.getValues(targetFields.sku)
      // Only generate SKU if empty (never overwrite existing)
      if (!currentSku) {
        const newSku = generateSKU(sourceValue, categoryCode)
        form.setValue(targetFields.sku, newSku as PathValue<T, Path<T>>, {
          shouldValidate: false,
        })
      }
    }
  }, [sourceValue, form, targetFields, isEditing, categoryCode, debouncedCheck])

  /**
   * Reset user modification tracking (call when form is reset)
   */
  const resetModifications = useCallback(() => {
    userModified.current.clear()
    initialMount.current = true
  }, [])

  return { resetModifications }
}
