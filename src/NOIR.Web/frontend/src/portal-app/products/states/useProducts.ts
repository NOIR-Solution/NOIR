import { useState, useEffect, useCallback } from 'react'
import type {
  Product,
  ProductPagedResult,
  ProductStatus,
} from '@/types/product'
import {
  getProducts,
  getProductById,
  getProductStats,
  deleteProduct,
  publishProduct,
  archiveProduct,
  duplicateProduct,
  bulkPublishProducts,
  bulkArchiveProducts,
  bulkDeleteProducts,
  type GetProductsParams,
  type BulkOperationResult,
} from '@/services/products'
import { ApiError } from '@/services/apiClient'
import { DEFAULT_PRODUCT_PAGE_SIZE } from '@/lib/constants/product'

// ============================================================================
// Products Hook
// ============================================================================

interface ProductStats {
  total: number
  active: number
  draft: number
  archived: number
  outOfStock: number
  lowStock: number
}

interface UseProductsState {
  data: ProductPagedResult | null
  stats: ProductStats
  loading: boolean
  error: string | null
}

interface UseProductsReturn extends UseProductsState {
  refresh: () => Promise<void>
  setPage: (page: number) => void
  setPageSize: (size: number) => void
  setSearch: (search: string) => void
  setStatus: (status: ProductStatus | undefined) => void
  setCategoryId: (categoryId: string | undefined) => void
  setBrand: (brand: string | undefined) => void
  setInStockOnly: (inStockOnly: boolean | undefined) => void
  setLowStockOnly: (lowStockOnly: boolean | undefined) => void
  setAttributeFilters: (filters: Record<string, string[]> | undefined) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  handlePublish: (id: string) => Promise<{ success: boolean; error?: string }>
  handleArchive: (id: string) => Promise<{ success: boolean; error?: string }>
  handleDuplicate: (id: string) => Promise<{ success: boolean; newId?: string; error?: string }>
  handleBulkPublish: (ids: string[]) => Promise<{ success: boolean; result?: BulkOperationResult; error?: string }>
  handleBulkArchive: (ids: string[]) => Promise<{ success: boolean; result?: BulkOperationResult; error?: string }>
  handleBulkDelete: (ids: string[]) => Promise<{ success: boolean; result?: BulkOperationResult; error?: string }>
  params: GetProductsParams
}

export const useProducts = (initialParams: GetProductsParams = {}): UseProductsReturn => {
  const [state, setState] = useState<UseProductsState>({
    data: null,
    stats: { total: 0, active: 0, draft: 0, archived: 0, outOfStock: 0, lowStock: 0 },
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<GetProductsParams>({
    page: 1,
    pageSize: DEFAULT_PRODUCT_PAGE_SIZE,
    ...initialParams,
  })

  const fetchProducts = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      // Fetch products and stats in parallel for optimal performance
      const [data, statsData] = await Promise.all([
        getProducts(params),
        getProductStats()
      ])

      // Use backend stats for accurate global counts
      const stats: ProductStats = {
        total: statsData.total,
        active: statsData.active,
        draft: statsData.draft,
        archived: statsData.archived,
        outOfStock: statsData.outOfStock,
        lowStock: statsData.lowStock,
      }

      setState({ data, stats, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load products'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchProducts()
  }, [fetchProducts])

  const setPage = useCallback((page: number) => {
    setParams((prev) => ({ ...prev, page }))
  }, [])

  const setPageSize = useCallback((size: number) => {
    setParams((prev) => ({ ...prev, pageSize: size, page: 1 }))
  }, [])

  const setSearch = useCallback((search: string) => {
    setParams((prev) => ({ ...prev, search: search || undefined, page: 1 }))
  }, [])

  const setStatus = useCallback((status: ProductStatus | undefined) => {
    setParams((prev) => ({ ...prev, status, page: 1 }))
  }, [])

  const setCategoryId = useCallback((categoryId: string | undefined) => {
    setParams((prev) => ({ ...prev, categoryId, page: 1 }))
  }, [])

  const setBrand = useCallback((brand: string | undefined) => {
    setParams((prev) => ({ ...prev, brand, page: 1 }))
  }, [])

  const setInStockOnly = useCallback((inStockOnly: boolean | undefined) => {
    setParams((prev) => ({ ...prev, inStockOnly, page: 1 }))
  }, [])

  const setLowStockOnly = useCallback((lowStockOnly: boolean | undefined) => {
    setParams((prev) => ({ ...prev, lowStockOnly, page: 1 }))
  }, [])

  const setAttributeFilters = useCallback((attributeFilters: Record<string, string[]> | undefined) => {
    setParams((prev) => ({ ...prev, attributeFilters, page: 1 }))
  }, [])

  const handleDelete = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      try {
        await deleteProduct(id)
        await fetchProducts()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to delete product'
        return { success: false, error: message }
      }
    },
    [fetchProducts]
  )

  const handlePublish = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      try {
        await publishProduct(id)
        await fetchProducts()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to publish product'
        return { success: false, error: message }
      }
    },
    [fetchProducts]
  )

  const handleArchive = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      try {
        await archiveProduct(id)
        await fetchProducts()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to archive product'
        return { success: false, error: message }
      }
    },
    [fetchProducts]
  )

  const handleDuplicate = useCallback(
    async (id: string): Promise<{ success: boolean; newId?: string; error?: string }> => {
      try {
        const newProduct = await duplicateProduct(id)
        await fetchProducts()
        return { success: true, newId: newProduct.id }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to duplicate product'
        return { success: false, error: message }
      }
    },
    [fetchProducts]
  )

  const handleBulkPublish = useCallback(
    async (ids: string[]): Promise<{ success: boolean; result?: BulkOperationResult; error?: string }> => {
      try {
        const result = await bulkPublishProducts(ids)
        await fetchProducts()
        return { success: true, result }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to bulk publish products'
        return { success: false, error: message }
      }
    },
    [fetchProducts]
  )

  const handleBulkArchive = useCallback(
    async (ids: string[]): Promise<{ success: boolean; result?: BulkOperationResult; error?: string }> => {
      try {
        const result = await bulkArchiveProducts(ids)
        await fetchProducts()
        return { success: true, result }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to bulk archive products'
        return { success: false, error: message }
      }
    },
    [fetchProducts]
  )

  const handleBulkDelete = useCallback(
    async (ids: string[]): Promise<{ success: boolean; result?: BulkOperationResult; error?: string }> => {
      try {
        const result = await bulkDeleteProducts(ids)
        await fetchProducts()
        return { success: true, result }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to bulk delete products'
        return { success: false, error: message }
      }
    },
    [fetchProducts]
  )

  return {
    ...state,
    refresh: fetchProducts,
    setPage,
    setPageSize,
    setSearch,
    setStatus,
    setCategoryId,
    setBrand,
    setInStockOnly,
    setLowStockOnly,
    setAttributeFilters,
    handleDelete,
    handlePublish,
    handleArchive,
    handleDuplicate,
    handleBulkPublish,
    handleBulkArchive,
    handleBulkDelete,
    params,
  }
}

// ============================================================================
// Single Product Hook
// ============================================================================

interface UseProductState {
  data: Product | null
  loading: boolean
  error: string | null
}

interface UseProductReturn extends UseProductState {
  refresh: () => Promise<void>
}

export const useProduct = (id: string | undefined): UseProductReturn => {
  const [state, setState] = useState<UseProductState>({
    data: null,
    loading: !!id,
    error: null,
  })

  const fetchProduct = useCallback(async () => {
    if (!id) {
      setState({ data: null, loading: false, error: null })
      return
    }

    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getProductById(id)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load product'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [id])

  useEffect(() => {
    fetchProduct()
  }, [fetchProduct])

  return {
    ...state,
    refresh: fetchProduct,
  }
}

