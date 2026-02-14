import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deletePost, deleteCategory, deleteTag } from '@/services/blog'
import { blogPostKeys, blogCategoryKeys, blogTagKeys } from './queryKeys'
import { optimisticListDelete, optimisticArrayDelete } from '@/hooks/useOptimisticMutation'

export const useDeleteBlogPostMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deletePost(id),
    ...optimisticListDelete(queryClient, blogPostKeys.lists(), blogPostKeys.all),
  })
}

export const useDeleteBlogCategoryMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteCategory(id),
    ...optimisticArrayDelete(queryClient, blogCategoryKeys.lists(), blogCategoryKeys.all),
  })
}

export const useDeleteBlogTagMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteTag(id),
    ...optimisticArrayDelete(queryClient, blogTagKeys.lists(), blogTagKeys.all),
  })
}
