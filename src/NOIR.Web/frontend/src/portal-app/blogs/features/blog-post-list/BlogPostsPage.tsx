import { useState, useDeferredValue, useMemo, useTransition, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, FileText, Plus, Pencil, Trash2, Send, EyeOff, EllipsisVertical, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Checkbox,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
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
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { useSelection } from '@/hooks/useSelection'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'

import { useBlogPostsQuery, useBlogCategoriesQuery, useDeleteBlogPostMutation, useBulkPublishPosts, useBulkUnpublishPosts, useBulkDeletePosts } from '@/portal-app/blogs/queries'
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
  const { hasPermission } = usePermissions()
  usePageContext('Blog Posts')
  const navigate = useNavigate()

  const canPublish = hasPermission(Permissions.BlogPostsPublish)
  const canDelete = hasPermission(Permissions.BlogPostsDelete)

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [isFilterPending, startFilterTransition] = useTransition()
  const [postToDelete, setPostToDelete] = useState<PostListItem | null>(null)
  const [params, setParams] = useState<GetPostsParams>({ page: 1, pageSize: 10 })
  const [showBulkDeleteConfirm, setShowBulkDeleteConfirm] = useState(false)

  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
  const { data, isLoading: loading, error: queryError, refetch } = useBlogPostsQuery(queryParams)
  const { data: categories = [] } = useBlogCategoriesQuery({})
  const deleteMutation = useDeleteBlogPostMutation()
  const error = queryError?.message ?? null

  const posts = data?.items ?? []
  const { selectedIds, setSelectedIds, handleSelectAll, handleSelectNone, handleToggleSelect, isAllSelected } = useSelection(posts)

  const handleCollectionUpdate = useCallback(() => {
    if (selectedIds.size === 0) refetch()
  }, [selectedIds.size, refetch])

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Post',
    onCollectionUpdate: handleCollectionUpdate,
  })

  // Bulk mutation hooks
  const bulkPublishMutation = useBulkPublishPosts()
  const bulkUnpublishMutation = useBulkUnpublishPosts()
  const bulkDeleteMutation = useBulkDeletePosts()
  const [isBulkPending, startBulkTransition] = useTransition()

  // Computed counts for bulk actions
  const selectedDraftCount = posts.filter(p => selectedIds.has(p.id) && p.status === 'Draft').length
  const selectedPublishedCount = posts.filter(p => selectedIds.has(p.id) && p.status === 'Published').length

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
      const message = err instanceof Error ? err.message : t('blog.deletePostFailed', 'Failed to delete post')
      return { success: false, error: message }
    }
  }

  // Bulk action handlers
  const onBulkPublish = () => {
    if (selectedIds.size === 0) return
    const draftPostIds = posts.filter(p => selectedIds.has(p.id) && p.status === 'Draft').map(p => p.id)
    if (draftPostIds.length === 0) {
      toast.warning(t('blog.noDraftPostsSelected', 'No draft posts selected'))
      return
    }
    startBulkTransition(async () => {
      try {
        const result = await bulkPublishMutation.mutateAsync(draftPostIds)
        if (result.failed > 0) {
          toast.warning(t('blog.bulkPublishPartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} published, ${result.failed} failed` }))
        } else {
          toast.success(t('blog.bulkPublishSuccess', { count: result.success, defaultValue: `${result.success} posts published` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('blog.bulkPublishFailed', 'Failed to publish posts'))
      }
      setSelectedIds(new Set())
    })
  }

  const onBulkUnpublish = () => {
    if (selectedIds.size === 0) return
    const publishedPostIds = posts.filter(p => selectedIds.has(p.id) && p.status === 'Published').map(p => p.id)
    if (publishedPostIds.length === 0) {
      toast.warning(t('blog.noPublishedPostsSelected', 'No published posts selected'))
      return
    }
    startBulkTransition(async () => {
      try {
        const result = await bulkUnpublishMutation.mutateAsync(publishedPostIds)
        if (result.failed > 0) {
          toast.warning(t('blog.bulkUnpublishPartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} unpublished, ${result.failed} failed` }))
        } else {
          toast.success(t('blog.bulkUnpublishSuccess', { count: result.success, defaultValue: `${result.success} posts unpublished` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('blog.bulkUnpublishFailed', 'Failed to unpublish posts'))
      }
      setSelectedIds(new Set())
    })
  }

  const onBulkDelete = () => {
    if (selectedIds.size === 0) return
    setShowBulkDeleteConfirm(true)
  }

  const handleBulkDeleteConfirm = () => {
    const selectedPostIds = Array.from(selectedIds)
    startBulkTransition(async () => {
      try {
        const result = await bulkDeleteMutation.mutateAsync(selectedPostIds)
        if (result.failed > 0) {
          toast.warning(t('blog.bulkDeletePartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} deleted, ${result.failed} failed` }))
        } else {
          toast.success(t('blog.bulkDeleteSuccess', { count: result.success, defaultValue: `${result.success} posts deleted` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('blog.bulkDeleteFailed', 'Failed to delete posts'))
      }
      setSelectedIds(new Set())
      setShowBulkDeleteConfirm(false)
    })
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={FileText}
        title={t('blog.posts')}
        description={t('blog.postsDescription')}
        action={
          <ViewTransitionLink to="/portal/blog/posts/new">
            <Button className="group transition-all duration-300">
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

          {/* Bulk Action Toolbar */}
          <BulkActionToolbar selectedCount={selectedIds.size} onClearSelection={handleSelectNone}>
            {canPublish && selectedDraftCount > 0 && (
              <Button
                variant="outline"
                size="sm"
                onClick={onBulkPublish}
                disabled={isBulkPending}
                className="cursor-pointer text-emerald-600 border-emerald-200 hover:bg-emerald-50 dark:text-emerald-400 dark:border-emerald-800 dark:hover:bg-emerald-950"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Send className="h-4 w-4 mr-2" />}
                {t('blog.publishCount', { count: selectedDraftCount, defaultValue: `Publish (${selectedDraftCount})` })}
              </Button>
            )}
            {canPublish && selectedPublishedCount > 0 && (
              <Button
                variant="outline"
                size="sm"
                onClick={onBulkUnpublish}
                disabled={isBulkPending}
                className="cursor-pointer text-amber-600 border-amber-200 hover:bg-amber-50 dark:text-amber-400 dark:border-amber-800 dark:hover:bg-amber-950"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <EyeOff className="h-4 w-4 mr-2" />}
                {t('blog.unpublishCount', { count: selectedPublishedCount, defaultValue: `Unpublish (${selectedPublishedCount})` })}
              </Button>
            )}
            {canDelete && (
              <Button
                variant="outline"
                size="sm"
                onClick={onBulkDelete}
                disabled={isBulkPending}
                className="cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Trash2 className="h-4 w-4 mr-2" />}
                {t('blog.deleteCount', { count: selectedIds.size, defaultValue: `Delete (${selectedIds.size})` })}
              </Button>
            )}
          </BulkActionToolbar>

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background" />
                  <TableHead className="w-[40px]">
                    <Checkbox
                      checked={isAllSelected}
                      onCheckedChange={(checked) => {
                        if (checked) handleSelectAll()
                        else handleSelectNone()
                      }}
                      aria-label={t('labels.selectAll', 'Select all')}
                      className="cursor-pointer"
                    />
                  </TableHead>
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
                      <TableCell><Skeleton className="h-4 w-4 rounded" /></TableCell>
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
                    <TableCell colSpan={7} className="p-0">
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
                      className={`group transition-colors hover:bg-muted/50 ${selectedIds.size === 0 ? 'cursor-pointer' : ''} ${selectedIds.has(post.id) ? 'bg-primary/5' : ''}`}
                      onClick={() => { if (selectedIds.size === 0) navigate(`/portal/blog/posts/${post.id}/edit`) }}
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
                      <TableCell onClick={(e) => e.stopPropagation()}>
                        <Checkbox
                          checked={selectedIds.has(post.id)}
                          onCheckedChange={() => handleToggleSelect(post.id)}
                          aria-label={t('labels.selectItem', { name: post.title, defaultValue: `Select ${post.title}` })}
                          className="cursor-pointer"
                        />
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

      {/* Bulk Delete Confirmation Dialog */}
      <Credenza open={showBulkDeleteConfirm} onOpenChange={setShowBulkDeleteConfirm}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('blog.bulkDeleteConfirmTitle', { count: selectedIds.size, defaultValue: `Delete ${selectedIds.size} posts` })}</CredenzaTitle>
                <CredenzaDescription>
                  {t('blog.bulkDeleteConfirmDescription', {
                    count: selectedIds.size,
                    defaultValue: `Are you sure you want to delete ${selectedIds.size} posts? This action cannot be undone.`,
                  })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button
              variant="outline"
              onClick={() => setShowBulkDeleteConfirm(false)}
              disabled={isBulkPending}
              className="cursor-pointer"
            >
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleBulkDeleteConfirm}
              disabled={isBulkPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {isBulkPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isBulkPending
                ? t('labels.deleting', 'Deleting...')
                : t('blog.deleteCount', { count: selectedIds.size, defaultValue: `Delete (${selectedIds.size})` })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default BlogPostsPage
