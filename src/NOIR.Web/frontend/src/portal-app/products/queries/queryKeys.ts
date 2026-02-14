import type { GetProductsParams, GetProductCategoriesParams } from '@/services/products'
import type { GetProductAttributesParams } from '@/services/productAttributes'

export const productKeys = {
  all: ['products'] as const,
  lists: () => [...productKeys.all, 'list'] as const,
  list: (params: GetProductsParams) => [...productKeys.lists(), params] as const,
  stats: () => [...productKeys.all, 'stats'] as const,
  details: () => [...productKeys.all, 'detail'] as const,
  detail: (id: string) => [...productKeys.details(), id] as const,
}

export const productCategoryKeys = {
  all: ['productCategories'] as const,
  lists: () => [...productCategoryKeys.all, 'list'] as const,
  list: (params: GetProductCategoriesParams) => [...productCategoryKeys.lists(), params] as const,
  details: () => [...productCategoryKeys.all, 'detail'] as const,
  detail: (id: string) => [...productCategoryKeys.details(), id] as const,
}

export const productAttributeKeys = {
  all: ['productAttributes'] as const,
  lists: () => [...productAttributeKeys.all, 'list'] as const,
  list: (params: GetProductAttributesParams) => [...productAttributeKeys.lists(), params] as const,
  active: () => [...productAttributeKeys.all, 'active'] as const,
  filterable: () => [...productAttributeKeys.all, 'filterable'] as const,
  details: () => [...productAttributeKeys.all, 'detail'] as const,
  detail: (id: string) => [...productAttributeKeys.details(), id] as const,
  categoryAttributes: (categoryId: string) => [...productAttributeKeys.all, 'category', categoryId] as const,
  productFormSchema: (productId: string, variantId?: string) => [...productAttributeKeys.all, 'formSchema', productId, variantId] as const,
  categoryFormSchema: (categoryId: string) => [...productAttributeKeys.all, 'categoryFormSchema', categoryId] as const,
}
