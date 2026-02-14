/**
 * React hooks for Product Attribute management
 */
import { useState, useEffect, useCallback } from 'react'
import {
  getProductAttributes,
  getActiveProductAttributes,
  getFilterableAttributesWithValues,
  getProductAttributeById,
  createProductAttribute,
  updateProductAttribute,
  deleteProductAttribute,
  addProductAttributeValue,
  updateProductAttributeValue,
  removeProductAttributeValue,
  getCategoryAttributes,
  getCategoryAttributeFormSchema,
  assignCategoryAttribute,
  updateCategoryAttribute,
  removeCategoryAttribute,
  getProductAttributeFormSchema,
  bulkUpdateProductAttributes,
  setProductAttributeValue,
  type GetProductAttributesParams,
} from '@/services/productAttributes'
import type {
  ProductAttribute,
  ProductAttributeListItem,
  ProductAttributePagedResult,
  ProductAttributeValue,
  CreateProductAttributeRequest,
  UpdateProductAttributeRequest,
  AddProductAttributeValueRequest,
  UpdateProductAttributeValueRequest,
  CategoryAttribute,
  AssignCategoryAttributeRequest,
  UpdateCategoryAttributeRequest,
  ProductAttributeFormSchema,
  CategoryAttributeFormSchema,
  ProductAttributeFormField,
  ProductAttributeAssignment,
  SetProductAttributeValueRequest,
  BulkUpdateProductAttributesRequest,
} from '@/types/productAttribute'
import { ApiError } from '@/services/apiClient'

// ============================================================================
// Product Attributes List Hook
// ============================================================================

interface UseProductAttributesState {
  data: ProductAttributePagedResult | null
  loading: boolean
  error: string | null
}

interface UseProductAttributesReturn extends UseProductAttributesState {
  refresh: () => Promise<void>
  setPage: (page: number) => void
  setPageSize: (size: number) => void
  setSearch: (search: string) => void
  handleDelete: (id: string) => Promise<{ success: boolean; error?: string }>
  params: GetProductAttributesParams
}

export const useProductAttributes = (
  initialParams: GetProductAttributesParams = {}
): UseProductAttributesReturn => {
  const [state, setState] = useState<UseProductAttributesState>({
    data: null,
    loading: true,
    error: null,
  })

  const [params, setParams] = useState<GetProductAttributesParams>({
    page: 1,
    pageSize: 20,
    ...initialParams,
  })

  const fetchProductAttributes = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getProductAttributes(params)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load product attributes'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [params])

  useEffect(() => {
    fetchProductAttributes()
  }, [fetchProductAttributes])

  const setPage = useCallback((page: number) => {
    setParams((prev) => ({ ...prev, page }))
  }, [])

  const setPageSize = useCallback((size: number) => {
    setParams((prev) => ({ ...prev, pageSize: size, page: 1 }))
  }, [])

  const setSearch = useCallback((search: string) => {
    setParams((prev) => ({ ...prev, search: search || undefined, page: 1 }))
  }, [])

  const handleDelete = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      try {
        await deleteProductAttribute(id)
        await fetchProductAttributes()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to delete product attribute'
        return { success: false, error: message }
      }
    },
    [fetchProductAttributes]
  )

  return {
    ...state,
    refresh: fetchProductAttributes,
    setPage,
    setPageSize,
    setSearch,
    handleDelete,
    params,
  }
}

// ============================================================================
// Active Product Attributes Hook (for dropdowns)
// ============================================================================

interface UseActiveProductAttributesState {
  data: ProductAttributeListItem[]
  loading: boolean
  error: string | null
}

interface UseActiveProductAttributesReturn extends UseActiveProductAttributesState {
  refresh: () => Promise<void>
}

export const useActiveProductAttributes = (): UseActiveProductAttributesReturn => {
  const [state, setState] = useState<UseActiveProductAttributesState>({
    data: [],
    loading: true,
    error: null,
  })

  const fetchActiveProductAttributes = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getActiveProductAttributes()
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load product attributes'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [])

  useEffect(() => {
    fetchActiveProductAttributes()
  }, [fetchActiveProductAttributes])

  return {
    ...state,
    refresh: fetchActiveProductAttributes,
  }
}

// ============================================================================
// Filterable Product Attributes Hook (for admin product filters)
// ============================================================================

interface UseFilterableProductAttributesState {
  data: ProductAttribute[]
  loading: boolean
  error: string | null
}

interface UseFilterableProductAttributesReturn extends UseFilterableProductAttributesState {
  refresh: () => Promise<void>
}

/**
 * Hook to fetch filterable attributes with their values.
 * Used for admin product list filtering by attributes.
 */
export const useFilterableProductAttributes = (): UseFilterableProductAttributesReturn => {
  const [state, setState] = useState<UseFilterableProductAttributesState>({
    data: [],
    loading: true,
    error: null,
  })

  const fetchFilterableAttributes = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getFilterableAttributesWithValues()
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load filterable attributes'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [])

  useEffect(() => {
    fetchFilterableAttributes()
  }, [fetchFilterableAttributes])

  return {
    ...state,
    refresh: fetchFilterableAttributes,
  }
}

// ============================================================================
// Single Product Attribute Hook
// ============================================================================

interface UseProductAttributeState {
  data: ProductAttribute | null
  loading: boolean
  error: string | null
}

interface UseProductAttributeReturn extends UseProductAttributeState {
  refresh: () => Promise<void>
}

export const useProductAttribute = (id: string | undefined): UseProductAttributeReturn => {
  const [state, setState] = useState<UseProductAttributeState>({
    data: null,
    loading: !!id,
    error: null,
  })

  const fetchProductAttribute = useCallback(async () => {
    if (!id) {
      setState({ data: null, loading: false, error: null })
      return
    }

    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getProductAttributeById(id)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load product attribute'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [id])

  useEffect(() => {
    fetchProductAttribute()
  }, [fetchProductAttribute])

  return {
    ...state,
    refresh: fetchProductAttribute,
  }
}

// ============================================================================
// Create Product Attribute Hook
// ============================================================================

interface UseCreateProductAttributeReturn {
  createProductAttribute: (
    request: CreateProductAttributeRequest
  ) => Promise<{ success: boolean; data?: ProductAttribute; error?: string }>
  isPending: boolean
}

export const useCreateProductAttribute = (): UseCreateProductAttributeReturn => {
  const [isPending, setIsPending] = useState(false)

  const create = useCallback(
    async (
      request: CreateProductAttributeRequest
    ): Promise<{ success: boolean; data?: ProductAttribute; error?: string }> => {
      setIsPending(true)
      try {
        const data = await createProductAttribute(request)
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to create product attribute'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    createProductAttribute: create,
    isPending,
  }
}

// ============================================================================
// Update Product Attribute Hook
// ============================================================================

interface UseUpdateProductAttributeReturn {
  updateProductAttribute: (
    id: string,
    request: UpdateProductAttributeRequest
  ) => Promise<{ success: boolean; data?: ProductAttribute; error?: string }>
  isPending: boolean
}

export const useUpdateProductAttribute = (): UseUpdateProductAttributeReturn => {
  const [isPending, setIsPending] = useState(false)

  const update = useCallback(
    async (
      id: string,
      request: UpdateProductAttributeRequest
    ): Promise<{ success: boolean; data?: ProductAttribute; error?: string }> => {
      setIsPending(true)
      try {
        const data = await updateProductAttribute(id, request)
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to update product attribute'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    updateProductAttribute: update,
    isPending,
  }
}

// ============================================================================
// Delete Product Attribute Hook
// ============================================================================

interface UseDeleteProductAttributeReturn {
  deleteProductAttribute: (id: string) => Promise<{ success: boolean; error?: string }>
  isPending: boolean
}

export const useDeleteProductAttribute = (): UseDeleteProductAttributeReturn => {
  const [isPending, setIsPending] = useState(false)

  const remove = useCallback(
    async (id: string): Promise<{ success: boolean; error?: string }> => {
      setIsPending(true)
      try {
        await deleteProductAttribute(id)
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to delete product attribute'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    deleteProductAttribute: remove,
    isPending,
  }
}

// ============================================================================
// Attribute Value Hooks
// ============================================================================

interface UseAddProductAttributeValueReturn {
  addValue: (
    attributeId: string,
    request: AddProductAttributeValueRequest
  ) => Promise<{ success: boolean; data?: ProductAttributeValue; error?: string }>
  isPending: boolean
}

export const useAddProductAttributeValue = (): UseAddProductAttributeValueReturn => {
  const [isPending, setIsPending] = useState(false)

  const add = useCallback(
    async (
      attributeId: string,
      request: AddProductAttributeValueRequest
    ): Promise<{ success: boolean; data?: ProductAttributeValue; error?: string }> => {
      setIsPending(true)
      try {
        const data = await addProductAttributeValue(attributeId, request)
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to add attribute value'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    addValue: add,
    isPending,
  }
}

interface UseUpdateProductAttributeValueReturn {
  updateValue: (
    attributeId: string,
    valueId: string,
    request: UpdateProductAttributeValueRequest
  ) => Promise<{ success: boolean; data?: ProductAttributeValue; error?: string }>
  isPending: boolean
}

export const useUpdateProductAttributeValue = (): UseUpdateProductAttributeValueReturn => {
  const [isPending, setIsPending] = useState(false)

  const update = useCallback(
    async (
      attributeId: string,
      valueId: string,
      request: UpdateProductAttributeValueRequest
    ): Promise<{ success: boolean; data?: ProductAttributeValue; error?: string }> => {
      setIsPending(true)
      try {
        const data = await updateProductAttributeValue(attributeId, valueId, request)
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to update attribute value'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    updateValue: update,
    isPending,
  }
}

interface UseRemoveProductAttributeValueReturn {
  removeValue: (
    attributeId: string,
    valueId: string
  ) => Promise<{ success: boolean; error?: string }>
  isPending: boolean
}

export const useRemoveProductAttributeValue = (): UseRemoveProductAttributeValueReturn => {
  const [isPending, setIsPending] = useState(false)

  const remove = useCallback(
    async (
      attributeId: string,
      valueId: string
    ): Promise<{ success: boolean; error?: string }> => {
      setIsPending(true)
      try {
        await removeProductAttributeValue(attributeId, valueId)
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to remove attribute value'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    removeValue: remove,
    isPending,
  }
}

// ============================================================================
// Category Attribute Hooks
// ============================================================================

interface UseCategoryAttributesState {
  data: CategoryAttribute[]
  loading: boolean
  error: string | null
}

interface UseCategoryAttributesReturn extends UseCategoryAttributesState {
  refresh: () => Promise<void>
  handleAssign: (
    request: AssignCategoryAttributeRequest
  ) => Promise<{ success: boolean; data?: CategoryAttribute; error?: string }>
  handleUpdate: (
    attributeId: string,
    request: UpdateCategoryAttributeRequest
  ) => Promise<{ success: boolean; data?: CategoryAttribute; error?: string }>
  handleRemove: (attributeId: string) => Promise<{ success: boolean; error?: string }>
}

export const useCategoryAttributes = (categoryId: string | undefined): UseCategoryAttributesReturn => {
  const [state, setState] = useState<UseCategoryAttributesState>({
    data: [],
    loading: !!categoryId,
    error: null,
  })

  const fetchCategoryAttributes = useCallback(async () => {
    if (!categoryId) {
      setState({ data: [], loading: false, error: null })
      return
    }

    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getCategoryAttributes(categoryId)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load category attributes'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [categoryId])

  useEffect(() => {
    fetchCategoryAttributes()
  }, [fetchCategoryAttributes])

  const handleAssign = useCallback(
    async (
      request: AssignCategoryAttributeRequest
    ): Promise<{ success: boolean; data?: CategoryAttribute; error?: string }> => {
      if (!categoryId) {
        return { success: false, error: 'No category selected' }
      }
      try {
        const data = await assignCategoryAttribute(categoryId, request)
        await fetchCategoryAttributes()
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to assign attribute'
        return { success: false, error: message }
      }
    },
    [categoryId, fetchCategoryAttributes]
  )

  const handleUpdate = useCallback(
    async (
      attributeId: string,
      request: UpdateCategoryAttributeRequest
    ): Promise<{ success: boolean; data?: CategoryAttribute; error?: string }> => {
      if (!categoryId) {
        return { success: false, error: 'No category selected' }
      }
      try {
        const data = await updateCategoryAttribute(categoryId, attributeId, request)
        await fetchCategoryAttributes()
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to update category attribute'
        return { success: false, error: message }
      }
    },
    [categoryId, fetchCategoryAttributes]
  )

  const handleRemove = useCallback(
    async (attributeId: string): Promise<{ success: boolean; error?: string }> => {
      if (!categoryId) {
        return { success: false, error: 'No category selected' }
      }
      try {
        await removeCategoryAttribute(categoryId, attributeId)
        await fetchCategoryAttributes()
        return { success: true }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to remove category attribute'
        return { success: false, error: message }
      }
    },
    [categoryId, fetchCategoryAttributes]
  )

  return {
    ...state,
    refresh: fetchCategoryAttributes,
    handleAssign,
    handleUpdate,
    handleRemove,
  }
}

// ============================================================================
// Product Attribute Form Hook (Phase 9)
// ============================================================================

interface UseProductAttributeFormState {
  data: ProductAttributeFormSchema | null
  loading: boolean
  error: string | null
}

interface UseProductAttributeFormReturn extends UseProductAttributeFormState {
  refresh: () => Promise<void>
}

/**
 * Hook to fetch the attribute form schema for a product.
 * Returns the fields based on the product's category with current values.
 */
export const useProductAttributeForm = (
  productId: string | undefined,
  variantId?: string
): UseProductAttributeFormReturn => {
  const [state, setState] = useState<UseProductAttributeFormState>({
    data: null,
    loading: !!productId,
    error: null,
  })

  const fetchFormSchema = useCallback(async () => {
    if (!productId) {
      setState({ data: null, loading: false, error: null })
      return
    }

    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getProductAttributeFormSchema(productId, variantId)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load attribute form'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [productId, variantId])

  useEffect(() => {
    fetchFormSchema()
  }, [fetchFormSchema])

  return {
    ...state,
    refresh: fetchFormSchema,
  }
}

// ============================================================================
// Category Attribute Form Hook (for new product creation)
// ============================================================================

interface UseCategoryAttributeFormState {
  data: CategoryAttributeFormSchema | null
  loading: boolean
  error: string | null
}

interface UseCategoryAttributeFormReturn extends UseCategoryAttributeFormState {
  refresh: () => Promise<void>
}

/**
 * Hook to fetch the attribute form schema for a category.
 * Used for new product creation - returns form fields without requiring a productId.
 * Unlike useProductAttributeForm, this works before the product is saved.
 */
export const useCategoryAttributeForm = (
  categoryId: string | null | undefined
): UseCategoryAttributeFormReturn => {
  const [state, setState] = useState<UseCategoryAttributeFormState>({
    data: null,
    loading: !!categoryId,
    error: null,
  })

  const fetchFormSchema = useCallback(async () => {
    if (!categoryId) {
      setState({ data: null, loading: false, error: null })
      return
    }

    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const data = await getCategoryAttributeFormSchema(categoryId)
      setState({ data, loading: false, error: null })
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Failed to load category attributes'
      setState((prev) => ({ ...prev, loading: false, error: message }))
    }
  }, [categoryId])

  useEffect(() => {
    fetchFormSchema()
  }, [fetchFormSchema])

  return {
    ...state,
    refresh: fetchFormSchema,
  }
}

// ============================================================================
// Bulk Update Product Attributes Hook (Phase 9)
// ============================================================================

interface UseBulkUpdateProductAttributesReturn {
  bulkUpdate: (
    productId: string,
    request: BulkUpdateProductAttributesRequest
  ) => Promise<{ success: boolean; data?: ProductAttributeAssignment[]; error?: string }>
  isPending: boolean
}

/**
 * Hook to bulk update multiple attribute values for a product.
 */
export const useBulkUpdateProductAttributes = (): UseBulkUpdateProductAttributesReturn => {
  const [isPending, setIsPending] = useState(false)

  const bulkUpdate = useCallback(
    async (
      productId: string,
      request: BulkUpdateProductAttributesRequest
    ): Promise<{ success: boolean; data?: ProductAttributeAssignment[]; error?: string }> => {
      setIsPending(true)
      try {
        const data = await bulkUpdateProductAttributes(productId, request)
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to update attributes'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    bulkUpdate,
    isPending,
  }
}

// ============================================================================
// Set Single Product Attribute Value Hook (Phase 9)
// ============================================================================

interface UseSetProductAttributeValueReturn {
  setValue: (
    productId: string,
    attributeId: string,
    request: SetProductAttributeValueRequest
  ) => Promise<{ success: boolean; data?: ProductAttributeAssignment; error?: string }>
  isPending: boolean
}

/**
 * Hook to set a single attribute value for a product.
 */
export const useSetProductAttributeValue = (): UseSetProductAttributeValueReturn => {
  const [isPending, setIsPending] = useState(false)

  const setValue = useCallback(
    async (
      productId: string,
      attributeId: string,
      request: SetProductAttributeValueRequest
    ): Promise<{ success: boolean; data?: ProductAttributeAssignment; error?: string }> => {
      setIsPending(true)
      try {
        const data = await setProductAttributeValue(productId, attributeId, request)
        return { success: true, data }
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Failed to set attribute value'
        return { success: false, error: message }
      } finally {
        setIsPending(false)
      }
    },
    []
  )

  return {
    setValue,
    isPending,
  }
}

// Re-export types for convenience
export type {
  ProductAttribute,
  ProductAttributeListItem,
  ProductAttributeValue,
  CreateProductAttributeRequest,
  UpdateProductAttributeRequest,
  AddProductAttributeValueRequest,
  UpdateProductAttributeValueRequest,
  CategoryAttribute,
  AssignCategoryAttributeRequest,
  UpdateCategoryAttributeRequest,
  ProductAttributeFormSchema,
  ProductAttributeFormField,
  ProductAttributeAssignment,
  SetProductAttributeValueRequest,
  BulkUpdateProductAttributesRequest,
}
