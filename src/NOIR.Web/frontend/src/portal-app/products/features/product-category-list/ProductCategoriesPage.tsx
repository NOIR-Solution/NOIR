import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FolderTree, Plus, Pencil, Trash2, ChevronRight, List, GitBranch, Tags } from 'lucide-react'
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
import { usePermissions, Permissions } from '@/hooks/usePermissions'
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

import { useProductCategoriesQuery, useDeleteProductCategory, useReorderProductCategories } from '@/portal-app/products/queries'
import { ProductCategoryDialog } from '../../components/product-categories/ProductCategoryDialog'
import { DeleteProductCategoryDialog } from '../../components/product-categories/DeleteProductCategoryDialog'
import { ProductCategoryAttributesDialog } from '../../components/product-categories/ProductCategoryAttributesDialog'

import type { ProductCategoryListItem } from '@/types/product'

const toTreeCategory = (category: ProductCategoryListItem): TreeCategory & ProductCategoryListItem => ({
  ...category,
  itemCount: category.productCount,
})

const ch = createColumnHelper<ProductCategoryListItem>()

export const ProductCategoriesPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Product Categories')

  const canCreateCategories = hasPermission(Permissions.ProductCategoriesCreate)
  const canUpdateCategories = hasPermission(Permissions.ProductCategoriesUpdate)
  const canDeleteCategories = hasPermission(Permissions.ProductCategoriesDelete)
  const showActions = canUpdateCategories || canDeleteCategories

  const { params, searchInput, setSearchInput, isSearchStale } = useTableParams({ defaultPageSize: 1000 })
  const queryParams = useMemo(() => ({ search: params.search }), [params.search])
  const { data: categories = [], isLoading, error: queryError, refetch: refresh } = useProductCategoriesQuery(queryParams)
  const { editItem: categoryToEdit, openEdit: openEditCategory, closeEdit: closeEditCategory } = useUrlEditDialog<ProductCategoryListItem>(categories)
  const deleteMutation = useDeleteProductCategory()
  const reorderMutation = useReorderProductCategories()

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-product-category' })
  const [parentIdForCreate, setParentIdForCreate] = useState<string | null>(null)
  const [categoryToDelete, setCategoryToDelete] = useState<ProductCategoryListItem | null>(null)
  const [categoryToManageAttributes, setCategoryToManageAttributes] = useState<ProductCategoryListItem | null>(null)
  const [viewMode, setViewMode] = useState<'table' | 'tree'>('tree')

  const viewModeOptions: ViewModeOption<'table' | 'tree'>[] = useMemo(() => [
    { value: 'table', label: t('labels.list', 'List'), icon: List, ariaLabel: t('labels.tableView', 'Table view') },
    { value: 'tree', label: t('labels.tree', 'Tree'), icon: GitBranch, ariaLabel: t('labels.treeView', 'Tree view') },
  ], [t])

  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'ProductCategory',
    onCollectionUpdate: refresh,
  })

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('categories.deleteFailed', 'Failed to delete category')
      return { success: false, error: message }
    }
  }

  const handleReorder = (items: ReorderItem[]) => {
    reorderMutation.mutate({
      items: items.map(i => ({
        categoryId: i.id,
        parentId: i.parentId,
        sortOrder: i.sortOrder,
      })),
    })
  }

  const columns = useMemo((): ColumnDef<ProductCategoryListItem, unknown>[] => [
    ...(showActions ? [
      createActionsColumn<ProductCategoryListItem>((category) => (
        <>
          {canUpdateCategories && (
            <DropdownMenuItem className="cursor-pointer" onClick={() => openEditCategory(category)}>
              <Pencil className="h-4 w-4 mr-2" />
              {t('labels.edit', 'Edit')}
            </DropdownMenuItem>
          )}
          {canUpdateCategories && (
            <DropdownMenuItem className="cursor-pointer" onClick={() => setCategoryToManageAttributes(category)}>
              <Tags className="h-4 w-4 mr-2" />
              {t('categoryAttributes.manageAttributes', 'Manage Attributes')}
            </DropdownMenuItem>
          )}
          {canDeleteCategories && (
            <DropdownMenuItem
              className="text-destructive cursor-pointer"
              onClick={() => setCategoryToDelete(category)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete', 'Delete')}
            </DropdownMenuItem>
          )}
        </>
      )),
    ] : []),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      meta: { label: t('labels.name', 'Name') },
      cell: ({ row }) => (
        <div>
          <div className="flex items-center gap-2">
            <FolderTree className="h-4 w-4 text-muted-foreground shrink-0" />
            <span className="font-medium">{row.original.name}</span>
          </div>
          {row.original.description && (
            <p className="text-sm text-muted-foreground line-clamp-1 mt-1 ml-6">
              {row.original.description}
            </p>
          )}
        </div>
      ),
    }) as ColumnDef<ProductCategoryListItem, unknown>,
    ch.accessor('slug', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.slug', 'Slug')} />,
      meta: { label: t('labels.slug', 'Slug') },
      cell: ({ getValue }) => <code className="text-sm bg-muted px-1.5 py-0.5 rounded">{getValue()}</code>,
    }) as ColumnDef<ProductCategoryListItem, unknown>,
    ch.accessor('parentName', {
      id: 'parent',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('categories.parent', 'Parent')} />,
      meta: { label: t('categories.parent', 'Parent') },
      cell: ({ getValue }) =>
        getValue() ? (
          <div className="flex items-center gap-1 text-sm text-muted-foreground">
            <ChevronRight className="h-3 w-3" />
            {getValue()}
          </div>
        ) : (
          <span className="text-muted-foreground">—</span>
        ),
    }) as ColumnDef<ProductCategoryListItem, unknown>,
    ch.accessor('productCount', {
      id: 'products',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('categories.products', 'Products')} />,
      meta: { label: t('categories.products', 'Products'), align: 'center' },
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<ProductCategoryListItem, unknown>,
    ch.accessor('childCount', {
      id: 'children',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('categories.children', 'Children')} />,
      meta: { label: t('categories.children', 'Children'), align: 'center' },
      cell: ({ getValue }) =>
        (getValue() ?? 0) > 0 ? (
          <Badge variant="outline">{getValue()}</Badge>
        ) : (
          <span className="text-muted-foreground">—</span>
        ),
    }) as ColumnDef<ProductCategoryListItem, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canUpdateCategories, canDeleteCategories, showActions])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: categories,
    columns,
    tableKey: 'product-categories',
    rowCount: categories.length,
    manualSorting: false,
    state: {
      pagination: { pageIndex: 0, pageSize: 1000 },
      sorting: [],
    },
    onPaginationChange: () => {},
    getRowId: (row) => row.id,
  })

  const treeCategories = categories.map(toTreeCategory)

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={FolderTree}
        title={t('categories.title', 'Product Categories')}
        description={t('categories.description', 'Organize products into categories')}
        responsive
        action={
          canCreateCategories && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('categories.newCategory', 'New Category')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg">{t('categories.allCategories', 'All Categories')}</CardTitle>
                <CardDescription>
                  {categories.length > 0 ? t('labels.showingCountOfTotal', { count: categories.length, total: categories.length }) : ''}
                </CardDescription>
              </div>
              <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('categories.searchPlaceholder', 'Search categories...')}
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
                onEdit={(cat) => openEditCategory(cat as ProductCategoryListItem)}
                onDelete={(cat) => setCategoryToDelete(cat as ProductCategoryListItem)}
                onAddChild={canCreateCategories ? (cat) => { setParentIdForCreate((cat as ProductCategoryListItem).id); openCreate() } : undefined}
                canEdit={canUpdateCategories}
                canDelete={canDeleteCategories}
                itemCountLabel={t('labels.products', 'products')}
                emptyMessage={t('categories.noCategoriesFound', 'No categories found')}
                emptyDescription={t('categories.noCategoriesDescription', 'Get started by creating your first category to organize products.')}
                onCreateClick={canCreateCategories ? () => { setParentIdForCreate(null); openCreate() } : undefined}
                onReorder={canUpdateCategories ? handleReorder : undefined}
              />
            </div>
          ) : (
            <div className="space-y-3">
              <DataTable
                table={table}
                density={settings.density}
                isLoading={isLoading}
                isStale={isSearchStale}
                onRowClick={canUpdateCategories ? openEditCategory : undefined}
                emptyState={
                  <EmptyState
                    icon={FolderTree}
                    title={t('categories.noCategoriesFound', 'No categories found')}
                    description={t('categories.noCategoriesDescription', 'Get started by creating your first category to organize products.')}
                    action={canCreateCategories ? { label: t('categories.addCategory', 'Add Category'), onClick: () => openCreate() } : undefined}
                  />
                }
              />
            </div>
          )}
        </CardContent>
      </Card>

      <ProductCategoryDialog
        open={isCreateOpen || !!categoryToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (categoryToEdit) closeEditCategory()
            setParentIdForCreate(null)
          }
        }}
        category={categoryToEdit}
        categories={categories}
        parentId={!categoryToEdit ? parentIdForCreate : null}
        onSuccess={refresh}
      />

      <DeleteProductCategoryDialog
        category={categoryToDelete}
        open={!!categoryToDelete}
        onOpenChange={(open) => !open && setCategoryToDelete(null)}
        onConfirm={handleDelete}
      />

      <ProductCategoryAttributesDialog
        open={!!categoryToManageAttributes}
        onOpenChange={(open) => !open && setCategoryToManageAttributes(null)}
        category={categoryToManageAttributes}
      />
    </div>
  )
}

export default ProductCategoriesPage
