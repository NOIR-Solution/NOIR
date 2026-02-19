import { useState, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Award, Plus, Pencil, Trash2, MoreHorizontal, Globe, ExternalLink, ChevronLeft, ChevronRight } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
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

import { useBrandsQuery, useDeleteBrandMutation } from '@/portal-app/brands/queries'
import type { GetBrandsParams } from '@/services/brands'
import { BrandDialog } from '../../components/BrandDialog'
import type { BrandListItem } from '@/types/brand'

import { toast } from 'sonner'

export const BrandsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('Brands')

  // Permission checks
  const canCreateBrands = hasPermission(Permissions.BrandsCreate)
  const canUpdateBrands = hasPermission(Permissions.BrandsUpdate)
  const canDeleteBrands = hasPermission(Permissions.BrandsDelete)

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [isFilterPending, startFilterTransition] = useTransition()
  const [brandToEdit, setBrandToEdit] = useState<BrandListItem | null>(null)
  const [brandToDelete, setBrandToDelete] = useState<BrandListItem | null>(null)
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [params, setParams] = useState<GetBrandsParams>({ page: 1, pageSize: 20 })

  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
  const { data: brandsResponse, isLoading: loading, error: queryError, refetch: refresh } = useBrandsQuery(queryParams)
  const deleteMutation = useDeleteBrandMutation()
  const error = queryError?.message ?? null

  const brands = brandsResponse?.items ?? []
  const totalCount = brandsResponse?.totalCount ?? 0
  const totalPages = brandsResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
    startFilterTransition(() => setParams((prev) => ({ ...prev, page: 1 })))
  }

  const handlePageChange = (page: number) => {
    startFilterTransition(() => {
      setParams(prev => ({ ...prev, page }))
    })
  }

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

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Award}
        title={t('brands.title', 'Brands')}
        description={t('brands.description', 'Manage product brands and manufacturers')}
        responsive
        action={
          canCreateBrands && (
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={() => setShowCreateDialog(true)}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('brands.newBrand', 'New Brand')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('brands.allBrands', 'All Brands')}</CardTitle>
              <CardDescription>
                {t('brands.totalCount', { count: totalCount, defaultValue: `${totalCount} brands total` })}
              </CardDescription>
            </div>
            <div className="flex items-center gap-3">
              {/* Search */}
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('brands.searchPlaceholder', 'Search brands...')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-10 w-full sm:w-48"
                  aria-label={t('brands.searchBrands', 'Search brands')}
                />
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
              {error}
            </div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[80px]">{t('labels.logo', 'Logo')}</TableHead>
                  <TableHead className="w-[25%]">{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('labels.slug', 'Slug')}</TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead className="text-center">{t('brands.products', 'Products')}</TableHead>
                  <TableHead>{t('labels.website', 'Website')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  // Skeleton loading
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell>
                        <Skeleton className="h-10 w-10 rounded-lg" />
                      </TableCell>
                      <TableCell>
                        <Skeleton className="h-4 w-32" />
                      </TableCell>
                      <TableCell><Skeleton className="h-5 w-24 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-8 w-8 rounded ml-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : brands.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="p-0">
                      <EmptyState
                        icon={Award}
                        title={t('brands.noBrandsFound', 'No brands found')}
                        description={t('brands.noBrandsDescription', 'Get started by creating your first brand.')}
                        action={canCreateBrands ? {
                          label: t('brands.addBrand', 'Add Brand'),
                          onClick: () => setShowCreateDialog(true),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  brands.map((brand) => (
                    <TableRow key={brand.id} className="group transition-colors hover:bg-muted/50">
                      <TableCell>
                        {brand.logoUrl ? (
                          <img
                            src={brand.logoUrl}
                            alt={brand.name}
                            className="h-10 w-10 rounded-lg object-contain bg-muted"
                          />
                        ) : (
                          <div className="h-10 w-10 rounded-lg bg-muted flex items-center justify-center">
                            <Award className="h-5 w-5 text-muted-foreground" />
                          </div>
                        )}
                      </TableCell>
                      <TableCell>
                        <span className="font-medium">{brand.name}</span>
                        {brand.description && (
                          <p className="text-sm text-muted-foreground line-clamp-1 mt-0.5">
                            {brand.description}
                          </p>
                        )}
                      </TableCell>
                      <TableCell>
                        <code className="text-sm bg-muted px-1.5 py-0.5 rounded">
                          {brand.slug}
                        </code>
                      </TableCell>
                      <TableCell>
                        <Badge variant={brand.isActive ? 'default' : 'secondary'}>
                          {brand.isActive ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">{brand.productCount}</Badge>
                      </TableCell>
                      <TableCell>
                        {brand.websiteUrl ? (
                          <a
                            href={brand.websiteUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="flex items-center gap-1 text-sm text-primary hover:underline"
                          >
                            <Globe className="h-3 w-3" />
                            <span className="truncate max-w-[120px]">{new URL(brand.websiteUrl).hostname}</span>
                            <ExternalLink className="h-3 w-3" />
                          </a>
                        ) : (
                          <span className="text-muted-foreground">-</span>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: brand.name, defaultValue: `Actions for ${brand.name}` })}
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            {canUpdateBrands && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={() => setBrandToEdit(brand)}
                              >
                                <Pencil className="h-4 w-4 mr-2" />
                                {t('labels.edit', 'Edit')}
                              </DropdownMenuItem>
                            )}
                            {canDeleteBrands && (
                              <DropdownMenuItem
                                className="text-destructive cursor-pointer"
                                onClick={() => setBrandToDelete(brand)}
                              >
                                <Trash2 className="h-4 w-4 mr-2" />
                                {t('labels.delete', 'Delete')}
                              </DropdownMenuItem>
                            )}
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
          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <p className="text-sm text-muted-foreground">
                {t('labels.pageOf', { current: currentPage, total: totalPages, defaultValue: `Page ${currentPage} of ${totalPages}` })}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="cursor-pointer"
                  disabled={currentPage <= 1}
                  onClick={() => handlePageChange(currentPage - 1)}
                  aria-label={t('labels.previousPage', 'Previous page')}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  className="cursor-pointer"
                  disabled={currentPage >= totalPages}
                  onClick={() => handlePageChange(currentPage + 1)}
                  aria-label={t('labels.nextPage', 'Next page')}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Brand Dialog */}
      <BrandDialog
        open={showCreateDialog || !!brandToEdit}
        onOpenChange={(open) => {
          if (!open) {
            setShowCreateDialog(false)
            setBrandToEdit(null)
          }
        }}
        brand={brandToEdit}
        onSuccess={() => refresh()}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={!!brandToDelete} onOpenChange={(open) => !open && setBrandToDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <AlertDialogTitle>{t('brands.deleteTitle', 'Delete Brand')}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t('brands.deleteDescription', {
                    name: brandToDelete?.name,
                    defaultValue: `Are you sure you want to delete "${brandToDelete?.name}"? This action cannot be undone.`
                  })}
                </AlertDialogDescription>
              </div>
            </div>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">{t('labels.cancel', 'Cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {t('labels.delete', 'Delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

export default BrandsPage
