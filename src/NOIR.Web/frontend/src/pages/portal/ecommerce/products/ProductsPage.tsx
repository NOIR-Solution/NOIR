import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  Search,
  Package,
  Plus,
  Eye,
  Pencil,
  Trash2,
  Send,
  Archive,
  MoreHorizontal,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Filter,
  LayoutGrid,
  List,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Pagination } from '@/components/ui/pagination'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/ui/empty-state'
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
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useProducts, useProductCategories } from '@/hooks/useProducts'
import { DeleteProductDialog } from './components/DeleteProductDialog'
import { ProductStatsCards } from './components/ProductStatsCards'
import { EnhancedProductGridView } from './components/EnhancedProductGridView'
import type { ProductListItem, ProductStatus } from '@/types/product'
import { formatDistanceToNow } from 'date-fns'
import { toast } from 'sonner'
import { formatCurrency } from '@/lib/utils/currency'
import { PRODUCT_STATUS_CONFIG, DEFAULT_PRODUCT_PAGE_SIZE } from '@/lib/constants/product'

export default function ProductsPage() {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Products')
  const navigate = useNavigate()

  // Permission checks
  const canCreateProducts = hasPermission(Permissions.ProductsCreate)
  const canUpdateProducts = hasPermission(Permissions.ProductsUpdate)
  const canDeleteProducts = hasPermission(Permissions.ProductsDelete)
  const canPublishProducts = hasPermission(Permissions.ProductsPublish)

  const {
    data,
    stats,
    loading,
    error,
    setPage,
    setSearch,
    setStatus,
    setCategoryId,
    setInStockOnly,
    handleDelete,
    handlePublish,
    handleArchive,
    params,
  } = useProducts()
  const { data: categories } = useProductCategories()

  const [searchInput, setSearchInput] = useState('')
  const [productToDelete, setProductToDelete] = useState<ProductListItem | null>(null)
  const [showFilters, setShowFilters] = useState(false)
  const [viewMode, setViewMode] = useState<'table' | 'grid'>('table')

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setSearch(searchInput)
  }

  const handleStatusChange = (value: string) => {
    setStatus(value === 'all' ? undefined : (value as ProductStatus))
  }

  const handleCategoryChange = (value: string) => {
    setCategoryId(value === 'all' ? undefined : value)
  }

  const handleStockFilterChange = (value: string) => {
    setInStockOnly(value === 'inStock' ? true : undefined)
  }

  const onPublish = async (product: ProductListItem) => {
    const result = await handlePublish(product.id)
    if (result.success) {
      toast.success(`Product "${product.name}" published successfully`)
    } else {
      toast.error(result.error || 'Failed to publish product')
    }
  }

  const onArchive = async (product: ProductListItem) => {
    const result = await handleArchive(product.id)
    if (result.success) {
      toast.success(`Product "${product.name}" archived successfully`)
    } else {
      toast.error(result.error || 'Failed to archive product')
    }
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      {/* Page Header with Glassmorphism */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div className="flex items-center gap-4">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br from-primary/20 to-primary/10 shadow-lg shadow-primary/20 backdrop-blur-sm border border-primary/20 transition-all duration-300 hover:shadow-xl hover:shadow-primary/30 hover:scale-105">
            <Package className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight bg-gradient-to-r from-foreground to-foreground/70 bg-clip-text text-transparent">
              {t('products.title', 'Products')}
            </h1>
            <p className="text-sm text-muted-foreground mt-1">{t('products.description', 'Manage your product catalog')}</p>
          </div>
        </div>
        {canCreateProducts && (
          <Link to="/portal/ecommerce/products/new">
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300">
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('products.newProduct', 'New Product')}
            </Button>
          </Link>
        )}
      </div>

      {/* Stats Dashboard */}
      <ProductStatsCards
        stats={stats}
        hasActiveFilters={!!(params.search || params.status || params.categoryId || params.inStockOnly)}
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-border/50 backdrop-blur-sm bg-card/95">
        <CardHeader className="pb-4 space-y-4">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle className="text-xl">{t('products.allProducts', 'All Products')}</CardTitle>
              <CardDescription className="text-sm">
                {data ? t('labels.showingOfItems', { showing: data.items.length, total: data.totalCount, defaultValue: `Showing ${data.items.length} of ${data.totalCount} products` }) : t('labels.loading', 'Loading...')}
              </CardDescription>
            </div>
            <div className="flex items-center gap-2">
              {/* View Toggle */}
              <div className="flex items-center gap-1 p-1 rounded-lg bg-muted">
                <Button
                  variant={viewMode === 'table' ? 'secondary' : 'ghost'}
                  size="sm"
                  onClick={() => setViewMode('table')}
                  className="cursor-pointer h-8 px-3"
                >
                  <List className="h-4 w-4" />
                </Button>
                <Button
                  variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
                  size="sm"
                  onClick={() => setViewMode('grid')}
                  className="cursor-pointer h-8 px-3"
                >
                  <LayoutGrid className="h-4 w-4" />
                </Button>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
                className="sm:hidden cursor-pointer"
              >
                <Filter className="h-4 w-4 mr-2" />
                Filters
              </Button>
            </div>
          </div>

          {/* Filters - Responsive Design */}
          <div className={`flex flex-col gap-3 ${showFilters ? 'block' : 'hidden sm:flex'} sm:flex-row sm:items-center sm:flex-wrap`}>
            <form onSubmit={handleSearchSubmit} className="flex items-center gap-2 flex-1 min-w-[200px]">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground pointer-events-none" />
                <Input
                  placeholder={t('products.searchPlaceholder', 'Search products...')}
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  className="pl-10 transition-all duration-200 focus:ring-2 focus:ring-primary/20"
                  aria-label={t('products.searchProducts', 'Search products')}
                />
              </div>
              <Button type="submit" variant="secondary" size="sm" className="cursor-pointer">
                {t('labels.search', 'Search')}
              </Button>
            </form>

            <Select onValueChange={handleStatusChange} defaultValue="all">
              <SelectTrigger className="w-full sm:w-36 cursor-pointer transition-all duration-200 hover:border-primary/50">
                <SelectValue placeholder={t('labels.status', 'Status')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" className="cursor-pointer">{t('labels.allStatus', 'All Status')}</SelectItem>
                <SelectItem value="Draft" className="cursor-pointer">{t('products.status.draft', 'Draft')}</SelectItem>
                <SelectItem value="Active" className="cursor-pointer">{t('products.status.active', 'Active')}</SelectItem>
                <SelectItem value="Archived" className="cursor-pointer">{t('products.status.archived', 'Archived')}</SelectItem>
                <SelectItem value="OutOfStock" className="cursor-pointer">{t('products.status.outOfStock', 'Out of Stock')}</SelectItem>
              </SelectContent>
            </Select>

            <Select onValueChange={handleCategoryChange} defaultValue="all">
              <SelectTrigger className="w-full sm:w-40 cursor-pointer transition-all duration-200 hover:border-primary/50">
                <SelectValue placeholder={t('labels.category', 'Category')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" className="cursor-pointer">{t('labels.allCategories', 'All Categories')}</SelectItem>
                {categories.map((cat) => (
                  <SelectItem key={cat.id} value={cat.id} className="cursor-pointer">
                    {cat.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select onValueChange={handleStockFilterChange} defaultValue="all">
              <SelectTrigger className="w-full sm:w-36 cursor-pointer transition-all duration-200 hover:border-primary/50">
                <SelectValue placeholder={t('labels.stock', 'Stock')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" className="cursor-pointer">{t('labels.allStock', 'All Stock')}</SelectItem>
                <SelectItem value="inStock" className="cursor-pointer">{t('products.inStock', 'In Stock')}</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardHeader>

        <CardContent>
          {error && (
            <div className="mb-4 p-4 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive animate-in fade-in-0 slide-in-from-top-2 duration-300">
              <p className="text-sm font-medium">{error}</p>
            </div>
          )}

          {viewMode === 'grid' ? (
            // Grid View
            loading ? (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                {[...Array(8)].map((_, i) => (
                  <div key={i} className="animate-pulse">
                    <div className="aspect-square bg-muted rounded-xl mb-4" />
                    <div className="space-y-3">
                      <Skeleton className="h-4 w-3/4" />
                      <Skeleton className="h-3 w-1/2" />
                      <Skeleton className="h-6 w-full" />
                    </div>
                  </div>
                ))}
              </div>
            ) : data?.items.length === 0 ? (
              <EmptyState
                icon={Package}
                title={t('products.noProductsFound', 'No products found')}
                description={t('products.noProductsDescription', 'Get started by creating your first product to build your catalog.')}
                action={canCreateProducts ? {
                  label: t('products.addProduct', 'Add Product'),
                  onClick: () => navigate('/portal/ecommerce/products/new'),
                } : undefined}
                className="border-0 rounded-none py-12"
              />
            ) : (
              <EnhancedProductGridView
                products={data?.items || []}
                onDelete={canDeleteProducts ? setProductToDelete : undefined}
                onPublish={canPublishProducts ? onPublish : undefined}
                onArchive={canUpdateProducts ? onArchive : undefined}
                canEdit={canUpdateProducts}
                canDelete={canDeleteProducts}
                canPublish={canPublishProducts}
              />
            )
          ) : (
            // Table View
            <div className="rounded-xl border border-border/50 overflow-hidden">
              <Table>
              <TableHeader>
                <TableRow className="bg-muted/50 hover:bg-muted/50">
                  <TableHead className="w-[35%] font-semibold">{t('products.product', 'Product')}</TableHead>
                  <TableHead className="font-semibold">{t('labels.status', 'Status')}</TableHead>
                  <TableHead className="font-semibold">{t('labels.category', 'Category')}</TableHead>
                  <TableHead className="text-right font-semibold">{t('products.price', 'Price')}</TableHead>
                  <TableHead className="text-right font-semibold">{t('labels.stock', 'Stock')}</TableHead>
                  <TableHead className="font-semibold">{t('labels.created', 'Created')}</TableHead>
                  <TableHead className="text-right font-semibold">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  // Enhanced loading skeletons
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell>
                        <div className="flex items-center gap-3">
                          <Skeleton className="h-14 w-14 rounded-xl" />
                          <div className="space-y-2 flex-1">
                            <Skeleton className="h-4 w-3/4" />
                            <Skeleton className="h-3 w-1/2" />
                          </div>
                        </div>
                      </TableCell>
                      <TableCell><Skeleton className="h-6 w-20 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-20 ml-auto" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-12 ml-auto" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-9 w-9 rounded-lg ml-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : data?.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="p-0">
                      <EmptyState
                        icon={Package}
                        title={t('products.noProductsFound', 'No products found')}
                        description={t('products.noProductsDescription', 'Get started by creating your first product to build your catalog.')}
                        action={canCreateProducts ? {
                          label: t('products.addProduct', 'Add Product'),
                          onClick: () => navigate('/portal/ecommerce/products/new'),
                        } : undefined}
                        className="border-0 rounded-none py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  data?.items.map((product) => {
                    const status = PRODUCT_STATUS_CONFIG[product.status]
                    const StatusIcon = status.icon

                    return (
                      <TableRow
                        key={product.id}
                        className="group transition-all duration-200 hover:bg-muted/30"
                      >
                        <TableCell>
                          <div className="flex items-center gap-3">
                            {/* Enhanced product image with animation */}
                            <div className="relative h-14 w-14 rounded-xl border-2 border-border/50 bg-muted overflow-hidden flex-shrink-0 transition-all duration-300 group-hover:border-primary/50 group-hover:shadow-lg group-hover:shadow-primary/10">
                              {product.primaryImageUrl ? (
                                <img
                                  src={product.primaryImageUrl}
                                  alt={product.name}
                                  className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-110"
                                />
                              ) : (
                                <div className="h-full w-full flex items-center justify-center bg-gradient-to-br from-muted to-muted/50">
                                  <Package className="h-6 w-6 text-muted-foreground/50" />
                                </div>
                              )}
                            </div>
                            <div className="flex flex-col min-w-0">
                              <span className="font-medium truncate group-hover:text-primary transition-colors duration-200">
                                {product.name}
                              </span>
                              {product.sku && (
                                <span className="text-xs text-muted-foreground font-mono">
                                  SKU: {product.sku}
                                </span>
                              )}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge
                            className={`${status.color} border transition-all duration-200 hover:scale-105`}
                            variant="secondary"
                          >
                            <StatusIcon className="h-3 w-3 mr-1.5" />
                            {status.label}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <span className="text-sm">{product.categoryName || '—'}</span>
                        </TableCell>
                        <TableCell className="text-right">
                          <span className="font-semibold text-foreground">
                            {formatCurrency(product.basePrice, product.currency)}
                          </span>
                        </TableCell>
                        <TableCell className="text-right">
                          <Badge
                            variant={product.inStock ? 'default' : 'destructive'}
                            className="transition-all duration-200 hover:scale-105"
                          >
                            {product.totalStock}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <span className="text-sm text-muted-foreground">
                            {formatDistanceToNow(new Date(product.createdAt), { addSuffix: true })}
                          </span>
                        </TableCell>
                        <TableCell className="text-right">
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button
                                variant="ghost"
                                size="sm"
                                className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                                aria-label={`Actions for ${product.name}`}
                              >
                                <MoreHorizontal className="h-4 w-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end" className="w-48">
                              <DropdownMenuItem className="cursor-pointer" asChild>
                                <Link to={`/portal/ecommerce/products/${product.id}`}>
                                  <Eye className="h-4 w-4 mr-2" />
                                  {t('labels.viewDetails', 'View Details')}
                                </Link>
                              </DropdownMenuItem>
                              {canUpdateProducts && (
                                <DropdownMenuItem className="cursor-pointer" asChild>
                                  <Link to={`/portal/ecommerce/products/${product.id}/edit`}>
                                    <Pencil className="h-4 w-4 mr-2" />
                                    {t('products.editProduct', 'Edit Product')}
                                  </Link>
                                </DropdownMenuItem>
                              )}
                              <DropdownMenuSeparator />
                              {canPublishProducts && product.status === 'Draft' && (
                                <DropdownMenuItem
                                  className="cursor-pointer text-emerald-600 dark:text-emerald-400"
                                  onClick={() => onPublish(product)}
                                >
                                  <Send className="h-4 w-4 mr-2" />
                                  {t('labels.publish', 'Publish')}
                                </DropdownMenuItem>
                              )}
                              {canUpdateProducts && product.status === 'Active' && (
                                <DropdownMenuItem
                                  className="cursor-pointer text-amber-600 dark:text-amber-400"
                                  onClick={() => onArchive(product)}
                                >
                                  <Archive className="h-4 w-4 mr-2" />
                                  {t('labels.archive', 'Archive')}
                                </DropdownMenuItem>
                              )}
                              {canDeleteProducts && (
                                <>
                                  <DropdownMenuSeparator />
                                  <DropdownMenuItem
                                    className="cursor-pointer text-destructive focus:text-destructive"
                                    onClick={() => setProductToDelete(product)}
                                  >
                                    <Trash2 className="h-4 w-4 mr-2" />
                                    {t('labels.delete', 'Delete')}
                                  </DropdownMenuItem>
                                </>
                              )}
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </TableCell>
                      </TableRow>
                    )
                  })
                )}
              </TableBody>
            </Table>
            </div>
          )}

          {/* Enhanced Pagination */}
          {data && data.totalPages > 1 && (
            <div className="mt-6 pt-6 border-t border-border/50">
              <Pagination
                currentPage={data.page}
                totalPages={data.totalPages}
                totalItems={data.totalCount}
                pageSize={params.pageSize || DEFAULT_PRODUCT_PAGE_SIZE}
                onPageChange={setPage}
                showPageSizeSelector={false}
                className="justify-center"
              />
            </div>
          )}
        </CardContent>
      </Card>

      {/* Delete Confirmation Dialog */}
      <DeleteProductDialog
        product={productToDelete}
        open={!!productToDelete}
        onOpenChange={(open) => !open && setProductToDelete(null)}
        onConfirm={handleDelete}
      />
    </div>
  )
}
