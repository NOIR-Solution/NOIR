/**
 * Product Attributes API Service
 *
 * Provides methods for managing product attributes.
 */
import { apiClient } from './apiClient'
import type {
  ProductAttribute,
  ProductAttributeListItem,
  ProductAttributePagedResult,
  ProductAttributeValue,
  CreateProductAttributeRequest,
  UpdateProductAttributeRequest,
  AddProductAttributeValueRequest,
  UpdateProductAttributeValueRequest,
  CategoryAttribute,
  AssignCategoryAttributeRequest,
  UpdateCategoryAttributeRequest,
  ProductAttributeAssignment,
  ProductAttributeFormSchema,
  CategoryAttributeFormSchema,
  SetProductAttributeValueRequest,
  BulkUpdateProductAttributesRequest,
} from '@/types/productAttribute'

// ============================================================================
// Product Attributes
// ============================================================================

export interface GetProductAttributesParams {
  search?: string
  isActive?: boolean
  isFilterable?: boolean
  isVariantAttribute?: boolean
  type?: string
  page?: number
  pageSize?: number
}

/**
 * Fetch paginated list of product attributes
 */
export const getProductAttributes = async (
  params: GetProductAttributesParams = {}
): Promise<ProductAttributePagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)
  if (params.isActive !== undefined) queryParams.append('isActive', String(params.isActive))
  if (params.isFilterable !== undefined) queryParams.append('isFilterable', String(params.isFilterable))
  if (params.isVariantAttribute !== undefined) queryParams.append('isVariantAttribute', String(params.isVariantAttribute))
  if (params.type) queryParams.append('type', params.type)
  if (params.page) queryParams.append('page', params.page.toString())
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<ProductAttributePagedResult>(`/product-attributes${query ? `?${query}` : ''}`)
}

/**
 * Fetch all active product attributes (for dropdowns)
 */
export const getActiveProductAttributes = async (): Promise<ProductAttributeListItem[]> => {
  const result = await getProductAttributes({ isActive: true, pageSize: 1000 })
  return result.items
}

/**
 * Fetch all filterable product attributes with their values (for admin product filters)
 */
export const getFilterableAttributesWithValues = async (): Promise<ProductAttribute[]> => {
  const result = await getProductAttributes({ isActive: true, isFilterable: true, pageSize: 1000 })
  // Fetch full details for each filterable attribute to get values
  const attributesWithValues = await Promise.all(
    result.items.map(item => getProductAttributeById(item.id))
  )
  // Filter to only Select/MultiSelect types (they have predefined values)
  return attributesWithValues.filter(
    attr => (attr.type === 'Select' || attr.type === 'MultiSelect') && attr.values.length > 0
  )
}

/**
 * Fetch a single product attribute by ID
 */
export const getProductAttributeById = async (id: string): Promise<ProductAttribute> => {
  return apiClient<ProductAttribute>(`/product-attributes/${id}`)
}

/**
 * Create a new product attribute
 */
export const createProductAttribute = async (
  request: CreateProductAttributeRequest
): Promise<ProductAttribute> => {
  return apiClient<ProductAttribute>('/product-attributes', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing product attribute
 */
export const updateProductAttribute = async (
  id: string,
  request: UpdateProductAttributeRequest
): Promise<ProductAttribute> => {
  return apiClient<ProductAttribute>(`/product-attributes/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product attribute (soft delete)
 */
export const deleteProductAttribute = async (id: string): Promise<void> => {
  return apiClient<void>(`/product-attributes/${id}`, {
    method: 'DELETE',
  })
}

// ============================================================================
// Attribute Values
// ============================================================================

/**
 * Add a value to a product attribute
 */
export const addProductAttributeValue = async (
  attributeId: string,
  request: AddProductAttributeValueRequest
): Promise<ProductAttributeValue> => {
  return apiClient<ProductAttributeValue>(`/product-attributes/${attributeId}/values`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an attribute value
 */
export const updateProductAttributeValue = async (
  attributeId: string,
  valueId: string,
  request: UpdateProductAttributeValueRequest
): Promise<ProductAttributeValue> => {
  return apiClient<ProductAttributeValue>(`/product-attributes/${attributeId}/values/${valueId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Remove an attribute value
 */
export const removeProductAttributeValue = async (
  attributeId: string,
  valueId: string
): Promise<void> => {
  return apiClient<void>(`/product-attributes/${attributeId}/values/${valueId}`, {
    method: 'DELETE',
  })
}

// ============================================================================
// Category Attributes
// ============================================================================

/**
 * Get all attributes assigned to a category
 */
export const getCategoryAttributes = async (categoryId: string): Promise<CategoryAttribute[]> => {
  return apiClient<CategoryAttribute[]>(`/products/categories/${categoryId}/attributes`)
}

/**
 * Get attribute form schema for a category (for new product creation).
 * Unlike getProductAttributeFormSchema, this does NOT require a productId.
 * Returns form fields with default values but no currentValue.
 */
export const getCategoryAttributeFormSchema = async (
  categoryId: string
): Promise<CategoryAttributeFormSchema> => {
  return apiClient<CategoryAttributeFormSchema>(`/products/categories/${categoryId}/attribute-form-schema`)
}

/**
 * Assign an attribute to a category
 */
export const assignCategoryAttribute = async (
  categoryId: string,
  request: AssignCategoryAttributeRequest
): Promise<CategoryAttribute> => {
  return apiClient<CategoryAttribute>(`/products/categories/${categoryId}/attributes`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update category attribute settings
 */
export const updateCategoryAttribute = async (
  categoryId: string,
  attributeId: string,
  request: UpdateCategoryAttributeRequest
): Promise<CategoryAttribute> => {
  return apiClient<CategoryAttribute>(`/products/categories/${categoryId}/attributes/${attributeId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Remove an attribute from a category
 */
export const removeCategoryAttribute = async (
  categoryId: string,
  attributeId: string
): Promise<void> => {
  return apiClient<void>(`/products/categories/${categoryId}/attributes/${attributeId}`, {
    method: 'DELETE',
  })
}

// ============================================================================
// Product Attribute Assignments (Product's actual values)
// ============================================================================

/**
 * Get attribute form schema for a product (for dynamic form rendering)
 */
export const getProductAttributeFormSchema = async (
  productId: string,
  variantId?: string
): Promise<ProductAttributeFormSchema> => {
  const params = variantId ? `?variantId=${variantId}` : ''
  return apiClient<ProductAttributeFormSchema>(`/products/${productId}/attributes/form-schema${params}`)
}

/**
 * Get a product's attribute values
 */
export const getProductAttributeAssignments = async (
  productId: string,
  variantId?: string
): Promise<ProductAttributeAssignment[]> => {
  const params = variantId ? `?variantId=${variantId}` : ''
  return apiClient<ProductAttributeAssignment[]>(`/products/${productId}/attributes${params}`)
}

/**
 * Set a single attribute value for a product
 */
export const setProductAttributeValue = async (
  productId: string,
  attributeId: string,
  request: SetProductAttributeValueRequest
): Promise<ProductAttributeAssignment> => {
  return apiClient<ProductAttributeAssignment>(`/products/${productId}/attributes/${attributeId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Bulk update multiple attribute values for a product
 */
export const bulkUpdateProductAttributes = async (
  productId: string,
  request: BulkUpdateProductAttributesRequest
): Promise<ProductAttributeAssignment[]> => {
  return apiClient<ProductAttributeAssignment[]>(`/products/${productId}/attributes`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}
