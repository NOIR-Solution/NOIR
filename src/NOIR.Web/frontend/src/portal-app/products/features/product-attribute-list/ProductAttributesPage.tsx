import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Check, Loader2, Minus, Pencil, Plus, Tags, Trash2 } from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { createActionsColumn } from '@/lib/table/columnHelpers'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
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
  EmptyState,
  PageHeader,
} from '@uikit'

import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useProductAttributesQuery, useDeleteProductAttributeMutation } from '@/portal-app/products/queries'
import { ProductAttributeDialog } from '../../components/product-attributes/ProductAttributeDialog'
import type { ProductAttributeListItem } from '@/types/productAttribute'
import { getTypeBadge } from '../../utils/attribute.utils'

import { toast } from 'sonner'

const ch = createColumnHelper<ProductAttributeListItem>()

export const ProductAttributesPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('ProductAttributes')

  const canCreateAttributes = hasPermission(Permissions.AttributesCreate)
  const canUpdateAttributes = hasPermission(Permissions.AttributesUpdate)
  const canDeleteAttributes = hasPermission(Permissions.AttributesDelete)
  const showActions = canUpdateAttributes || canDeleteAttributes

  const { params, searchInput, setSearchInput, isSearchStale, setSorting, setPage, setPageSize, defaultPageSize } = useTableParams({ defaultPageSize: 20, tableKey: 'product-attributes' })
  const { data: attributesResponse, isLoading, error: queryError, refetch: refresh } = useProductAttributesQuery(params)
  const deleteMutation = useDeleteProductAttributeMutation()

  const attributes = attributesResponse?.items ?? []
  const { editItem: attributeToEdit, openEdit: openEditAttribute, closeEdit: closeEditAttribute } = useUrlEditDialog<ProductAttributeListItem>(attributes)
  const [attributeToDelete, setAttributeToDelete] = useState<ProductAttributeListItem | null>(null)
  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-attribute' })

  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'ProductAttribute',
    onCollectionUpdate: refresh,
  })

  const handleDelete = async () => {
    if (!attributeToDelete) return
    try {
      await deleteMutation.mutateAsync(attributeToDelete.id)
      toast.success(t('productAttributes.deleteSuccess', 'Product attribute deleted successfully'))
      setAttributeToDelete(null)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('productAttributes.deleteError', 'Failed to delete product attribute')
      toast.error(message)
    }
  }

  const columns = useMemo((): ColumnDef<ProductAttributeListItem, unknown>[] => [
    ...(showActions ? [
      createActionsColumn<ProductAttributeListItem>((attribute) => (
        <>
          {canUpdateAttributes && (
            <DropdownMenuItem className="cursor-pointer" onClick={() => openEditAttribute(attribute)}>
              <Pencil className="h-4 w-4 mr-2" />
              {t('labels.edit', 'Edit')}
            </DropdownMenuItem>
          )}
          {canDeleteAttributes && (
            <DropdownMenuItem
              className="text-destructive cursor-pointer"
              onClick={() => setAttributeToDelete(attribute)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('labels.delete', 'Delete')}
            </DropdownMenuItem>
          )}
        </>
      )),
    ] : []),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      meta: { label: t('labels.name', 'Name') },
      cell: ({ getValue }) => <span className="font-medium">{getValue()}</span>,
    }) as ColumnDef<ProductAttributeListItem, unknown>,
    ch.accessor('code', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.code', 'Code')} />,
      meta: { label: t('labels.code', 'Code') },
      cell: ({ getValue }) => <code className="text-sm bg-muted px-1.5 py-0.5 rounded">{getValue()}</code>,
    }) as ColumnDef<ProductAttributeListItem, unknown>,
    ch.accessor('type', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.type', 'Type')} />,
      meta: { label: t('labels.type', 'Type') },
      cell: ({ row }) => {
        const { label, className, icon: TypeIcon } = getTypeBadge(row.original.type, t)
        return (
          <Badge variant="outline" className={className}>
            <TypeIcon className="h-3 w-3 mr-1" />
            {label}
          </Badge>
        )
      },
    }) as ColumnDef<ProductAttributeListItem, unknown>,
    ch.accessor('valueCount', {
      id: 'values',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('productAttributes.values', 'Values')} />,
      meta: { label: t('productAttributes.values', 'Values'), align: 'center' },
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<ProductAttributeListItem, unknown>,
    ch.accessor('isFilterable', {
      id: 'filterable',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('productAttributes.filterable', 'Filterable')} />,
      meta: { label: t('productAttributes.filterable', 'Filterable'), align: 'center' },
      cell: ({ getValue }) =>
        getValue() ? <Check className="h-4 w-4 text-emerald-500 mx-auto" /> : <Minus className="h-4 w-4 text-muted-foreground mx-auto" />,
    }) as ColumnDef<ProductAttributeListItem, unknown>,
    ch.accessor('isVariantAttribute', {
      id: 'variant',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('productAttributes.variant', 'Variant')} />,
      meta: { label: t('productAttributes.variant', 'Variant'), align: 'center' },
      cell: ({ getValue }) =>
        getValue() ? <Check className="h-4 w-4 text-emerald-500 mx-auto" /> : <Minus className="h-4 w-4 text-muted-foreground mx-auto" />,
    }) as ColumnDef<ProductAttributeListItem, unknown>,
    ch.accessor('isActive', {
      id: 'status',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.status', 'Status')} />,
      meta: { label: t('labels.status', 'Status') },
      cell: ({ row }) => (
        <Badge variant="outline" className={getStatusBadgeClasses(row.original.isActive ? 'green' : 'gray')}>
          {row.original.isActive ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
        </Badge>
      ),
    }) as ColumnDef<ProductAttributeListItem, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, canUpdateAttributes, canDeleteAttributes, showActions])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: attributes,
    columns,
    tableKey: 'product-attributes',
    rowCount: attributesResponse?.totalCount ?? 0,
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: params.sorting as SortingState,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function' ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize }) : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    onSortingChange: setSorting,
    getRowId: (row) => row.id,
  })

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Tags}
        title={t('productAttributes.title', 'Product Attributes')}
        description={t('productAttributes.description', 'Manage product attributes for specifications and filtering')}
        responsive
        action={
          canCreateAttributes && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('productAttributes.newAttribute', 'New Attribute')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('productAttributes.allAttributes', 'All Attributes')}</CardTitle>
              <CardDescription>
                {attributesResponse ? t('labels.showingCountOfTotal', { count: attributes.length, total: attributesResponse.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('productAttributes.searchPlaceholder', 'Search attributes...')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              density={settings.density}
              onDensityChange={setDensity}
            />
          </div>
        </CardHeader>
        <CardContent className={`space-y-3 ${isSearchStale ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}`}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <DataTable
            table={table}
            density={settings.density}
            isLoading={isLoading}
            isStale={isSearchStale}
            onRowClick={canUpdateAttributes ? openEditAttribute : undefined}
            emptyState={
              <EmptyState
                icon={Tags}
                title={t('productAttributes.noAttributesFound', 'No attributes found')}
                description={t('productAttributes.noAttributesDescription', 'Get started by creating your first product attribute.')}
                action={canCreateAttributes ? { label: t('productAttributes.addAttribute', 'Add Attribute'), onClick: () => openCreate() } : undefined}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      <ProductAttributeDialog
        open={isCreateOpen || !!attributeToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (attributeToEdit) closeEditAttribute()
          }
        }}
        attribute={attributeToEdit}
        onSuccess={() => refresh()}
      />

      <Credenza open={!!attributeToDelete} onOpenChange={(open) => !open && setAttributeToDelete(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('productAttributes.deleteTitle', 'Delete Product Attribute')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('productAttributes.deleteDescription', {
                    name: attributeToDelete?.name,
                    defaultValue: `Are you sure you want to delete "${attributeToDelete?.name}"? This action cannot be undone.`,
                  })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setAttributeToDelete(null)} disabled={deleteMutation.isPending} className="cursor-pointer">{t('labels.cancel', 'Cancel')}</Button>
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

export default ProductAttributesPage
