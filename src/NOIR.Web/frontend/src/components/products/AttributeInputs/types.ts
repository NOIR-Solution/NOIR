/**
 * Shared types for attribute input components
 */
import type { ProductAttributeFormField, ProductAttributeValue } from '@/types/productAttribute'

export type AttributeValue = string | number | boolean | string[] | null | undefined

export interface AttributeInputProps {
  /** The attribute field definition from form schema */
  field: ProductAttributeFormField
  /** Current value of the attribute */
  value: AttributeValue
  /** Callback when value changes */
  onChange: (value: AttributeValue) => void
  /** Whether the input is disabled (view mode) */
  disabled?: boolean
  /** Error message to display */
  error?: string
}

export interface ColorSwatchProps {
  options: ProductAttributeValue[]
  value: string | string[] | null | undefined
  onChange: (value: string | string[]) => void
  disabled?: boolean
  isMultiple?: boolean
}
