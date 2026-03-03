import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { EntityConflictDialog } from '@/components/EntityConflictDialog'
import { EntityDeletedDialog } from '@/components/EntityDeletedDialog'
import {
  ArrowLeft,
  Building2,
  Calendar,
  Mail,
  Pencil,
  Phone,
  Tags,
  User,
  UserCheck,
  Users,
  UserX,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useUrlTab } from '@/hooks/useUrlTab'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
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
  EmptyState,
  PageHeader,
  Separator,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import {
  useEmployeeQuery,
  useDeactivateEmployee,
  useReactivateEmployee,
} from '@/portal-app/hr/queries'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { EmployeeFormDialog } from '../../components/EmployeeFormDialog'
import { TagChips } from '../../components/TagChips'
import { TagSelector } from '../../components/TagSelector'

const getEmployeeStatusColor = (status: string) => {
  switch (status) {
    case 'Active': return getStatusBadgeClasses('green')
    case 'Suspended': return getStatusBadgeClasses('yellow')
    case 'Resigned': return getStatusBadgeClasses('gray')
    case 'Terminated': return getStatusBadgeClasses('red')
    default: return getStatusBadgeClasses('gray')
  }
}

const getEmploymentTypeColor = (type: string) => {
  switch (type) {
    case 'FullTime': return getStatusBadgeClasses('blue')
    case 'PartTime': return getStatusBadgeClasses('purple')
    case 'Contract': return getStatusBadgeClasses('orange')
    case 'Intern': return getStatusBadgeClasses('cyan')
    default: return getStatusBadgeClasses('gray')
  }
}

export const EmployeeDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Employees')

  const { data: employee, isLoading, error: queryError, refetch } = useEmployeeQuery(id)
  const deactivateMutation = useDeactivateEmployee()
  const reactivateMutation = useReactivateEmployee()
  const { activeTab, handleTabChange, isPending: isTabPending } = useUrlTab({ defaultTab: 'overview' })

  const { hasPermission } = usePermissions()
  const canManageTags = hasPermission(Permissions.HrTagsManage)

  const { conflictSignal, deletedSignal, dismissConflict, reloadAndRestart, isReconnecting } = useEntityUpdateSignal({
    entityType: 'Employee',
    entityId: id,
    onAutoReload: refetch,
    onNavigateAway: () => navigate('/portal/hr/employees'),
  })

  const [showEditDialog, setShowEditDialog] = useState(false)
  const [showDeactivateDialog, setShowDeactivateDialog] = useState(false)
  const [showTagSelector, setShowTagSelector] = useState(false)

  const handleDeactivateConfirm = async () => {
    if (!employee) return
    try {
      await deactivateMutation.mutateAsync({ id: employee.id, status: 'Resigned' })
      toast.success(t('hr.employeeDeactivated', 'Employee deactivated'))
      setShowDeactivateDialog(false)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }

  const handleReactivate = async () => {
    if (!employee) return
    try {
      await reactivateMutation.mutateAsync(employee.id)
      toast.success(t('hr.employeeReactivated', 'Employee reactivated'))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }

  if (isLoading) {
    return (
      <div className="py-6 space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded" />
          <div className="space-y-2">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-72" />
          </div>
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6">
                <div className="space-y-3">
                  {[...Array(6)].map((_, i) => <Skeleton key={i} className="h-3 w-full" />)}
                </div>
              </CardContent>
            </Card>
          </div>
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardContent className="pt-6">
              <div className="space-y-3">
                {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-3 w-full" />)}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    )
  }

  if (queryError || !employee) {
    return (
      <div className="py-6 space-y-6">
        <Button variant="ghost" onClick={() => navigate('/portal/hr/employees')} className="cursor-pointer">
          <ArrowLeft className="h-4 w-4 mr-2" />
          {t('hr.backToEmployees', 'Back to Employees')}
        </Button>
        <div className="p-8 text-center">
          <p className="text-destructive">{queryError?.message || t('hr.employeeNotFound', 'Employee not found')}</p>
        </div>
      </div>
    )
  }

  const fullName = `${employee.firstName} ${employee.lastName}`

  return (
    <div className="py-6 space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <EntityConflictDialog signal={conflictSignal} onContinueEditing={dismissConflict} onReloadAndRestart={reloadAndRestart} />
      <EntityDeletedDialog signal={deletedSignal} onGoBack={() => navigate('/portal/hr/employees')} />

      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/portal/hr/employees')} className="cursor-pointer" aria-label={t('hr.backToEmployees', 'Back to Employees')}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <PageHeader
          icon={Users}
          title={fullName}
          description={`${employee.employeeCode} - ${employee.departmentName}`}
          responsive
          action={
            <div className="flex items-center gap-2">
              <Badge variant="outline" className={`text-sm px-3 py-1 ${getEmployeeStatusColor(employee.status)}`}>
                {t(`hr.statuses.${employee.status.toLowerCase()}`)}
              </Badge>
              <Badge variant="outline" className={`text-sm px-3 py-1 ${getEmploymentTypeColor(employee.employmentType)}`}>
                {t(`hr.employmentTypes.${employee.employmentType.charAt(0).toLowerCase() + employee.employmentType.slice(1).replace(/([A-Z])/g, (m) => m.toLowerCase())}`)}
              </Badge>
            </div>
          }
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column - Tabs */}
        <div className="lg:col-span-2 space-y-6">
          <Tabs value={activeTab} onValueChange={handleTabChange} className={`w-full${isTabPending ? ' opacity-70 transition-opacity duration-200' : ' transition-opacity duration-200'}`}>
            <TabsList>
              <TabsTrigger value="overview" className="cursor-pointer">
                <User className="h-4 w-4 mr-2" />
                {t('hr.overview')}
              </TabsTrigger>
              <TabsTrigger value="directReports" className="cursor-pointer">
                <Users className="h-4 w-4 mr-2" />
                {t('hr.directReports')}
              </TabsTrigger>
              <TabsTrigger value="activity" className="cursor-pointer">
                <Calendar className="h-4 w-4 mr-2" />
                {t('hr.activity')}
              </TabsTrigger>
            </TabsList>

            {/* Overview Tab */}
            <TabsContent value="overview">
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
                <CardHeader>
                  <CardTitle className="text-sm flex items-center gap-2">
                    <User className="h-4 w-4" />
                    {t('hr.employeeInformation', 'Employee Information')}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <p className="text-xs text-muted-foreground mb-1">{t('hr.fullName')}</p>
                      <p className="font-medium">{fullName}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted-foreground mb-1">{t('hr.employeeCode')}</p>
                      <p className="font-mono">{employee.employeeCode}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted-foreground mb-1">{t('hr.email')}</p>
                      <div className="flex items-center gap-2">
                        <Mail className="h-4 w-4 text-muted-foreground" />
                        <p>{employee.email}</p>
                      </div>
                    </div>
                    {employee.phone && (
                      <div>
                        <p className="text-xs text-muted-foreground mb-1">{t('hr.phone')}</p>
                        <div className="flex items-center gap-2">
                          <Phone className="h-4 w-4 text-muted-foreground" />
                          <p>{employee.phone}</p>
                        </div>
                      </div>
                    )}
                    <div>
                      <p className="text-xs text-muted-foreground mb-1">{t('hr.department')}</p>
                      <div className="flex items-center gap-2">
                        <Building2 className="h-4 w-4 text-muted-foreground" />
                        <p>{employee.departmentName}</p>
                      </div>
                    </div>
                    {employee.position && (
                      <div>
                        <p className="text-xs text-muted-foreground mb-1">{t('hr.position')}</p>
                        <p>{employee.position}</p>
                      </div>
                    )}
                    {employee.managerName && (
                      <div>
                        <p className="text-xs text-muted-foreground mb-1">{t('hr.manager')}</p>
                        <p>{employee.managerName}</p>
                      </div>
                    )}
                    <div>
                      <p className="text-xs text-muted-foreground mb-1">{t('hr.joinDate')}</p>
                      <div className="flex items-center gap-2">
                        <Calendar className="h-4 w-4 text-muted-foreground" />
                        <p>{formatDateTime(employee.joinDate)}</p>
                      </div>
                    </div>
                    {employee.endDate && (
                      <div>
                        <p className="text-xs text-muted-foreground mb-1">{t('hr.endDate')}</p>
                        <p>{formatDateTime(employee.endDate)}</p>
                      </div>
                    )}
                  </div>
                  {employee.notes && (
                    <>
                      <Separator />
                      <div>
                        <p className="text-xs text-muted-foreground mb-1">{t('hr.notes')}</p>
                        <p className="text-sm whitespace-pre-wrap">{employee.notes}</p>
                      </div>
                    </>
                  )}
                  {(employee.tags.length > 0 || canManageTags) && (
                    <>
                      <Separator />
                      <div>
                        <div className="flex items-center justify-between mb-2">
                          <p className="text-xs text-muted-foreground">{t('labels.tags', 'Tags')}</p>
                          {canManageTags && (
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-7 text-xs cursor-pointer"
                              onClick={() => setShowTagSelector(true)}
                            >
                              <Tags className="h-3.5 w-3.5 mr-1" />
                              {t('hr.tags.manageTags')}
                            </Button>
                          )}
                        </div>
                        {employee.tags.length > 0 ? (
                          <TagChips tags={employee.tags} maxVisible={10} size="md" />
                        ) : (
                          <p className="text-xs text-muted-foreground">{t('hr.tags.noTags')}</p>
                        )}
                      </div>
                    </>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            {/* Direct Reports Tab */}
            <TabsContent value="directReports">
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
                <CardHeader>
                  <CardTitle className="text-sm flex items-center gap-2">
                    <Users className="h-4 w-4" />
                    {t('hr.directReports')}
                  </CardTitle>
                  <CardDescription>
                    {t('hr.employeeCount', { count: employee.directReports.length })}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  {employee.directReports.length === 0 ? (
                    <EmptyState
                      icon={Users}
                      title={t('hr.noDirectReports')}
                      description={t('hr.noDirectReportsDescription', 'This employee has no direct reports.')}
                      className="border-0 rounded-none px-4 py-12"
                    />
                  ) : (
                    <div className="rounded-lg border overflow-hidden">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>{t('labels.name', 'Name')}</TableHead>
                            <TableHead>{t('hr.employeeCode')}</TableHead>
                            <TableHead>{t('hr.position')}</TableHead>
                            <TableHead>{t('hr.status')}</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {employee.directReports.map((report) => (
                            <TableRow
                              key={report.id}
                              className="cursor-pointer transition-colors hover:bg-muted/50"
                              onClick={() => navigate(`/portal/hr/employees/${report.id}`)}
                            >
                              <TableCell>
                                <div className="flex items-center gap-3">
                                  <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center text-xs font-semibold text-primary flex-shrink-0">
                                    {report.fullName.split(' ').map(n => n.charAt(0)).join('').slice(0, 2)}
                                  </div>
                                  <span className="font-medium text-sm">{report.fullName}</span>
                                </div>
                              </TableCell>
                              <TableCell>
                                <span className="font-mono text-sm text-muted-foreground">{report.employeeCode}</span>
                              </TableCell>
                              <TableCell>
                                <span className="text-sm text-muted-foreground">{report.position || '-'}</span>
                              </TableCell>
                              <TableCell>
                                <Badge variant="outline" className={getEmployeeStatusColor(report.status)}>
                                  {t(`hr.statuses.${report.status.toLowerCase()}`)}
                                </Badge>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </div>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            {/* Activity Tab (placeholder) */}
            <TabsContent value="activity">
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
                <CardHeader>
                  <CardTitle className="text-sm flex items-center gap-2">
                    <Calendar className="h-4 w-4" />
                    {t('hr.activity')}
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <EmptyState
                    icon={Calendar}
                    title={t('hr.noActivity', 'No activity yet')}
                    description={t('hr.noActivityDescription', 'Employee activity will appear here.')}
                    className="border-0 rounded-none px-4 py-12"
                  />
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>

        {/* Right Column - Actions & Info */}
        <div className="space-y-6">
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="text-sm">{t('labels.actions', 'Actions')}</CardTitle>
              </div>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button
                variant="outline"
                className="w-full justify-start cursor-pointer"
                onClick={() => setShowEditDialog(true)}
              >
                <Pencil className="h-4 w-4 mr-2" />
                {t('hr.editEmployee')}
              </Button>
              {employee.status === 'Active' ? (
                <Button
                  variant="outline"
                  className="w-full justify-start cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
                  onClick={() => setShowDeactivateDialog(true)}
                >
                  <UserX className="h-4 w-4 mr-2" />
                  {t('hr.deactivateEmployee')}
                </Button>
              ) : (
                <Button
                  variant="outline"
                  className="w-full justify-start cursor-pointer"
                  onClick={handleReactivate}
                  disabled={reactivateMutation.isPending}
                >
                  <UserCheck className="h-4 w-4 mr-2" />
                  {t('hr.reactivateEmployee')}
                </Button>
              )}
            </CardContent>
          </Card>

          <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
            <CardHeader>
              <CardTitle className="text-sm">{t('hr.accountInfo', 'Account Information')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div>
                <p className="text-muted-foreground text-xs mb-1">{t('hr.hasUserAccount', 'User Account')}</p>
                <Badge variant="outline" className={employee.hasUserAccount ? getStatusBadgeClasses('green') : getStatusBadgeClasses('gray')}>
                  {employee.hasUserAccount ? t('labels.yes', 'Yes') : t('labels.no', 'No')}
                </Badge>
              </div>
              <div>
                <p className="text-muted-foreground text-xs mb-1">{t('labels.createdAt', 'Created At')}</p>
                <p className="font-medium">{formatDateTime(employee.createdAt)}</p>
              </div>
              <div>
                <p className="text-muted-foreground text-xs mb-1">{t('labels.lastModified', 'Last Modified')}</p>
                <p className="font-medium">{formatDateTime(employee.lastModifiedAt)}</p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Edit Employee Dialog */}
      <EmployeeFormDialog
        open={showEditDialog}
        onOpenChange={setShowEditDialog}
        employee={employee}
      />

      {/* Deactivate Confirmation Dialog */}
      <Credenza open={showDeactivateDialog} onOpenChange={setShowDeactivateDialog}>
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
              onClick={() => setShowDeactivateDialog(false)}
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

      {/* Tag Selector Dialog */}
      {canManageTags && (
        <TagSelector
          open={showTagSelector}
          onOpenChange={setShowTagSelector}
          employeeId={employee.id}
          currentTags={employee.tags}
        />
      )}
    </div>
  )
}

export default EmployeeDetailPage
