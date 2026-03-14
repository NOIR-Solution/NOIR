import { useState, useMemo, useTransition, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { FileText, Plus, Pencil, Trash2, Send, EyeOff, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable, useSelectedIds } from '@/hooks/useEnterpriseTable'
import { createSelectColumn, createActionsColumn } from '@/lib/table/columnHelpers'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  EmptyState,
  FilePreviewTrigger,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'

import { useBlogPostsQuery, useBlogCategoriesQuery, useDeleteBlogPostMutation, useBulkPublishPosts, useBulkUnpublishPosts, useBulkDeletePosts } from '@/portal-app/blogs/queries'
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

const ch = createColumnHelper<PostListItem>()

export const BlogPostsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Blog Posts')
  const navigate = useNavigate()

  const canPublish = hasPermission(Permissions.BlogPostsPublish)
  const canDelete = hasPermission(Permissions.BlogPostsDelete)

  const [postToDelete, setPostToDelete] = useState<PostListItem | null>(null)
  const [showBulkDeleteConfirm, setShowBulkDeleteConfirm] = useState(false)
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [categoryFilter, setCategoryFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()
  const [isBulkPending, startBulkTransition] = useTransition()

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    setSorting,
    setPage,
    setPageSize,
    defaultPageSize,
  } = useTableParams({ defaultPageSize: 10, tableKey: 'blog-posts' })

  const queryParams = useMemo(() => ({
    ...params,
    status: statusFilter !== 'all' ? statusFilter as PostStatus : undefined,
    categoryId: categoryFilter !== 'all' ? categoryFilter : undefined,
  }), [params, statusFilter, categoryFilter])

  const { data, isLoading, error: queryError, refetch: refresh } = useBlogPostsQuery(queryParams)
  const { data: categories = [] } = useBlogCategoriesQuery({})
  const deleteMutation = useDeleteBlogPostMutation()
  const bulkPublishMutation = useBulkPublishPosts()
  const bulkUnpublishMutation = useBulkUnpublishPosts()
  const bulkDeleteMutation = useBulkDeletePosts()

  const tableData = useMemo(() => data?.items ?? [], [data?.items])

  const handleStatusChange = (value: string) => {
    startFilterTransition(() => {
      setStatusFilter(value)
      setPage(1)
    })
  }

  const handleCategoryChange = (value: string) => {
    startFilterTransition(() => {
      setCategoryFilter(value)
      setPage(1)
    })
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

  const onBulkPublish = () => {
    const currentRowSelection = table.getState().rowSelection
    const draftPostIds = tableData.filter(p => currentRowSelection[p.id] && p.status === 'Draft').map(p => p.id)
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
      table.resetRowSelection()
    })
  }

  const onBulkUnpublish = () => {
    const currentRowSelection = table.getState().rowSelection
    const publishedPostIds = tableData.filter(p => currentRowSelection[p.id] && p.status === 'Published').map(p => p.id)
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
      table.resetRowSelection()
    })
  }

  const columns = useMemo((): ColumnDef<PostListItem, unknown>[] => [
    createActionsColumn<PostListItem>((post) => (
      <>
        <DropdownMenuItem className="cursor-pointer" asChild>
          <ViewTransitionLink to={`/portal/blog/posts/${post.id}/edit`}>
            <Pencil className="h-4 w-4 mr-2" />
            {t('buttons.edit')}
          </ViewTransitionLink>
        </DropdownMenuItem>
        <DropdownMenuItem
          className="text-destructive cursor-pointer"
          onClick={() => setPostToDelete(post)}
        >
          <Trash2 className="h-4 w-4 mr-2" />
          {t('buttons.delete')}
        </DropdownMenuItem>
      </>
    )),
    createSelectColumn<PostListItem>(),
    ch.accessor('title', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('blog.titleColumn', 'Title')} />,
      meta: { label: t('blog.titleColumn', 'Title') },
      cell: ({ row }) => {
        const post = row.original
        return (
          <div className="flex items-center gap-3">
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
                <span className="text-sm text-muted-foreground line-clamp-1">{post.excerpt}</span>
              )}
            </div>
          </div>
        )
      },
    }) as ColumnDef<PostListItem, unknown>,
    ch.accessor('status', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status')} />,
      meta: { label: t('labels.status') },
      size: 110,
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getStatusBadgeClasses(statusColors[getValue()])}>
          {t(`blog.status.${getValue().toLowerCase()}`)}
        </Badge>
      ),
    }) as ColumnDef<PostListItem, unknown>,
    ch.accessor('categoryName', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.category')} />,
      meta: { label: t('labels.category') },
      cell: ({ getValue }) => getValue() || '-',
    }) as ColumnDef<PostListItem, unknown>,
    ch.accessor('viewCount', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('blog.views', 'Views')} />,
      meta: { align: 'center', label: t('blog.views', 'Views') },
      size: 80,
      cell: ({ getValue }) => getValue().toLocaleString(),
    }) as ColumnDef<PostListItem, unknown>,
    ch.accessor('createdAt', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.created')} />,
      meta: { label: t('labels.created') },
      size: 130,
      cell: ({ getValue }) => formatDistanceToNow(new Date(getValue()), { addSuffix: true }),
    }) as ColumnDef<PostListItem, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: tableData,
    columns,
    rowCount: data?.totalCount ?? 0,
    enableRowSelection: true,
    tableKey: 'blog-posts',
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: params.sorting as SortingState,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function'
        ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize })
        : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    onSortingChange: setSorting,
    getRowId: (row) => row.id,
  })

  const currentRowSelection = table.getState().rowSelection
  const selectedIds = useSelectedIds(currentRowSelection)
  const selectedCount = selectedIds.length

  const selectedDraftCount = useMemo(
    () => tableData.filter(p => currentRowSelection[p.id] && p.status === 'Draft').length,
    [tableData, currentRowSelection],
  )
  const selectedPublishedCount = useMemo(
    () => tableData.filter(p => currentRowSelection[p.id] && p.status === 'Published').length,
    [tableData, currentRowSelection],
  )

  const handleCollectionUpdate = useCallback(() => {
    if (selectedCount === 0) refresh()
  }, [selectedCount, refresh])

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Post',
    onCollectionUpdate: handleCollectionUpdate,
  })

  const handleBulkDeleteConfirm = () => {
    const postIds = [...selectedIds]
    startBulkTransition(async () => {
      try {
        const result = await bulkDeleteMutation.mutateAsync(postIds)
        if (result.failed > 0) {
          toast.warning(t('blog.bulkDeletePartial', { success: result.success, failed: result.failed, defaultValue: `${result.success} deleted, ${result.failed} failed` }))
        } else {
          toast.success(t('blog.bulkDeleteSuccess', { count: result.success, defaultValue: `${result.success} posts deleted` }))
        }
      } catch (err) {
        toast.error(err instanceof Error ? err.message : t('blog.bulkDeleteFailed', 'Failed to delete posts'))
      }
      table.resetRowSelection()
      setShowBulkDeleteConfirm(false)
    })
  }

  if (queryError) {
    console.error(queryError)
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

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('blog.allPosts', 'All Posts')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('blog.searchPlaceholder')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              density={settings.density}
              onDensityChange={setDensity}
              filterSlot={
                <>
                  <Select value={statusFilter} onValueChange={handleStatusChange}>
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
                  <Select value={categoryFilter} onValueChange={handleCategoryChange}>
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
                </>
              }
            />
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <BulkActionToolbar selectedCount={selectedCount} onClearSelection={() => table.resetRowSelection()}>
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
                onClick={() => setShowBulkDeleteConfirm(true)}
                disabled={isBulkPending}
                className="cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
              >
                {isBulkPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Trash2 className="h-4 w-4 mr-2" />}
                {t('blog.deleteCount', { count: selectedCount, defaultValue: `Delete (${selectedCount})` })}
              </Button>
            )}
          </BulkActionToolbar>

          <DataTable
            table={table}
            density={settings.density}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={selectedCount === 0 ? (post) => navigate(`/portal/blog/posts/${post.id}/edit`) : undefined}
            emptyState={
              <EmptyState
                icon={FileText}
                title={t('blog.noPostsFound', 'No posts found')}
                description={t('blog.noPostsDescription', 'Get started by creating your first blog post to share with your audience.')}
                action={{
                  label: t('blog.newPost'),
                  onClick: () => navigate('/portal/blog/posts/new'),
                }}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      <DeleteBlogPostDialog
        post={postToDelete}
        open={!!postToDelete}
        onOpenChange={(open) => !open && setPostToDelete(null)}
        onConfirm={handleDelete}
      />

      <Credenza open={showBulkDeleteConfirm} onOpenChange={setShowBulkDeleteConfirm}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('blog.bulkDeleteConfirmTitle', { count: selectedCount, defaultValue: `Delete ${selectedCount} posts` })}</CredenzaTitle>
                <CredenzaDescription>
                  {t('blog.bulkDeleteConfirmDescription', {
                    count: selectedCount,
                    defaultValue: `Are you sure you want to delete ${selectedCount} posts? This action cannot be undone.`,
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
                : t('blog.deleteCount', { count: selectedCount, defaultValue: `Delete (${selectedCount})` })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default BlogPostsPage
