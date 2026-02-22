/**
 * Products API Service
 *
 * Provides methods for managing products and product categories.
 */
import { apiClient } from './apiClient'
import { i18n } from '@/i18n'
import type {
  Product,
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
export const getProducts = async (params: GetProductsParams = {}): Promise<ProductPagedResult> => {
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
export const getProductStats = async (): Promise<ProductStatsDto> => {
  return apiClient<ProductStatsDto>('/products/stats')
}

/**
 * Fetch a single product by ID
 */
export const getProductById = async (id: string): Promise<Product> => {
  return apiClient<Product>(`/products/${id}`)
}

/**
 * Fetch a single product by slug
 */
export const getProductBySlug = async (slug: string): Promise<Product> => {
  return apiClient<Product>(`/products/by-slug/${slug}`)
}

/**
 * Create a new product
 */
export const createProduct = async (request: CreateProductRequest): Promise<Product> => {
  return apiClient<Product>('/products', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing product
 */
export const updateProduct = async (id: string, request: UpdateProductRequest): Promise<Product> => {
  return apiClient<Product>(`/products/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product (soft delete)
 */
export const deleteProduct = async (id: string): Promise<void> => {
  return apiClient<void>(`/products/${id}`, {
    method: 'DELETE',
  })
}

/**
 * Publish a product
 */
export const publishProduct = async (id: string): Promise<Product> => {
  return apiClient<Product>(`/products/${id}/publish`, {
    method: 'POST',
  })
}

/**
 * Archive a product
 */
export const archiveProduct = async (id: string): Promise<Product> => {
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
export const duplicateProduct = async (
  id: string,
  options?: DuplicateProductOptions
): Promise<Product> => {
  return apiClient<Product>(`/products/${id}/duplicate`, {
    method: 'POST',
    body: JSON.stringify(options || {}),
  })
}

// ============================================================================
// Bulk Operations
// ============================================================================

/**
 * Single product/variant row data for import.
 * Flat format where multiple rows with same slug create variants.
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
  // Enhanced fields for variants
  variantName?: string
  variantPrice?: number
  compareAtPrice?: number
  images?: string  // Pipe-separated URLs: "url1|url2|url3"
  attributes?: Record<string, string>  // attr_code -> value
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
export const bulkImportProducts = async (
  products: ImportProductDto[]
): Promise<BulkImportResult> => {
  return apiClient<BulkImportResult>('/products/import', {
    method: 'POST',
    body: JSON.stringify({ products }),
  })
}

/**
 * Export product row (flat format for CSV)
 */
export interface ExportProductRowDto {
  name: string
  slug: string
  sku?: string | null
  barcode?: string | null
  basePrice: number
  currency: string
  status: string
  categoryName?: string | null
  brand?: string | null
  shortDescription?: string | null
  variantName?: string | null
  variantPrice?: number | null
  compareAtPrice?: number | null
  stock: number
  images?: string | null
  attributes: Record<string, string>
}

/**
 * Result of export operation
 */
export interface ExportProductsResult {
  rows: ExportProductRowDto[]
  attributeColumns: string[]
}

/**
 * Export products as flat rows
 */
export const exportProducts = async (params?: {
  categoryId?: string
  status?: string
  includeAttributes?: boolean
  includeImages?: boolean
}): Promise<ExportProductsResult> => {
  const queryParams = new URLSearchParams()
  if (params?.categoryId) queryParams.append('categoryId', params.categoryId)
  if (params?.status) queryParams.append('status', params.status)
  queryParams.append('includeAttributes', String(params?.includeAttributes ?? true))
  queryParams.append('includeImages', String(params?.includeImages ?? true))

  return apiClient<ExportProductsResult>(`/products/export?${queryParams.toString()}`)
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
export const bulkPublishProducts = async (productIds: string[]): Promise<BulkOperationResult> => {
  return apiClient<BulkOperationResult>('/products/bulk-publish', {
    method: 'POST',
    body: JSON.stringify({ productIds }),
  })
}

/**
 * Bulk archive products
 */
export const bulkArchiveProducts = async (productIds: string[]): Promise<BulkOperationResult> => {
  return apiClient<BulkOperationResult>('/products/bulk-archive', {
    method: 'POST',
    body: JSON.stringify({ productIds }),
  })
}

/**
 * Bulk delete products
 */
export const bulkDeleteProducts = async (productIds: string[]): Promise<BulkOperationResult> => {
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
export const addProductVariant = async (
  productId: string,
  request: AddProductVariantRequest
): Promise<ProductVariant> => {
  return apiClient<ProductVariant>(`/products/${productId}/variants`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update a product variant
 */
export const updateProductVariant = async (
  productId: string,
  variantId: string,
  request: UpdateProductVariantRequest
): Promise<Product> => {
  return apiClient<Product>(`/products/${productId}/variants/${variantId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product variant
 */
export const deleteProductVariant = async (productId: string, variantId: string): Promise<void> => {
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
export const addProductImage = async (
  productId: string,
  request: AddProductImageRequest
): Promise<ProductImage> => {
  return apiClient<ProductImage>(`/products/${productId}/images`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update a product image
 */
export const updateProductImage = async (
  productId: string,
  imageId: string,
  request: UpdateProductImageRequest
): Promise<Product> => {
  return apiClient<Product>(`/products/${productId}/images/${imageId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product image
 */
export const deleteProductImage = async (productId: string, imageId: string): Promise<void> => {
  return apiClient<void>(`/products/${productId}/images/${imageId}`, {
    method: 'DELETE',
  })
}

/**
 * Set an image as primary
 */
export const setPrimaryProductImage = async (productId: string, imageId: string): Promise<Product> => {
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
export const uploadProductImage = async (
  productId: string,
  file: File,
  altText?: string,
  isPrimary: boolean = false
): Promise<ProductImageUploadResult> => {
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
    const error = await response.json().catch(() => ({ message: i18n.t('errors.uploadFailed', { ns: 'common' }) }))
    throw new Error(error.message || i18n.t('errors.uploadFailed', { ns: 'common' }))
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
export const reorderProductImages = async (
  productId: string,
  items: { imageId: string; sortOrder: number }[]
): Promise<Product> => {
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
export const getProductCategories = async (
  params: GetProductCategoriesParams = {}
): Promise<ProductCategoryListItem[]> => {
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
export const getProductCategoryById = async (id: string): Promise<ProductCategory> => {
  return apiClient<ProductCategory>(`/products/categories/${id}`)
}

/**
 * Create a new product category
 */
export const createProductCategory = async (
  request: CreateProductCategoryRequest
): Promise<ProductCategory> => {
  return apiClient<ProductCategory>('/products/categories', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing product category
 */
export const updateProductCategory = async (
  id: string,
  request: UpdateProductCategoryRequest
): Promise<ProductCategory> => {
  return apiClient<ProductCategory>(`/products/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product category
 */
export const deleteProductCategory = async (id: string): Promise<void> => {
  return apiClient<void>(`/products/categories/${id}`, {
    method: 'DELETE',
  })
}

/**
 * Request to reorder product categories
 */
export interface ReorderProductCategoriesRequest {
  items: { categoryId: string; parentId: string | null; sortOrder: number }[]
}

/**
 * Reorder product categories in bulk (sort order + reparenting)
 */
export const reorderProductCategories = async (
  request: ReorderProductCategoriesRequest
): Promise<ProductCategoryListItem[]> => {
  return apiClient<ProductCategoryListItem[]>('/products/categories/reorder', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

// ============================================================================
// Options
// ============================================================================

/**
 * Add an option to a product
 */
export const addProductOption = async (
  productId: string,
  request: AddProductOptionRequest
): Promise<ProductOption> => {
  return apiClient<ProductOption>(`/products/${productId}/options`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update a product option
 */
export const updateProductOption = async (
  productId: string,
  optionId: string,
  request: UpdateProductOptionRequest
): Promise<ProductOption> => {
  return apiClient<ProductOption>(`/products/${productId}/options/${optionId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a product option
 */
export const deleteProductOption = async (productId: string, optionId: string): Promise<void> => {
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
export const addProductOptionValue = async (
  productId: string,
  optionId: string,
  request: AddProductOptionValueRequest
): Promise<ProductOptionValue> => {
  return apiClient<ProductOptionValue>(`/products/${productId}/options/${optionId}/values`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update a product option value
 */
export const updateProductOptionValue = async (
  productId: string,
  optionId: string,
  valueId: string,
  request: UpdateProductOptionValueRequest
): Promise<ProductOptionValue> => {
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
export const deleteProductOptionValue = async (
  productId: string,
  optionId: string,
  valueId: string
): Promise<void> => {
  return apiClient<void>(`/products/${productId}/options/${optionId}/values/${valueId}`, {
    method: 'DELETE',
  })
}

// ============================================================================
// Stock History
// ============================================================================

import type { StockHistoryPagedResult, GetStockHistoryParams } from '@/types/inventory'

/**
 * Get stock movement history for a product variant
 */
export const getStockHistory = async (
  params: GetStockHistoryParams
): Promise<StockHistoryPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page) queryParams.append('page', params.page.toString())
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<StockHistoryPagedResult>(
    `/products/${params.productId}/variants/${params.variantId}/stock-history${query ? `?${query}` : ''}`
  )
}
