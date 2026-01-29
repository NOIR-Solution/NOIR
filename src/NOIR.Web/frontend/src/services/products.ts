/**
 * Products API Service
 *
 * Provides methods for managing products and product categories.
 */
import { apiClient } from './apiClient'
import type {
  Product,
  ProductListItem,
  ProductPagedResult,
  ProductVariant,
  ProductImage,
  ProductCategory,
  ProductCategoryListItem,
  ProductOption,
  ProductOptionValue,
  ProductStatus,
  CreateProductRequest,
  UpdateProductRequest,
  AddProductVariantRequest,
  UpdateProductVariantRequest,
  AddProductImageRequest,
  UpdateProductImageRequest,
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
  AddProductOptionRequest,
  UpdateProductOptionRequest,
  AddProductOptionValueRequest,
  UpdateProductOptionValueRequest,
} from '@/types/product'

// ============================================================================
// Products
// ============================================================================

export interface GetProductsParams {
  search?: string
  status?: ProductStatus
  categoryId?: string
  brand?: string
  minPrice?: number
  maxPrice?: number
  inStockOnly?: boolean
  lowStockOnly?: boolean
  page?: number
  pageSize?: number
  /** Attribute filters: key is attribute code, value is array of display values to match */
  attributeFilters?: Record<string, string[]>
}

/**
 * Fetch paginated list of products
 */
export async function getProducts(params: GetProductsParams = {}): Promise<ProductPagedResult> {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)
  if (params.status) queryParams.append('status', params.status)
  if (params.categoryId) queryParams.append('categoryId', params.categoryId)
  if (params.brand) queryParams.append('brand', params.brand)
  if (params.minPrice !== undefined) queryParams.append('minPrice', params.minPrice.toString())
  if (params.maxPrice !== undefined) queryParams.append('maxPrice', params.maxPrice.toString())
  if (params.inStockOnly) queryParams.append('inStockOnly', 'true')
  if (params.lowStockOnly) queryParams.append('lowStockOnly', 'true')
  if (params.page) queryParams.append('page', params.page.toString())
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString())
  if (params.attributeFilters && Object.keys(params.attributeFilters).length > 0) {
    queryParams.append('attributeFilters', JSON.stringify(params.attributeFilters))
  }

  const query = queryParams.toString()
  return apiClient<ProductPagedResult>(`/products${query ? `?${query}` : ''}`)
}

/**
 * Product statistics for dashboard display
 */
export interface ProductStatsDto {
  total: number
  active: number
  draft: number
  archived: number
  outOfStock: number
  lowStock: number
}

/**
 * Fetch global product statistics
 * Returns counts by status independent of current filters
 */
export async function getProductStats(): Promise<ProductStatsDto> {
  return apiClient<ProductStatsDto>('/products/stats')
}

/**
 * Fetch a single product by ID
 */
export async function getProductById(id: string): Promise<Product> {
  return apiClient<Product>(`/products/${id}`)
}

/**
 * Fetch a single product by slug
 */
export async function getProductBySlug(slug: string): Promise<Product> {
  return apiClient<Product>(`/products/by-slug/${slug}`)
}

/**
 * Create a new product
 */
export async function createProduct(request: CreateProductRequest): Promise<Product> {
  return apiClient<Product>('/products', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing product
 */
export async function updateProduct(id: string, request: UpdateProductRequest): Promise<Product> {
  return apiClient<Product>(`/products/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product (soft delete)
 */
export async function deleteProduct(id: string): Promise<void> {
  return apiClient<void>(`/products/${id}`, {
    method: 'DELETE',
  })
}

/**
 * Publish a product
 */
export async function publishProduct(id: string): Promise<Product> {
  return apiClient<Product>(`/products/${id}/publish`, {
    method: 'POST',
  })
}

/**
 * Archive a product
 */
export async function archiveProduct(id: string): Promise<Product> {
  return apiClient<Product>(`/products/${id}/archive`, {
    method: 'POST',
  })
}

/**
 * Options for duplicating a product
 */
export interface DuplicateProductOptions {
  copyVariants?: boolean
  copyImages?: boolean
  copyOptions?: boolean
}

/**
 * Duplicate a product
 * Creates a copy of the product as a new draft on the server
 */
export async function duplicateProduct(
  id: string,
  options?: DuplicateProductOptions
): Promise<Product> {
  return apiClient<Product>(`/products/${id}/duplicate`, {
    method: 'POST',
    body: JSON.stringify(options || {}),
  })
}

// ============================================================================
// Bulk Operations
// ============================================================================

/**
 * Single product data for import
 */
export interface ImportProductDto {
  name: string
  slug?: string
  basePrice: number
  currency?: string
  shortDescription?: string
  sku?: string
  barcode?: string
  categoryName?: string
  brand?: string
  stock?: number
}

/**
 * Result of bulk import operation
 */
export interface BulkImportResult {
  success: number
  failed: number
  errors: { row: number; message: string }[]
}

/**
 * Bulk import products from parsed CSV data
 */
export async function bulkImportProducts(
  products: ImportProductDto[]
): Promise<BulkImportResult> {
  return apiClient<BulkImportResult>('/products/import', {
    method: 'POST',
    body: JSON.stringify({ products }),
  })
}

/**
 * Result of bulk operation (publish/archive/delete)
 */
export interface BulkOperationResult {
  success: number
  failed: number
  errors: { productId: string; message: string }[]
}

/**
 * Bulk publish products
 */
export async function bulkPublishProducts(productIds: string[]): Promise<BulkOperationResult> {
  return apiClient<BulkOperationResult>('/products/bulk-publish', {
    method: 'POST',
    body: JSON.stringify({ productIds }),
  })
}

/**
 * Bulk archive products
 */
export async function bulkArchiveProducts(productIds: string[]): Promise<BulkOperationResult> {
  return apiClient<BulkOperationResult>('/products/bulk-archive', {
    method: 'POST',
    body: JSON.stringify({ productIds }),
  })
}

/**
 * Bulk delete products
 */
export async function bulkDeleteProducts(productIds: string[]): Promise<BulkOperationResult> {
  return apiClient<BulkOperationResult>('/products/bulk-delete', {
    method: 'POST',
    body: JSON.stringify({ productIds }),
  })
}

// ============================================================================
// Variants
// ============================================================================

/**
 * Add a variant to a product
 */
export async function addProductVariant(
  productId: string,
  request: AddProductVariantRequest
): Promise<ProductVariant> {
  return apiClient<ProductVariant>(`/products/${productId}/variants`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update a product variant
 */
export async function updateProductVariant(
  productId: string,
  variantId: string,
  request: UpdateProductVariantRequest
): Promise<Product> {
  return apiClient<Product>(`/products/${productId}/variants/${variantId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product variant
 */
export async function deleteProductVariant(productId: string, variantId: string): Promise<void> {
  return apiClient<void>(`/products/${productId}/variants/${variantId}`, {
    method: 'DELETE',
  })
}

// ============================================================================
// Images
// ============================================================================

/**
 * Add an image to a product
 */
export async function addProductImage(
  productId: string,
  request: AddProductImageRequest
): Promise<ProductImage> {
  return apiClient<ProductImage>(`/products/${productId}/images`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update a product image
 */
export async function updateProductImage(
  productId: string,
  imageId: string,
  request: UpdateProductImageRequest
): Promise<Product> {
  return apiClient<Product>(`/products/${productId}/images/${imageId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product image
 */
export async function deleteProductImage(productId: string, imageId: string): Promise<void> {
  return apiClient<void>(`/products/${productId}/images/${imageId}`, {
    method: 'DELETE',
  })
}

/**
 * Set an image as primary
 */
export async function setPrimaryProductImage(productId: string, imageId: string): Promise<Product> {
  return apiClient<Product>(`/products/${productId}/images/${imageId}/set-primary`, {
    method: 'POST',
  })
}

/**
 * Upload result from the upload endpoint
 */
export interface ProductImageUploadResult {
  id: string
  url: string
  altText?: string | null
  sortOrder: number
  isPrimary: boolean
  thumbUrl?: string | null
  mediumUrl?: string | null
  largeUrl?: string | null
  width?: number | null
  height?: number | null
  thumbHash?: string | null
  dominantColor?: string | null
  message: string
}

/**
 * Upload an image to a product (with processing)
 */
export async function uploadProductImage(
  productId: string,
  file: File,
  altText?: string,
  isPrimary: boolean = false
): Promise<ProductImageUploadResult> {
  const formData = new FormData()
  formData.append('file', file)

  const queryParams = new URLSearchParams()
  if (altText) queryParams.append('altText', altText)
  queryParams.append('isPrimary', String(isPrimary))

  const query = queryParams.toString()
  const url = `/products/${productId}/images/upload${query ? `?${query}` : ''}`

  // Use fetch directly for FormData upload (apiClient uses JSON)
  const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:4000/api'
  const response = await fetch(`${baseUrl}${url}`, {
    method: 'POST',
    body: formData,
    credentials: 'include',
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Upload failed' }))
    throw new Error(error.message || 'Upload failed')
  }

  return response.json()
}

/**
 * Request to reorder images
 */
export interface ReorderImagesRequest {
  items: { imageId: string; sortOrder: number }[]
}

/**
 * Reorder product images in bulk
 */
export async function reorderProductImages(
  productId: string,
  items: { imageId: string; sortOrder: number }[]
): Promise<Product> {
  return apiClient<Product>(`/products/${productId}/images/reorder`, {
    method: 'PUT',
    body: JSON.stringify({ items }),
  })
}

// ============================================================================
// Categories
// ============================================================================

export interface GetProductCategoriesParams {
  search?: string
  topLevelOnly?: boolean
  includeChildren?: boolean
}

/**
 * Fetch list of product categories
 */
export async function getProductCategories(
  params: GetProductCategoriesParams = {}
): Promise<ProductCategoryListItem[]> {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)
  if (params.topLevelOnly) queryParams.append('topLevelOnly', 'true')
  if (params.includeChildren) queryParams.append('includeChildren', 'true')

  const query = queryParams.toString()
  return apiClient<ProductCategoryListItem[]>(`/products/categories${query ? `?${query}` : ''}`)
}

/**
 * Fetch a product category by ID
 */
export async function getProductCategoryById(id: string): Promise<ProductCategory> {
  return apiClient<ProductCategory>(`/products/categories/${id}`)
}

/**
 * Create a new product category
 */
export async function createProductCategory(
  request: CreateProductCategoryRequest
): Promise<ProductCategory> {
  return apiClient<ProductCategory>('/products/categories', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing product category
 */
export async function updateProductCategory(
  id: string,
  request: UpdateProductCategoryRequest
): Promise<ProductCategory> {
  return apiClient<ProductCategory>(`/products/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product category
 */
export async function deleteProductCategory(id: string): Promise<void> {
  return apiClient<void>(`/products/categories/${id}`, {
    method: 'DELETE',
  })
}

// ============================================================================
// Options
// ============================================================================

/**
 * Add an option to a product
 */
export async function addProductOption(
  productId: string,
  request: AddProductOptionRequest
): Promise<ProductOption> {
  return apiClient<ProductOption>(`/products/${productId}/options`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update a product option
 */
export async function updateProductOption(
  productId: string,
  optionId: string,
  request: UpdateProductOptionRequest
): Promise<ProductOption> {
  return apiClient<ProductOption>(`/products/${productId}/options/${optionId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product option
 */
export async function deleteProductOption(productId: string, optionId: string): Promise<void> {
  return apiClient<void>(`/products/${productId}/options/${optionId}`, {
    method: 'DELETE',
  })
}

// ============================================================================
// Option Values
// ============================================================================

/**
 * Add a value to a product option
 */
export async function addProductOptionValue(
  productId: string,
  optionId: string,
  request: AddProductOptionValueRequest
): Promise<ProductOptionValue> {
  return apiClient<ProductOptionValue>(`/products/${productId}/options/${optionId}/values`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update a product option value
 */
export async function updateProductOptionValue(
  productId: string,
  optionId: string,
  valueId: string,
  request: UpdateProductOptionValueRequest
): Promise<ProductOptionValue> {
  return apiClient<ProductOptionValue>(
    `/products/${productId}/options/${optionId}/values/${valueId}`,
    {
      method: 'PUT',
      body: JSON.stringify(request),
    }
  )
}

/**
 * Delete a product option value
 */
export async function deleteProductOptionValue(
  productId: string,
  optionId: string,
  valueId: string
): Promise<void> {
  return apiClient<void>(`/products/${productId}/options/${optionId}/values/${valueId}`, {
    method: 'DELETE',
  })
}
