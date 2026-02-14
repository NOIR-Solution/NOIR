import { useState, useRef, useCallback, useEffect } from 'react'
import { useDebouncedCallback } from 'use-debounce'
import { z } from 'zod'
import { updateProductVariant } from '@/services/products'
import type { ProductVariant, UpdateProductVariantRequest } from '@/types/product'
import { ApiError } from '@/services/apiClient'

// Validation schema for variant row
export const variantRowSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  sku: z.string().optional().nullable(),
  price: z.coerce.number().min(0, 'Price must be non-negative'),
  compareAtPrice: z.coerce.number().min(0, 'Compare price must be non-negative').optional().nullable(),
  stockQuantity: z.coerce.number().int('Stock must be a whole number').min(0, 'Stock must be non-negative'),
  sortOrder: z.coerce.number().int('Sort order must be a whole number').min(0, 'Sort order must be non-negative'),
})

export type VariantRowData = z.infer<typeof variantRowSchema>

export type AutoSaveStatus = 'idle' | 'dirty' | 'saving' | 'saved' | 'error'

export interface VariantFieldError {
  field: keyof VariantRowData
  message: string
}

export interface UseVariantAutoSaveOptions {
  /** Product ID (required for API calls) */
  productId: string
  /** Initial variant data */
  variant: ProductVariant
  /** Debounce delay in milliseconds (default: 1500ms) */
  debounceMs?: number
  /** Callback when save completes successfully */
  onSaveSuccess?: (updatedVariant: ProductVariant) => void
  /** Callback when save fails */
  onSaveError?: (error: string) => void
  /** Duration to show 'saved' status before returning to 'idle' (default: 2000ms) */
  savedDisplayMs?: number
}

export interface UseVariantAutoSaveReturn {
  /** Current form values */
  values: VariantRowData
  /** Update a single field value */
  setFieldValue: <K extends keyof VariantRowData>(field: K, value: VariantRowData[K]) => void
  /** Current auto-save status */
  status: AutoSaveStatus
  /** Current error message (if status is 'error') */
  error: string | null
  /** Field-level validation errors */
  fieldErrors: VariantFieldError[]
  /** Whether any field has been modified */
  isDirty: boolean
  /** Manually trigger save (bypasses debounce) */
  saveNow: () => Promise<void>
  /** Revert to last saved values */
  revert: () => void
  /** Check if a specific field has an error */
  hasFieldError: (field: keyof VariantRowData) => boolean
  /** Get error message for a specific field */
  getFieldError: (field: keyof VariantRowData) => string | undefined
}

export const useVariantAutoSave = ({
  productId,
  variant,
  debounceMs = 1500,
  onSaveSuccess,
  onSaveError,
  savedDisplayMs = 2000,
}: UseVariantAutoSaveOptions): UseVariantAutoSaveReturn => {
  // Current form values
  const [values, setValues] = useState<VariantRowData>(() => ({
    name: variant.name,
    sku: variant.sku || null,
    price: variant.price,
    compareAtPrice: variant.compareAtPrice || null,
    stockQuantity: variant.stockQuantity,
    sortOrder: variant.sortOrder,
  }))

  // Last successfully saved values (for revert)
  const lastSavedRef = useRef<VariantRowData>({ ...values })

  // Status tracking
  const [status, setStatus] = useState<AutoSaveStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<VariantFieldError[]>([])

  // Track if dirty
  const [isDirty, setIsDirty] = useState(false)

  // Timeout for 'saved' display
  const savedTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Update values when variant prop changes (e.g., after external update)
  useEffect(() => {
    const newValues: VariantRowData = {
      name: variant.name,
      sku: variant.sku || null,
      price: variant.price,
      compareAtPrice: variant.compareAtPrice || null,
      stockQuantity: variant.stockQuantity,
      sortOrder: variant.sortOrder,
    }
    setValues(newValues)
    lastSavedRef.current = { ...newValues }
    setIsDirty(false)
    setStatus('idle')
    setError(null)
    setFieldErrors([])
  }, [variant.id]) // Only reset when variant ID changes, not on every prop update

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (savedTimeoutRef.current) {
        clearTimeout(savedTimeoutRef.current)
      }
    }
  }, [])

  // Validate current values
  const validate = useCallback((data: VariantRowData): VariantFieldError[] => {
    const result = variantRowSchema.safeParse(data)
    if (result.success) {
      return []
    }
    return result.error.issues.map((issue) => ({
      field: issue.path[0] as keyof VariantRowData,
      message: issue.message,
    }))
  }, [])

  // Save function
  const performSave = useCallback(async (data: VariantRowData) => {
    // Validate first
    const errors = validate(data)
    setFieldErrors(errors)

    if (errors.length > 0) {
      setStatus('error')
      setError('Validation failed')
      return
    }

    setStatus('saving')
    setError(null)

    try {
      const request: UpdateProductVariantRequest = {
        name: data.name,
        price: data.price,
        sku: data.sku || null,
        compareAtPrice: data.compareAtPrice || null,
        stockQuantity: data.stockQuantity,
        sortOrder: data.sortOrder,
      }

      const updatedProduct = await updateProductVariant(productId, variant.id, request)

      // Find the updated variant from the returned product
      const updatedVariant = updatedProduct.variants?.find(v => v.id === variant.id)

      // Update last saved reference
      lastSavedRef.current = { ...data }
      setIsDirty(false)
      setStatus('saved')

      // Clear any existing saved timeout
      if (savedTimeoutRef.current) {
        clearTimeout(savedTimeoutRef.current)
      }

      // Return to idle after display duration
      savedTimeoutRef.current = setTimeout(() => {
        setStatus('idle')
      }, savedDisplayMs)

      onSaveSuccess?.(updatedVariant || variant)
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to save variant'
      setStatus('error')
      setError(message)
      onSaveError?.(message)
    }
  }, [productId, variant.id, validate, savedDisplayMs, onSaveSuccess, onSaveError])

  // Debounced save
  const debouncedSave = useDebouncedCallback((data: VariantRowData) => {
    performSave(data)
  }, debounceMs)

  // Update a single field
  const setFieldValue = useCallback(<K extends keyof VariantRowData>(
    field: K,
    value: VariantRowData[K]
  ) => {
    setValues((prev) => {
      const newValues = { ...prev, [field]: value }

      // Check if actually changed from last saved
      const isChanged = JSON.stringify(newValues) !== JSON.stringify(lastSavedRef.current)
      setIsDirty(isChanged)

      // Validate on change
      const errors = validate(newValues)
      setFieldErrors(errors)

      if (isChanged) {
        setStatus('dirty')
        // Only trigger auto-save if no validation errors
        if (errors.length === 0) {
          debouncedSave(newValues)
        }
      }

      return newValues
    })
  }, [validate, debouncedSave])

  // Immediate save (bypasses debounce)
  const saveNow = useCallback(async () => {
    debouncedSave.cancel()
    await performSave(values)
  }, [debouncedSave, performSave, values])

  // Revert to last saved values
  const revert = useCallback(() => {
    debouncedSave.cancel()
    setValues({ ...lastSavedRef.current })
    setIsDirty(false)
    setStatus('idle')
    setError(null)
    setFieldErrors([])
  }, [debouncedSave])

  // Helper to check field errors
  const hasFieldError = useCallback((field: keyof VariantRowData): boolean => {
    return fieldErrors.some((e) => e.field === field)
  }, [fieldErrors])

  const getFieldError = useCallback((field: keyof VariantRowData): string | undefined => {
    return fieldErrors.find((e) => e.field === field)?.message
  }, [fieldErrors])

  return {
    values,
    setFieldValue,
    status,
    error,
    fieldErrors,
    isDirty,
    saveNow,
    revert,
    hasFieldError,
    getFieldError,
  }
}
