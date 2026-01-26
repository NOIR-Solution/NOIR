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
  ProductStatus,
  CreateProductRequest,
  UpdateProductRequest,
  AddProductVariantRequest,
  UpdateProductVariantRequest,
  AddProductImageRequest,
  UpdateProductImageRequest,
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
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
  page?: number
  pageSize?: number
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
  if (params.page) queryParams.append('page', params.page.toString())
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<ProductPagedResult>(`/products${query ? `?${query}` : ''}`)
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
