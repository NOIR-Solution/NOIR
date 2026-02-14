/**
 * Stock History Hook
 *
 * Provides stock movement history data for a product variant.
 */
import { useState, useEffect, useCallback } from 'react'
import type { StockHistoryPagedResult } from '@/types/inventory'
import { getStockHistory } from '@/services/products'
import { ApiError } from '@/services/apiClient'

interface UseStockHistoryState {
  data: StockHistoryPagedResult | null
  loading: boolean
  error: string | null
}

interface UseStockHistoryReturn extends UseStockHistoryState {
  refresh: () => Promise<void>
  setPage: (page: number) => void
}

/**
 * Hook to fetch and manage stock history for a product variant.
 *
 * @param productId - The product ID
 * @param variantId - The variant ID (optional - if not provided, no data is fetched)
 * @param initialPage - Initial page number (default: 1)
 * @param pageSize - Number of items per page (default: 20)
 */
export const useStockHistory = (
  productId: string | undefined,
  variantId: string | undefined,
  initialPage = 1,
  pageSize = 20
): UseStockHistoryReturn => {
  const [state, setState] = useState<UseStockHistoryState>({
    data: null,
    loading: !!(productId && variantId),
    error: null,
  })
  const [page, setPage] = useState(initialPage)

  const fetchHistory = useCallback(async () => {
    if (!productId || !variantId) {
      setState({ data: null, loading: false, error: null })
      return
    }

    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getStockHistory({ productId, variantId, page, pageSize })
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load stock history'
      setState({ data: null, loading: false, error: message }) // Clear data on error
    }
  }, [productId, variantId, page, pageSize])

  useEffect(() => {
    fetchHistory()
  }, [fetchHistory])

  return {
    ...state,
    refresh: fetchHistory,
    setPage,
  }
}
