import { useQuery, keepPreviousData } from '@tanstack/react-query'
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
    placeholderData: keepPreviousData,
  })

export const useProductStatsQuery = () =>
  useQuery({
    queryKey: productKeys.stats(),
    queryFn: () => getProductStats(),
    staleTime: 30_000,
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
