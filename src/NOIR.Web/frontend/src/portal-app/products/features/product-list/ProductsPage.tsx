import { useState, useMemo, useTransition, useCallback } from 'react'
import { useRowHighlight } from '@/hooks/useRowHighlight'
import { useDelayedLoading } from '@/hooks/useDelayedLoading'
import { useNavigate } from 'react-router-dom'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { useTranslation } from 'react-i18next'
import { getPaginationRange } from '@/lib/utils/pagination'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable, useSelectedIds } from '@/hooks/useEnterpriseTable'
import { createSelectColumn, createActionsColumn } from '@/lib/table/columnHelpers'
import {
  Search,
  Package,
  Plus,
  Eye,
  Pencil,
  Trash2,
  Send,
  Archive,
  LayoutGrid,
  List as ListIcon,
  Loader2,
  Copy,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  DropdownMenuItem,
  DropdownMenuSeparator,
  EmptyState,
  FilePreviewTrigger,
  Input,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  Pagination,
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
import { aggregatedCells } from '@/lib/table/aggregationHelpers'
import { PRODUCT_STATUS_CONFIG, DEFAULT_PRODUCT_PAGE_SIZE, LOW_STOCK_THRESHOLD } from '@/lib/constants/product'

type ProductFilters = {
  status?: ProductStatus
  categoryId?: string
  brand?: string
  inStockOnly?: boolean
  lowStockOnly?: boolean
  attributeFilters?: Record<string, string[]>
}

const ch = createColumnHelper<ProductListItem>()

export const ProductsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Products')
  const navigate = useNavigate()
  const { getRowAnimationClass, fadeOutRow } = useRowHighlight()

  // Permission checks
  const canCreateProducts = hasPermission(Permissions.ProductsCreate)
  const canUpdateProducts = hasPermission(Permissions.ProductsUpdate)
  const canDeleteProducts = hasPermission(Permissions.ProductsDelete)
  const canPublishProducts = hasPermission(Permissions.ProductsPublish)

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    isFilterPending,
    setFilter,
    setSorting,
    setPage,
    setPageSize,
    defaultPageSize,
  } = useTableParams<ProductFilters>({ defaultPageSize: DEFAULT_PRODUCT_PAGE_SIZE, tableKey: 'products' })

  const queryParams = useMemo((): GetProductsParams => ({
    page: params.page,
    pageSize: params.pageSize,
    search: params.search,
    status: params.filters.status,
    categoryId: params.filters.categoryId,
    brand: params.filters.brand,
    inStockOnly: params.filters.inStockOnly,
    lowStockOnly: params.filters.lowStockOnly,
    attributeFilters: params.filters.attributeFilters,
  }), [params])

  const { data, isLoading: loading, isPlaceholderData, error: queryError, refetch } = useProductsQuery(queryParams)
  const isContentStale = useDelayedLoading(isSearchStale || isFilterPending || isPlaceholderData)
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
      await fadeOutRow(id)
      await deleteProductMutation.mutateAsync(id)
      return { success: true }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('products.deleteFailed', 'Failed to delete product')
      return { success: false, error: message }
    }
  }

  const [productToDelete, setProductToDelete] = useState<ProductListItem | null>(null)
  const [showBulkDeleteConfirm, setShowBulkDeleteConfirm] = useState(false)
  const [viewMode, setViewMode] = useState<'table' | 'grid'>('table')
  const viewModeOptions: ViewModeOption<'table' | 'grid'>[] = useMemo(() => [
    { value: 'table', label: t('labels.list', 'List'), icon: ListIcon, ariaLabel: t('labels.tableView', 'Table view') },
    { value: 'grid', label: t('labels.grid', 'Grid'), icon: LayoutGrid, ariaLabel: t('labels.gridView', 'Grid view') },
  ], [t])

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Product',
    onCollectionUpdate: refetch,
  })

  // Transition for bulk operations
  const [isBulkPending, startBulkTransition] = useTransition()

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
    setPage(1)
  }

  // Bulk action handlers - use bulk API endpoints for better performance
  const onBulkPublish = () => {
    if (selectedIds.length === 0) return

    // Get IDs of draft products only (bulk publish only works on drafts)
    const draftProductIds = data?.items
      .filter(p => selectedIdsSet.has(p.id) && p.status === 'Draft')
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
      table.resetRowSelection()
    })
  }

  const onBulkArchive = () => {
    if (selectedIds.length === 0) return

    // Get IDs of active products only (bulk archive only works on active)
    const activeProductIds = data?.items
      .filter(p => selectedIdsSet.has(p.id) && p.status === 'Active')
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
      table.resetRowSelection()
    })
  }

  const onBulkDelete = () => {
    if (selectedIds.length === 0) return
    setShowBulkDeleteConfirm(true)
  }

  const handleBulkDeleteConfirm = () => {
    const selectedProductIds = [...selectedIds]

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
      table.resetRowSelection()
      setShowBulkDeleteConfirm(false)
    })
  }

  // Count items with low stock (in stock but below threshold)
  const lowStockCount = data?.items.filter(
    p => p.totalStock > 0 && p.totalStock < LOW_STOCK_THRESHOLD
  ).length || 0

  const handleStatusChange = (value: string) => {
    setFilter('status', value === 'all' ? undefined : (value as ProductStatus))
  }

  const handleCategoryChange = (value: string) => {
    setFilter('categoryId', value === 'all' ? undefined : value)
  }

  const handleBrandChange = (value: string) => {
    setFilter('brand', value === 'all' ? undefined : value)
  }

  const handleStockFilterChange = (value: string) => {
    setFilter('inStockOnly', value === 'inStock' ? true : undefined)
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

  const columns = useMemo((): ColumnDef<ProductListItem, unknown>[] => [
    createActionsColumn<ProductListItem>((product) => (
      <>
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
          <DropdownMenuItem className="cursor-pointer" onClick={() => onDuplicate(product)}>
            <Copy className="h-4 w-4 mr-2" />
            {t('products.duplicate', 'Duplicate')}
          </DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        {canPublishProducts && product.status === 'Draft' && (
          <DropdownMenuItem className="cursor-pointer text-emerald-600 dark:text-emerald-400" onClick={() => onPublish(product)}>
            <Send className="h-4 w-4 mr-2" />
            {t('labels.publish', 'Publish')}
          </DropdownMenuItem>
        )}
        {canUpdateProducts && product.status === 'Active' && (
          <DropdownMenuItem className="cursor-pointer text-amber-600 dark:text-amber-400" onClick={() => onArchive(product)}>
            <Archive className="h-4 w-4 mr-2" />
            {t('labels.archive', 'Archive')}
          </DropdownMenuItem>
        )}
        {canDeleteProducts && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem className="cursor-pointer text-destructive focus:text-destructive" onClick={() => setProductToDelete(product)}>
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete', 'Delete')}
            </DropdownMenuItem>
          </>
        )}
      </>
    )),
    createSelectColumn<ProductListItem>(),
    ch.accessor('name', {
      id: 'product',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('products.product', 'Product')} />,
      meta: { label: t('products.product', 'Product') },
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div style={{ viewTransitionName: `product-image-${row.original.id}` }} onClick={(e) => e.stopPropagation()}>
            <FilePreviewTrigger
              file={{ url: row.original.primaryImageUrl ?? '', name: row.original.name }}
              thumbnailWidth={56}
              thumbnailHeight={56}
              className="rounded-xl"
            />
          </div>
          <div className="flex flex-col min-w-0">
            <span className="font-medium truncate">{row.original.name}</span>
            {row.original.sku && (
              <span className="text-xs text-muted-foreground font-mono">SKU: {row.original.sku}</span>
            )}
          </div>
        </div>
      ),
    }) as ColumnDef<ProductListItem, unknown>,
    ch.accessor('status', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status', 'Status')} />,
      meta: { label: t('labels.status', 'Status') },
      enableGrouping: true,
      cell: ({ getValue }) => {
        const status = PRODUCT_STATUS_CONFIG[getValue()]
        const StatusIcon = status.icon
        return (
          <Badge className={`${status.color} border`} variant="outline">
            <StatusIcon className="h-3 w-3" />
            {t(status.labelKey)}
          </Badge>
        )
      },
    }) as ColumnDef<ProductListItem, unknown>,
    ch.accessor('categoryName', {
      id: 'category',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.category', 'Category')} />,
      meta: { label: t('labels.category', 'Category') },
      enableGrouping: true,
      cell: ({ getValue }) => <span className="text-sm">{getValue() || '—'}</span>,
    }) as ColumnDef<ProductListItem, unknown>,
    ch.accessor((r) => r.brandName || r.brand, {
      id: 'brand',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.brand', 'Brand')} />,
      meta: { label: t('labels.brand', 'Brand') },
      enableGrouping: true,
      cell: ({ getValue }) => <span className="text-sm">{getValue() || '—'}</span>,
    }) as ColumnDef<ProductListItem, unknown>,
    ch.accessor('basePrice', {
      id: 'price',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('products.price', 'Price')} />,
      meta: { align: 'right' as const, label: t('products.price', 'Price') },
      aggregationFn: 'mean',
      aggregatedCell: aggregatedCells.average(),
      cell: ({ row }) => (
        <span className="font-semibold text-foreground">
          {formatCurrency(row.original.basePrice, row.original.currency)}
        </span>
      ),
    }) as ColumnDef<ProductListItem, unknown>,
    ch.accessor('totalStock', {
      id: 'stock',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.stock', 'Stock')} />,
      meta: { align: 'right' as const, label: t('labels.stock', 'Stock') },
      aggregationFn: 'sum',
      aggregatedCell: aggregatedCells.sum(),
      cell: ({ getValue, row }) => (
        <Badge variant={row.original.inStock ? 'default' : 'destructive'}>
          {getValue()}
        </Badge>
      ),
    }) as ColumnDef<ProductListItem, unknown>,
    ch.accessor('createdAt', {
      id: 'created',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.created', 'Created')} />,
      meta: { label: t('labels.created', 'Created') },
      cell: ({ getValue }) => (
        <span className="text-sm text-muted-foreground">
          {formatDistanceToNow(new Date(getValue()), { addSuffix: true })}
        </span>
      ),
    }) as ColumnDef<ProductListItem, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canUpdateProducts, canCreateProducts, canPublishProducts, canDeleteProducts])

  const tableData = useMemo(() => data?.items ?? [], [data?.items])
  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: tableData,
    columns,
    tableKey: 'products',
    rowCount: data?.totalCount ?? 0,
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: params.sorting as SortingState,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function'
        ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize })
        : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    onSortingChange: setSorting,
    enableRowSelection: true,
    enableGrouping: true,
    getRowId: (row) => row.id,
  })

  const currentRowSelection = table.getState().rowSelection
  const selectedIds = useSelectedIds(currentRowSelection)
  const selectedIdsSet = useMemo(() => new Set(selectedIds), [selectedIds])
  const clearSelection = useCallback(() => table.resetRowSelection(), [table])

  const selectedDraftCount = data?.items.filter(
    p => selectedIdsSet.has(p.id) && p.status === 'Draft'
  ).length || 0

  const selectedActiveCount = data?.items.filter(
    p => selectedIdsSet.has(p.id) && p.status === 'Active'
  ).length || 0

  const paginationRange = data
    ? getPaginationRange(data.page, params.pageSize || DEFAULT_PRODUCT_PAGE_SIZE, data.totalCount)
    : { from: 0, to: 0 }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
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
                <Button className="group transition-all duration-300">
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
          setFilter('inStockOnly', undefined)
          setFilter('lowStockOnly', true)
        }}
      />

      {/* Stats Dashboard */}
      <ProductStatsCards
        stats={stats}
        hasActiveFilters={!!(params.search || params.filters.categoryId || params.filters.brand || params.filters.inStockOnly || params.filters.lowStockOnly || (params.filters.attributeFilters && Object.keys(params.filters.attributeFilters).length > 0))}
        activeFilter={params.filters.status || null}
        onFilterChange={(status) => setFilter('status', status || undefined)}
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
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

            {/* Filter Bar - DataTableToolbar for table view, inline for grid view */}
            {viewMode === 'table' ? (
              <DataTableToolbar
                table={table}
                searchInput={searchInput}
                onSearchChange={setSearchInput}
                searchPlaceholder={t('products.searchPlaceholder', 'Search products...')}
                isSearchStale={isSearchStale}
                showColumnToggle={true}
                columnOrder={settings.columnOrder}
                onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
                isCustomized={isCustomized}
                onResetSettings={resetToDefault}
                density={settings.density}
                onDensityChange={setDensity}
                groupableColumnIds={['status', 'category', 'brand']}
                grouping={settings.grouping}
                onGroupingChange={(ids) => table.setGrouping(ids)}
                filterSlot={
                  <>
                    <Select value={params.filters.status || 'all'} onValueChange={handleStatusChange}>
                      <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer" aria-label={t('products.filterByStatus', 'Filter by status')}>
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
                    <Select value={params.filters.categoryId || 'all'} onValueChange={handleCategoryChange}>
                      <SelectTrigger className="w-full sm:w-40 h-9 cursor-pointer" aria-label={t('products.filterByCategory', 'Filter by category')}>
                        <SelectValue placeholder={t('labels.category', 'Category')} />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all" className="cursor-pointer">{t('labels.allCategories', 'All Categories')}</SelectItem>
                        {categories.map((cat) => (
                          <SelectItem key={cat.id} value={cat.id} className="cursor-pointer">{cat.name}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Select value={params.filters.brand || 'all'} onValueChange={handleBrandChange}>
                      <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer" aria-label={t('products.filterByBrand', 'Filter by brand')}>
                        <SelectValue placeholder={t('labels.brand', 'Brand')} />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all" className="cursor-pointer">{t('labels.allBrands', 'All Brands')}</SelectItem>
                        {brands.map((brand) => (
                          <SelectItem key={brand.id} value={brand.name} className="cursor-pointer">{brand.name}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Select value={params.filters.inStockOnly ? 'inStock' : 'all'} onValueChange={handleStockFilterChange}>
                      <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer" aria-label={t('products.filterByStock', 'Filter by stock')}>
                        <SelectValue placeholder={t('labels.stock', 'Stock')} />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all" className="cursor-pointer">{t('labels.allStock', 'All Stock')}</SelectItem>
                        <SelectItem value="inStock" className="cursor-pointer">{t('products.inStock', 'In Stock')}</SelectItem>
                      </SelectContent>
                    </Select>
                    {filterableAttributes.length > 0 && (
                      <AttributeFilterDialog
                        attributes={filterableAttributes}
                        activeFilters={params.filters.attributeFilters}
                        onApply={(f) => setFilter('attributeFilters', f)}
                      />
                    )}
                  </>
                }
              />
            ) : (
              <div className="flex flex-wrap items-center gap-2">
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
                <Select value={params.filters.status || 'all'} onValueChange={handleStatusChange}>
                  <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer" aria-label={t('products.filterByStatus', 'Filter by status')}>
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
                <Select value={params.filters.categoryId || 'all'} onValueChange={handleCategoryChange}>
                  <SelectTrigger className="w-full sm:w-40 h-9 cursor-pointer" aria-label={t('products.filterByCategory', 'Filter by category')}>
                    <SelectValue placeholder={t('labels.category', 'Category')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="cursor-pointer">{t('labels.allCategories', 'All Categories')}</SelectItem>
                    {categories.map((cat) => (
                      <SelectItem key={cat.id} value={cat.id} className="cursor-pointer">{cat.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Select value={params.filters.brand || 'all'} onValueChange={handleBrandChange}>
                  <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer" aria-label={t('products.filterByBrand', 'Filter by brand')}>
                    <SelectValue placeholder={t('labels.brand', 'Brand')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="cursor-pointer">{t('labels.allBrands', 'All Brands')}</SelectItem>
                    {brands.map((brand) => (
                      <SelectItem key={brand.id} value={brand.name} className="cursor-pointer">{brand.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Select value={params.filters.inStockOnly ? 'inStock' : 'all'} onValueChange={handleStockFilterChange}>
                  <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer" aria-label={t('products.filterByStock', 'Filter by stock')}>
                    <SelectValue placeholder={t('labels.stock', 'Stock')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="cursor-pointer">{t('labels.allStock', 'All Stock')}</SelectItem>
                    <SelectItem value="inStock" className="cursor-pointer">{t('products.inStock', 'In Stock')}</SelectItem>
                  </SelectContent>
                </Select>
                {filterableAttributes.length > 0 && (
                  <AttributeFilterDialog
                    attributes={filterableAttributes}
                    activeFilters={params.filters.attributeFilters}
                    onApply={(f) => setFilter('attributeFilters', f)}
                  />
                )}
              </div>
            )}
          </div>
        </CardHeader>

        <CardContent className={isContentStale ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive animate-in fade-in-0 slide-in-from-top-2 duration-300">
              <p className="text-sm font-medium">{error}</p>
            </div>
          )}

          {/* Bulk Action Toolbar */}
          {viewMode === 'table' && (
            <BulkActionToolbar selectedCount={selectedIds.length} onClearSelection={clearSelection}>
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
                  {t('products.deleteCount', { count: selectedIds.length, defaultValue: `Delete ${selectedIds.length}` })}
                </Button>
              )}
            </BulkActionToolbar>
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
              <>
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
              </>
            )
          ) : (
            // Table View - DataTable
            <div className="space-y-3">
              <DataTable
                table={table}
                density={settings.density}
                isLoading={loading}
                isStale={isContentStale}
                onRowClick={selectedIds.length === 0 ? (product) => navigate(`/portal/ecommerce/products/${product.id}`) : undefined}
                getRowAnimationClass={getRowAnimationClass}
                emptyState={
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
                }
              />
              {data && data.totalPages > 1 && (
                <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
              )}
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

      {/* Bulk Delete Confirmation Dialog */}
      <Credenza open={showBulkDeleteConfirm} onOpenChange={setShowBulkDeleteConfirm}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('products.bulkDeleteTitle', 'Delete Products')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('products.bulkDeleteConfirmation', {
                    count: selectedIds.length,
                    defaultValue: `Are you sure you want to delete ${selectedIds.length} selected products? This action cannot be undone.`,
                  })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button
              variant="outline"
              onClick={() => setShowBulkDeleteConfirm(false)}
              disabled={isBulkPending}
              className="cursor-pointer"
            >
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleBulkDeleteConfirm}
              disabled={isBulkPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {isBulkPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isBulkPending
                ? t('labels.deleting', 'Deleting...')
                : t('products.deleteCount', { count: selectedIds.length, defaultValue: `Delete ${selectedIds.length} Products` })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default ProductsPage
