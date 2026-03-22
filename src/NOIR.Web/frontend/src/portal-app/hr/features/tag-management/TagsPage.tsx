import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef } from '@tanstack/react-table'
import {
  Pencil,
  Plus,
  Tags,
  Trash2,
  Users,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { useRowHighlight } from '@/hooks/useRowHighlight'
import { useDelayedLoading } from '@/hooks/useDelayedLoading'
import { createActionsColumn, createFullAuditColumns } from '@/lib/table/columnHelpers'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

type BadgeColor = Parameters<typeof getStatusBadgeClasses>[0]
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  ColorPopover,
  DataTable,
  DataTableColumnHeader,
  DataTableToolbar,
  DropdownMenuItem,
  EmptyState,
  PageHeader,
} from '@uikit'
import { useTagsQuery, useDeleteTag } from '@/portal-app/hr/queries'
import { TagFormDialog } from '../../components/TagFormDialog'
import { DeleteEmployeeTagDialog } from '../../components/DeleteEmployeeTagDialog'
import type { EmployeeTagDto, EmployeeTagCategory } from '@/types/hr'

const ch = createColumnHelper<EmployeeTagDto>()

const CATEGORY_COLORS: Record<EmployeeTagCategory, BadgeColor> = {
  Team: 'blue',
  Skill: 'green',
  Project: 'purple',
  Location: 'yellow',
  Seniority: 'gray',
  Employment: 'red',
  Custom: 'gray',
}

export const TagsPage = () => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Tags')

  const { hasPermission } = usePermissions()
  const canManage = hasPermission(Permissions.HrTagsManage)
  const { getRowAnimationClass, fadeOutRow } = useRowHighlight()

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-employee-tag' })
  const [tagToDelete, setTagToDelete] = useState<EmployeeTagDto | null>(null)

  const { params, searchInput, setSearchInput, isSearchStale } = useTableParams({ defaultPageSize: 1000 })
  const { data: tags, isLoading, isPlaceholderData, refetch } = useTagsQuery()
  const isContentStale = useDelayedLoading(isSearchStale || isPlaceholderData)

  const allTags = tags ?? []

  // Client-side search filter
  const filteredTags = useMemo(() => {
    if (!params.search) return allTags
    const search = params.search.toLowerCase()
    return allTags.filter(tag =>
      tag.name.toLowerCase().includes(search) ||
      tag.description?.toLowerCase().includes(search) ||
      tag.category.toLowerCase().includes(search),
    )
  }, [allTags, params.search])

  const { editItem: tagToEdit, openEdit: openEditTag, closeEdit: closeEditTag } = useUrlEditDialog<EmployeeTagDto>(allTags)
  const deleteMutation = useDeleteTag()

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'EmployeeTag',
    onCollectionUpdate: refetch,
  })

  const handleDelete = async () => {
    if (!tagToDelete) return
    try {
      await fadeOutRow(tagToDelete.id)
      await deleteMutation.mutateAsync(tagToDelete.id)
      toast.success(t('hr.tags.tagDeleted'))
      setTagToDelete(null)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }

  const columns = useMemo((): ColumnDef<EmployeeTagDto, unknown>[] => [
    ...(canManage ? [createActionsColumn<EmployeeTagDto>((tag) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => openEditTag(tag)}>
          <Pencil className="h-4 w-4 mr-2" />
          {t('labels.edit', 'Edit')}
        </DropdownMenuItem>
        <DropdownMenuItem
          className="text-destructive cursor-pointer"
          onClick={() => setTagToDelete(tag)}
        >
          <Trash2 className="h-4 w-4 mr-2" />
          {t('labels.delete', 'Delete')}
        </DropdownMenuItem>
      </>
    ))] : []),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      meta: { label: t('labels.name', 'Name') },
      cell: ({ row }) => (
        <div className="flex items-center gap-2.5">
          {row.original.color ? (
            <ColorPopover color={row.original.color} />
          ) : (
            <div className="w-4 h-4 rounded-full bg-muted border border-border shrink-0" />
          )}
          <span className="font-medium">{row.original.name}</span>
        </div>
      ),
    }) as ColumnDef<EmployeeTagDto, unknown>,
    ch.accessor('category', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.tags.category')} />,
      meta: {
        label: t('hr.tags.category'),
        groupValueFormatter: (v: unknown) => t(`hr.tags.categories.${String(v)}`, String(v)),
      },
      enableGrouping: true,
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getStatusBadgeClasses(CATEGORY_COLORS[getValue() as EmployeeTagCategory] || 'gray')}>
          {t(`hr.tags.categories.${getValue()}`)}
        </Badge>
      ),
    }) as ColumnDef<EmployeeTagDto, unknown>,
    ch.accessor('description', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.tags.descriptionLabel')} />,
      meta: { label: t('hr.tags.descriptionLabel') },
      cell: ({ getValue }) => (
        <span className="text-muted-foreground text-sm truncate max-w-[200px] block">
          {getValue() || '—'}
        </span>
      ),
    }) as ColumnDef<EmployeeTagDto, unknown>,
    ch.accessor('employeeCount', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.employees', 'Employees')} />,
      meta: { align: 'center' as const, label: t('hr.employees', 'Employees') },
      size: 100,
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getStatusBadgeClasses(getValue() as number > 0 ? 'blue' : 'gray')}>
          <Users className="h-3 w-3 mr-1" />
          {getValue() as number}
        </Badge>
      ),
    }) as ColumnDef<EmployeeTagDto, unknown>,
    {
      id: 'sortOrder',
      accessorFn: (row) => row.sortOrder,
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.tags.sortOrder')} />,
      meta: { label: t('hr.tags.sortOrder'), defaultHidden: true },
      size: 80,
    } as ColumnDef<EmployeeTagDto, unknown>,
    ...createFullAuditColumns<EmployeeTagDto>(t, formatDateTime),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, formatDateTime, canManage])

  const tableData = useMemo(() => filteredTags, [filteredTags])

  const { table, settings, isCustomized, resetToDefault } = useEnterpriseTable({
    data: tableData,
    columns,
    tableKey: 'employee-tags',
    rowCount: filteredTags.length,
    manualSorting: false,
    enableGrouping: true,
    state: {
      pagination: { pageIndex: 0, pageSize: 1000 },
      sorting: [],
    },
    onPaginationChange: () => {},
    getRowId: (row) => row.id,
  })

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Tags}
        title={t('hr.tags.title')}
        description={t('hr.tags.description')}
        responsive
        action={
          canManage ? (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('hr.tags.createTag')}
            </Button>
          ) : undefined
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('hr.tags.title')}</CardTitle>
              <CardDescription>
                {t('labels.showingCountOfTotal', { count: filteredTags.length, total: allTags.length })}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('hr.tags.searchPlaceholder', 'Search tags...')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
            />
          </div>
        </CardHeader>
        <CardContent className={isContentStale
          ? 'space-y-3 opacity-70 transition-opacity duration-200'
          : 'space-y-3 transition-opacity duration-200'}>
          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isContentStale}
            getRowAnimationClass={getRowAnimationClass}
            onRowClick={canManage ? openEditTag : undefined}
            emptyState={
              <EmptyState
                icon={Tags}
                title={t('hr.tags.noTags')}
                description={t('hr.tags.noTagsDescription')}
                action={canManage ? {
                  label: t('hr.tags.createTag'),
                  onClick: () => openCreate(),
                } : undefined}
              />
            }
          />
        </CardContent>
      </Card>

      {/* Create/Edit Tag Dialog */}
      <TagFormDialog
        open={isCreateOpen || !!tagToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (tagToEdit) closeEditTag()
          }
        }}
        tag={tagToEdit}
      />

      {/* Delete Confirmation Dialog */}
      <DeleteEmployeeTagDialog
        tag={tagToDelete}
        open={!!tagToDelete}
        onOpenChange={(open) => !open && setTagToDelete(null)}
        onConfirm={handleDelete}
        isPending={deleteMutation.isPending}
      />
    </div>
  )
}

export default TagsPage
