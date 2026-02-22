import { useState, useDeferredValue, useMemo, useTransition } from 'react'
import { useNavigate } from 'react-router-dom'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { useTranslation } from 'react-i18next'
import { getPaginationRange } from '@/lib/utils/pagination'
import {
  Search,
  Package,
  Plus,
  Eye,
  Pencil,
  Trash2,
  Send,
  Archive,
  EllipsisVertical,
  LayoutGrid,
  List as ListIcon,
  X,
  Loader2,
  Copy,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Checkbox,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  EmptyState,
  FilePreviewTrigger,
  Input,
  PageHeader,
  Pagination,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  ViewModeToggle,
  type ViewModeOption,
} from '@uikit'

import {
  useProductsQuery,
  useProductStatsQuery,
  useProductCategoriesQuery,
  useFilterableProductAttributesQuery,
  useDeleteProduct,
  usePublishProduct,
  useArchiveProduct,
  useDuplicateProduct,
  useBulkPublishProducts,
  useBulkArchiveProducts,
  useBulkDeleteProducts,
} from '@/portal-app/products/queries'
import type { GetProductsParams } from '@/services/products'
import { useActiveBrandsQuery } from '@/portal-app/brands/queries'
import { DeleteProductDialog } from '../../components/products/DeleteProductDialog'
import { ProductStatsCards } from '../../components/products/ProductStatsCards'
import { EnhancedProductGridView } from '../../components/products/EnhancedProductGridView'
import { AttributeFilterDialog } from '../../components/products/AttributeFilterDialog'
import { LowStockAlert } from '../../components/products/LowStockAlert'
import { ProductImportExport } from '../../components/products/ProductImportExport'
import type { ProductListItem, ProductStatus } from '@/types/product'
import { formatDistanceToNow } from 'date-fns'
import { toast } from 'sonner'
import { formatCurrency } from '@/lib/utils/currency'
import { PRODUCT_STATUS_CONFIG, DEFAULT_PRODUCT_PAGE_SIZE, LOW_STOCK_THRESHOLD } from '@/lib/constants/product'

export const ProductsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Products')
  const navigate = useNavigate()

  // Permission checks
  const canCreateProducts = hasPermission(Permissions.ProductsCreate)
  const canUpdateProducts = hasPermission(Permissions.ProductsUpdate)
  const canDeleteProducts = hasPermission(Permissions.ProductsDelete)
  const canPublishProducts = hasPermission(Permissions.ProductsPublish)

  // Filter params managed as component state
  const [params, setParams] = useState<GetProductsParams>({
    page: 1,
    pageSize: DEFAULT_PRODUCT_PAGE_SIZE,
  })
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])

  const { data, isLoading: loading, isPlaceholderData, error: queryError } = useProductsQuery(queryParams)
  const { data: stats = { total: 0, active: 0, draft: 0, archived: 0, outOfStock: 0, lowStock: 0 } } = useProductStatsQuery()
  const { data: categories = [] } = useProductCategoriesQuery()
  const { data: brands = [] } = useActiveBrandsQuery()
  const { data: filterableAttributes = [] } = useFilterableProductAttributesQuery()
  const error = queryError?.message ?? null

  // Mutation hooks
  const deleteProductMutation = useDeleteProduct()
  const publishProductMutation = usePublishProduct()
  const archiveProductMutation = useArchiveProduct()
  const duplicateProductMutation = useDuplicateProduct()
  const bulkPublishMutation = useBulkPublishProducts()
  const bulkArchiveMutation = useBulkArchiveProducts()
  const bulkDeleteMutation = useBulkDeleteProducts()

  // Delete handler for DeleteProductDialog (expects { success, error } return)
  const handleDelete = async (id: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await deleteProductMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete product'
      return { success: false, error: message }
    }
  }

  // Transition for filter/pagination updates
  const [isFilterPending, startFilterTransition] = useTransition()

  // Param setters — wrapped in startFilterTransition for smooth UI
  const setPage = (page: number) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, page }))
  )
  const setStatus = (status: ProductStatus | undefined) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, status, page: 1 }))
  )
  const setCategoryId = (categoryId: string | undefined) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, categoryId, page: 1 }))
  )
  const setBrand = (brand: string | undefined) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, brand, page: 1 }))
  )
  const setInStockOnly = (inStockOnly: boolean | undefined) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, inStockOnly, page: 1 }))
  )
  const setLowStockOnly = (lowStockOnly: boolean | undefined) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, lowStockOnly, page: 1 }))
  )
  const setAttributeFilters = (attributeFilters: Record<string, string[]> | undefined) => startFilterTransition(() =>
    setParams((prev) => ({ ...prev, attributeFilters, page: 1 }))
  )


  const [productToDelete, setProductToDelete] = useState<ProductListItem | null>(null)
  const [viewMode, setViewMode] = useState<'table' | 'grid'>('table')
  const viewModeOptions: ViewModeOption<'table' | 'grid'>[] = useMemo(() => [
    { value: 'table', label: t('labels.list', 'List'), icon: ListIcon, ariaLabel: t('labels.tableView', 'Table view') },
    { value: 'grid', label: t('labels.grid', 'Grid'), icon: LayoutGrid, ariaLabel: t('labels.gridView', 'Grid view') },
  ], [t])
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  // Transition for bulk operations
  const [isBulkPending, startBulkTransition] = useTransition()

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
    setParams((prev) => ({ ...prev, page: 1 }))
  }

  // Bulk selection handlers
  const handleSelectAll = () => {
    if (data?.items) {
      setSelectedIds(new Set(data.items.map(p => p.id)))
    }
  }

  const handleSelectNone = () => {
    setSelectedIds(new Set())
  }

  const handleToggleSelect = (id: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  const isAllSelected = !!(data?.items && data.items.length > 0 &&
    data.items.every(p => selectedIds.has(p.id)))

  // Bulk action handlers - use bulk API endpoints for better performance
  const onBulkPublish = () => {
    if (selectedIds.size === 0) return

    // Get IDs of draft products only (bulk publish only works on drafts)
    const draftProductIds = data?.items
      .filter(p => selectedIds.has(p.id) && p.status === 'Draft')
      .map(p => p.id) || []

    if (draftProductIds.length === 0) {
      toast.warning(t('products.noDraftProductsSelected', 'No draft products selected'))
      return
    }

    startBulkTransition(async () => {
      try {
        const result = await bulkPublishMutation.mutateAsync(draftProductIds)
        const { success, failed } = result
        if (failed > 0) {
          toast.warning(t('products.bulkPublishPartial', { success, failed, defaultValue: `${success} products published, ${failed} failed` }))
        } else {
          toast.success(t('products.bulkPublishSuccess', { count: success, defaultValue: `${success} products published` }))
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : t('products.bulkPublishFailed', 'Failed to publish products')
        toast.error(message)
      }
      setSelectedIds(new Set())
    })
  }

  const onBulkArchive = () => {
    if (selectedIds.size === 0) return

    // Get IDs of active products only (bulk archive only works on active)
    const activeProductIds = data?.items
      .filter(p => selectedIds.has(p.id) && p.status === 'Active')
      .map(p => p.id) || []

    if (activeProductIds.length === 0) {
      toast.warning(t('products.noActiveProductsSelected', 'No active products selected'))
      return
    }

    startBulkTransition(async () => {
      try {
        const result = await bulkArchiveMutation.mutateAsync(activeProductIds)
        const { success, failed } = result
        if (failed > 0) {
          toast.warning(t('products.bulkArchivePartial', { success, failed, defaultValue: `${success} products archived, ${failed} failed` }))
        } else {
          toast.success(t('products.bulkArchiveSuccess', { count: success, defaultValue: `${success} products archived` }))
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : t('products.bulkArchiveFailed', 'Failed to archive products')
        toast.error(message)
      }
      setSelectedIds(new Set())
    })
  }

  const onBulkDelete = () => {
    if (selectedIds.size === 0) return

    const selectedProductIds = Array.from(selectedIds)

    startBulkTransition(async () => {
      try {
        const result = await bulkDeleteMutation.mutateAsync(selectedProductIds)
        const { success, failed } = result
        if (failed > 0) {
          toast.warning(t('products.bulkDeletePartial', { success, failed, defaultValue: `${success} products deleted, ${failed} failed` }))
        } else {
          toast.success(t('products.bulkDeleteSuccess', { count: success, defaultValue: `${success} products deleted` }))
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : t('products.bulkDeleteFailed', 'Failed to delete products')
        toast.error(message)
      }
      setSelectedIds(new Set())
    })
  }

  // Get counts for bulk action buttons
  const selectedDraftCount = data?.items.filter(
    p => selectedIds.has(p.id) && p.status === 'Draft'
  ).length || 0

  const selectedActiveCount = data?.items.filter(
    p => selectedIds.has(p.id) && p.status === 'Active'
  ).length || 0

  // Count items with low stock (in stock but below threshold)
  const lowStockCount = data?.items.filter(
    p => p.totalStock > 0 && p.totalStock < LOW_STOCK_THRESHOLD
  ).length || 0

  const handleStatusChange = (value: string) => {
    setStatus(value === 'all' ? undefined : (value as ProductStatus))
  }

  const handleCategoryChange = (value: string) => {
    setCategoryId(value === 'all' ? undefined : value)
  }

  const handleBrandChange = (value: string) => {
    setBrand(value === 'all' ? undefined : value)
  }

  const handleStockFilterChange = (value: string) => {
    setInStockOnly(value === 'inStock' ? true : undefined)
  }


  const onPublish = async (product: ProductListItem) => {
    try {
      await publishProductMutation.mutateAsync(product.id)
      toast.success(t('products.publishSuccess', { name: product.name, defaultValue: `Product "${product.name}" published successfully` }))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('products.publishFailed', 'Failed to publish product')
      toast.error(message)
    }
  }

  const onArchive = async (product: ProductListItem) => {
    try {
      await archiveProductMutation.mutateAsync(product.id)
      toast.success(t('products.archiveSuccess', { name: product.name, defaultValue: `Product "${product.name}" archived successfully` }))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('products.archiveFailed', 'Failed to archive product')
      toast.error(message)
    }
  }

  const onDuplicate = async (product: ProductListItem) => {
    try {
      const newProduct = await duplicateProductMutation.mutateAsync(product.id)
      toast.success(t('products.duplicateSuccess', { name: product.name, defaultValue: `"${product.name}" duplicated as draft` }))
      navigate(`/portal/ecommerce/products/${newProduct.id}/edit`)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('products.duplicateFailed', 'Failed to duplicate product')
      toast.error(message)
    }
  }

  const paginationRange = data
    ? getPaginationRange(data.page, params.pageSize || DEFAULT_PRODUCT_PAGE_SIZE, data.totalCount)
    : { from: 0, to: 0 }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Package}
        title={t('products.title', 'Products')}
        description={t('products.description', 'Manage your product catalog')}
        responsive
        action={
          <div className="flex items-center gap-2">
            <ProductImportExport
              products={data?.items || []}
              onImportComplete={() => {
                // Refresh the product list after import
                setPage(1)
              }}
            />
            {canCreateProducts && (
              <ViewTransitionLink to="/portal/ecommerce/products/new">
                <Button className="group shadow-lg hover:shadow-xl transition-all duration-300">
                  <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
                  {t('products.newProduct', 'New Product')}
                </Button>
              </ViewTransitionLink>
            )}
          </div>
        }
      />

      {/* Low Stock Alert */}
      <LowStockAlert
        lowStockCount={lowStockCount}
        onViewLowStock={() => {
          // Clear other stock filters and set low stock filter
          setInStockOnly(undefined)
          setLowStockOnly(true)
        }}
      />

      {/* Stats Dashboard */}
      <ProductStatsCards
        stats={stats}
        hasActiveFilters={!!(deferredSearch || params.categoryId || params.brand || params.inStockOnly || params.lowStockOnly || (params.attributeFilters && Object.keys(params.attributeFilters).length > 0))}
        activeFilter={params.status || null}
        onFilterChange={(status) => setStatus(status || undefined)}
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg">{t('products.allProducts', 'All Products')}</CardTitle>
                <CardDescription className="text-sm">
                  {data ? t('labels.showingOfItems', { from: paginationRange.from, to: paginationRange.to, total: data.totalCount }) : t('labels.loading', 'Loading...')}
                </CardDescription>
              </div>
              <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
            </div>

            {/* Filter Bar */}
            <div className="flex flex-wrap items-center gap-2">
            {/* Search */}
            <div className="relative flex-1 min-w-[200px]">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground pointer-events-none" />
              <Input
                placeholder={t('products.searchPlaceholder', 'Search products...')}
                value={searchInput}
                onChange={handleSearchChange}
                className="pl-9 h-9"
                aria-label={t('products.searchProducts', 'Search products')}
              />
            </div>

            <Select onValueChange={handleStatusChange} defaultValue="all">
              <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer transition-all duration-200 hover:border-primary/50" aria-label={t('products.filterByStatus', 'Filter by status')}>
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
              <SelectTrigger className="w-full sm:w-40 h-9 cursor-pointer transition-all duration-200 hover:border-primary/50" aria-label={t('products.filterByCategory', 'Filter by category')}>
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

            <Select onValueChange={handleBrandChange} defaultValue="all">
              <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer transition-all duration-200 hover:border-primary/50" aria-label={t('products.filterByBrand', 'Filter by brand')}>
                <SelectValue placeholder={t('labels.brand', 'Brand')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" className="cursor-pointer">{t('labels.allBrands', 'All Brands')}</SelectItem>
                {brands.map((brand) => (
                  <SelectItem key={brand.id} value={brand.name} className="cursor-pointer">
                    {brand.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select onValueChange={handleStockFilterChange} defaultValue="all">
              <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer transition-all duration-200 hover:border-primary/50" aria-label={t('products.filterByStock', 'Filter by stock')}>
                <SelectValue placeholder={t('labels.stock', 'Stock')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" className="cursor-pointer">{t('labels.allStock', 'All Stock')}</SelectItem>
                <SelectItem value="inStock" className="cursor-pointer">{t('products.inStock', 'In Stock')}</SelectItem>
              </SelectContent>
            </Select>

            {/* Attribute Filter Dialog */}
            {filterableAttributes.length > 0 && (
              <AttributeFilterDialog
                attributes={filterableAttributes}
                activeFilters={params.attributeFilters}
                onApply={setAttributeFilters}
              />
            )}

            </div>
          </div>
        </CardHeader>

        <CardContent className={(isFilterPending || isSearchStale || isPlaceholderData) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive animate-in fade-in-0 slide-in-from-top-2 duration-300">
              <p className="text-sm font-medium">{error}</p>
            </div>
          )}

          {/* Bulk Action Toolbar */}
          {selectedIds.size > 0 && viewMode === 'table' && (
            <div className="mb-4 p-4 rounded-lg bg-primary/5 border border-primary/20 animate-in fade-in-0 slide-in-from-top-2 duration-200">
              <div className="flex items-center flex-wrap gap-3">
                <Badge variant="secondary" className="text-sm py-1 px-3">
                  {selectedIds.size} {t('labels.selected', 'selected')}
                </Badge>
                {canPublishProducts && selectedDraftCount > 0 && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={onBulkPublish}
                    disabled={isBulkPending}
                    className="cursor-pointer text-emerald-600 border-emerald-200 hover:bg-emerald-50 dark:text-emerald-400 dark:border-emerald-800 dark:hover:bg-emerald-950"
                  >
                    {isBulkPending ? (
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    ) : (
                      <Send className="h-4 w-4 mr-2" />
                    )}
                    {t('products.publishCount', { count: selectedDraftCount, defaultValue: `Publish ${selectedDraftCount}` })}
                  </Button>
                )}
                {canUpdateProducts && selectedActiveCount > 0 && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={onBulkArchive}
                    disabled={isBulkPending}
                    className="cursor-pointer text-amber-600 border-amber-200 hover:bg-amber-50 dark:text-amber-400 dark:border-amber-800 dark:hover:bg-amber-950"
                  >
                    {isBulkPending ? (
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    ) : (
                      <Archive className="h-4 w-4 mr-2" />
                    )}
                    {t('products.archiveCount', { count: selectedActiveCount, defaultValue: `Archive ${selectedActiveCount}` })}
                  </Button>
                )}
                {canDeleteProducts && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={onBulkDelete}
                    disabled={isBulkPending}
                    className="cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
                  >
                    {isBulkPending ? (
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    ) : (
                      <Trash2 className="h-4 w-4 mr-2" />
                    )}
                    {t('products.deleteCount', { count: selectedIds.size, defaultValue: `Delete ${selectedIds.size}` })}
                  </Button>
                )}
                <div className="flex-1" />
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleSelectNone}
                  className="cursor-pointer"
                >
                  <X className="h-4 w-4 mr-2" />
                  {t('buttons.clearSelection', 'Clear Selection')}
                </Button>
              </div>
            </div>
          )}

          {viewMode === 'grid' ? (
            // Grid View
            loading ? (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                {[...Array(DEFAULT_PRODUCT_PAGE_SIZE)].map((_, i) => (
                  <div key={i} className="animate-pulse">
                    <div className="aspect-square bg-muted rounded-xl" />
                    <div className="p-3 space-y-2">
                      <Skeleton className="h-3 w-1/3" />
                      <Skeleton className="h-4 w-3/4" />
                      <Skeleton className="h-5 w-1/2" />
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
                className="border-0 rounded-none px-4 py-12"
              />
            ) : (
              <EnhancedProductGridView
                products={data?.items || []}
                onDelete={canDeleteProducts ? setProductToDelete : undefined}
                onPublish={canPublishProducts ? onPublish : undefined}
                onArchive={canUpdateProducts ? onArchive : undefined}
                onDuplicate={canCreateProducts ? onDuplicate : undefined}
                canEdit={canUpdateProducts}
                canDelete={canDeleteProducts}
                canPublish={canPublishProducts}
                canCreate={canCreateProducts}
              />
            )
          ) : (
            // Table View
            <div className="rounded-xl border border-border/50 overflow-hidden min-h-[400px]">
              <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                  <TableHead className="w-[40px]">
                    <Checkbox
                      checked={isAllSelected}
                      onCheckedChange={(checked) => {
                        if (checked) handleSelectAll()
                        else handleSelectNone()
                      }}
                      aria-label={t('labels.selectAll', 'Select all')}
                      className="cursor-pointer"
                    />
                  </TableHead>
                  <TableHead className="w-[35%] font-semibold">{t('products.product', 'Product')}</TableHead>
                  <TableHead className="font-semibold">{t('labels.status', 'Status')}</TableHead>
                  <TableHead className="font-semibold">{t('labels.category', 'Category')}</TableHead>
                  <TableHead className="font-semibold">{t('labels.brand', 'Brand')}</TableHead>
                  <TableHead className="text-right font-semibold">{t('products.price', 'Price')}</TableHead>
                  <TableHead className="text-right font-semibold">{t('labels.stock', 'Stock')}</TableHead>
                  <TableHead className="font-semibold">{t('labels.created', 'Created')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  // Enhanced loading skeletons
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-9 w-9 rounded-lg" /></TableCell>
                      <TableCell>
                        <Skeleton className="h-4 w-4 rounded" />
                      </TableCell>
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
                      <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-20 ml-auto" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-4 w-12 ml-auto" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                    </TableRow>
                  ))
                ) : data?.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} className="p-0">
                      <EmptyState
                        icon={Package}
                        title={t('products.noProductsFound', 'No products found')}
                        description={t('products.noProductsDescription', 'Get started by creating your first product to build your catalog.')}
                        action={canCreateProducts ? {
                          label: t('products.addProduct', 'Add Product'),
                          onClick: () => navigate('/portal/ecommerce/products/new'),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
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
                        className={`group transition-all duration-200 hover:bg-muted/30 ${selectedIds.size === 0 ? 'cursor-pointer' : ''} ${selectedIds.has(product.id) ? 'bg-primary/5' : ''}`}
                        onClick={() => { if (selectedIds.size === 0) navigate(`/portal/ecommerce/products/${product.id}`) }}
                      >
                        <TableCell className="sticky left-0 z-10 bg-background" onClick={(e) => e.stopPropagation()}>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button
                                variant="ghost"
                                size="sm"
                                className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                                aria-label={`Actions for ${product.name}`}
                              >
                                <EllipsisVertical className="h-4 w-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="start" className="w-48">
                              <DropdownMenuItem className="cursor-pointer" asChild>
                                <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}`}>
                                  <Eye className="h-4 w-4 mr-2" />
                                  {t('labels.viewDetails', 'View Details')}
                                </ViewTransitionLink>
                              </DropdownMenuItem>
                              {canUpdateProducts && (
                                <DropdownMenuItem className="cursor-pointer" asChild>
                                  <ViewTransitionLink to={`/portal/ecommerce/products/${product.id}/edit`}>
                                    <Pencil className="h-4 w-4 mr-2" />
                                    {t('products.editProduct', 'Edit Product')}
                                  </ViewTransitionLink>
                                </DropdownMenuItem>
                              )}
                              {canCreateProducts && (
                                <DropdownMenuItem
                                  className="cursor-pointer"
                                  onClick={() => onDuplicate(product)}
                                >
                                  <Copy className="h-4 w-4 mr-2" />
                                  {t('products.duplicate', 'Duplicate')}
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
                        <TableCell onClick={(e) => e.stopPropagation()}>
                          <Checkbox
                            checked={selectedIds.has(product.id)}
                            onCheckedChange={() => handleToggleSelect(product.id)}
                            aria-label={`Select ${product.name}`}
                            className="cursor-pointer"
                          />
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-3">
                            {/* Product image thumbnail - Click to view full image */}
                            <div style={{ viewTransitionName: `product-image-${product.id}` }}>
                              <FilePreviewTrigger
                                file={{
                                  url: product.primaryImageUrl ?? '',
                                  name: product.name,
                                }}
                                thumbnailWidth={56}
                                thumbnailHeight={56}
                                className="rounded-xl"
                              />
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
                            variant="outline"
                          >
                            <StatusIcon className="h-3 w-3" />
                            {status.label}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <span className="text-sm">{product.categoryName || '—'}</span>
                        </TableCell>
                        <TableCell>
                          <span className="text-sm">{product.brandName || product.brand || '—'}</span>
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
            <Pagination
              currentPage={data.page}
              totalPages={data.totalPages}
              totalItems={data.totalCount}
              pageSize={params.pageSize || DEFAULT_PRODUCT_PAGE_SIZE}
              onPageChange={setPage}
              showPageSizeSelector={false}
              className="mt-4 justify-center"
            />
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

export default ProductsPage
