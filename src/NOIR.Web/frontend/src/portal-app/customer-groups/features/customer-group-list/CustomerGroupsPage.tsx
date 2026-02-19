import { useState, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, UsersRound, Plus, Pencil, Trash2, MoreHorizontal, ChevronLeft, ChevronRight } from 'lucide-react'
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

import { useCustomerGroupsQuery, useDeleteCustomerGroupMutation } from '@/portal-app/customer-groups/queries'
import type { GetCustomerGroupsParams } from '@/services/customerGroups'
import { CustomerGroupDialog } from '../../components/CustomerGroupDialog'
import type { CustomerGroupListItem } from '@/types/customerGroup'

import { toast } from 'sonner'

export const CustomerGroupsPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  usePageContext('CustomerGroups')

  // Permission checks
  const canCreateGroups = hasPermission(Permissions.CustomerGroupsCreate)
  const canUpdateGroups = hasPermission(Permissions.CustomerGroupsUpdate)
  const canDeleteGroups = hasPermission(Permissions.CustomerGroupsDelete)

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [isFilterPending, startFilterTransition] = useTransition()
  const [groupToEdit, setGroupToEdit] = useState<CustomerGroupListItem | null>(null)
  const [groupToDelete, setGroupToDelete] = useState<CustomerGroupListItem | null>(null)
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [params, setParams] = useState<GetCustomerGroupsParams>({ page: 1, pageSize: 20 })

  const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
  const { data: groupsResponse, isLoading: loading, error: queryError, refetch: refresh } = useCustomerGroupsQuery(queryParams)
  const deleteMutation = useDeleteCustomerGroupMutation()
  const error = queryError?.message ?? null

  const groups = groupsResponse?.items ?? []
  const totalCount = groupsResponse?.totalCount ?? 0
  const totalPages = groupsResponse?.totalPages ?? 1
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
    if (!groupToDelete) return
    try {
      await deleteMutation.mutateAsync(groupToDelete.id)
      toast.success(t('customerGroups.deleteSuccess', 'Customer group deleted successfully'))
      setGroupToDelete(null)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('customerGroups.deleteError', 'Failed to delete customer group')
      toast.error(message)
    }
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      <PageHeader
        icon={UsersRound}
        title={t('customerGroups.title', 'Customer Groups')}
        description={t('customerGroups.description', 'Manage customer groups for segmentation and targeting')}
        responsive
        action={
          canCreateGroups && (
            <Button className="group shadow-lg hover:shadow-xl transition-all duration-300" onClick={() => setShowCreateDialog(true)}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('customerGroups.newGroup', 'New Group')}
            </Button>
          )
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4 backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('customerGroups.allGroups', 'All Groups')}</CardTitle>
              <CardDescription>
                {t('customerGroups.totalCount', { count: totalCount, defaultValue: `${totalCount} groups total` })}
              </CardDescription>
            </div>
            <div className="flex items-center gap-3">
              {/* Search */}
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('customerGroups.searchPlaceholder', 'Search groups...')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-10 w-full sm:w-48"
                  aria-label={t('customerGroups.searchGroups', 'Search customer groups')}
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
                  <TableHead className="w-[30%]">{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('labels.slug', 'Slug')}</TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead className="text-center">{t('customerGroups.members', 'Members')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  // Skeleton loading
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell>
                        <Skeleton className="h-4 w-32" />
                      </TableCell>
                      <TableCell><Skeleton className="h-5 w-24 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-8 w-8 rounded ml-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : groups.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="p-0">
                      <EmptyState
                        icon={UsersRound}
                        title={t('customerGroups.noGroupsFound', 'No customer groups found')}
                        description={t('customerGroups.noGroupsDescription', 'Get started by creating your first customer group.')}
                        action={canCreateGroups ? {
                          label: t('customerGroups.addGroup', 'Add Group'),
                          onClick: () => setShowCreateDialog(true),
                        } : undefined}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  groups.map((group) => (
                    <TableRow key={group.id} className="group transition-colors hover:bg-muted/50">
                      <TableCell>
                        <span className="font-medium">{group.name}</span>
                        {group.description && (
                          <p className="text-sm text-muted-foreground line-clamp-1 mt-0.5">
                            {group.description}
                          </p>
                        )}
                      </TableCell>
                      <TableCell>
                        <code className="text-sm bg-muted px-1.5 py-0.5 rounded">
                          {group.slug}
                        </code>
                      </TableCell>
                      <TableCell>
                        <Badge variant={group.isActive ? 'default' : 'secondary'}>
                          {group.isActive ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-center">
                        <Badge variant="secondary">{group.memberCount}</Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: group.name, defaultValue: `Actions for ${group.name}` })}
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            {canUpdateGroups && (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={() => setGroupToEdit(group)}
                              >
                                <Pencil className="h-4 w-4 mr-2" />
                                {t('labels.edit', 'Edit')}
                              </DropdownMenuItem>
                            )}
                            {canDeleteGroups && (
                              <DropdownMenuItem
                                className="text-destructive cursor-pointer"
                                onClick={() => setGroupToDelete(group)}
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

      {/* Create/Edit Customer Group Dialog */}
      <CustomerGroupDialog
        open={showCreateDialog || !!groupToEdit}
        onOpenChange={(open) => {
          if (!open) {
            setShowCreateDialog(false)
            setGroupToEdit(null)
          }
        }}
        group={groupToEdit}
        onSuccess={() => refresh()}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={!!groupToDelete} onOpenChange={(open) => !open && setGroupToDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <AlertDialogTitle>{t('customerGroups.deleteTitle', 'Delete Customer Group')}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t('customerGroups.deleteDescription', {
                    name: groupToDelete?.name,
                    defaultValue: `Are you sure you want to delete "${groupToDelete?.name}"? This action cannot be undone.`
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

export default CustomerGroupsPage
