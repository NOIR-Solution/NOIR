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
  description?: string | null
  descriptionHtml?: string | null
  basePrice: number
  currency: string
  status: ProductStatus
  categoryId?: string | null
  categoryName?: string | null
  categorySlug?: string | null
  brand?: string | null
  sku?: string | null
  barcode?: string | null
  weight?: number | null
  trackInventory: boolean
  metaTitle?: string | null
  metaDescription?: string | null
  sortOrder: number
  totalStock: number
  inStock: boolean
  variants: ProductVariant[]
  images: ProductImage[]
  createdAt: string
  modifiedAt?: string | null
}

/**
 * Simplified product for list views
 */
export interface ProductListItem {
  id: string
  name: string
  slug: string
  basePrice: number
  currency: string
  status: ProductStatus
  categoryName?: string | null
  brand?: string | null
  sku?: string | null
  totalStock: number
  inStock: boolean
  primaryImageUrl?: string | null
  discountPercentage?: number | null
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
  stockQuantity: number
  inStock: boolean
  lowStock: boolean
  onSale: boolean
  options?: Record<string, string> | null
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
  description?: string | null
  descriptionHtml?: string | null
  basePrice: number
  currency: string
  categoryId?: string | null
  brand?: string | null
  sku?: string | null
  barcode?: string | null
  weight?: number | null
  trackInventory: boolean
  metaTitle?: string | null
  metaDescription?: string | null
  sortOrder: number
  variants?: CreateProductVariantRequest[] | null
  images?: CreateProductImageRequest[] | null
}

export interface UpdateProductRequest {
  name: string
  slug: string
  description?: string | null
  descriptionHtml?: string | null
  basePrice: number
  currency: string
  categoryId?: string | null
  brand?: string | null
  sku?: string | null
  barcode?: string | null
  weight?: number | null
  trackInventory: boolean
  metaTitle?: string | null
  metaDescription?: string | null
  sortOrder: number
}

export interface CreateProductVariantRequest {
  name: string
  sku?: string | null
  price: number
  compareAtPrice?: number | null
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
  stockQuantity: number
  options?: Record<string, string> | null
  sortOrder: number
}

export interface UpdateProductVariantRequest {
  name: string
  price: number
  sku?: string | null
  compareAtPrice?: number | null
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
// Paged Result Types
// ============================================================================

export interface ProductPagedResult {
  items: ProductListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
