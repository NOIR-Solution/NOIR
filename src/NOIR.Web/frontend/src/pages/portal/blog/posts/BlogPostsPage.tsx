import { useState } from 'react'
import { Search, FileText, Plus, Eye, Pencil, Trash2, Send } from 'lucide-react'
import { ImageLightbox } from '@/components/ui/image-lightbox'
import { usePageContext } from '@/hooks/usePageContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Pagination } from '@/components/ui/pagination'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { usePosts, useCategories } from '@/hooks/useBlog'
import { DeletePostDialog } from './components/DeletePostDialog'
import type { PostListItem, PostStatus } from '@/types'
import { formatDistanceToNow } from 'date-fns'
import { Link } from 'react-router-dom'

const statusColors: Record<PostStatus, string> = {
  Draft: 'bg-gray-100 text-gray-800',
  Published: 'bg-green-100 text-green-800',
  Scheduled: 'bg-blue-100 text-blue-800',
  Archived: 'bg-yellow-100 text-yellow-800',
}

export default function BlogPostsPage() {
  usePageContext('Blog Posts')

  const { data, loading, error, setPage, setSearch, setStatus, setCategoryId, handleDelete, params } = usePosts()
  const { data: categories } = useCategories()

  const [searchInput, setSearchInput] = useState('')
  const [postToDelete, setPostToDelete] = useState<PostListItem | null>(null)

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setSearch(searchInput)
  }

  const handleStatusChange = (value: string) => {
    setStatus(value === 'all' ? undefined : (value as PostStatus))
  }

  const handleCategoryChange = (value: string) => {
    setCategoryId(value === 'all' ? undefined : value)
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-primary/10 rounded-xl shadow-sm">
            <FileText className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Blog Posts</h1>
            <p className="text-muted-foreground">Manage your blog content</p>
          </div>
        </div>
        <Link to="/portal/blog/posts/new">
          <Button>
            <Plus className="h-4 w-4 mr-2" />
            New Post
          </Button>
        </Link>
      </div>

      <Card className="shadow-sm hover:shadow-md transition-shadow duration-300">
        <CardHeader className="pb-4 backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>All Posts</CardTitle>
              <CardDescription>
                {data ? `Showing ${data.items.length} of ${data.totalCount} posts` : ''}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <form onSubmit={handleSearchSubmit} className="flex items-center gap-2">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    placeholder="Search posts..."
                    value={searchInput}
                    onChange={(e) => setSearchInput(e.target.value)}
                    className="pl-10 w-full sm:w-48"
                  />
                </div>
                <Button type="submit" variant="secondary" size="sm">
                  Search
                </Button>
              </form>
              <Select onValueChange={handleStatusChange} defaultValue="all">
                <SelectTrigger className="w-32 cursor-pointer">
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">All Status</SelectItem>
                  <SelectItem value="Draft" className="cursor-pointer">Draft</SelectItem>
                  <SelectItem value="Published" className="cursor-pointer">Published</SelectItem>
                  <SelectItem value="Scheduled" className="cursor-pointer">Scheduled</SelectItem>
                  <SelectItem value="Archived" className="cursor-pointer">Archived</SelectItem>
                </SelectContent>
              </Select>
              <Select onValueChange={handleCategoryChange} defaultValue="all">
                <SelectTrigger className="w-36 cursor-pointer">
                  <SelectValue placeholder="Category" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">All Categories</SelectItem>
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
                  <TableHead className="w-[40%]">Title</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Category</TableHead>
                  <TableHead>Views</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center py-8">
                      Loading...
                    </TableCell>
                  </TableRow>
                ) : data?.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center py-8 text-muted-foreground">
                      No posts found
                    </TableCell>
                  </TableRow>
                ) : (
                  data?.items.map((post) => (
                    <TableRow key={post.id}>
                      <TableCell>
                        <div className="flex items-center gap-3">
                          {/* Featured Image Thumbnail - Click to view full image */}
                          <ImageLightbox
                            src={post.featuredImageUrl ?? ''}
                            thumbnailSrc={post.featuredImageThumbnailUrl}
                            alt={post.title}
                            thumbnailWidth={48}
                            thumbnailHeight={48}
                          />
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
                        <Badge className={statusColors[post.status]} variant="secondary">
                          {post.status}
                        </Badge>
                      </TableCell>
                      <TableCell>{post.categoryName || '-'}</TableCell>
                      <TableCell>{post.viewCount.toLocaleString()}</TableCell>
                      <TableCell>
                        {formatDistanceToNow(new Date(post.createdAt), { addSuffix: true })}
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm" className="cursor-pointer">
                              •••
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem className="cursor-pointer" asChild>
                              <Link to={`/portal/blog/posts/${post.id}`}>
                                <Eye className="h-4 w-4 mr-2" />
                                View
                              </Link>
                            </DropdownMenuItem>
                            <DropdownMenuItem className="cursor-pointer" asChild>
                              <Link to={`/portal/blog/posts/${post.id}/edit`}>
                                <Pencil className="h-4 w-4 mr-2" />
                                Edit
                              </Link>
                            </DropdownMenuItem>
                            {post.status === 'Draft' && (
                              <DropdownMenuItem className="cursor-pointer">
                                <Send className="h-4 w-4 mr-2" />
                                Publish
                              </DropdownMenuItem>
                            )}
                            <DropdownMenuItem
                              className="text-destructive cursor-pointer"
                              onClick={() => setPostToDelete(post)}
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
      <DeletePostDialog
        post={postToDelete}
        open={!!postToDelete}
        onOpenChange={(open) => !open && setPostToDelete(null)}
        onConfirm={handleDelete}
      />
    </div>
  )
}
