import { useState, useDeferredValue, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Building2,
  GitBranch,
  List,
  Pencil,
  Plus,
  Trash2,
} from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { useRowHighlight } from '@/hooks/useRowHighlight'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { createActionsColumn } from '@/lib/table/columnHelpers'
import { OfflineBanner } from '@/components/OfflineBanner'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  CategoryTreeView,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  EmptyState,
  PageHeader,
  ViewModeToggle,
  type ViewModeOption,
  type TreeCategory,
  type ReorderItem,
} from '@uikit'
import { useDepartmentsQuery, useReorderDepartments } from '@/portal-app/hr/queries'
import type { DepartmentTreeNodeDto } from '@/types/hr'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { DepartmentFormDialog } from '../../components/DepartmentFormDialog'
import { DeleteDepartmentDialog } from '../../components/DeleteDepartmentDialog'

// Extended flat item that satisfies TreeCategory + keeps department-specific fields
interface DepartmentFlatItem extends TreeCategory {
  code: string
  isActive: boolean
  managerName?: string | null
  employeeCount: number
}

// Flatten tree → flat list for CategoryTreeView
const flattenDepartmentTree = (
  nodes: DepartmentTreeNodeDto[],
  parentId: string | null = null,
): DepartmentFlatItem[] =>
  nodes.flatMap((node, index) => [
    {
      id: node.id,
      name: node.name,
      slug: node.code,
      code: node.code,
      description: undefined,
      sortOrder: index,
      parentId,
      parentName: null,
      childCount: node.children.length,
      itemCount: node.employeeCount,
      isActive: node.isActive,
      managerName: node.managerName,
      employeeCount: node.employeeCount,
    },
    ...flattenDepartmentTree(node.children, node.id),
  ])

const ch = createColumnHelper<DepartmentFlatItem>()

export const DepartmentsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Departments')

  const { getRowAnimationClass } = useRowHighlight()

  const { params, searchInput, setSearchInput, isSearchStale, setPage, setPageSize, defaultPageSize } = useTableParams({ defaultPageSize: 20, tableKey: 'departments' })

  const [viewMode, setViewMode] = useState<'table' | 'tree'>('tree')
  const viewModeOptions: ViewModeOption<'table' | 'tree'>[] = useMemo(() => [
    { value: 'table', label: t('labels.list', 'List'), icon: List, ariaLabel: t('labels.tableView', 'Table view') },
    { value: 'tree', label: t('labels.tree', 'Tree'), icon: GitBranch, ariaLabel: t('labels.treeView', 'Tree view') },
  ], [t])

  const { data: departments, isLoading: loading, error: queryError, refetch: refresh } = useDepartmentsQuery()
  const reorderMutation = useReorderDepartments()
  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Department',
    onCollectionUpdate: refresh,
  })

  const flatDepartments = useMemo(
    () => (departments ? flattenDepartmentTree(departments) : []),
    [departments],
  )

  const deferredSearch = useDeferredValue(searchInput)
  const filteredDepartments = useMemo(() => {
    if (!deferredSearch) return flatDepartments
    const q = deferredSearch.toLowerCase()
    return flatDepartments.filter(
      (d) => d.name.toLowerCase().includes(q) || d.code.toLowerCase().includes(q),
    )
  }, [flatDepartments, deferredSearch])

  const paginatedDepartments = useMemo(() => {
    const start = (params.page - 1) * params.pageSize
    return filteredDepartments.slice(start, start + params.pageSize)
  }, [filteredDepartments, params.page, params.pageSize])

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-department' })
  const { editItem: departmentToEdit, openEdit: openEditDepartment, closeEdit: closeEditDepartment } = useUrlEditDialog<DepartmentFlatItem>(flatDepartments)

  const [parentIdForCreate, setParentIdForCreate] = useState<string | null>(null)
  const [departmentToDelete, setDepartmentToDelete] = useState<DepartmentFlatItem | null>(null)

  const handleReorder = (items: ReorderItem[]) => {
    reorderMutation.mutate({
      items: items.map(i => ({
        id: i.id,
        parentDepartmentId: i.parentId ?? null,
        sortOrder: i.sortOrder,
      })),
    })
  }

  const columns = useMemo((): ColumnDef<DepartmentFlatItem, unknown>[] => [
    createActionsColumn<DepartmentFlatItem>((dept) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => openEditDepartment(dept)}>
          <Pencil className="h-4 w-4 mr-2" />
          {t('labels.edit', 'Edit')}
        </DropdownMenuItem>
        <DropdownMenuItem className="cursor-pointer" onClick={() => { setParentIdForCreate(dept.id); openCreate() }}>
          <Plus className="h-4 w-4 mr-2" />
          {t('hr.addSubDepartment')}
        </DropdownMenuItem>
        <DropdownMenuItem
          className="text-destructive cursor-pointer"
          onClick={() => setDepartmentToDelete(dept)}
        >
          <Trash2 className="h-4 w-4 mr-2" />
          {t('labels.delete', 'Delete')}
        </DropdownMenuItem>
      </>
    )),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      meta: { label: t('labels.name', 'Name') },
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <Building2 className="h-4 w-4 text-muted-foreground shrink-0" />
          <span className="font-medium">{row.original.name}</span>
          <Badge variant="outline" className="text-xs font-mono">{row.original.code}</Badge>
          {!row.original.isActive && (
            <Badge variant="outline" className={getStatusBadgeClasses('gray')}>
              {t('labels.inactive', 'Inactive')}
            </Badge>
          )}
        </div>
      ),
    }) as ColumnDef<DepartmentFlatItem, unknown>,
    ch.accessor('managerName', {
      id: 'manager',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.manager')} />,
      meta: { label: t('hr.manager') },
      cell: ({ getValue }) => (
        <span className="text-sm text-muted-foreground">{getValue() || '—'}</span>
      ),
    }) as ColumnDef<DepartmentFlatItem, unknown>,
    ch.accessor((row) => row.parentId, {
      id: 'parentDepartment',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.parentDepartment')} />,
      meta: { label: t('hr.parentDepartment') },
      enableSorting: false,
      cell: ({ row }) => {
        const parentName = row.original.parentId
          ? flatDepartments.find(d => d.id === row.original.parentId)?.name ?? '—'
          : '—'
        return <span className="text-sm text-muted-foreground">{parentName}</span>
      },
    }) as ColumnDef<DepartmentFlatItem, unknown>,
    ch.accessor('employeeCount', {
      id: 'employeeCount',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.employees', 'Employees')} />,
      meta: { label: t('hr.employees', 'Employees'), align: 'center' },
      cell: ({ getValue }) => <Badge variant="secondary">{getValue()}</Badge>,
    }) as ColumnDef<DepartmentFlatItem, unknown>,
    ch.accessor('childCount', {
      id: 'subDepartments',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.subDepartments', 'Sub-depts')} />,
      meta: { label: t('hr.subDepartments', 'Sub-depts'), align: 'center' },
      cell: ({ getValue }) => (
        getValue() > 0 ? (
          <Badge variant="outline">{getValue()}</Badge>
        ) : (
          <span className="text-muted-foreground">—</span>
        )
      ),
    }) as ColumnDef<DepartmentFlatItem, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, flatDepartments])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: paginatedDepartments,
    columns,
    tableKey: 'departments',
    rowCount: filteredDepartments.length,
    manualSorting: false,
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: [],
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function' ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize }) : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    getRowId: (row) => row.id,
  })

  const displayCount = viewMode === 'tree' ? filteredDepartments.length : paginatedDepartments.length
  const displayTotal = viewMode === 'tree' ? flatDepartments.length : filteredDepartments.length

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Building2}
        title={t('hr.departments')}
        description={t('hr.departmentsDescription', 'Manage your organization\'s department hierarchy')}
        responsive
        action={
          <Button
            className="group transition-all duration-300 cursor-pointer"
            onClick={() => { setParentIdForCreate(null); openCreate() }}
          >
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('hr.createDepartment')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg">{t('hr.departmentHierarchy', 'Department Hierarchy')}</CardTitle>
                <CardDescription>
                  {flatDepartments.length > 0
                    ? t('labels.showingCountOfTotal', { count: displayCount, total: displayTotal })
                    : t('hr.departmentHierarchyDescription', 'View and manage your organizational structure')}
                </CardDescription>
              </div>
              <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('hr.searchDepartments', 'Search departments...')}
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

        <CardContent className={isSearchStale ? 'space-y-3 opacity-70 transition-opacity duration-200' : 'space-y-3 transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          {viewMode === 'tree' ? (
            <div className="rounded-xl border border-border/50 p-4">
              <CategoryTreeView
                maxHeight="100%"
                categories={filteredDepartments}
                loading={loading}
                onEdit={(dept) => openEditDepartment(dept as DepartmentFlatItem)}
                onDelete={(dept) => setDepartmentToDelete(dept as DepartmentFlatItem)}
                onAddChild={(dept) => { setParentIdForCreate((dept as DepartmentFlatItem).id); openCreate() }}
                canEdit={true}
                canDelete={true}
                itemCountLabel={t('hr.employees', 'employees')}
                emptyMessage={t('hr.noDepartmentsFound')}
                emptyDescription={t('hr.noDepartmentsDescription')}
                onCreateClick={() => { setParentIdForCreate(null); openCreate() }}
                onReorder={handleReorder}
              />
            </div>
          ) : (
            <>
              <DataTable
                table={table}
                density={settings.density}
                isLoading={loading}
                isStale={isSearchStale}
                onRowClick={openEditDepartment}
                getRowAnimationClass={getRowAnimationClass}
                emptyState={
                  <EmptyState
                    icon={Building2}
                    title={t('hr.noDepartmentsFound')}
                    description={t('hr.noDepartmentsDescription')}
                    action={{
                      label: t('hr.createDepartment'),
                      onClick: () => { setParentIdForCreate(null); openCreate() },
                    }}
                  />
                }
              />
              <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
            </>
          )}
        </CardContent>
      </Card>

      <DepartmentFormDialog
        open={isCreateOpen || !!departmentToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (departmentToEdit) closeEditDepartment()
            setParentIdForCreate(null)
          }
        }}
        department={departmentToEdit ? { ...departmentToEdit, children: [] } : null}
        parentDepartmentId={!departmentToEdit ? parentIdForCreate : undefined}
      />

      <DeleteDepartmentDialog
        department={departmentToDelete}
        open={!!departmentToDelete}
        onOpenChange={(open) => !open && setDepartmentToDelete(null)}
      />
    </div>
  )
}

export default DepartmentsPage
