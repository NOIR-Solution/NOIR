import { useQuery } from '@tanstack/react-query'
import {
  getPosts,
  getCategories,
  getTags,
  type GetPostsParams,
  type GetCategoriesParams,
  type GetTagsParams,
} from '@/services/blog'
import { blogPostKeys, blogCategoryKeys, blogTagKeys } from './queryKeys'

export const useBlogPostsQuery = (params: GetPostsParams) =>
  useQuery({
    queryKey: blogPostKeys.list(params),
    queryFn: () => getPosts(params),
  })

export const useBlogCategoriesQuery = (params: GetCategoriesParams = {}) =>
  useQuery({
    queryKey: blogCategoryKeys.list(params),
    queryFn: () => getCategories(params),
  })

export const useBlogTagsQuery = (params: GetTagsParams = {}) =>
  useQuery({
    queryKey: blogTagKeys.list(params),
    queryFn: () => getTags(params),
  })