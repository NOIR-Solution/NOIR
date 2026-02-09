import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useDebouncedCallback } from 'use-debounce'
import { Search, Tags, Plus, Pencil, Trash2, MoreHorizontal, Filter, List } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { useProductAttributes } from '@/hooks/useProductAttributes'
import { ProductAttributeDialog } from './components/ProductAttributeDialog'
import type { ProductAttributeListItem } from '@/types/productAttribute'
import { PageHeader } from '@/components/ui/page-header'
import { toast } from 'sonner'

export default function ProductAttributesPage() {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('ProductAttributes')

  // Permission checks
  const canCreateAttributes = hasPermission(Permissions.AttributesCreate)
  const canUpdateAttributes = hasPermission(Permissions.AttributesUpdate)
  const canDeleteAttributes = hasPermission(Permissions.AttributesDelete)

  const [searchInput, setSearchInput] = useState('')
  const [attributeToEdit, setAttributeToEdit] = useState<ProductAttributeListItem | null>(null)
  const [attributeToDelete, setAttributeToDelete] = useState<ProductAttributeListItem | null>(null)
  const [showCreateDialog, setShowCreateDialog] = useState(false)

  const { data: attributesResponse, loading, error, refresh, setSearch, handleDelete: handleDeleteAttribute } = useProductAttributes()

  const attributes = attributesResponse?.items ?? []

  // Debounced search
  const debouncedSearch = useDebouncedCallback((value: string) => {
    setSearch(value)
  }, 300)

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value
    setSearchInput(value)
    debouncedSearch(value)
  }

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setSearch(searchInput)
  }

  const handleDelete = async () => {
    if (!attributeToDelete) return
    const result = await handleDeleteAttribute(attributeToDelete.id)
    if (result.success) {
      toast.success(t('productAttributes.deleteSuccess', 'Product attribute deleted successfully'))
      setAttributeToDelete(null)
    } else {
      toast.error(result.error || t('productAttributes.deleteError', 'Failed to delete product attribute'))
    }
  }

  // Helper to get type display label
  const getTypeLabel = (type: string) => {
    const typeLabels: Record<string, string> = {
      Select: t('productAttributes.types.select', 'Select'),
      MultiSelect: t('productAttributes.types.multiSelect', 'Multi-Select'),
      Text: t('productAttributes.types.text', 'Text'),
      TextArea: t('productAttributes.types.textArea', 'Text Area'),
      Number: t('productAttributes.types.number', 'Number'),
      Decimal: t('productAttributes.types.decimal', 'Decimal'),
      Boolean: t('productAttributes.types.boolean', 'Boolean'),
      Date: t('productAttributes.types.date', 'Date'),
      DateTime: t('productAttributes.types.dateTime', 'Date Time'),
      Color: t('productAttributes.types.color', 'Color'),
      Range: t('productAttributes.types.range', 'Range'),
      Url: t('productAttributes.types.url', 'URL'),
      File: t('productAttributes.types.file', 'File'),
    }
    return typeLabels[type] || type
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <PageHeader
        icon={Tags}
        title={t('productAttributes.title', 'Product Attributes')}
        description={t('productAttributes.description', 'Manage product attributes for specifications and filtering')}
        responsive
        action={
          canCreateAttributes && (
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={() => setShowCreateDialog(true)}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('productAttributes.newAttribute', 'New Attribute')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4 backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('productAttributes.allAttributes', 'All Attributes')}</CardTitle>
              <CardDescription>
                {t('productAttributes.totalCount', { count: attributes.length, defaultValue: `${attributes.length} attributes total` })}
              </CardDescription>
            </div>
            <div className="flex items-center gap-3">
              {/* Search */}
              <form onSubmit={handleSearchSubmit} className="flex items-center gap-2">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    placeholder={t('productAttributes.searchPlaceholder', 'Search attributes...')}
                    value={searchInput}
                    onChange={handleSearchChange}
                    className="pl-10 w-full sm:w-48"
                    aria-label={t('productAttributes.searchAttributes', 'Search product attributes')}
                  />
                </div>
              </form>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
              {(error as any) instanceof Error ? (error as unknown as Error).message : t('errors.generic', 'An error occurred')}
            </div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[20%]">{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('labels.code', 'Code')}</TableHead>
                  <TableHead>{t('labels.type', 'Type')}</TableHead>
                  <TableHead className="text-center">{t('productAttributes.values', 'Values')}</TableHead>
                  <TableHead className="text-center">
                    <Filter className="h-4 w-4 inline mr-1" />
                    {t('productAttributes.filterable', 'Filterable')}
                  </TableHead>
                  <TableHead className="text-center">
                    <List className="h-4 w-4 inline mr-1" />
                    {t('productAttributes.variant', 'Variant')}
                  </TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  // Skeleton loading
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-24 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-12 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-12 mx-auto rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-8 w-8 rounded ml-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : attributes.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} className="p-0">
                      <EmptyState
                        icon={Tags}
                        title={t('productAttributes.noAttributesFound', 'No attributes found')}
                        description={t('productAttributes.noAttributesDescription', 'Get started by creating your first product attribute.')}
                        action={canCreateAttributes ? {
                          label: t('productAttributes.addAttribute', 'Add Attribute'),
                          onClick: () => setShowCreateDialog(true),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  attributes.map((attribute) => (
                    <TableRow key={attribute.id} className="group transition-colors hover:bg-muted/50">
                      <TableCell>
                        <span className="font-medium">{attribute.name}</span>
                      </TableCell>
                      <TableCell>
                        <code className="text-sm bg-muted px-1.5 py-0.5 rounded">
                          {attribute.code}
                        </code>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline">{getTypeLabel(attribute.type)}</Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">{attribute.valueCount}</Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant={attribute.isFilterable ? 'default' : 'secondary'}>
                          {attribute.isFilterable ? t('labels.yes', 'Yes') : t('labels.no', 'No')}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant={attribute.isVariantAttribute ? 'default' : 'secondary'}>
                          {attribute.isVariantAttribute ? t('labels.yes', 'Yes') : t('labels.no', 'No')}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant={attribute.isActive ? 'default' : 'secondary'}>
                          {attribute.isActive ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: attribute.name, defaultValue: `Actions for ${attribute.name}` })}
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            {canUpdateAttributes && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={() => setAttributeToEdit(attribute)}
                              >
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

      {/* Create/Edit Attribute Dialog */}
      <ProductAttributeDialog
        open={showCreateDialog || !!attributeToEdit}
        onOpenChange={(open) => {
          if (!open) {
            setShowCreateDialog(false)
            setAttributeToEdit(null)
          }
        }}
        attribute={attributeToEdit}
        onSuccess={() => refresh()}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={!!attributeToDelete} onOpenChange={(open) => !open && setAttributeToDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <AlertDialogTitle>{t('productAttributes.deleteTitle', 'Delete Product Attribute')}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t('productAttributes.deleteDescription', {
                    name: attributeToDelete?.name,
                    defaultValue: `Are you sure you want to delete "${attributeToDelete?.name}"? This action cannot be undone.`
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
