import { useState, useDeferredValue, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, FolderTree, Plus, Pencil, Trash2, List, GitBranch, MoreHorizontal } from 'lucide-react'
import { cn } from '@/lib/utils'
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  type TreeCategory,
  type ReorderItem,
} from '@uikit'

import { usePageContext } from '@/hooks/usePageContext'

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
  const [categoryDialogOpen, setCategoryDialogOpen] = useState(false)
  const [categoryToEdit, setCategoryToEdit] = useState<PostCategoryListItem | null>(null)
  const [categoryToDelete, setCategoryToDelete] = useState<PostCategoryListItem | null>(null)
  const [viewMode, setViewMode] = useState<'table' | 'tree'>('tree')

  const queryParams = useMemo(() => ({ search: deferredSearch || undefined }), [deferredSearch])
  const { data = [], isLoading: loading, error: queryError, refetch: refresh } = useBlogCategoriesQuery(queryParams)
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

  const handleCreateClick = () => {
    setCategoryToEdit(null)
    setCategoryDialogOpen(true)
  }

  const handleEditClick = (category: PostCategoryListItem) => {
    setCategoryToEdit(category)
    setCategoryDialogOpen(true)
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
          <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={handleCreateClick}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('blogCategories.newCategory', 'New Category')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4 backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('blogCategories.allCategories', 'All Categories')}</CardTitle>
              <CardDescription>
                {data ? t('blogCategories.totalCount', { count: data.length, defaultValue: `${data.length} categories` }) : ''}
              </CardDescription>
            </div>
            <div className="flex items-center gap-3">
              {/* View Toggle */}
              <div className="flex items-center gap-1 p-1 rounded-lg bg-muted">
                <Button
                  variant={viewMode === 'table' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setViewMode('table')}
                  className={cn(
                    'cursor-pointer h-8 px-3 transition-all duration-200',
                    viewMode === 'table'
                      ? 'shadow-sm'
                      : 'text-muted-foreground hover:text-foreground',
                  )}
                  aria-label={t('labels.tableView', 'Table view')}
                >
                  <List className="h-4 w-4" />
                </Button>
                <Button
                  variant={viewMode === 'tree' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setViewMode('tree')}
                  className={cn(
                    'cursor-pointer h-8 px-3 transition-all duration-200',
                    viewMode === 'tree'
                      ? 'shadow-sm'
                      : 'text-muted-foreground hover:text-foreground',
                  )}
                  aria-label={t('labels.treeView', 'Tree view')}
                >
                  <GitBranch className="h-4 w-4" />
                </Button>
              </div>

              {/* Search */}
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('blogCategories.searchPlaceholder', 'Search categories...')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-10 w-full sm:w-64"
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
                onEdit={(cat) => handleEditClick(cat as PostCategoryListItem)}
                onDelete={(cat) => setCategoryToDelete(cat as PostCategoryListItem)}
                canEdit={true}
                canDelete={true}
                itemCountLabel={t('labels.posts', 'posts')}
                emptyMessage={t('blogCategories.noCategoriesFound', 'No categories found')}
                emptyDescription={t('blogCategories.noCategoriesDescription', 'Get started by creating your first category to organize your blog posts.')}
                onCreateClick={handleCreateClick}
                onReorder={handleReorder}
              />
            </div>
          ) : (
          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[30%]">{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('labels.slug', 'Slug')}</TableHead>
                  <TableHead>{t('blogCategories.parent', 'Parent')}</TableHead>
                  <TableHead>{t('blogCategories.posts', 'Posts')}</TableHead>
                  <TableHead>{t('blogCategories.children', 'Children')}</TableHead>
                  <TableHead>{t('labels.sortOrder', 'Sort Order')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  <TableRow>
                    <TableCell colSpan={7} className="text-center py-8">
                      {t('labels.loading', 'Loading...')}
                    </TableCell>
                  </TableRow>
                ) : data.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="p-0">
                      <EmptyState
                        icon={FolderTree}
                        title={t('blogCategories.noCategoriesFound', 'No categories found')}
                        description={t('blogCategories.noCategoriesDescription', 'Get started by creating your first category to organize your blog posts.')}
                        action={{
                          label: t('blogCategories.newCategory', 'New Category'),
                          onClick: handleCreateClick,
                        }}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  data.map((category) => (
                    <TableRow key={category.id}>
                      <TableCell className="font-medium">{category.name}</TableCell>
                      <TableCell>
                        <code className="text-sm text-muted-foreground">{category.slug}</code>
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
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0"
                              aria-label={t('labels.actionsFor', { name: category.name, defaultValue: `Actions for ${category.name}` })}
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem
                              className="cursor-pointer"
                              onClick={() => handleEditClick(category)}
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
        open={categoryDialogOpen}
        onOpenChange={setCategoryDialogOpen}
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
