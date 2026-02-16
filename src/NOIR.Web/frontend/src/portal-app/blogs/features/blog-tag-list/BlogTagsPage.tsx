import { useState, useDeferredValue, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Tag, Plus, Pencil, Trash2, MoreHorizontal } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  ColorPopover,
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
} from '@uikit'

import { useBlogTagsQuery, useDeleteBlogTagMutation } from '@/portal-app/blogs/queries'
import { BlogTagDialog } from '../../components/blog-tags/BlogTagDialog'
import { DeleteBlogTagDialog } from '../../components/blog-tags/DeleteBlogTagDialog'
import type { PostTagListItem } from '@/types'

export const BlogTagsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Blog Tags')

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [tagDialogOpen, setTagDialogOpen] = useState(false)
  const [tagToEdit, setTagToEdit] = useState<PostTagListItem | null>(null)
  const [tagToDelete, setTagToDelete] = useState<PostTagListItem | null>(null)

  const queryParams = useMemo(() => ({ search: deferredSearch || undefined }), [deferredSearch])
  const { data = [], isLoading: loading, error: queryError, refetch: refresh } = useBlogTagsQuery(queryParams)
  const deleteMutation = useDeleteBlogTagMutation()
  const error = queryError?.message ?? null

  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete tag'
      return { success: false, error: message }
    }
  }

  const handleCreateClick = () => {
    setTagToEdit(null)
    setTagDialogOpen(true)
  }

  const handleEditClick = (tag: PostTagListItem) => {
    setTagToEdit(tag)
    setTagDialogOpen(true)
  }

  const handleDialogSuccess = () => {
    refresh()
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <PageHeader
        icon={Tag}
        title={t('blogTags.title', 'Tags')}
        description={t('blogTags.description', 'Label and organize your content')}
        action={
          <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={handleCreateClick}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('blogTags.newTag', 'New Tag')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4 backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('blogTags.allTags', 'All Tags')}</CardTitle>
              <CardDescription>
                {data ? t('blogTags.totalCount', { count: data.length, defaultValue: `${data.length} tags` }) : ''}
              </CardDescription>
            </div>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={t('blogTags.searchPlaceholder', 'Search tags...')}
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="pl-10 w-full sm:w-64"
                aria-label={t('blogTags.searchTags', 'Search tags')}
              />
            </div>
          </div>
        </CardHeader>
        <CardContent className={isSearchStale ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
              {error}
            </div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[35%]">{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('labels.slug', 'Slug')}</TableHead>
                  <TableHead className="text-center">{t('blogTags.posts', 'Posts')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Skeleton className="h-4 w-4 rounded-full" />
                          <Skeleton className="h-4 w-28" />
                        </div>
                      </TableCell>
                      <TableCell><Skeleton className="h-5 w-24 rounded" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-8 w-8 rounded ml-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : data.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={4} className="p-0">
                      <EmptyState
                        icon={Tag}
                        title={t('blogTags.noTagsFound', 'No tags found')}
                        description={t('blogTags.noTagsDescription', 'Get started by creating your first tag to label and organize your content.')}
                        action={{
                          label: t('blogTags.newTag', 'New Tag'),
                          onClick: handleCreateClick,
                        }}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  data.map((tag) => (
                    <TableRow key={tag.id} className="group transition-colors hover:bg-muted/50">
                      <TableCell>
                        <div className="flex items-center gap-2.5">
                          {tag.color ? (
                            <ColorPopover color={tag.color} />
                          ) : (
                            <div className="w-4 h-4 rounded-full bg-muted border border-border shrink-0" />
                          )}
                          <span className="font-medium">{tag.name}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <code className="text-sm text-muted-foreground bg-muted px-1.5 py-0.5 rounded">{tag.slug}</code>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">{tag.postCount}</Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: tag.name, defaultValue: `Actions for ${tag.name}` })}
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem
                              className="cursor-pointer"
                              onClick={() => handleEditClick(tag)}
                            >
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
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      {/* Create/Edit Tag Dialog */}
      <BlogTagDialog
        open={tagDialogOpen}
        onOpenChange={setTagDialogOpen}
        tag={tagToEdit}
        onSuccess={handleDialogSuccess}
      />

      {/* Delete Confirmation Dialog */}
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
