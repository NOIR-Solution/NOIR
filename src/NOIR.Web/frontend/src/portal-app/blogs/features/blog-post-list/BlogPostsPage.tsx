import { useState, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, FileText, Plus, Eye, Pencil, Trash2, Send } from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
  FilePreviewTrigger,
  Input,
  PageHeader,
  Pagination,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'

import { usePageContext } from '@/hooks/usePageContext'

import { useBlogPostsQuery, useBlogCategoriesQuery, useDeleteBlogPostMutation } from '@/portal-app/blogs/queries'
import type { GetPostsParams } from '@/services/blog'
import { DeleteBlogPostDialog } from '../../components/blog-posts/DeleteBlogPostDialog'
import type { PostListItem, PostStatus } from '@/types'
import { formatDistanceToNow } from 'date-fns'
import { useNavigate } from 'react-router-dom'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'

const statusColors: Record<PostStatus, string> = {
  Draft: 'bg-gray-100 text-gray-800',
  Published: 'bg-green-100 text-green-800',
  Scheduled: 'bg-blue-100 text-blue-800',
  Archived: 'bg-yellow-100 text-yellow-800',
}

export const BlogPostsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Blog Posts')
  const navigate = useNavigate()

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [isFilterPending, startFilterTransition] = useTransition()
  const [postToDelete, setPostToDelete] = useState<PostListItem | null>(null)
  const [params, setParams] = useState<GetPostsParams>({ page: 1, pageSize: 10 })

  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
  const { data, isLoading: loading, error: queryError } = useBlogPostsQuery(queryParams)
  const { data: categories = [] } = useBlogCategoriesQuery({})
  const deleteMutation = useDeleteBlogPostMutation()
  const error = queryError?.message ?? null

  const setPage = (page: number) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, page }))
  )

  const handleStatusChange = (value: string) => {
    startFilterTransition(() =>
      setParams((prev) => ({ ...prev, status: value === 'all' ? undefined : (value as PostStatus), page: 1 }))
    )
  }

  const handleCategoryChange = (value: string) => {
    startFilterTransition(() =>
      setParams((prev) => ({ ...prev, categoryId: value === 'all' ? undefined : value, page: 1 }))
    )
  }

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete post'
      return { success: false, error: message }
    }
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <PageHeader
        icon={FileText}
        title="Blog Posts"
        description="Manage your blog content"
        action={
          <ViewTransitionLink to="/portal/blog/posts/new">
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300">
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              New Post
            </Button>
          </ViewTransitionLink>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4 backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('blog.allPosts', 'All Posts')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder="Search posts..."
                  value={searchInput}
                  onChange={(e) => { setSearchInput(e.target.value); setParams((prev) => ({ ...prev, page: 1 })) }}
                  className="pl-10 w-full sm:w-48"
                  aria-label={t('labels.searchPosts', 'Search posts')}
                />
              </div>
              <Select onValueChange={handleStatusChange} defaultValue="all">
                <SelectTrigger className="w-32 cursor-pointer" aria-label={t('labels.filterByStatus', 'Filter by status')}>
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">All Status</SelectItem>
                  <SelectItem value="Draft" className="cursor-pointer">Draft</SelectItem>
                  <SelectItem value="Published" className="cursor-pointer">Published</SelectItem>
                  <SelectItem value="Scheduled" className="cursor-pointer">Scheduled</SelectItem>
                  <SelectItem value="Archived" className="cursor-pointer">Archived</SelectItem>
                </SelectContent>
              </Select>
              <Select onValueChange={handleCategoryChange} defaultValue="all">
                <SelectTrigger className="w-36 cursor-pointer" aria-label={t('labels.filterByCategory', 'Filter by category')}>
                  <SelectValue placeholder="Category" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">All Categories</SelectItem>
                  {categories.map((cat) => (
                    <SelectItem key={cat.id} value={cat.id} className="cursor-pointer">
                      {cat.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
              {error}
            </div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[40%]">Title</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Category</TableHead>
                  <TableHead>Views</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center py-8">
                      Loading...
                    </TableCell>
                  </TableRow>
                ) : data?.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="p-0">
                      <EmptyState
                        icon={FileText}
                        title="No posts found"
                        description="Get started by creating your first blog post to share with your audience."
                        action={{
                          label: 'New Post',
                          onClick: () => navigate('/portal/blog/posts/new'),
                        }}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  data?.items.map((post) => (
                    <TableRow key={post.id}>
                      <TableCell>
                        <div className="flex items-center gap-3">
                          {/* Featured Image Thumbnail - Click to view full image */}
                          <div style={{ viewTransitionName: `blog-featured-${post.id}` }}>
                            <FilePreviewTrigger
                              file={{
                                url: post.featuredImageUrl ?? '',
                                name: post.title,
                                thumbnailUrl: post.featuredImageThumbnailUrl,
                              }}
                              thumbnailWidth={48}
                              thumbnailHeight={48}
                            />
                          </div>
                          <div className="flex flex-col min-w-0">
                            <span className="font-medium truncate">{post.title}</span>
                            {post.excerpt && (
                              <span className="text-sm text-muted-foreground line-clamp-1">
                                {post.excerpt}
                              </span>
                            )}
                          </div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge className={statusColors[post.status]} variant="secondary">
                          {post.status}
                        </Badge>
                      </TableCell>
                      <TableCell>{post.categoryName || '-'}</TableCell>
                      <TableCell>{post.viewCount.toLocaleString()}</TableCell>
                      <TableCell>
                        {formatDistanceToNow(new Date(post.createdAt), { addSuffix: true })}
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm" className="cursor-pointer">
                              •••
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem className="cursor-pointer" asChild>
                              <ViewTransitionLink to={`/portal/blog/posts/${post.id}`}>
                                <Eye className="h-4 w-4 mr-2" />
                                View
                              </ViewTransitionLink>
                            </DropdownMenuItem>
                            <DropdownMenuItem className="cursor-pointer" asChild>
                              <ViewTransitionLink to={`/portal/blog/posts/${post.id}/edit`}>
                                <Pencil className="h-4 w-4 mr-2" />
                                Edit
                              </ViewTransitionLink>
                            </DropdownMenuItem>
                            {post.status === 'Draft' && (
                              <DropdownMenuItem className="cursor-pointer">
                                <Send className="h-4 w-4 mr-2" />
                                Publish
                              </DropdownMenuItem>
                            )}
                            <DropdownMenuItem
                              className="text-destructive cursor-pointer"
                              onClick={() => setPostToDelete(post)}
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              Delete
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <Pagination
              currentPage={data.page}
              totalPages={data.totalPages}
              totalItems={data.totalCount}
              pageSize={params.pageSize || 10}
              onPageChange={setPage}
              showPageSizeSelector={false}
              className="mt-4"
            />
          )}
        </CardContent>
      </Card>

      {/* Delete Confirmation Dialog */}
      <DeleteBlogPostDialog
        post={postToDelete}
        open={!!postToDelete}
        onOpenChange={(open) => !open && setPostToDelete(null)}
        onConfirm={handleDelete}
      />
    </div>
  )
}

export default BlogPostsPage
