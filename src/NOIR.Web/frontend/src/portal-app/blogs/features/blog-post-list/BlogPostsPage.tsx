import { useState, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, FileText, Plus, Pencil, Trash2, Send, EllipsisVertical } from 'lucide-react'
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
  Skeleton,
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
import { getStatusBadgeClasses } from '@/utils/statusBadge'

const statusColors: Record<PostStatus, 'gray' | 'green' | 'blue' | 'yellow'> = {
  Draft: 'gray',
  Published: 'green',
  Scheduled: 'blue',
  Archived: 'yellow',
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
    <div className="space-y-6">
      <PageHeader
        icon={FileText}
        title={t('blog.posts')}
        description={t('blog.postsDescription')}
        action={
          <ViewTransitionLink to="/portal/blog/posts/new">
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300">
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('blog.newPost')}
            </Button>
          </ViewTransitionLink>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('blog.allPosts', 'All Posts')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {/* Search */}
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('blog.searchPlaceholder')}
                  value={searchInput}
                  onChange={(e) => { setSearchInput(e.target.value); setParams((prev) => ({ ...prev, page: 1 })) }}
                  className="pl-9 h-9"
                  aria-label={t('labels.searchPosts')}
                />
              </div>
              <Select onValueChange={handleStatusChange} defaultValue="all">
                <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('blog.filterByStatus')}>
                  <SelectValue placeholder={t('labels.status')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('labels.allStatus')}</SelectItem>
                  <SelectItem value="Draft" className="cursor-pointer">{t('blog.status.draft')}</SelectItem>
                  <SelectItem value="Published" className="cursor-pointer">{t('blog.status.published')}</SelectItem>
                  <SelectItem value="Scheduled" className="cursor-pointer">{t('blog.status.scheduled')}</SelectItem>
                  <SelectItem value="Archived" className="cursor-pointer">{t('blog.status.archived')}</SelectItem>
                </SelectContent>
              </Select>
              <Select onValueChange={handleCategoryChange} defaultValue="all">
                <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('blog.filterByCategory')}>
                  <SelectValue placeholder={t('labels.category')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('labels.allCategories')}</SelectItem>
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
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background" />
                  <TableHead className="w-[40%]">{t('blog.titleColumn', 'Title')}</TableHead>
                  <TableHead>{t('labels.status')}</TableHead>
                  <TableHead>{t('labels.category')}</TableHead>
                  <TableHead>{t('blog.views', 'Views')}</TableHead>
                  <TableHead>{t('labels.created')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  // Skeleton loading
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell>
                        <div className="flex items-center gap-3">
                          <Skeleton className="h-12 w-12 rounded-lg flex-shrink-0" />
                          <div className="space-y-1.5">
                            <Skeleton className="h-4 w-40" />
                            <Skeleton className="h-3 w-24" />
                          </div>
                        </div>
                      </TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                    </TableRow>
                  ))
                ) : data?.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="p-0">
                      <EmptyState
                        icon={FileText}
                        title={t('blog.noPostsFound', 'No posts found')}
                        description={t('blog.noPostsDescription', 'Get started by creating your first blog post to share with your audience.')}
                        action={{
                          label: t('blog.newPost'),
                          onClick: () => navigate('/portal/blog/posts/new'),
                        }}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  data?.items.map((post) => (
                    <TableRow
                      key={post.id}
                      className="group transition-colors hover:bg-muted/50 cursor-pointer"
                      onClick={() => navigate(`/portal/blog/posts/${post.id}/edit`)}
                    >
                      <TableCell className="sticky left-0 z-10 bg-background" onClick={(e) => e.stopPropagation()}>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: post.title, defaultValue: `Actions for ${post.title}` })}
                            >
                              <EllipsisVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="start">
                            <DropdownMenuItem className="cursor-pointer" asChild>
                              <ViewTransitionLink to={`/portal/blog/posts/${post.id}/edit`}>
                                <Pencil className="h-4 w-4 mr-2" />
                                {t('buttons.edit')}
                              </ViewTransitionLink>
                            </DropdownMenuItem>
                            {post.status === 'Draft' && (
                              <DropdownMenuItem className="cursor-pointer opacity-50" disabled>
                                <Send className="h-4 w-4 mr-2" />
                                {t('buttons.publish')}
                              </DropdownMenuItem>
                            )}
                            <DropdownMenuItem
                              className="text-destructive cursor-pointer"
                              onClick={() => setPostToDelete(post)}
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              {t('buttons.delete')}
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
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
                        <Badge variant="outline" className={getStatusBadgeClasses(statusColors[post.status])}>
                          {t(`blog.status.${post.status.toLowerCase()}`)}
                        </Badge>
                      </TableCell>
                      <TableCell>{post.categoryName || '-'}</TableCell>
                      <TableCell>{post.viewCount.toLocaleString()}</TableCell>
                      <TableCell>
                        {formatDistanceToNow(new Date(post.createdAt), { addSuffix: true })}
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
