/**
 * Blog CMS Types
 */

// Post Status enum matching backend
export type PostStatus = 'Draft' | 'Published' | 'Scheduled' | 'Archived'

// Full post details for editing
export interface Post {
  id: string
  title: string
  slug: string
  excerpt?: string
  contentJson?: string
  contentHtml?: string
  featuredImageUrl?: string
  featuredImageAlt?: string
  status: PostStatus
  publishedAt?: string
  scheduledPublishAt?: string
  metaTitle?: string
  metaDescription?: string
  canonicalUrl?: string
  allowIndexing: boolean
  categoryId?: string
  categoryName?: string
  authorId: string
  authorName?: string
  viewCount: number
  readingTimeMinutes: number
  tags: PostTag[]
  createdAt: string
  modifiedAt?: string
}

// Simplified post for list views
export interface PostListItem {
  id: string
  title: string
  slug: string
  excerpt?: string
  featuredImageUrl?: string
  status: PostStatus
  publishedAt?: string
  scheduledPublishAt?: string
  categoryName?: string
  authorName?: string
  viewCount: number
  readingTimeMinutes: number
  createdAt: string
}

// Category with hierarchy support
export interface PostCategory {
  id: string
  name: string
  slug: string
  description?: string
  metaTitle?: string
  metaDescription?: string
  imageUrl?: string
  sortOrder: number
  postCount: number
  parentId?: string
  parentName?: string
  children?: PostCategory[]
  createdAt: string
  modifiedAt?: string
}

// Simplified category for list views
export interface PostCategoryListItem {
  id: string
  name: string
  slug: string
  description?: string
  sortOrder: number
  postCount: number
  parentId?: string
  parentName?: string
  childCount: number
}

// Tag details
export interface PostTag {
  id: string
  name: string
  slug: string
  description?: string
  color?: string
  postCount: number
  createdAt: string
  modifiedAt?: string
}

// Simplified tag for list views
export interface PostTagListItem {
  id: string
  name: string
  slug: string
  description?: string
  color?: string
  postCount: number
}

// Blog paginated response
export interface BlogPagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// Create/Update post request
export interface CreatePostRequest {
  title: string
  slug: string
  excerpt?: string
  contentJson?: string
  contentHtml?: string
  featuredImageUrl?: string
  featuredImageAlt?: string
  metaTitle?: string
  metaDescription?: string
  canonicalUrl?: string
  allowIndexing: boolean
  categoryId?: string
  tagIds?: string[]
}

export interface UpdatePostRequest extends CreatePostRequest {}

// Publish post request
export interface PublishPostRequest {
  scheduledPublishAt?: string
}

// Create/Update category request
export interface CreateCategoryRequest {
  name: string
  slug: string
  description?: string
  metaTitle?: string
  metaDescription?: string
  imageUrl?: string
  sortOrder: number
  parentId?: string
}

export interface UpdateCategoryRequest extends CreateCategoryRequest {}

// Create/Update tag request
export interface CreateTagRequest {
  name: string
  slug: string
  description?: string
  color?: string
}

export interface UpdateTagRequest extends CreateTagRequest {}
