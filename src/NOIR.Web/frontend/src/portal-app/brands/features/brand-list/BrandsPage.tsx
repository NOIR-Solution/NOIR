import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Award, Plus, Eye, Trash2, Globe, ExternalLink, Loader2 } from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useServerTable } from '@/hooks/useServerTable'
import { createActionsColumn } from '@/lib/table/columnHelpers'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  DropdownMenuSeparator,
  EmptyState,
  FilePreviewTrigger,
  PageHeader,
} from '@uikit'

import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useBrandsQuery, useDeleteBrandMutation } from '@/portal-app/brands/queries'
import { BrandDialog } from '../../components/BrandDialog'
import type { BrandListItem } from '@/types/brand'
import { toast } from 'sonner'

const ch = createColumnHelper<BrandListItem>()

export const BrandsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Brands')

  const canCreateBrands = hasPermission(Permissions.BrandsCreate)
  const canUpdateBrands = hasPermission(Permissions.BrandsUpdate)
  const canDeleteBrands = hasPermission(Permissions.BrandsDelete)

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-brand' })
  const [brandToDelete, setBrandToDelete] = useState<BrandListItem | null>(null)

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    isFilterPending,
    setSorting,
    setPage,
    setPageSize,
  } = useTableParams({ defaultPageSize: 20 })

  const { data, isLoading, error: queryError, refetch: refresh } = useBrandsQuery(params)
  const deleteMutation = useDeleteBrandMutation()

  const brands = data?.items ?? []
  const { editItem: brandToEdit, openEdit: openEditBrand, closeEdit: closeEditBrand } = useUrlEditDialog<BrandListItem>(brands)

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Brand',
    onCollectionUpdate: refresh,
  })

  const handleDelete = async () => {
    if (!brandToDelete) return
    try {
      await deleteMutation.mutateAsync(brandToDelete.id)
      toast.success(t('brands.deleteSuccess', 'Brand deleted successfully'))
      setBrandToDelete(null)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('brands.deleteError', 'Failed to delete brand')
      toast.error(message)
    }
  }

  const columns = useMemo((): ColumnDef<BrandListItem, unknown>[] => [
    createActionsColumn<BrandListItem>((brand) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => openEditBrand(brand)}>
          <Eye className="h-4 w-4 mr-2" />
          {canUpdateBrands ? t('labels.edit', 'Edit') : t('labels.viewDetails', 'View Details')}
        </DropdownMenuItem>
        {canDeleteBrands && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-destructive focus:text-destructive cursor-pointer"
              onClick={() => setBrandToDelete(brand)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete', 'Delete')}
            </DropdownMenuItem>
          </>
        )}
      </>
    )),
    ch.accessor('logoUrl', {
      id: 'logo',
      header: t('labels.logo', 'Logo'),
      enableSorting: false,
      size: 64,
      cell: ({ row }) => row.original.logoUrl ? (
        <FilePreviewTrigger
          file={{ url: row.original.logoUrl, name: row.original.name }}
          thumbnailWidth={40}
          thumbnailHeight={40}
        />
      ) : (
        <div className="h-10 w-10 rounded-lg bg-muted flex items-center justify-center">
          <Award className="h-5 w-5 text-muted-foreground" />
        </div>
      ),
    }) as ColumnDef<BrandListItem, unknown>,
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      cell: ({ row }) => (
        <div>
          <span className="font-medium">{row.original.name}</span>
          {row.original.description && (
            <p className="text-sm text-muted-foreground line-clamp-1 mt-0.5">{row.original.description}</p>
          )}
        </div>
      ),
    }) as ColumnDef<BrandListItem, unknown>,
    ch.accessor('slug', {
      header: t('labels.slug', 'Slug'),
      enableSorting: false,
      cell: ({ getValue }) => (
        <code className="text-sm bg-muted px-1.5 py-0.5 rounded">{getValue()}</code>
      ),
    }) as ColumnDef<BrandListItem, unknown>,
    ch.accessor('isActive', {
      id: 'status',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status', 'Status')} />,
      enableSorting: false,
      size: 120,
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getStatusBadgeClasses(getValue() ? 'green' : 'gray')}>
          {getValue() ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
        </Badge>
      ),
    }) as ColumnDef<BrandListItem, unknown>,
    ch.accessor('productCount', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('brands.products', 'Products')} />,
      enableSorting: false,
      meta: { align: 'center', label: t('brands.products', 'Products') },
      size: 90,
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<BrandListItem, unknown>,
    ch.accessor('websiteUrl', {
      id: 'website',
      header: t('labels.website', 'Website'),
      enableSorting: false,
      cell: ({ getValue }) => {
        const url = getValue()
        if (!url) return <span className="text-muted-foreground">-</span>
        try {
          return (
            <a
              href={url}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-1 text-sm text-primary hover:underline"
            >
              <Globe className="h-3 w-3" />
              <span className="truncate max-w-[120px]">{new URL(url).hostname}</span>
              <ExternalLink className="h-3 w-3" />
            </a>
          )
        } catch {
          return <span className="text-sm text-muted-foreground">{url}</span>
        }
      },
    }) as ColumnDef<BrandListItem, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canUpdateBrands, canDeleteBrands])

  const tableData = useMemo(() => data?.items ?? [], [data?.items])

  const table = useServerTable({
    data: tableData,
    columns,
    rowCount: data?.totalCount ?? 0,
    columnVisibilityStorageKey: 'brands',
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
    getRowId: (row) => row.id,
  })

  if (queryError) {
    console.error(queryError)
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Award}
        title={t('brands.title', 'Brands')}
        description={t('brands.description', 'Manage product brands and manufacturers')}
        action={
          canCreateBrands && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('brands.newBrand', 'New Brand')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">{t('brands.allBrands', 'All Brands')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <DataTableToolbar
            table={table}
            searchInput={searchInput}
            onSearchChange={setSearchInput}
            searchPlaceholder={t('brands.searchPlaceholder', 'Search brands...')}
            isSearchStale={isSearchStale}
            onResetColumnVisibility={table.resetColumnVisibility}
          />

          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={openEditBrand}
            emptyState={
              <EmptyState
                icon={Award}
                title={t('brands.noBrandsFound', 'No brands found')}
                description={t('brands.noBrandsDescription', 'Get started by creating your first brand.')}
                action={canCreateBrands ? {
                  label: t('brands.addBrand', 'Add Brand'),
                  onClick: () => openCreate(),
                } : undefined}
              />
            }
          />

          <DataTablePagination table={table} showPageSizeSelector={false} />
        </CardContent>
      </Card>

      <BrandDialog
        open={isCreateOpen || !!brandToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (brandToEdit) closeEditBrand()
          }
        }}
        brand={brandToEdit}
        onSuccess={() => refresh()}
      />

      <Credenza open={!!brandToDelete} onOpenChange={(open) => !open && setBrandToDelete(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('brands.deleteTitle', 'Delete Brand')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('brands.deleteDescription', {
                    name: brandToDelete?.name,
                    defaultValue: `Are you sure you want to delete "${brandToDelete?.name}"? This action cannot be undone.`
                  })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setBrandToDelete(null)} disabled={deleteMutation.isPending} className="cursor-pointer">
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {deleteMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {deleteMutation.isPending ? t('labels.deleting', 'Deleting...') : t('labels.delete', 'Delete')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default BrandsPage
