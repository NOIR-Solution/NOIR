import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  createProductAttribute,
  updateProductAttribute,
  deleteProductAttribute,
  addProductAttributeValue,
  updateProductAttributeValue,
  removeProductAttributeValue,
  assignCategoryAttribute,
  updateCategoryAttribute,
  removeCategoryAttribute,
  bulkUpdateProductAttributes,
  setProductAttributeValue,
} from '@/services/productAttributes'
import type {
  CreateProductAttributeRequest,
  UpdateProductAttributeRequest,
  AddProductAttributeValueRequest,
  UpdateProductAttributeValueRequest,
  AssignCategoryAttributeRequest,
  UpdateCategoryAttributeRequest,
  SetProductAttributeValueRequest,
  BulkUpdateProductAttributesRequest,
} from '@/types/productAttribute'
import { productAttributeKeys } from './queryKeys'
import { productKeys } from './queryKeys'
import { optimisticListDelete } from '@/hooks/useOptimisticMutation'

// ============================================================================
// Product Attribute CRUD Mutations
// ============================================================================

export const useCreateProductAttributeMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateProductAttributeRequest) => createProductAttribute(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.all })
    },
  })
}

export const useUpdateProductAttributeMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateProductAttributeRequest }) =>
      updateProductAttribute(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.all })
    },
  })
}

export const useDeleteProductAttributeMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteProductAttribute(id),
    ...optimisticListDelete(queryClient, productAttributeKeys.lists(), productAttributeKeys.all),
  })
}

// ============================================================================
// Attribute Value Mutations
// ============================================================================

export const useAddProductAttributeValueMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ attributeId, request }: { attributeId: string; request: AddProductAttributeValueRequest }) =>
      addProductAttributeValue(attributeId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.all })
    },
  })
}

export const useUpdateProductAttributeValueMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ attributeId, valueId, request }: { attributeId: string; valueId: string; request: UpdateProductAttributeValueRequest }) =>
      updateProductAttributeValue(attributeId, valueId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.all })
    },
  })
}

export const useRemoveProductAttributeValueMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ attributeId, valueId }: { attributeId: string; valueId: string }) =>
      removeProductAttributeValue(attributeId, valueId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.all })
    },
  })
}

// ============================================================================
// Category Attribute Mutations
// ============================================================================

export const useAssignCategoryAttributeMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ categoryId, request }: { categoryId: string; request: AssignCategoryAttributeRequest }) =>
      assignCategoryAttribute(categoryId, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.categoryAttributes(variables.categoryId) })
    },
  })
}

export const useUpdateCategoryAttributeMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ categoryId, attributeId, request }: { categoryId: string; attributeId: string; request: UpdateCategoryAttributeRequest }) =>
      updateCategoryAttribute(categoryId, attributeId, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.categoryAttributes(variables.categoryId) })
    },
  })
}

export const useRemoveCategoryAttributeMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ categoryId, attributeId }: { categoryId: string; attributeId: string }) =>
      removeCategoryAttribute(categoryId, attributeId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.categoryAttributes(variables.categoryId) })
    },
  })
}

// ============================================================================
// Product Attribute Assignment Mutations
// ============================================================================

export const useBulkUpdateProductAttributesMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, request }: { productId: string; request: BulkUpdateProductAttributesRequest }) =>
      bulkUpdateProductAttributes(productId, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.productFormSchema(variables.productId) })
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}

export const useSetProductAttributeValueMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, attributeId, request }: { productId: string; attributeId: string; request: SetProductAttributeValueRequest }) =>
      setProductAttributeValue(productId, attributeId, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productAttributeKeys.productFormSchema(variables.productId) })
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}
