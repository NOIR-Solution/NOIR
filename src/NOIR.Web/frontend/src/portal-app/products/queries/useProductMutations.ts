import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  deleteProduct,
  publishProduct,
  archiveProduct,
  duplicateProduct,
  bulkPublishProducts,
  bulkArchiveProducts,
  bulkDeleteProducts,
  createProduct,
  updateProduct,
  createProductCategory,
  updateProductCategory,
  deleteProductCategory,
  addProductVariant,
  deleteProductVariant,
  addProductImage,
  updateProductImage,
  deleteProductImage,
  setPrimaryProductImage,
  uploadProductImage,
  reorderProductImages,
} from '@/services/products'
import type {
  CreateProductRequest,
  UpdateProductRequest,
  AddProductVariantRequest,
  AddProductImageRequest,
  UpdateProductImageRequest,
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
} from '@/types/product'
import { productKeys, productCategoryKeys } from './queryKeys'
import { optimisticListDelete, optimisticListPatch } from '@/hooks/useOptimisticMutation'

// ============================================================================
// Product Mutations
// ============================================================================

export const useCreateProduct = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateProductRequest) => createProduct(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
  })
}

export const useUpdateProduct = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateProductRequest }) =>
      updateProduct(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
  })
}

export const useDeleteProduct = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteProduct(id),
    ...optimisticListDelete(queryClient, productKeys.lists(), productKeys.all),
  })
}

export const usePublishProduct = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => publishProduct(id),
    ...optimisticListPatch(queryClient, productKeys.lists(), productKeys.all, { status: 'Active' }),
  })
}

export const useArchiveProduct = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => archiveProduct(id),
    ...optimisticListPatch(queryClient, productKeys.lists(), productKeys.all, { status: 'Archived' }),
  })
}

export const useDuplicateProduct = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => duplicateProduct(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
  })
}

export const useBulkPublishProducts = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (ids: string[]) => bulkPublishProducts(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
  })
}

export const useBulkArchiveProducts = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (ids: string[]) => bulkArchiveProducts(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
  })
}

export const useBulkDeleteProducts = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (ids: string[]) => bulkDeleteProducts(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
  })
}

// ============================================================================
// Product Category Mutations
// ============================================================================

export const useCreateProductCategory = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateProductCategoryRequest) => createProductCategory(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productCategoryKeys.all })
    },
  })
}

export const useUpdateProductCategory = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateProductCategoryRequest }) =>
      updateProductCategory(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productCategoryKeys.all })
    },
  })
}

export const useDeleteProductCategory = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteProductCategory(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productCategoryKeys.all })
    },
  })
}

// ============================================================================
// Product Sub-resource Mutations (Variants, Images)
// ============================================================================

export const useAddProductVariant = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, request }: { productId: string; request: AddProductVariantRequest }) =>
      addProductVariant(productId, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}

export const useDeleteProductVariant = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, variantId }: { productId: string; variantId: string }) =>
      deleteProductVariant(productId, variantId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}

export const useAddProductImage = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, request }: { productId: string; request: AddProductImageRequest }) =>
      addProductImage(productId, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}

export const useUpdateProductImage = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, imageId, request }: { productId: string; imageId: string; request: UpdateProductImageRequest }) =>
      updateProductImage(productId, imageId, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}

export const useDeleteProductImage = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, imageId }: { productId: string; imageId: string }) =>
      deleteProductImage(productId, imageId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}

export const useSetPrimaryProductImage = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, imageId }: { productId: string; imageId: string }) =>
      setPrimaryProductImage(productId, imageId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}

export const useUploadProductImage = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, file, altText, isPrimary }: { productId: string; file: File; altText?: string; isPrimary?: boolean }) =>
      uploadProductImage(productId, file, altText, isPrimary),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}

export const useReorderProductImages = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ productId, items }: { productId: string; items: { imageId: string; sortOrder: number }[] }) =>
      reorderProductImages(productId, items),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: productKeys.detail(variables.productId) })
    },
  })
}

export const usePublishProductAction = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => publishProduct(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
  })
}
