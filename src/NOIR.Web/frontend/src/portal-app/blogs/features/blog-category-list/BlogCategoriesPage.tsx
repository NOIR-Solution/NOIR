import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FolderTree, Plus, Pencil, Trash2, List, GitBranch } from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { createActionsColumn } from '@/lib/table/columnHelpers'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  CategoryTreeView,
  DataTable,
  DataTableColumnHeader,
  DataTableToolbar,
  DropdownMenuItem,
  EmptyState,
  PageHeader,
  ViewModeToggle,
  type ViewModeOption,
  type TreeCategory,
  type ReorderItem,
} from '@uikit'

import { useBlogCategoriesQuery, useDeleteBlogCategoryMutation, useReorderBlogCategoriesMutation } from '@/portal-app/blogs/queries'
import { BlogCategoryDialog } from '../../components/blog-categories/BlogCategoryDialog'
import { DeleteBlogCategoryDialog } from '../../components/blog-categories/DeleteBlogCategoryDialog'

import type { PostCategoryListItem } from '@/types'

const toTreeCategory = (category: PostCategoryListItem): TreeCategory & PostCategoryListItem => ({
  ...category,
  itemCount: category.postCount,
})

const ch = createColumnHelper<PostCategoryListItem>()

export const BlogCategoriesPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Blog Categories')

  const { params, searchInput, setSearchInput, isSearchStale } = useTableParams({ defaultPageSize: 1000 })
  const queryParams = useMemo(() => ({ search: params.search }), [params.search])
  const { data = [], isLoading, error: queryError, refetch: refresh } = useBlogCategoriesQuery(queryParams)
  const { editItem: categoryToEdit, openEdit: openEditCategory, closeEdit: closeEditCategory } = useUrlEditDialog<PostCategoryListItem>(data)
  const deleteMutation = useDeleteBlogCategoryMutation()
  const reorderMutation = useReorderBlogCategoriesMutation()
  const error = queryError?.message ?? null

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-blog-category' })
  const [parentIdForCreate, setParentIdForCreate] = useState<string | null>(null)
  const [categoryToDelete, setCategoryToDelete] = useState<PostCategoryListItem | null>(null)
  const [viewMode, setViewMode] = useState<'table' | 'tree'>('tree')

  const viewModeOptions: ViewModeOption<'table' | 'tree'>[] = useMemo(() => [
    { value: 'table', label: t('labels.list', 'List'), icon: List, ariaLabel: t('labels.tableView', 'Table view') },
    { value: 'tree', label: t('labels.tree', 'Tree'), icon: GitBranch, ariaLabel: t('labels.treeView', 'Tree view') },
  ], [t])

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'PostCategory',
    onCollectionUpdate: refresh,
  })

  const handleReorder = (items: ReorderItem[]) => {
    reorderMutation.mutate({
      items: items.map(i => ({
        categoryId: i.id,
        parentId: i.parentId,
        sortOrder: i.sortOrder,
      })),
    })
  }

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('blog.deleteCategoryFailed', 'Failed to delete category')
      return { success: false, error: message }
    }
  }

  const handleDialogSuccess = () => refresh()

  const columns = useMemo((): ColumnDef<PostCategoryListItem, unknown>[] => [
    createActionsColumn<PostCategoryListItem>((category) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => openEditCategory(category)}>
          <Pencil className="h-4 w-4 mr-2" />
          {t('labels.edit', 'Edit')}
        </DropdownMenuItem>
        <DropdownMenuItem
          className="text-destructive cursor-pointer"
          onClick={() => setCategoryToDelete(category)}
        >
          <Trash2 className="h-4 w-4 mr-2" />
          {t('labels.delete', 'Delete')}
        </DropdownMenuItem>
      </>
    )),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      meta: { label: t('labels.name', 'Name') },
      cell: ({ getValue }) => <span className="font-medium">{getValue()}</span>,
    }) as ColumnDef<PostCategoryListItem, unknown>,
    ch.accessor('slug', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.slug', 'Slug')} />,
      meta: { label: t('labels.slug', 'Slug') },
      cell: ({ getValue }) => <code className="text-sm bg-muted px-1.5 py-0.5 rounded">{getValue()}</code>,
    }) as ColumnDef<PostCategoryListItem, unknown>,
    ch.accessor('parentName', {
      id: 'parent',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('blogCategories.parent', 'Parent')} />,
      meta: { label: t('blogCategories.parent', 'Parent') },
      cell: ({ getValue }) => getValue() || '-',
    }) as ColumnDef<PostCategoryListItem, unknown>,
    ch.accessor('postCount', {
      id: 'posts',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('blogCategories.posts', 'Posts')} />,
      meta: { label: t('blogCategories.posts', 'Posts') },
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<PostCategoryListItem, unknown>,
    ch.accessor('childCount', {
      id: 'children',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('blogCategories.children', 'Children')} />,
      meta: { label: t('blogCategories.children', 'Children') },
      cell: ({ getValue }) => (getValue() ?? 0) > 0 ? <Badge variant="outline">{getValue()}</Badge> : null,
    }) as ColumnDef<PostCategoryListItem, unknown>,
    ch.accessor('sortOrder', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.sortOrder', 'Sort Order')} />,
      meta: { label: t('labels.sortOrder', 'Sort Order') },
    }) as ColumnDef<PostCategoryListItem, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data,
    columns,
    rowCount: data.length,
    tableKey: 'blog-categories',
    manualSorting: false,
    state: {
      pagination: { pageIndex: 0, pageSize: 1000 },
      sorting: [],
    },
    onPaginationChange: () => {},
    getRowId: (row) => row.id,
  })

  const treeCategories = data.map(toTreeCategory)

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={FolderTree}
        title={t('blogCategories.title', 'Categories')}
        description={t('blogCategories.description', 'Organize your blog posts')}
        responsive
        action={
          <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('blogCategories.newCategory', 'New Category')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg">{t('blogCategories.allCategories', 'All Categories')}</CardTitle>
                <CardDescription>
                  {data.length > 0 ? t('labels.showingCountOfTotal', { count: data.length, total: data.length }) : ''}
                </CardDescription>
              </div>
              <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('blogCategories.searchPlaceholder', 'Search categories...')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              density={settings.density}
              onDensityChange={setDensity}
              showColumnToggle={viewMode === 'table'}
            />
          </div>
        </CardHeader>
        <CardContent className={isSearchStale ? 'space-y-3 opacity-70 transition-opacity duration-200' : 'space-y-3 transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          {viewMode === 'tree' ? (
            <div className="rounded-xl border border-border/50 p-4">
              <CategoryTreeView
                maxHeight="100%"
                categories={treeCategories}
                loading={isLoading}
                onEdit={(cat) => openEditCategory(cat as PostCategoryListItem)}
                onDelete={(cat) => setCategoryToDelete(cat as PostCategoryListItem)}
                onAddChild={(cat) => { setParentIdForCreate((cat as PostCategoryListItem).id); openCreate() }}
                canEdit={true}
                canDelete={true}
                itemCountLabel={t('labels.posts', 'posts')}
                emptyMessage={t('blogCategories.noCategoriesFound', 'No categories found')}
                emptyDescription={t('blogCategories.noCategoriesDescription', 'Get started by creating your first category to organize your blog posts.')}
                onCreateClick={() => { setParentIdForCreate(null); openCreate() }}
                onReorder={handleReorder}
              />
            </div>
          ) : (
            <div className="space-y-3">
              <DataTable
                table={table}
                density={settings.density}
                isLoading={isLoading}
                isStale={isSearchStale}
                onRowClick={openEditCategory}
                emptyState={
                  <EmptyState
                    icon={FolderTree}
                    title={t('blogCategories.noCategoriesFound', 'No categories found')}
                    description={t('blogCategories.noCategoriesDescription', 'Get started by creating your first category to organize your blog posts.')}
                    action={{ label: t('blogCategories.newCategory', 'New Category'), onClick: () => openCreate() }}
                  />
                }
              />
            </div>
          )}
        </CardContent>
      </Card>

      <BlogCategoryDialog
        open={isCreateOpen || !!categoryToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (categoryToEdit) closeEditCategory()
            setParentIdForCreate(null)
          }
        }}
        category={categoryToEdit}
        parentId={!categoryToEdit ? parentIdForCreate : null}
        onSuccess={handleDialogSuccess}
      />

      <DeleteBlogCategoryDialog
        category={categoryToDelete}
        open={!!categoryToDelete}
        onOpenChange={(open) => !open && setCategoryToDelete(null)}
        onConfirm={handleDelete}
      />
    </div>
  )
}

export default BlogCategoriesPage
