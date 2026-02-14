import type { GetPostsParams, GetCategoriesParams, GetTagsParams } from '@/services/blog'

export const blogPostKeys = {
  all: ['blogPosts'] as const,
  lists: () => [...blogPostKeys.all, 'list'] as const,
  list: (params: GetPostsParams) => [...blogPostKeys.lists(), params] as const,
  details: () => [...blogPostKeys.all, 'detail'] as const,
  detail: (id: string) => [...blogPostKeys.details(), id] as const,
}

export const blogCategoryKeys = {
  all: ['blogCategories'] as const,
  lists: () => [...blogCategoryKeys.all, 'list'] as const,
  list: (params: GetCategoriesParams) => [...blogCategoryKeys.lists(), params] as const,
}

export const blogTagKeys = {
  all: ['blogTags'] as const,
  lists: () => [...blogTagKeys.all, 'list'] as const,
  list: (params: GetTagsParams) => [...blogTagKeys.lists(), params] as const,
}