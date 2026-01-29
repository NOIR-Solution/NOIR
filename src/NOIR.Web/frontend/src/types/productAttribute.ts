/**
 * ProductAttribute-related TypeScript types
 * Mirrors backend DTOs from NOIR.Application.Features.ProductAttributes.DTOs
 */

// ============================================================================
// Attribute Type Enum
// ============================================================================

export type AttributeType =
  | 'Select'
  | 'MultiSelect'
  | 'Text'
  | 'TextArea'
  | 'Number'
  | 'Decimal'
  | 'Boolean'
  | 'Date'
  | 'DateTime'
  | 'Color'
  | 'Range'
  | 'Url'
  | 'File'

// ============================================================================
// Attribute Value Types
// ============================================================================

export interface ProductAttributeValue {
  id: string
  value: string
  displayValue: string
  colorCode?: string | null
  swatchUrl?: string | null
  iconUrl?: string | null
  sortOrder: number
  isActive: boolean
}

// ============================================================================
// Product Attribute Types
// ============================================================================

/**
 * Full product attribute details
 */
export interface ProductAttribute {
  id: string
  code: string
  name: string
  type: AttributeType
  isFilterable: boolean
  isSearchable: boolean
  isRequired: boolean
  isVariantAttribute: boolean
  showInProductCard: boolean
  showInSpecifications: boolean
  unit?: string | null
  validationRegex?: string | null
  minValue?: number | null
  maxValue?: number | null
  maxLength?: number | null
  defaultValue?: string | null
  placeholder?: string | null
  helpText?: string | null
  sortOrder: number
  isActive: boolean
  values: ProductAttributeValue[]
  createdAt: string
  modifiedAt?: string | null
}

/**
 * Simplified attribute for list views
 */
export interface ProductAttributeListItem {
  id: string
  code: string
  name: string
  type: AttributeType
  isFilterable: boolean
  isSearchable: boolean
  isRequired: boolean
  isVariantAttribute: boolean
  showInProductCard: boolean
  showInSpecifications: boolean
  sortOrder: number
  isActive: boolean
  valueCount: number
}

// ============================================================================
// Request Types
// ============================================================================

export interface CreateProductAttributeRequest {
  code: string
  name: string
  type: string
  isFilterable?: boolean
  isSearchable?: boolean
  isRequired?: boolean
  isVariantAttribute?: boolean
  showInProductCard?: boolean
  showInSpecifications?: boolean
  unit?: string | null
  validationRegex?: string | null
  minValue?: number | null
  maxValue?: number | null
  maxLength?: number | null
  defaultValue?: string | null
  placeholder?: string | null
  helpText?: string | null
}

export interface UpdateProductAttributeRequest {
  code: string
  name: string
  isFilterable: boolean
  isSearchable: boolean
  isRequired: boolean
  isVariantAttribute: boolean
  showInProductCard: boolean
  showInSpecifications: boolean
  unit?: string | null
  validationRegex?: string | null
  minValue?: number | null
  maxValue?: number | null
  maxLength?: number | null
  defaultValue?: string | null
  placeholder?: string | null
  helpText?: string | null
  sortOrder: number
  isActive: boolean
}

export interface AddProductAttributeValueRequest {
  value: string
  displayValue: string
  colorCode?: string | null
  swatchUrl?: string | null
  iconUrl?: string | null
  sortOrder?: number
}

export interface UpdateProductAttributeValueRequest {
  value: string
  displayValue: string
  colorCode?: string | null
  swatchUrl?: string | null
  iconUrl?: string | null
  sortOrder: number
  isActive: boolean
}

// ============================================================================
// Category Attribute Types
// ============================================================================

/**
 * Category attribute link - represents an attribute assigned to a category
 */
export interface CategoryAttribute {
  id: string
  categoryId: string
  categoryName: string
  attributeId: string
  attributeName: string
  attributeCode: string
  isRequired: boolean
  sortOrder: number
}

/**
 * Request to assign an attribute to a category
 */
export interface AssignCategoryAttributeRequest {
  attributeId: string
  isRequired?: boolean
  sortOrder?: number
}

/**
 * Request to update category attribute settings
 */
export interface UpdateCategoryAttributeRequest {
  isRequired: boolean
  sortOrder: number
}

// ============================================================================
// Product Attribute Assignment Types (Phase 4)
// ============================================================================

/**
 * Stores a product's actual attribute value
 */
export interface ProductAttributeAssignment {
  id: string
  productId: string
  attributeId: string
  attributeCode: string
  attributeName: string
  attributeType: AttributeType
  variantId?: string | null
  value?: unknown
  displayValue?: string | null
  isRequired: boolean
}

/**
 * Form schema for dynamic attribute form rendering
 */
export interface ProductAttributeFormSchema {
  productId: string
  productName: string
  categoryId?: string | null
  categoryName?: string | null
  fields: ProductAttributeFormField[]
}

/**
 * Individual form field for a product attribute
 */
export interface ProductAttributeFormField {
  attributeId: string
  code: string
  name: string
  type: AttributeType
  isRequired: boolean
  unit?: string | null
  placeholder?: string | null
  helpText?: string | null
  minValue?: number | null
  maxValue?: number | null
  maxLength?: number | null
  defaultValue?: string | null
  validationRegex?: string | null
  options?: ProductAttributeValue[] | null
  currentValue?: unknown
  currentDisplayValue?: string | null
}

/**
 * Form schema for category attributes (used for new product creation).
 * Unlike ProductAttributeFormSchema, this does NOT require a productId.
 */
export interface CategoryAttributeFormSchema {
  categoryId: string
  categoryName: string
  fields: ProductAttributeFormField[]
}

/**
 * Request to set a single attribute value
 */
export interface SetProductAttributeValueRequest {
  variantId?: string | null
  value?: unknown
}

/**
 * Request to bulk update multiple attribute values
 */
export interface BulkUpdateProductAttributesRequest {
  variantId?: string | null
  values: AttributeValueItem[]
}

/**
 * Individual attribute value item for bulk update
 */
export interface AttributeValueItem {
  attributeId: string
  value?: unknown
}

// ============================================================================
// Paged Result Types
// ============================================================================

export interface ProductAttributePagedResult {
  items: ProductAttributeListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
