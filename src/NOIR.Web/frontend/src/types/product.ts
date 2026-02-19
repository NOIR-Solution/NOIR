/**
 * Product-related TypeScript types
 * Mirrors backend DTOs from NOIR.Application.Features.Products.DTOs
 */

// ============================================================================
// Enums
// ============================================================================

export type ProductStatus = 'Draft' | 'Active' | 'Archived' | 'OutOfStock'

// ============================================================================
// Product Types
// ============================================================================

/**
 * Full product details for editing
 */
export interface Product {
  id: string
  name: string
  slug: string
  shortDescription?: string | null
  description?: string | null
  descriptionHtml?: string | null
  basePrice: number
  currency: string
  status: ProductStatus
  categoryId?: string | null
  categoryName?: string | null
  categorySlug?: string | null
  brandId?: string | null
  brandName?: string | null
  brand?: string | null // Legacy text field, prefer brandId/brandName
  sku?: string | null
  barcode?: string | null
  trackInventory: boolean
  metaTitle?: string | null
  metaDescription?: string | null
  sortOrder: number
  weight?: number | null
  weightUnit?: string | null
  length?: number | null
  width?: number | null
  height?: number | null
  dimensionUnit?: string | null
  totalStock: number
  inStock: boolean
  variants: ProductVariant[]
  images: ProductImage[]
  options: ProductOption[]
  attributes?: ProductAttributeAssignment[] | null
  createdAt: string
  modifiedAt?: string | null
}

/**
 * Product attribute assignment (full details)
 * Re-exported from productAttribute.ts for convenience
 */
export interface ProductAttributeAssignment {
  id: string
  productId: string
  attributeId: string
  attributeCode: string
  attributeName: string
  attributeType: string
  variantId?: string | null
  value?: unknown
  displayValue?: string | null
  isRequired: boolean
}

/**
 * Simplified product for list views
 */
export interface ProductListItem {
  id: string
  name: string
  slug: string
  shortDescription?: string | null
  basePrice: number
  currency: string
  status: ProductStatus
  categoryName?: string | null
  brandId?: string | null
  brandName?: string | null
  brand?: string | null // Legacy text field, prefer brandId/brandName
  sku?: string | null
  totalStock: number
  inStock: boolean
  primaryImageUrl?: string | null
  displayAttributes?: ProductAttributeDisplay[] | null
  createdAt: string
}

/**
 * Product variant details
 */
export interface ProductVariant {
  id: string
  name: string
  sku?: string | null
  price: number
  compareAtPrice?: number | null
  costPrice?: number | null
  stockQuantity: number
  inStock: boolean
  lowStock: boolean
  onSale: boolean
  options?: Record<string, string> | null
  sortOrder: number
  imageId?: string | null
}

/**
 * Product option type (e.g., "Color", "Size")
 */
export interface ProductOption {
  id: string
  name: string
  displayName?: string | null
  sortOrder: number
  values: ProductOptionValue[]
}

/**
 * Product option value (e.g., "Red", "Large")
 */
export interface ProductOptionValue {
  id: string
  value: string
  displayValue?: string | null
  colorCode?: string | null
  swatchUrl?: string | null
  sortOrder: number
}

/**
 * Product image details
 */
export interface ProductImage {
  id: string
  url: string
  altText?: string | null
  sortOrder: number
  isPrimary: boolean
}

// ============================================================================
// Category Types
// ============================================================================

/**
 * Full product category with hierarchy support
 */
export interface ProductCategory {
  id: string
  name: string
  slug: string
  description?: string | null
  metaTitle?: string | null
  metaDescription?: string | null
  imageUrl?: string | null
  sortOrder: number
  productCount: number
  parentId?: string | null
  parentName?: string | null
  children?: ProductCategory[] | null
  createdAt: string
  modifiedAt?: string | null
}

/**
 * Simplified category for list views and dropdowns
 */
export interface ProductCategoryListItem {
  id: string
  name: string
  slug: string
  description?: string | null
  sortOrder: number
  productCount: number
  parentId?: string | null
  parentName?: string | null
  childCount: number
}

// ============================================================================
// Request Types
// ============================================================================

export interface CreateProductRequest {
  name: string
  slug: string
  shortDescription?: string | null
  description?: string | null
  descriptionHtml?: string | null
  basePrice: number
  currency: string
  categoryId?: string | null
  brandId?: string | null
  brand?: string | null // Legacy, prefer brandId
  sku?: string | null
  barcode?: string | null
  trackInventory: boolean
  metaTitle?: string | null
  metaDescription?: string | null
  sortOrder: number
  weight?: number | null
  weightUnit?: string | null
  length?: number | null
  width?: number | null
  height?: number | null
  dimensionUnit?: string | null
  variants?: CreateProductVariantRequest[] | null
  images?: CreateProductImageRequest[] | null
}

export interface UpdateProductRequest {
  name: string
  slug: string
  shortDescription?: string | null
  description?: string | null
  descriptionHtml?: string | null
  basePrice: number
  currency: string
  categoryId?: string | null
  brandId?: string | null
  brand?: string | null // Legacy, prefer brandId
  sku?: string | null
  barcode?: string | null
  trackInventory: boolean
  metaTitle?: string | null
  metaDescription?: string | null
  sortOrder: number
  weight?: number | null
  weightUnit?: string | null
  length?: number | null
  width?: number | null
  height?: number | null
  dimensionUnit?: string | null
}

export interface CreateProductVariantRequest {
  name: string
  sku?: string | null
  price: number
  compareAtPrice?: number | null
  costPrice?: number | null
  stockQuantity: number
  options?: Record<string, string> | null
  sortOrder: number
}

export interface CreateProductImageRequest {
  url: string
  altText?: string | null
  sortOrder: number
  isPrimary: boolean
}

export interface AddProductVariantRequest {
  name: string
  price: number
  sku?: string | null
  compareAtPrice?: number | null
  costPrice?: number | null
  stockQuantity: number
  options?: Record<string, string> | null
  sortOrder: number
}

export interface UpdateProductVariantRequest {
  name: string
  price: number
  sku?: string | null
  compareAtPrice?: number | null
  costPrice?: number | null
  stockQuantity: number
  options?: Record<string, string> | null
  sortOrder: number
}

export interface AddProductImageRequest {
  url: string
  altText?: string | null
  sortOrder: number
  isPrimary: boolean
}

export interface UpdateProductImageRequest {
  url: string
  altText?: string | null
  sortOrder: number
}

export interface CreateProductCategoryRequest {
  name: string
  slug: string
  description?: string | null
  metaTitle?: string | null
  metaDescription?: string | null
  imageUrl?: string | null
  sortOrder: number
  parentId?: string | null
}

export interface UpdateProductCategoryRequest {
  name: string
  slug: string
  description?: string | null
  metaTitle?: string | null
  metaDescription?: string | null
  imageUrl?: string | null
  sortOrder: number
  parentId?: string | null
}

// ============================================================================
// Option Request Types
// ============================================================================

export interface AddProductOptionRequest {
  name: string
  displayName?: string | null
  sortOrder: number
  values?: AddProductOptionValueRequest[] | null
}

export interface UpdateProductOptionRequest {
  name: string
  displayName?: string | null
  sortOrder: number
}

export interface AddProductOptionValueRequest {
  value: string
  displayValue?: string | null
  colorCode?: string | null
  swatchUrl?: string | null
  sortOrder: number
}

export interface UpdateProductOptionValueRequest {
  value: string
  displayValue?: string | null
  colorCode?: string | null
  swatchUrl?: string | null
  sortOrder: number
}

// ============================================================================
// Paged Result Types
// ============================================================================

export interface ProductPagedResult {
  items: ProductListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// ============================================================================
// Display Types (for UI rendering)
// ============================================================================

/**
 * Attribute display info for product cards
 * Used by AttributeBadges component to render attribute badges
 * Mirrors backend ProductAttributeDisplayDto
 */
export interface ProductAttributeDisplay {
  code: string
  name: string
  type: string
  displayValue?: string | null
  colorCode?: string | null
}
