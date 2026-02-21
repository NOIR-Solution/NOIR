import { useState, useDeferredValue, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, FolderTree, Plus, Pencil, Trash2, List, GitBranch, EllipsisVertical } from 'lucide-react'
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

import { usePageContext } from '@/hooks/usePageContext'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'

import { useBlogCategoriesQuery, useDeleteBlogCategoryMutation, useReorderBlogCategoriesMutation } from '@/portal-app/blogs/queries'
import { BlogCategoryDialog } from '../../components/blog-categories/BlogCategoryDialog'
import { DeleteBlogCategoryDialog } from '../../components/blog-categories/DeleteBlogCategoryDialog'

import type { PostCategoryListItem } from '@/types'

// Adapter to map PostCategoryListItem to TreeCategory
const toTreeCategory = (category: PostCategoryListItem): TreeCategory & PostCategoryListItem => {
  return {
    ...category,
    itemCount: category.postCount,
  }
}

export const BlogCategoriesPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Blog Categories')

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-blog-category' })
  const [categoryToDelete, setCategoryToDelete] = useState<PostCategoryListItem | null>(null)
  const [viewMode, setViewMode] = useState<'table' | 'tree'>('tree')
  const viewModeOptions: ViewModeOption<'table' | 'tree'>[] = useMemo(() => [
    { value: 'table', label: t('labels.list', 'List'), icon: List, ariaLabel: t('labels.tableView', 'Table view') },
    { value: 'tree', label: t('labels.tree', 'Tree'), icon: GitBranch, ariaLabel: t('labels.treeView', 'Tree view') },
  ], [t])

  const queryParams = useMemo(() => ({ search: deferredSearch || undefined }), [deferredSearch])
  const { data = [], isLoading: loading, error: queryError, refetch: refresh } = useBlogCategoriesQuery(queryParams)
  const { editItem: categoryToEdit, openEdit: openEditCategory, closeEdit: closeEditCategory } = useUrlEditDialog<PostCategoryListItem>(data)
  const deleteMutation = useDeleteBlogCategoryMutation()
  const reorderMutation = useReorderBlogCategoriesMutation()
  const error = queryError?.message ?? null

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
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

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete category'
      return { success: false, error: message }
    }
  }

  const handleDialogSuccess = () => {
    refresh()
  }

  // Map categories to tree format
  const treeCategories = data.map(toTreeCategory)

  return (
    <div className="space-y-6">
      <PageHeader
        icon={FolderTree}
        title={t('blogCategories.title', 'Categories')}
        description={t('blogCategories.description', 'Organize your blog posts')}
        action={
          <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('blogCategories.newCategory', 'New Category')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>{t('blogCategories.allCategories', 'All Categories')}</CardTitle>
                <CardDescription>
                  {data ? t('blogCategories.totalCount', { count: data.length, defaultValue: `${data.length} categories` }) : ''}
                </CardDescription>
              </div>
              <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {/* Search */}
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('blogCategories.searchPlaceholder', 'Search categories...')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-9 h-9"
                  aria-label={t('blogCategories.searchCategories', 'Search categories')}
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
                onEdit={(cat) => openEditCategory(cat as PostCategoryListItem)}
                onDelete={(cat) => setCategoryToDelete(cat as PostCategoryListItem)}
                canEdit={true}
                canDelete={true}
                itemCountLabel={t('labels.posts', 'posts')}
                emptyMessage={t('blogCategories.noCategoriesFound', 'No categories found')}
                emptyDescription={t('blogCategories.noCategoriesDescription', 'Get started by creating your first category to organize your blog posts.')}
                onCreateClick={() => openCreate()}
                onReorder={handleReorder}
              />
            </div>
          ) : (
          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background" />
                  <TableHead className="w-[30%]">{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('labels.slug', 'Slug')}</TableHead>
                  <TableHead>{t('blogCategories.parent', 'Parent')}</TableHead>
                  <TableHead>{t('blogCategories.posts', 'Posts')}</TableHead>
                  <TableHead>{t('blogCategories.children', 'Children')}</TableHead>
                  <TableHead>{t('labels.sortOrder', 'Sort Order')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  Array.from({ length: 5 }).map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-24 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-8 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-8 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                    </TableRow>
                  ))
                ) : data.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="p-0">
                      <EmptyState
                        icon={FolderTree}
                        title={t('blogCategories.noCategoriesFound', 'No categories found')}
                        description={t('blogCategories.noCategoriesDescription', 'Get started by creating your first category to organize your blog posts.')}
                        action={{
                          label: t('blogCategories.newCategory', 'New Category'),
                          onClick: () => openCreate(),
                        }}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  data.map((category) => (
                    <TableRow key={category.id} className="group transition-colors hover:bg-muted/50">
                      <TableCell className="sticky left-0 z-10 bg-background">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0"
                              aria-label={t('labels.actionsFor', { name: category.name, defaultValue: `Actions for ${category.name}` })}
                            >
                              <EllipsisVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="start">
                            <DropdownMenuItem
                              className="cursor-pointer"
                              onClick={() => openEditCategory(category)}
                            >
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
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                      <TableCell className="font-medium">{category.name}</TableCell>
                      <TableCell>
                        <code className="text-sm bg-muted px-1.5 py-0.5 rounded">{category.slug}</code>
                      </TableCell>
                      <TableCell>{category.parentName || '-'}</TableCell>
                      <TableCell>
                        <Badge variant="secondary">{category.postCount}</Badge>
                      </TableCell>
                      <TableCell>
                        {category.childCount > 0 && (
                          <Badge variant="outline">{category.childCount}</Badge>
                        )}
                      </TableCell>
                      <TableCell>{category.sortOrder}</TableCell>
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
      <BlogCategoryDialog
        open={isCreateOpen || !!categoryToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (categoryToEdit) closeEditCategory()
          }
        }}
        category={categoryToEdit}
        onSuccess={handleDialogSuccess}
      />

      {/* Delete Confirmation Dialog */}
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
