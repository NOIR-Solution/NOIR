import { useState, useEffect, useCallback } from 'react'
import type {
  Product,
  ProductListItem,
  ProductPagedResult,
  ProductCategoryListItem,
  ProductStatus,
} from '@/types/product'
import {
  getProducts,
  getProductById,
  deleteProduct,
  publishProduct,
  archiveProduct,
  getProductCategories,
  deleteProductCategory,
  type GetProductsParams,
  type GetProductCategoriesParams,
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
  outOfStock: number
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
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  handlePublish: (id: string) => Promise<{ success: boolean; error?: string }>
  handleArchive: (id: string) => Promise<{ success: boolean; error?: string }>
  params: GetProductsParams
}

export function useProducts(initialParams: GetProductsParams = {}): UseProductsReturn {
  const [state, setState] = useState<UseProductsState>({
    data: null,
    stats: { total: 0, active: 0, draft: 0, outOfStock: 0 },
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
      const data = await getProducts(params)

      // TODO: Backend should provide aggregate stats in API response
      // Current limitation: Stats only reflect the current page/filter context
      // For accurate global stats, we need a dedicated /products/stats endpoint
      const stats: ProductStats = {
        total: data.totalCount, // ✅ Accurate from backend totalCount
        active: data.items.filter(p => p.status === 'Active').length, // ⚠️ Limited to current page
        draft: data.items.filter(p => p.status === 'Draft').length, // ⚠️ Limited to current page
        outOfStock: data.items.filter(p => p.status === 'OutOfStock').length, // ⚠️ Limited to current page
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
    handleDelete,
    handlePublish,
    handleArchive,
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

export function useProduct(id: string | undefined): UseProductReturn {
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

// ============================================================================
// Product Categories Hook
// ============================================================================

interface UseProductCategoriesState {
  data: ProductCategoryListItem[]
  loading: boolean
  error: string | null
}

interface UseProductCategoriesReturn extends UseProductCategoriesState {
  refresh: () => Promise<void>
  setSearch: (search: string) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  params: GetProductCategoriesParams
}

export function useProductCategories(
  initialParams: GetProductCategoriesParams = {}
): UseProductCategoriesReturn {
  const [state, setState] = useState<UseProductCategoriesState>({
    data: [],
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<GetProductCategoriesParams>(initialParams)

  const fetchCategories = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getProductCategories(params)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load categories'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchCategories()
  }, [fetchCategories])

  const setSearch = useCallback((search: string) => {
    setParams((prev) => ({ ...prev, search: search || undefined }))
  }, [])

  const handleDelete = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      try {
        await deleteProductCategory(id)
        await fetchCategories()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to delete category'
        return { success: false, error: message }
      }
    },
    [fetchCategories]
  )

  return {
    ...state,
    refresh: fetchCategories,
    setSearch,
    handleDelete,
    params,
  }
}
