import { useQuery } from '@tanstack/react-query'
import {
  getProductAttributes,
  getActiveProductAttributes,
  getFilterableAttributesWithValues,
  getProductAttributeById,
  getCategoryAttributes,
  getProductAttributeFormSchema,
  getCategoryAttributeFormSchema,
  type GetProductAttributesParams,
} from '@/services/productAttributes'
import { productAttributeKeys } from './queryKeys'

export const useProductAttributesQuery = (params: GetProductAttributesParams = {}) =>
  useQuery({
    queryKey: productAttributeKeys.list(params),
    queryFn: () => getProductAttributes(params),
  })

export const useActiveProductAttributesQuery = () =>
  useQuery({
    queryKey: productAttributeKeys.active(),
    queryFn: () => getActiveProductAttributes(),
  })

export const useFilterableProductAttributesQuery = () =>
  useQuery({
    queryKey: productAttributeKeys.filterable(),
    queryFn: () => getFilterableAttributesWithValues(),
  })

export const useProductAttributeQuery = (id: string | undefined) =>
  useQuery({
    queryKey: productAttributeKeys.detail(id!),
    queryFn: () => getProductAttributeById(id!),
    enabled: !!id,
  })

export const useCategoryAttributesQuery = (categoryId: string | undefined) =>
  useQuery({
    queryKey: productAttributeKeys.categoryAttributes(categoryId!),
    queryFn: () => getCategoryAttributes(categoryId!),
    enabled: !!categoryId,
  })

export const useProductAttributeFormQuery = (productId: string | undefined, variantId?: string) =>
  useQuery({
    queryKey: productAttributeKeys.productFormSchema(productId!, variantId),
    queryFn: () => getProductAttributeFormSchema(productId!, variantId),
    enabled: !!productId,
  })

export const useCategoryAttributeFormQuery = (categoryId: string | null | undefined) =>
  useQuery({
    queryKey: productAttributeKeys.categoryFormSchema(categoryId!),
    queryFn: () => getCategoryAttributeFormSchema(categoryId!),
    enabled: !!categoryId,
  })
