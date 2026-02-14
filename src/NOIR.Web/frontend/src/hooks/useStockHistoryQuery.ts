/**
 * Stock History Query Hook
 *
 * TanStack React Query replacement for useStockHistory.
 * Provides stock movement history data for a product variant.
 */
import { useQuery } from '@tanstack/react-query'
import { getStockHistory } from '@/services/products'

export const stockHistoryKeys = {
  all: ['stockHistory'] as const,
  list: (productId: string, variantId: string, page: number, pageSize: number) =>
    [...stockHistoryKeys.all, productId, variantId, page, pageSize] as const,
}

export const useStockHistoryQuery = (
  productId: string | undefined,
  variantId: string | undefined,
  page = 1,
  pageSize = 20
) =>
  useQuery({
    queryKey: stockHistoryKeys.list(productId!, variantId!, page, pageSize),
    queryFn: () => getStockHistory({ productId: productId!, variantId: variantId!, page, pageSize }),
    enabled: !!(productId && variantId),
  })
