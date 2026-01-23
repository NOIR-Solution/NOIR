/**
 * Blog CMS API Service
 *
 * Provides methods for managing blog posts, categories, and tags.
 */
import { apiClient } from './apiClient'
import type {
  Post,
  PostListItem,
  PostCategory,
  PostCategoryListItem,
  PostTag,
  PostTagListItem,
  BlogPagedResult,
  CreatePostRequest,
  UpdatePostRequest,
  PublishPostRequest,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreateTagRequest,
  UpdateTagRequest,
  PostStatus,
} from '@/types'

// ============================================================================
// Posts
// ============================================================================

export interface GetPostsParams {
  search?: string
  status?: PostStatus
  categoryId?: string
  authorId?: string
  tagId?: string
  publishedOnly?: boolean
  page?: number
  pageSize?: number
}

/**
 * Fetch paginated list of posts
 */
export async function getPosts(params: GetPostsParams = {}): Promise<BlogPagedResult<PostListItem>> {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)
  if (params.status) queryParams.append('status', params.status)
  if (params.categoryId) queryParams.append('categoryId', params.categoryId)
  if (params.authorId) queryParams.append('authorId', params.authorId)
  if (params.tagId) queryParams.append('tagId', params.tagId)
  if (params.publishedOnly) queryParams.append('publishedOnly', 'true')
  if (params.page) queryParams.append('page', params.page.toString())
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<BlogPagedResult<PostListItem>>(`/blog/posts${query ? `?${query}` : ''}`)
}

/**
 * Fetch a single post by ID
 */
export async function getPostById(id: string): Promise<Post> {
  return apiClient<Post>(`/blog/posts/${id}`)
}

/**
 * Fetch a single post by slug
 */
export async function getPostBySlug(slug: string): Promise<Post> {
  return apiClient<Post>(`/blog/posts/by-slug/${slug}`)
}

/**
 * Create a new post
 */
export async function createPost(request: CreatePostRequest): Promise<Post> {
  return apiClient<Post>('/blog/posts', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing post
 */
export async function updatePost(id: string, request: UpdatePostRequest): Promise<Post> {
  return apiClient<Post>(`/blog/posts/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a post
 */
export async function deletePost(id: string): Promise<void> {
  return apiClient<void>(`/blog/posts/${id}`, {
    method: 'DELETE',
  })
}

/**
 * Publish or schedule a post
 */
export async function publishPost(id: string, request?: PublishPostRequest): Promise<Post> {
  return apiClient<Post>(`/blog/posts/${id}/publish`, {
    method: 'POST',
    body: request ? JSON.stringify(request) : undefined,
  })
}

/**
 * Unpublish a post (revert to draft)
 */
export async function unpublishPost(id: string): Promise<Post> {
  return apiClient<Post>(`/blog/posts/${id}/unpublish`, {
    method: 'POST',
  })
}

// ============================================================================
// Categories
// ============================================================================

export interface GetCategoriesParams {
  search?: string
  topLevelOnly?: boolean
  includeChildren?: boolean
}

/**
 * Fetch list of categories
 */
export async function getCategories(params: GetCategoriesParams = {}): Promise<PostCategoryListItem[]> {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)
  if (params.topLevelOnly) queryParams.append('topLevelOnly', 'true')
  if (params.includeChildren) queryParams.append('includeChildren', 'true')

  const query = queryParams.toString()
  return apiClient<PostCategoryListItem[]>(`/blog/categories${query ? `?${query}` : ''}`)
}

/**
 * Create a new category
 */
export async function createCategory(request: CreateCategoryRequest): Promise<PostCategory> {
  return apiClient<PostCategory>('/blog/categories', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing category
 */
export async function updateCategory(id: string, request: UpdateCategoryRequest): Promise<PostCategory> {
  return apiClient<PostCategory>(`/blog/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a category
 */
export async function deleteCategory(id: string): Promise<void> {
  return apiClient<void>(`/blog/categories/${id}`, {
    method: 'DELETE',
  })
}

// ============================================================================
// Tags
// ============================================================================

export interface GetTagsParams {
  search?: string
}

/**
 * Fetch list of tags
 */
export async function getTags(params: GetTagsParams = {}): Promise<PostTagListItem[]> {
  const queryParams = new URLSearchParams()
  if (params.search) queryParams.append('search', params.search)

  const query = queryParams.toString()
  return apiClient<PostTagListItem[]>(`/blog/tags${query ? `?${query}` : ''}`)
}

/**
 * Create a new tag
 */
export async function createTag(request: CreateTagRequest): Promise<PostTag> {
  return apiClient<PostTag>('/blog/tags', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

/**
 * Update an existing tag
 */
export async function updateTag(id: string, request: UpdateTagRequest): Promise<PostTag> {
  return apiClient<PostTag>(`/blog/tags/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/**
 * Delete a tag
 */
export async function deleteTag(id: string): Promise<void> {
  return apiClient<void>(`/blog/tags/${id}`, {
    method: 'DELETE',
  })
}
