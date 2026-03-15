import { useMemo, useState, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Eye,
  Pencil,
  Plus,
  Users,
  UserX,
  UserCheck,
  Download,
  Upload,
  Tags,
  Building2,
} from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { useRowHighlight } from '@/hooks/useRowHighlight'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable, useSelectedIds } from '@/hooks/useEnterpriseTable'
import { createSelectColumn, createActionsColumn } from '@/lib/table/columnHelpers'
import { aggregatedCells } from '@/lib/table/aggregationHelpers'
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'
import {
  useEmployeesQuery,
  useDepartmentsQuery,
  useDeactivateEmployee,
  useReactivateEmployee,
  useTagsQuery,
  useBulkAssignTags,
  useBulkChangeDepartment,
  useImportEmployees,
} from '@/portal-app/hr/queries'
import { exportEmployees } from '@/services/hr'
import type { EmployeeListDto, EmployeeStatus, EmploymentType } from '@/types/hr'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { EmployeeFormDialog } from '../../components/EmployeeFormDialog'
import { TagChips } from '../../components/TagChips'

const EMPLOYEE_STATUSES: EmployeeStatus[] = ['Active', 'Suspended', 'Resigned', 'Terminated']
const EMPLOYMENT_TYPES: EmploymentType[] = ['FullTime', 'PartTime', 'Contract', 'Intern']

const getEmployeeStatusColor = (status: EmployeeStatus) => {
  switch (status) {
    case 'Active': return getStatusBadgeClasses('green')
    case 'Suspended': return getStatusBadgeClasses('yellow')
    case 'Resigned': return getStatusBadgeClasses('gray')
    case 'Terminated': return getStatusBadgeClasses('red')
    default: return getStatusBadgeClasses('gray')
  }
}

const getEmploymentTypeColor = (type: EmploymentType) => {
  switch (type) {
    case 'FullTime': return getStatusBadgeClasses('blue')
    case 'PartTime': return getStatusBadgeClasses('purple')
    case 'Contract': return getStatusBadgeClasses('orange')
    case 'Intern': return getStatusBadgeClasses('cyan')
    default: return getStatusBadgeClasses('gray')
  }
}

const ch = createColumnHelper<EmployeeListDto>()

export const EmployeesPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  usePageContext('Employees')

  const { getRowAnimationClass } = useRowHighlight()

  const [employeeToDeactivate, setEmployeeToDeactivate] = useState<EmployeeListDto | null>(null)
  const [bulkTagDialogOpen, setBulkTagDialogOpen] = useState(false)
  const [bulkDeptDialogOpen, setBulkDeptDialogOpen] = useState(false)
  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [selectedTagIds, setSelectedTagIds] = useState<Set<string>>(new Set())
  const [selectedDeptId, setSelectedDeptId] = useState<string>('')
  const fileInputRef = useRef<HTMLInputElement>(null)

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
  } = useTableParams<{ departmentId?: string; status?: EmployeeStatus; employmentType?: EmploymentType }>({ defaultPageSize: 20, tableKey: 'employees' })

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-employee' })

  const { data: employeesResponse, isLoading, error: queryError, refetch } = useEmployeesQuery(params)
  const { data: departments } = useDepartmentsQuery()
  const { data: allTags } = useTagsQuery({ isActive: true })
  const deactivateMutation = useDeactivateEmployee()
  const reactivateMutation = useReactivateEmployee()
  const bulkAssignTagsMutation = useBulkAssignTags()
  const bulkChangeDeptMutation = useBulkChangeDepartment()
  const importMutation = useImportEmployees()
  const error = queryError?.message ?? null

  const employees = employeesResponse?.items ?? []

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Employee',
    onCollectionUpdate: refetch,
  })
  const { editItem: employeeToEdit, openEdit: openEditEmployee, closeEdit: closeEditEmployee } = useUrlEditDialog<EmployeeListDto>(employees)

  const flattenDepartments = (nodes: typeof departments, prefix = ''): { id: string; name: string }[] => {
    if (!nodes) return []
    return nodes.flatMap(node => [
      { id: node.id, name: prefix + node.name },
      ...flattenDepartments(node.children, prefix + '  '),
    ])
  }
  const flatDepartments = flattenDepartments(departments)

  const handleViewEmployee = (employee: EmployeeListDto) => {
    navigate(`/portal/hr/employees/${employee.id}`)
  }

  const handleDeactivateConfirm = async () => {
    if (!employeeToDeactivate) return
    try {
      await deactivateMutation.mutateAsync({ id: employeeToDeactivate.id, status: 'Resigned' })
      toast.success(t('hr.employeeDeactivated', 'Employee deactivated'))
      setEmployeeToDeactivate(null)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }

  const handleReactivate = async (employee: EmployeeListDto) => {
    try {
      await reactivateMutation.mutateAsync(employee.id)
      toast.success(t('hr.employeeReactivated', 'Employee reactivated'))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }

  const handleBulkAssignTags = async () => {
    if (selectedIds.length === 0 || selectedTagIds.size === 0) return
    try {
      await bulkAssignTagsMutation.mutateAsync({
        employeeIds: selectedIds,
        tagIds: Array.from(selectedTagIds),
      })
      toast.success(t('hr.bulk.success'))
      setBulkTagDialogOpen(false)
      setSelectedTagIds(new Set())
      table.resetRowSelection()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }

  const handleBulkChangeDepartment = async () => {
    if (selectedIds.length === 0 || !selectedDeptId) return
    try {
      await bulkChangeDeptMutation.mutateAsync({
        employeeIds: selectedIds,
        newDepartmentId: selectedDeptId,
      })
      toast.success(t('hr.bulk.success'))
      setBulkDeptDialogOpen(false)
      setSelectedDeptId('')
      table.resetRowSelection()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }

  const handleExport = async () => {
    try {
      const blob = await exportEmployees({
        departmentId: params.filters.departmentId,
        status: params.filters.status,
        employmentType: params.filters.employmentType,
      })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'employees.xlsx'
      a.click()
      URL.revokeObjectURL(url)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }

  const handleImportFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    try {
      const result = await importMutation.mutateAsync(file)
      if (result.failedCount > 0) {
        toast.warning(t('hr.import.success') + ` (${result.successCount}/${result.totalRows})`)
      } else {
        toast.success(t('hr.import.success'))
      }
      setImportDialogOpen(false)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const handleToggleTagSelection = (tagId: string) => {
    setSelectedTagIds(prev => {
      const next = new Set(prev)
      if (next.has(tagId)) next.delete(tagId)
      else next.add(tagId)
      return next
    })
  }

  const setDepartmentFilter = (value: string) => setFilter('departmentId', value === 'all' ? undefined : value)
  const setStatusFilter = (value: string) => setFilter('status', value === 'all' ? undefined : (value as EmployeeStatus))
  const setTypeFilter = (value: string) => setFilter('employmentType', value === 'all' ? undefined : (value as EmploymentType))

  const columns = useMemo((): ColumnDef<EmployeeListDto, unknown>[] => [
    createActionsColumn<EmployeeListDto>((employee) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => handleViewEmployee(employee)}>
          <Eye className="h-4 w-4 mr-2" />
          {t('labels.viewDetails', 'View Details')}
        </DropdownMenuItem>
        <DropdownMenuItem className="cursor-pointer" onClick={() => openEditEmployee(employee)}>
          <Pencil className="h-4 w-4 mr-2" />
          {t('labels.edit', 'Edit')}
        </DropdownMenuItem>
        {employee.status === 'Active' ? (
          <DropdownMenuItem
            className="text-destructive cursor-pointer"
            onClick={() => setEmployeeToDeactivate(employee)}
          >
            <UserX className="h-4 w-4 mr-2" />
            {t('hr.deactivateEmployee')}
          </DropdownMenuItem>
        ) : (
          <DropdownMenuItem className="cursor-pointer" onClick={() => handleReactivate(employee)}>
            <UserCheck className="h-4 w-4 mr-2" />
            {t('hr.reactivateEmployee')}
          </DropdownMenuItem>
        )}
      </>
    )),
    createSelectColumn<EmployeeListDto>(),
    ch.accessor((row) => `${row.firstName} ${row.lastName}`, {
      id: 'name',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.name', 'Name')} />,
      meta: { label: t('labels.name', 'Name') },
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div className="h-8 w-8 rounded-full bg-primary/5 flex items-center justify-center text-xs font-semibold text-primary flex-shrink-0">
            {row.original.firstName.charAt(0)}{row.original.lastName.charAt(0)}
          </div>
          <span className="font-medium text-sm">
            {row.original.firstName} {row.original.lastName}
          </span>
        </div>
      ),
    }) as ColumnDef<EmployeeListDto, unknown>,
    ch.accessor('employeeCode', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.employeeCode')} />,
      meta: { label: t('hr.employeeCode') },
      cell: ({ getValue }) => <span className="font-mono text-sm text-muted-foreground">{getValue()}</span>,
    }) as ColumnDef<EmployeeListDto, unknown>,
    ch.accessor('email', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.email')} />,
      meta: { label: t('hr.email') },
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue()}</span>,
    }) as ColumnDef<EmployeeListDto, unknown>,
    ch.accessor('departmentName', {
      id: 'department',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.department')} />,
      meta: { label: t('hr.department') },
      cell: ({ getValue }) => <span className="text-sm">{getValue()}</span>,
      enableGrouping: true,
      aggregationFn: 'count',
      aggregatedCell: aggregatedCells.count(),
    }) as ColumnDef<EmployeeListDto, unknown>,
    ch.accessor('position', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.position')} />,
      meta: { label: t('hr.position') },
      cell: ({ getValue }) => <span className="text-sm text-muted-foreground">{getValue() || '-'}</span>,
    }) as ColumnDef<EmployeeListDto, unknown>,
    ch.accessor('tags', {
      id: 'tags',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.tags', 'Tags')} />,
      meta: { label: t('labels.tags', 'Tags') },
      enableSorting: false,
      cell: ({ row }) => <TagChips tags={row.original.tags} maxVisible={2} />,
    }) as ColumnDef<EmployeeListDto, unknown>,
    ch.accessor('status', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.status')} />,
      meta: { label: t('hr.status') },
      cell: ({ row }) => (
        <Badge variant="outline" className={getEmployeeStatusColor(row.original.status)}>
          {t(`hr.statuses.${row.original.status.toLowerCase()}`)}
        </Badge>
      ),
      enableGrouping: true,
      aggregationFn: 'count',
      aggregatedCell: aggregatedCells.count(),
    }) as ColumnDef<EmployeeListDto, unknown>,
    ch.accessor('employmentType', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('hr.employmentType')} />,
      meta: { label: t('hr.employmentType') },
      cell: ({ row }) => (
        <Badge variant="outline" className={getEmploymentTypeColor(row.original.employmentType)}>
          {t(`hr.employmentTypes.${row.original.employmentType.charAt(0).toLowerCase() + row.original.employmentType.slice(1).replace(/([A-Z])/g, (m) => m.toLowerCase())}`)}
        </Badge>
      ),
      enableGrouping: true,
      aggregationFn: 'count',
      aggregatedCell: aggregatedCells.count(),
    }) as ColumnDef<EmployeeListDto, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t])

  const { table, settings, isCustomized, resetToDefault, setDensity, setGrouping } = useEnterpriseTable({
    data: employees,
    columns,
    tableKey: 'employees',
    rowCount: employeesResponse?.totalCount ?? 0,
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

  const selectedIds = useSelectedIds(table.getState().rowSelection)

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={Users}
        title={t('hr.employees')}
        description={t('hr.employeesDescription', 'Manage your organization\'s employees')}
        responsive
        action={
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" className="cursor-pointer" onClick={() => setImportDialogOpen(true)}>
              <Upload className="h-4 w-4 mr-2" />
              {t('hr.import.title')}
            </Button>
            <Button variant="outline" size="sm" className="cursor-pointer" onClick={handleExport}>
              <Download className="h-4 w-4 mr-2" />
              {t('hr.export.title')}
            </Button>
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('hr.createEmployee')}
            </Button>
          </div>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('hr.allEmployees', 'All Employees')}</CardTitle>
              <CardDescription>
                {employeesResponse ? t('labels.showingCountOfTotal', { count: employees.length, total: employeesResponse.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('hr.searchPlaceholder')}
              isSearchStale={isSearchStale}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              density={settings.density}
              onDensityChange={setDensity}
              groupableColumnIds={['department', 'status', 'employmentType']}
              grouping={settings.grouping}
              onGroupingChange={setGrouping}
              filterSlot={
                <>
                  <Select value={params.filters.departmentId ?? 'all'} onValueChange={setDepartmentFilter}>
                    <SelectTrigger className="w-[160px] h-9 cursor-pointer" aria-label={t('hr.filterByDepartment')}>
                      <SelectValue placeholder={t('hr.filterByDepartment')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all" className="cursor-pointer">{t('hr.allDepartments')}</SelectItem>
                      {flatDepartments.map((dept) => (
                        <SelectItem key={dept.id} value={dept.id} className="cursor-pointer">
                          {dept.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Select value={params.filters.status ?? 'all'} onValueChange={setStatusFilter}>
                    <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('hr.filterByStatus')}>
                      <SelectValue placeholder={t('hr.filterByStatus')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all" className="cursor-pointer">{t('hr.allStatuses')}</SelectItem>
                      {EMPLOYEE_STATUSES.map((status) => (
                        <SelectItem key={status} value={status} className="cursor-pointer">
                          {t(`hr.statuses.${status.toLowerCase()}`)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Select value={params.filters.employmentType ?? 'all'} onValueChange={setTypeFilter}>
                    <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label={t('hr.filterByType')}>
                      <SelectValue placeholder={t('hr.filterByType')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all" className="cursor-pointer">{t('hr.allTypes')}</SelectItem>
                      {EMPLOYMENT_TYPES.map((type) => (
                        <SelectItem key={type} value={type} className="cursor-pointer">
                          {t(`hr.employmentTypes.${type.charAt(0).toLowerCase() + type.slice(1).replace(/([A-Z])/g, (m) => m.toLowerCase())}`)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </>
              }
            />
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'space-y-3 opacity-70 transition-opacity duration-200' : 'space-y-3 transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <BulkActionToolbar selectedCount={selectedIds.length} onClearSelection={() => table.resetRowSelection()}>
            <Button variant="outline" size="sm" className="cursor-pointer" onClick={() => setBulkTagDialogOpen(true)}>
              <Tags className="h-4 w-4 mr-2" />
              {t('hr.bulk.assignTags')}
            </Button>
            <Button variant="outline" size="sm" className="cursor-pointer" onClick={() => setBulkDeptDialogOpen(true)}>
              <Building2 className="h-4 w-4 mr-2" />
              {t('hr.bulk.changeDepartment')}
            </Button>
          </BulkActionToolbar>

          <DataTable
            table={table}
            density={settings.density}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={handleViewEmployee}
            getRowAnimationClass={getRowAnimationClass}
            emptyState={
              <EmptyState
                icon={Users}
                title={t('hr.noEmployeesFound')}
                description={t('hr.noEmployeesDescription')}
                action={{
                  label: t('hr.createEmployee'),
                  onClick: () => openCreate(),
                }}
              />
            }
          />

          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      {/* Create/Edit Employee Dialog */}
      <EmployeeFormDialog
        open={isCreateOpen || !!employeeToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (employeeToEdit) closeEditEmployee()
          }
        }}
        employee={employeeToEdit}
      />

      {/* Deactivate Confirmation Dialog */}
      <Credenza open={!!employeeToDeactivate} onOpenChange={(open) => !open && setEmployeeToDeactivate(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <UserX className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('hr.deactivateEmployee')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('hr.deactivateConfirmation')}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button
              variant="outline"
              onClick={() => setEmployeeToDeactivate(null)}
              disabled={deactivateMutation.isPending}
              className="cursor-pointer"
            >
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeactivateConfirm}
              disabled={deactivateMutation.isPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {t('hr.deactivateEmployee')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Bulk Assign Tags Dialog */}
      <Credenza open={bulkTagDialogOpen} onOpenChange={setBulkTagDialogOpen}>
        <CredenzaContent>
          <CredenzaHeader>
            <CredenzaTitle>{t('hr.bulk.assignTags')}</CredenzaTitle>
            <CredenzaDescription>
              {t('hr.bulk.assignTagsDescription', { count: selectedIds.length })}
            </CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody>
            <div className="space-y-4">
              <div className="flex flex-wrap gap-2">
                {allTags?.map(tag => (
                  <Badge
                    key={tag.id}
                    variant="outline"
                    className={`cursor-pointer transition-colors ${selectedTagIds.has(tag.id) ? 'bg-primary/10 border-primary' : 'hover:bg-muted'}`}
                    onClick={() => handleToggleTagSelection(tag.id)}
                  >
                    <div className="h-2 w-2 rounded-full mr-1.5" style={{ backgroundColor: tag.color }} />
                    {tag.name}
                  </Badge>
                ))}
              </div>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setBulkTagDialogOpen(false)} className="cursor-pointer">
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              onClick={handleBulkAssignTags}
              disabled={selectedTagIds.size === 0 || bulkAssignTagsMutation.isPending}
              className="cursor-pointer"
            >
              {t('hr.bulk.assignTags')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Bulk Change Department Dialog */}
      <Credenza open={bulkDeptDialogOpen} onOpenChange={setBulkDeptDialogOpen}>
        <CredenzaContent>
          <CredenzaHeader>
            <CredenzaTitle>{t('hr.bulk.changeDepartment')}</CredenzaTitle>
            <CredenzaDescription>
              {t('hr.bulk.changeDepartmentDescription', { count: selectedIds.length })}
            </CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody>
            <div className="space-y-4">
              <Select value={selectedDeptId} onValueChange={setSelectedDeptId}>
                <SelectTrigger className="w-full cursor-pointer" aria-label={t('hr.selectDepartment')}>
                  <SelectValue placeholder={t('hr.selectDepartment')} />
                </SelectTrigger>
                <SelectContent>
                  {flatDepartments.map((dept) => (
                    <SelectItem key={dept.id} value={dept.id} className="cursor-pointer">
                      {dept.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setBulkDeptDialogOpen(false)} className="cursor-pointer">
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              onClick={handleBulkChangeDepartment}
              disabled={!selectedDeptId || bulkChangeDeptMutation.isPending}
              className="cursor-pointer"
            >
              {t('hr.bulk.changeDepartment')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Import Dialog */}
      <Credenza open={importDialogOpen} onOpenChange={setImportDialogOpen}>
        <CredenzaContent>
          <CredenzaHeader>
            <CredenzaTitle>{t('hr.import.title')}</CredenzaTitle>
            <CredenzaDescription>
              {t('hr.import.description')}
            </CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody>
            <div className="space-y-4">
              <input
                ref={fileInputRef}
                type="file"
                accept=".csv,.xlsx"
                onChange={handleImportFile}
                className="block w-full text-sm text-muted-foreground file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-primary/10 file:text-primary hover:file:bg-primary/20 file:cursor-pointer cursor-pointer"
                aria-label={t('hr.import.selectFile')}
              />
              {importMutation.isPending && (
                <p className="text-sm text-muted-foreground">{t('hr.import.importing')}</p>
              )}
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setImportDialogOpen(false)} className="cursor-pointer">
              {t('labels.cancel', 'Cancel')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default EmployeesPage
