import { useState, useDeferredValue, useMemo } from 'react'
import { useVirtualTableRows } from '@/hooks/useVirtualTableRows'
import { useTranslation } from 'react-i18next'
import {
  Building2,
  EllipsisVertical,
  GitBranch,
  List,
  Pencil,
  Plus,
  Search,
  Trash2,
  Users,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
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
// Assigns sortOrder from position in parent's children array (backend returns in order)
const flattenDepartmentTree = (
  nodes: DepartmentTreeNodeDto[],
  parentId: string | null = null,
): DepartmentFlatItem[] =>
  nodes.flatMap((node, index) => [
    {
      id: node.id,
      name: node.name,
      slug: node.code,        // CategoryTreeView shows slug field as code badge
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

export const DepartmentsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Departments')

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch

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

  // Flat list used by CategoryTreeView, useUrlEditDialog, and table
  const flatDepartments = useMemo(
    () => (departments ? flattenDepartmentTree(departments) : []),
    [departments],
  )

  // Client-side search (name or code)
  const filteredDepartments = useMemo(() => {
    if (!deferredSearch) return flatDepartments
    const q = deferredSearch.toLowerCase()
    return flatDepartments.filter(
      (d) => d.name.toLowerCase().includes(q) || d.code.toLowerCase().includes(q),
    )
  }, [flatDepartments, deferredSearch])

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-department' })
  const { editItem: departmentToEdit, openEdit: openEditDepartment, closeEdit: closeEditDepartment } = useUrlEditDialog<DepartmentFlatItem>(flatDepartments)

  const [parentIdForCreate, setParentIdForCreate] = useState<string | null>(null)
  const [departmentToDelete, setDepartmentToDelete] = useState<DepartmentFlatItem | null>(null)

  const { scrollRef, height, shouldVirtualize, virtualItems, topPad, bottomPad } =
    useVirtualTableRows(filteredDepartments)

  const handleReorder = (items: ReorderItem[]) => {
    // Always send parentDepartmentId — backend handles reparenting + sortOrder in one call.
    // null = root level (no parent).
    reorderMutation.mutate({
      items: items.map(i => ({
        id: i.id,
        parentDepartmentId: i.parentId ?? null,
        sortOrder: i.sortOrder,
      })),
    })
  }

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

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg">{t('hr.departmentHierarchy', 'Department Hierarchy')}</CardTitle>
                <CardDescription>
                  {flatDepartments.length > 0
                    ? t('hr.departmentCount', { count: flatDepartments.length, defaultValue: `${flatDepartments.length} departments total` })
                    : t('hr.departmentHierarchyDescription', 'View and manage your organizational structure')}
                </CardDescription>
              </div>
              <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
            </div>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={t('hr.searchDepartments', 'Search departments...')}
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="pl-9 h-9"
                aria-label={t('hr.searchDepartments', 'Search departments')}
              />
            </div>
          </div>
        </CardHeader>

        <CardContent className={isSearchStale ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
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
            <div
              ref={scrollRef}
              className="rounded-xl border border-border/50 overflow-auto"
              style={{ height }}
            >
              <Table>
                <TableHeader className="sticky top-0 z-10 bg-background shadow-sm">
                  <TableRow>
                    <TableHead className="w-10 sticky left-0 z-10 bg-background" />
                    <TableHead className="w-[35%]">{t('labels.name', 'Name')}</TableHead>
                    <TableHead>{t('hr.manager')}</TableHead>
                    <TableHead>{t('hr.parentDepartment')}</TableHead>
                    <TableHead className="text-center">
                      <span className="flex items-center justify-center gap-1">
                        <Users className="h-3.5 w-3.5" />
                        {t('hr.employees', 'Employees')}
                      </span>
                    </TableHead>
                    <TableHead className="text-center">{t('hr.subDepartments', 'Sub-depts')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {loading ? (
                    [...Array(5)].map((_, i) => (
                      <TableRow key={i} className="animate-pulse">
                        <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Skeleton className="h-4 w-4" />
                            <Skeleton className="h-4 w-32" />
                            <Skeleton className="h-5 w-12 rounded-full" />
                          </div>
                        </TableCell>
                        <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                        <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                        <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                        <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto rounded-full" /></TableCell>
                      </TableRow>
                    ))
                  ) : filteredDepartments.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="p-0">
                        <EmptyState
                          icon={Building2}
                          title={t('hr.noDepartmentsFound')}
                          description={t('hr.noDepartmentsDescription')}
                          action={{
                            label: t('hr.createDepartment'),
                            onClick: () => { setParentIdForCreate(null); openCreate() },
                          }}
                          className="border-0 rounded-none px-4 py-12"
                        />
                      </TableCell>
                    </TableRow>
                  ) : (
                    <>
                      {topPad > 0 && (
                        <TableRow><TableCell colSpan={6} className="p-0 border-0" style={{ height: topPad }} /></TableRow>
                      )}
                      {(shouldVirtualize ? virtualItems.map(vr => filteredDepartments[vr.index]) : filteredDepartments).map((dept) => (
                        <TableRow
                          key={dept.id}
                          className="group cursor-pointer transition-colors hover:bg-muted/50"
                          onClick={(e) => {
                            if ((e.target as HTMLElement).closest('[data-no-row-click]')) return
                            openEditDepartment(dept)
                          }}
                        >
                          <TableCell className="sticky left-0 z-10 bg-background" data-no-row-click>
                            <DropdownMenu>
                              <DropdownMenuTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                                  aria-label={t('labels.actionsFor', { name: dept.name, defaultValue: `Actions for ${dept.name}` })}
                                >
                                  <EllipsisVertical className="h-4 w-4" />
                                </Button>
                              </DropdownMenuTrigger>
                              <DropdownMenuContent align="start">
                                <DropdownMenuItem className="cursor-pointer" onClick={() => openEditDepartment(dept)}>
                                  <Pencil className="h-4 w-4 mr-2" />
                                  {t('labels.edit', 'Edit')}
                                </DropdownMenuItem>
                                <DropdownMenuItem
                                  className="cursor-pointer"
                                  onClick={() => { setParentIdForCreate(dept.id); openCreate() }}
                                >
                                  <Plus className="h-4 w-4 mr-2" />
                                  {t('hr.addSubDepartment')}
                                </DropdownMenuItem>
                                <DropdownMenuItem
                                  className="text-destructive cursor-pointer"
                                  onClick={(e) => { e.stopPropagation(); setDepartmentToDelete(dept) }}
                                >
                                  <Trash2 className="h-4 w-4 mr-2" />
                                  {t('labels.delete', 'Delete')}
                                </DropdownMenuItem>
                              </DropdownMenuContent>
                            </DropdownMenu>
                          </TableCell>

                          <TableCell>
                            <div className="flex items-center gap-2">
                              <Building2 className="h-4 w-4 text-muted-foreground shrink-0" />
                              <span className="font-medium">{dept.name}</span>
                              <Badge variant="outline" className="text-xs font-mono">{dept.code}</Badge>
                              {!dept.isActive && (
                                <Badge variant="outline" className={getStatusBadgeClasses('gray')}>
                                  {t('labels.inactive', 'Inactive')}
                                </Badge>
                              )}
                            </div>
                          </TableCell>

                          <TableCell>
                            <span className="text-sm text-muted-foreground">
                              {dept.managerName || '—'}
                            </span>
                          </TableCell>

                          <TableCell>
                            <span className="text-sm text-muted-foreground">
                              {dept.parentId
                                ? flatDepartments.find(d => d.id === dept.parentId)?.name ?? '—'
                                : '—'}
                            </span>
                          </TableCell>

                          <TableCell className="text-center">
                            <Badge variant="secondary">{dept.employeeCount}</Badge>
                          </TableCell>

                          <TableCell className="text-center">
                            {dept.childCount > 0 ? (
                              <Badge variant="outline">{dept.childCount}</Badge>
                            ) : (
                              <span className="text-muted-foreground">—</span>
                            )}
                          </TableCell>
                        </TableRow>
                      ))}
                      {bottomPad > 0 && (
                        <TableRow><TableCell colSpan={6} className="p-0 border-0" style={{ height: bottomPad }} /></TableRow>
                      )}
                    </>
                  )}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Department Dialog */}
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

      {/* Delete Confirmation Dialog */}
      <DeleteDepartmentDialog
        department={departmentToDelete}
        open={!!departmentToDelete}
        onOpenChange={(open) => !open && setDepartmentToDelete(null)}
      />
    </div>
  )
}

export default DepartmentsPage
