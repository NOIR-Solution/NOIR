import { useQuery } from '@tanstack/react-query'
import {
  getProducts,
  getProductStats,
  getProductById,
  getProductCategories,
  type GetProductsParams,
  type GetProductCategoriesParams,
} from '@/services/products'
import { productKeys, productCategoryKeys } from './queryKeys'

export const useProductsQuery = (params: GetProductsParams) =>
  useQuery({
    queryKey: productKeys.list(params),
    queryFn: () => getProducts(params),
  })

export const useProductStatsQuery = () =>
  useQuery({
    queryKey: productKeys.stats(),
    queryFn: () => getProductStats(),
  })

export const useProductQuery = (id: string | undefined) =>
  useQuery({
    queryKey: productKeys.detail(id!),
    queryFn: () => getProductById(id!),
    enabled: !!id,
  })

export const useProductCategoriesQuery = (params: GetProductCategoriesParams = {}) =>
  useQuery({
    queryKey: productCategoryKeys.list(params),
    queryFn: () => getProductCategories(params),
  })
