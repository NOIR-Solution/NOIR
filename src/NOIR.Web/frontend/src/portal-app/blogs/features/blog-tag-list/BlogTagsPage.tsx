import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Tag, Plus, Pencil, Trash2 } from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { useRowHighlight } from '@/hooks/useRowHighlight'
import { createActionsColumn } from '@/lib/table/columnHelpers'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  ColorPopover,
  DataTable,
  DataTableColumnHeader,
  DataTableToolbar,
  DropdownMenuItem,
  EmptyState,
  PageHeader,
} from '@uikit'

import { useBlogTagsQuery, useDeleteBlogTagMutation } from '@/portal-app/blogs/queries'
import { BlogTagDialog } from '../../components/blog-tags/BlogTagDialog'
import { DeleteBlogTagDialog } from '../../components/blog-tags/DeleteBlogTagDialog'
import type { PostTagListItem } from '@/types'

const ch = createColumnHelper<PostTagListItem>()

export const BlogTagsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Blog Tags')

  const { getRowAnimationClass, fadeOutRow } = useRowHighlight()

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-tag' })
  const [tagToDelete, setTagToDelete] = useState<PostTagListItem | null>(null)

  const { params, searchInput, setSearchInput, isSearchStale } = useTableParams({ defaultPageSize: 1000 })
  const queryParams = useMemo(() => ({ search: params.search }), [params.search])
  const { data = [], isLoading, error: queryError, refetch: refresh } = useBlogTagsQuery(queryParams)
  const { editItem: tagToEdit, openEdit: openEditTag, closeEdit: closeEditTag } = useUrlEditDialog<PostTagListItem>(data)
  const deleteMutation = useDeleteBlogTagMutation()

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'PostTag',
    onCollectionUpdate: refresh,
  })

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await fadeOutRow(id)
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('blog.deleteTagFailed', 'Failed to delete tag')
      return { success: false, error: message }
    }
  }

  const columns = useMemo((): ColumnDef<PostTagListItem, unknown>[] => [
    createActionsColumn<PostTagListItem>((tag) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => openEditTag(tag)}>
          <Pencil className="h-4 w-4 mr-2" />
          {t('labels.edit', 'Edit')}
        </DropdownMenuItem>
        <DropdownMenuItem
          className="text-destructive cursor-pointer"
          onClick={() => setTagToDelete(tag)}
        >
          <Trash2 className="h-4 w-4 mr-2" />
          {t('labels.delete', 'Delete')}
        </DropdownMenuItem>
      </>
    )),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      meta: { label: t('labels.name', 'Name') },
      cell: ({ row }) => (
        <div className="flex items-center gap-2.5">
          {row.original.color ? (
            <ColorPopover color={row.original.color} />
          ) : (
            <div className="w-4 h-4 rounded-full bg-muted border border-border shrink-0" />
          )}
          <span className="font-medium">{row.original.name}</span>
        </div>
      ),
    }) as ColumnDef<PostTagListItem, unknown>,
    ch.accessor('slug', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.slug', 'Slug')} />,
      meta: { label: t('labels.slug', 'Slug') },
      cell: ({ getValue }) => (
        <code className="text-sm bg-muted px-1.5 py-0.5 rounded">{getValue()}</code>
      ),
    }) as ColumnDef<PostTagListItem, unknown>,
    ch.accessor('postCount', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('blogTags.posts', 'Posts')} />,
      meta: { align: 'center', label: t('blogTags.posts', 'Posts') },
      size: 80,
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<PostTagListItem, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t])

  const tableData = useMemo(() => data, [data])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: tableData,
    columns,
    tableKey: 'blog-tags',
    rowCount: data.length,
    manualSorting: false,
    state: {
      pagination: { pageIndex: 0, pageSize: 1000 },
      sorting: [],
    },
    onPaginationChange: () => {},
    getRowId: (row) => row.id,
  })

  if (queryError) {
    console.error(queryError)
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Tag}
        title={t('blogTags.title', 'Tags')}
        description={t('blogTags.description', 'Label and organize your content')}
        action={
          <Button className="group transition-all duration-300" onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('blogTags.newTag', 'New Tag')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('blogTags.allTags', 'All Tags')}</CardTitle>
              <CardDescription>
                {t('labels.showingCountOfTotal', { count: data.length, total: data.length })}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('blogTags.searchPlaceholder', 'Search tags...')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              density={settings.density}
              onDensityChange={setDensity}
            />
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <DataTable
            table={table}
            density={settings.density}
            isLoading={isLoading}
            isStale={isSearchStale}
            getRowAnimationClass={getRowAnimationClass}
            onRowClick={openEditTag}
            emptyState={
              <EmptyState
                icon={Tag}
                title={t('blogTags.noTagsFound', 'No tags found')}
                description={t('blogTags.noTagsDescription', 'Get started by creating your first tag to label and organize your content.')}
                action={{ label: t('blogTags.newTag', 'New Tag'), onClick: () => openCreate() }}
              />
            }
          />
        </CardContent>
      </Card>

      <BlogTagDialog
        open={isCreateOpen || !!tagToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (tagToEdit) closeEditTag()
          }
        }}
        tag={tagToEdit}
        onSuccess={() => refresh()}
      />

      <DeleteBlogTagDialog
        tag={tagToDelete}
        open={!!tagToDelete}
        onOpenChange={(open) => !open && setTagToDelete(null)}
        onConfirm={handleDelete}
      />
    </div>
  )
}

export default BlogTagsPage
