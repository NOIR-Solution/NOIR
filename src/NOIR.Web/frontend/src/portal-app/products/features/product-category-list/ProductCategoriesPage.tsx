import { useState, useDeferredValue, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, FolderTree, Plus, Pencil, Trash2, ChevronRight, EllipsisVertical, List, GitBranch, Tags } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
  Input,
  PageHeader,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
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

// Adapter to map ProductCategoryListItem to TreeCategory
const toTreeCategory = (category: ProductCategoryListItem): TreeCategory & ProductCategoryListItem => {
  return {
    ...category,
    itemCount: category.productCount,
  }
}

export const ProductCategoriesPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Product Categories')

  // Permission checks
  const canCreateCategories = hasPermission(Permissions.ProductCategoriesCreate)
  const canUpdateCategories = hasPermission(Permissions.ProductCategoriesUpdate)
  const canDeleteCategories = hasPermission(Permissions.ProductCategoriesDelete)

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-product-category' })
  const [categoryToDelete, setCategoryToDelete] = useState<ProductCategoryListItem | null>(null)
  const [categoryToManageAttributes, setCategoryToManageAttributes] = useState<ProductCategoryListItem | null>(null)
  const [viewMode, setViewMode] = useState<'table' | 'tree'>('tree')
  const viewModeOptions: ViewModeOption<'table' | 'tree'>[] = useMemo(() => [
    { value: 'table', label: t('labels.list', 'List'), icon: List, ariaLabel: t('labels.tableView', 'Table view') },
    { value: 'tree', label: t('labels.tree', 'Tree'), icon: GitBranch, ariaLabel: t('labels.treeView', 'Tree view') },
  ], [t])

  const queryParams = useMemo(() => ({ search: deferredSearch || undefined }), [deferredSearch])
  const { data: categories = [], isLoading: loading, error: queryError, refetch: refresh } = useProductCategoriesQuery(queryParams)
  const { editItem: categoryToEdit, openEdit: openEditCategory, closeEdit: closeEditCategory } = useUrlEditDialog<ProductCategoryListItem>(categories)
  const deleteMutation = useDeleteProductCategory()
  const reorderMutation = useReorderProductCategories()
  const error = queryError?.message ?? null

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete category'
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

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
  }

  // Map categories to tree format
  const treeCategories = categories.map(toTreeCategory)

  return (
    <div className="space-y-6">
      <PageHeader
        icon={FolderTree}
        title={t('categories.title', 'Product Categories')}
        description={t('categories.description', 'Organize products into categories')}
        responsive
        action={
          canCreateCategories && (
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('categories.newCategory', 'New Category')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>{t('categories.allCategories', 'All Categories')}</CardTitle>
                <CardDescription>
                  {t('categories.totalCount', { count: categories.length, defaultValue: `${categories.length} categories total` })}
                </CardDescription>
              </div>
              <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {/* Search */}
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('categories.searchPlaceholder', 'Search categories...')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-9 h-9"
                  aria-label={t('categories.searchCategories', 'Search categories')}
                />
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent className={isSearchStale ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
              {error}
            </div>
          )}

          {viewMode === 'tree' ? (
            <div className="rounded-xl border border-border/50 p-4">
              <CategoryTreeView
                categories={treeCategories}
                loading={loading}
                onEdit={(cat) => openEditCategory(cat as ProductCategoryListItem)}
                onDelete={(cat) => setCategoryToDelete(cat as ProductCategoryListItem)}
                canEdit={canUpdateCategories}
                canDelete={canDeleteCategories}
                itemCountLabel={t('labels.products', 'products')}
                emptyMessage={t('categories.noCategoriesFound', 'No categories found')}
                emptyDescription={t('categories.noCategoriesDescription', 'Get started by creating your first category to organize products.')}
                onCreateClick={canCreateCategories ? () => openCreate() : undefined}
                onReorder={canUpdateCategories ? handleReorder : undefined}
              />
            </div>
          ) : (
          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                  <TableHead className="w-[40%]">{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('labels.slug', 'Slug')}</TableHead>
                  <TableHead>{t('categories.parent', 'Parent')}</TableHead>
                  <TableHead className="text-center">{t('categories.products', 'Products')}</TableHead>
                  <TableHead className="text-center">{t('categories.children', 'Children')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  // Skeleton loading - 21st.dev pattern
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Skeleton className="h-4 w-4" />
                          <Skeleton className="h-4 w-32" />
                        </div>
                      </TableCell>
                      <TableCell><Skeleton className="h-5 w-24 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                    </TableRow>
                  ))
                ) : categories.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="p-0">
                      <EmptyState
                        icon={FolderTree}
                        title={t('categories.noCategoriesFound', 'No categories found')}
                        description={t('categories.noCategoriesDescription', 'Get started by creating your first category to organize products.')}
                        action={canCreateCategories ? {
                          label: t('categories.addCategory', 'Add Category'),
                          onClick: () => openCreate(),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  categories.map((category) => (
                    <TableRow
                      key={category.id}
                      className={`group transition-colors hover:bg-muted/50${canUpdateCategories ? ' cursor-pointer' : ''}`}
                      onClick={canUpdateCategories ? (e) => {
                        if ((e.target as HTMLElement).closest('[data-no-row-click]')) return
                        openEditCategory(category)
                      } : undefined}
                    >
                      <TableCell className="sticky left-0 z-10 bg-background" data-no-row-click>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: category.name, defaultValue: `Actions for ${category.name}` })}
                            >
                              <EllipsisVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="start">
                            {canUpdateCategories && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={() => openEditCategory(category)}
                              >
                                <Pencil className="h-4 w-4 mr-2" />
                                {t('labels.edit', 'Edit')}
                              </DropdownMenuItem>
                            )}
                            {canUpdateCategories && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={() => setCategoryToManageAttributes(category)}
                              >
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
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <FolderTree className="h-4 w-4 text-muted-foreground" />
                          <span className="font-medium">{category.name}</span>
                        </div>
                        {category.description && (
                          <p className="text-sm text-muted-foreground line-clamp-1 mt-1 ml-6">
                            {category.description}
                          </p>
                        )}
                      </TableCell>
                      <TableCell>
                        <code className="text-sm bg-muted px-1.5 py-0.5 rounded">
                          {category.slug}
                        </code>
                      </TableCell>
                      <TableCell>
                        {category.parentName ? (
                          <div className="flex items-center gap-1 text-sm text-muted-foreground">
                            <ChevronRight className="h-3 w-3" />
                            {category.parentName}
                          </div>
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">{category.productCount}</Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        {category.childCount > 0 ? (
                          <Badge variant="outline">{category.childCount}</Badge>
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Category Dialog */}
      <ProductCategoryDialog
        open={isCreateOpen || !!categoryToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (categoryToEdit) closeEditCategory()
          }
        }}
        category={categoryToEdit}
        categories={categories}
        onSuccess={refresh}
      />

      {/* Delete Confirmation Dialog */}
      <DeleteProductCategoryDialog
        category={categoryToDelete}
        open={!!categoryToDelete}
        onOpenChange={(open) => !open && setCategoryToDelete(null)}
        onConfirm={handleDelete}
      />

      {/* Category Attributes Dialog */}
      <ProductCategoryAttributesDialog
        open={!!categoryToManageAttributes}
        onOpenChange={(open) => !open && setCategoryToManageAttributes(null)}
        category={categoryToManageAttributes}
      />
    </div>
  )
}

export default ProductCategoriesPage
