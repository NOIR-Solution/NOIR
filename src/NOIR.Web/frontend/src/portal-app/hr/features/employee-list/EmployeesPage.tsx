import { useState, useEffect, useDeferredValue, useMemo, useTransition, useRef, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Eye,
  EllipsisVertical,
  Pencil,
  Plus,
  Search,
  Users,
  UserX,
  UserCheck,
  Download,
  Upload,
  Tags,
  Building2,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { useSelection } from '@/hooks/useSelection'
import {
  Badge,
  Button,
  Checkbox,
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
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
import type { GetEmployeesParams } from '@/types/hr'
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

export const EmployeesPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  usePageContext('Employees')

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [departmentFilter, setDepartmentFilter] = useState<string>('all')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [typeFilter, setTypeFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()
  const [params, setParams] = useState<GetEmployeesParams>({ page: 1, pageSize: 20 })

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-employee' })
  const [employeeToDeactivate, setEmployeeToDeactivate] = useState<EmployeeListDto | null>(null)
  const [bulkTagDialogOpen, setBulkTagDialogOpen] = useState(false)
  const [bulkDeptDialogOpen, setBulkDeptDialogOpen] = useState(false)
  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [selectedTagIds, setSelectedTagIds] = useState<Set<string>>(new Set())
  const [selectedDeptId, setSelectedDeptId] = useState<string>('')
  const fileInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    setParams(prev => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  const queryParams = useMemo(() => ({
    ...params,
    search: deferredSearch || undefined,
    departmentId: departmentFilter !== 'all' ? departmentFilter : undefined,
    status: statusFilter !== 'all' ? statusFilter as EmployeeStatus : undefined,
    employmentType: typeFilter !== 'all' ? typeFilter as EmploymentType : undefined,
  }), [params, deferredSearch, departmentFilter, statusFilter, typeFilter])

  const { data: employeesResponse, isLoading: loading, error: queryError, refetch } = useEmployeesQuery(queryParams)
  const { data: departments } = useDepartmentsQuery()
  const { data: allTags } = useTagsQuery({ isActive: true })
  const deactivateMutation = useDeactivateEmployee()
  const reactivateMutation = useReactivateEmployee()
  const bulkAssignTagsMutation = useBulkAssignTags()
  const bulkChangeDeptMutation = useBulkChangeDepartment()
  const importMutation = useImportEmployees()
  const error = queryError?.message ?? null

  const employees = employeesResponse?.items ?? []
  const { selectedIds, handleSelectAll, handleSelectNone, handleToggleSelect, isAllSelected } = useSelection(employees)

  const handleCollectionUpdate = useCallback(() => {
    if (selectedIds.size === 0) refetch()
  }, [selectedIds.size, refetch])

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Employee',
    onCollectionUpdate: handleCollectionUpdate,
  })
  const { editItem: employeeToEdit, openEdit: openEditEmployee, closeEdit: closeEditEmployee } = useUrlEditDialog<EmployeeListDto>(employees)
  const totalCount = employeesResponse?.totalCount ?? 0
  const totalPages = employeesResponse?.totalPages ?? 1
  const currentPage = params.page ?? 1

  // Flatten departments for filter dropdown
  const flattenDepartments = (nodes: typeof departments, prefix = ''): { id: string; name: string }[] => {
    if (!nodes) return []
    return nodes.flatMap(node => [
      { id: node.id, name: prefix + node.name },
      ...flattenDepartments(node.children, prefix + '  '),
    ])
  }
  const flatDepartments = flattenDepartments(departments)

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
  }

  const handleDepartmentFilter = (value: string) => {
    startFilterTransition(() => {
      setDepartmentFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handleStatusFilter = (value: string) => {
    startFilterTransition(() => {
      setStatusFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handleTypeFilter = (value: string) => {
    startFilterTransition(() => {
      setTypeFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handlePageChange = (page: number) => {
    startFilterTransition(() => {
      setParams(prev => ({ ...prev, page }))
    })
  }

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

  const handleBulkAssignTags = useCallback(async () => {
    if (selectedIds.size === 0 || selectedTagIds.size === 0) return
    try {
      await bulkAssignTagsMutation.mutateAsync({
        employeeIds: Array.from(selectedIds),
        tagIds: Array.from(selectedTagIds),
      })
      toast.success(t('hr.bulk.success'))
      setBulkTagDialogOpen(false)
      setSelectedTagIds(new Set())
      handleSelectNone()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }, [selectedIds, selectedTagIds, bulkAssignTagsMutation, t, handleSelectNone])

  const handleBulkChangeDepartment = useCallback(async () => {
    if (selectedIds.size === 0 || !selectedDeptId) return
    try {
      await bulkChangeDeptMutation.mutateAsync({
        employeeIds: Array.from(selectedIds),
        newDepartmentId: selectedDeptId,
      })
      toast.success(t('hr.bulk.success'))
      setBulkDeptDialogOpen(false)
      setSelectedDeptId('')
      handleSelectNone()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }, [selectedIds, selectedDeptId, bulkChangeDeptMutation, t, handleSelectNone])

  const handleExport = useCallback(async () => {
    try {
      const blob = await exportEmployees({
        departmentId: departmentFilter !== 'all' ? departmentFilter : undefined,
        status: statusFilter !== 'all' ? statusFilter as EmployeeStatus : undefined,
        employmentType: typeFilter !== 'all' ? typeFilter as EmploymentType : undefined,
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
  }, [departmentFilter, statusFilter, typeFilter, t])

  const handleImportFile = useCallback(async (e: React.ChangeEvent<HTMLInputElement>) => {
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
    // Reset file input
    if (fileInputRef.current) fileInputRef.current.value = ''
  }, [importMutation, t])

  const handleToggleTagSelection = useCallback((tagId: string) => {
    setSelectedTagIds(prev => {
      const next = new Set(prev)
      if (next.has(tagId)) next.delete(tagId)
      else next.add(tagId)
      return next
    })
  }, [])

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

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('hr.allEmployees', 'All Employees')}</CardTitle>
              <CardDescription>
                {t('labels.showingCountOfTotal', { count: employees.length, total: totalCount })}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('hr.searchPlaceholder')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-9 h-9"
                  aria-label={t('hr.searchPlaceholder')}
                />
              </div>
              <Select value={departmentFilter} onValueChange={handleDepartmentFilter}>
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
              <Select value={statusFilter} onValueChange={handleStatusFilter}>
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
              <Select value={typeFilter} onValueChange={handleTypeFilter}>
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
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <BulkActionToolbar selectedCount={selectedIds.size} onClearSelection={handleSelectNone}>
            <Button variant="outline" size="sm" className="cursor-pointer" onClick={() => setBulkTagDialogOpen(true)}>
              <Tags className="h-4 w-4 mr-2" />
              {t('hr.bulk.assignTags')}
            </Button>
            <Button variant="outline" size="sm" className="cursor-pointer" onClick={() => setBulkDeptDialogOpen(true)}>
              <Building2 className="h-4 w-4 mr-2" />
              {t('hr.bulk.changeDepartment')}
            </Button>
          </BulkActionToolbar>

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10">
                    <Checkbox
                      checked={isAllSelected}
                      onCheckedChange={() => isAllSelected ? handleSelectNone() : handleSelectAll()}
                      className="cursor-pointer"
                      aria-label={t('labels.selectAll', 'Select all')}
                    />
                  </TableHead>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                  <TableHead>{t('labels.name', 'Name')}</TableHead>
                  <TableHead>{t('hr.employeeCode')}</TableHead>
                  <TableHead>{t('hr.email')}</TableHead>
                  <TableHead>{t('hr.department')}</TableHead>
                  <TableHead>{t('hr.position')}</TableHead>
                  <TableHead>{t('labels.tags', 'Tags')}</TableHead>
                  <TableHead>{t('hr.status')}</TableHead>
                  <TableHead>{t('hr.employmentType')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell><Skeleton className="h-4 w-4" /></TableCell>
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-40" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                    </TableRow>
                  ))
                ) : employees.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={10} className="p-0">
                      <EmptyState
                        icon={Users}
                        title={t('hr.noEmployeesFound')}
                        description={t('hr.noEmployeesDescription')}
                        action={{
                          label: t('hr.createEmployee'),
                          onClick: () => openCreate(),
                        }}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  employees.map((employee) => (
                    <TableRow
                      key={employee.id}
                      className="group cursor-pointer transition-colors hover:bg-muted/50"
                      onClick={() => handleViewEmployee(employee)}
                    >
                      <TableCell onClick={(e) => e.stopPropagation()}>
                        <Checkbox
                          checked={selectedIds.has(employee.id)}
                          onCheckedChange={() => handleToggleSelect(employee.id)}
                          className="cursor-pointer"
                          aria-label={t('labels.select', { name: `${employee.firstName} ${employee.lastName}` })}
                        />
                      </TableCell>
                      <TableCell className="sticky left-0 z-10 bg-background">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                              aria-label={t('labels.actionsFor', { name: `${employee.firstName} ${employee.lastName}` })}
                              onClick={(e) => e.stopPropagation()}
                            >
                              <EllipsisVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="start">
                            <DropdownMenuItem
                              className="cursor-pointer"
                              onClick={(e) => {
                                e.stopPropagation()
                                handleViewEmployee(employee)
                              }}
                            >
                              <Eye className="h-4 w-4 mr-2" />
                              {t('labels.viewDetails', 'View Details')}
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              className="cursor-pointer"
                              onClick={(e) => {
                                e.stopPropagation()
                                openEditEmployee(employee)
                              }}
                            >
                              <Pencil className="h-4 w-4 mr-2" />
                              {t('labels.edit', 'Edit')}
                            </DropdownMenuItem>
                            {employee.status === 'Active' ? (
                              <DropdownMenuItem
                                className="text-destructive cursor-pointer"
                                onClick={(e) => {
                                  e.stopPropagation()
                                  setEmployeeToDeactivate(employee)
                                }}
                              >
                                <UserX className="h-4 w-4 mr-2" />
                                {t('hr.deactivateEmployee')}
                              </DropdownMenuItem>
                            ) : (
                              <DropdownMenuItem
                                className="cursor-pointer"
                                onClick={(e) => {
                                  e.stopPropagation()
                                  handleReactivate(employee)
                                }}
                              >
                                <UserCheck className="h-4 w-4 mr-2" />
                                {t('hr.reactivateEmployee')}
                              </DropdownMenuItem>
                            )}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-3">
                          <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center text-xs font-semibold text-primary flex-shrink-0">
                            {employee.firstName.charAt(0)}{employee.lastName.charAt(0)}
                          </div>
                          <span className="font-medium text-sm">
                            {employee.firstName} {employee.lastName}
                          </span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <span className="font-mono text-sm text-muted-foreground">{employee.employeeCode}</span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">{employee.email}</span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm">{employee.departmentName}</span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">{employee.position || '-'}</span>
                      </TableCell>
                      <TableCell onClick={(e) => e.stopPropagation()}>
                        <TagChips tags={employee.tags} maxVisible={2} />
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className={getEmployeeStatusColor(employee.status)}>
                          {t(`hr.statuses.${employee.status.toLowerCase()}`)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className={getEmploymentTypeColor(employee.employmentType)}>
                          {t(`hr.employmentTypes.${employee.employmentType.charAt(0).toLowerCase() + employee.employmentType.slice(1).replace(/([A-Z])/g, (m) => m.toLowerCase())}`)}
                        </Badge>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {totalPages > 1 && (
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalItems={totalCount}
              pageSize={params.pageSize || 20}
              onPageChange={handlePageChange}
              showPageSizeSelector={false}
              className="mt-4"
            />
          )}
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
              {t('hr.bulk.assignTagsDescription', { count: selectedIds.size })}
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
              {t('hr.bulk.changeDepartmentDescription', { count: selectedIds.size })}
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
