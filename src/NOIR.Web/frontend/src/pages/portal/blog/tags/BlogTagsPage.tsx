import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Tag, Plus, Pencil, Trash2 } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useTags } from '@/hooks/useBlog'
import { TagDialog } from './components/TagDialog'
import { DeleteTagDialog } from './components/DeleteTagDialog'
import type { PostTagListItem } from '@/types'

export default function BlogTagsPage() {
  const { t } = useTranslation('common')
  usePageContext('Blog Tags')

  const { data, loading, error, refresh, setSearch, handleDelete, params } = useTags()

  const [searchInput, setSearchInput] = useState('')
  const [tagDialogOpen, setTagDialogOpen] = useState(false)
  const [tagToEdit, setTagToEdit] = useState<PostTagListItem | null>(null)
  const [tagToDelete, setTagToDelete] = useState<PostTagListItem | null>(null)

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setSearch(searchInput)
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
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-primary/10 rounded-lg">
            <Tag className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Tags</h1>
            <p className="text-muted-foreground">Label and organize your content</p>
          </div>
        </div>
        <Button onClick={handleCreateClick}>
          <Plus className="h-4 w-4 mr-2" />
          New Tag
        </Button>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>All Tags</CardTitle>
              <CardDescription>
                {data ? `${data.length} tags` : ''}
              </CardDescription>
            </div>
            <form onSubmit={handleSearchSubmit} className="flex items-center gap-2">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder="Search tags..."
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  className="pl-10 w-full sm:w-64"
                />
              </div>
              <Button type="submit" variant="secondary">
                Search
              </Button>
            </form>
          </div>
        </CardHeader>
        <CardContent>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
              {error}
            </div>
          )}

          <div className="rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[30%]">Name</TableHead>
                  <TableHead>Slug</TableHead>
                  <TableHead>Color</TableHead>
                  <TableHead>Posts</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center py-8">
                      Loading...
                    </TableCell>
                  </TableRow>
                ) : data.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                      No tags found
                    </TableCell>
                  </TableRow>
                ) : (
                  data.map((tag) => (
                    <TableRow key={tag.id}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          {tag.color && (
                            <div
                              className="w-3 h-3 rounded-full"
                              style={{ backgroundColor: tag.color }}
                            />
                          )}
                          <span className="font-medium">{tag.name}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <code className="text-sm text-muted-foreground">{tag.slug}</code>
                      </TableCell>
                      <TableCell>
                        {tag.color ? (
                          <code className="text-xs">{tag.color}</code>
                        ) : (
                          <span className="text-muted-foreground">-</span>
                        )}
                      </TableCell>
                      <TableCell>
                        <Badge variant="secondary">{tag.postCount}</Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm" className="cursor-pointer">
                              •••
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem
                              className="cursor-pointer"
                              onClick={() => handleEditClick(tag)}
                            >
                              <Pencil className="h-4 w-4 mr-2" />
                              Edit
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              className="text-destructive cursor-pointer"
                              onClick={() => setTagToDelete(tag)}
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              Delete
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
      <TagDialog
        open={tagDialogOpen}
        onOpenChange={setTagDialogOpen}
        tag={tagToEdit}
        onSuccess={handleDialogSuccess}
      />

      {/* Delete Confirmation Dialog */}
      <DeleteTagDialog
        tag={tagToDelete}
        open={!!tagToDelete}
        onOpenChange={(open) => !open && setTagToDelete(null)}
        onConfirm={handleDelete}
      />
    </div>
  )
}
